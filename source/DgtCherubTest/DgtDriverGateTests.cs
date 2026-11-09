using DgtRabbitWrapper.DgtEbDll;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace DgtCherubTest
{
    /// <summary>
    /// The driver is a single non-thread-safe instance reached from both Cherub's own
    /// threads and gRPC server threads. Everything that touches it must serialise through
    /// this gate, and the gate must never swallow an exception - a driver call that fails
    /// silently is worse than one that throws.
    /// </summary>
    /// <remarks>
    /// DgtDriverGate is static, so each test that depends on the starting state calls
    /// <c>ResetForTests</c> first. Nothing here asserts that another test left the gate in a
    /// particular state.
    /// </remarks>
    [TestClass]
    public class DgtDriverGateTests
    {
        [TestMethod(DisplayName = "A query returns the delegate's result")]
        public void RunReturnsResult()
        {
            Assert.AreEqual(42, DgtDriverGate.Run(() => 42));
        }

        [TestMethod(DisplayName = "An action is executed exactly once")]
        public void RunExecutesAction()
        {
            int calls = 0;

            DgtDriverGate.Run(() => calls++);

            Assert.AreEqual(1, calls);
        }

        [TestMethod(DisplayName = "Concurrent calls are serialised")]
        public void RunSerialisesConcurrentCalls()
        {
            const int iterations = 5000;
            int counter = 0;

            _ = Parallel.For(0, iterations, _ => DgtDriverGate.Run(() => counter++));

            Assert.AreEqual(iterations, counter, "an unsynchronised increment inside the gate means the gate is not holding");
        }

        [TestMethod(DisplayName = "A throwing delegate propagates and leaves the gate usable")]
        public void ThrowingDelegatePropagatesAndReleasesTheLock()
        {
            _ = Assert.ThrowsExactly<InvalidOperationException>(() => DgtDriverGate.Run((Func<int>)(() => throw new InvalidOperationException("driver fault"))));

            // If the exception escaped without releasing the monitor, this deadlocks.
            Assert.AreEqual(7, DgtDriverGate.Run(() => 7));
        }

        [TestMethod(DisplayName = "A throwing action propagates and leaves the gate usable")]
        public void ThrowingActionPropagatesAndReleasesTheLock()
        {
            _ = Assert.ThrowsExactly<InvalidOperationException>(() => DgtDriverGate.Run(() => throw new InvalidOperationException("driver fault")));

            int ran = 0;
            DgtDriverGate.Run(() => ran++);

            Assert.AreEqual(1, ran);
        }

        [TestMethod(DisplayName = "Marking the driver initialised is observable and permanent")]
        public void MarkInitialisedIsObservable()
        {
            DgtDriverGate.MarkInitialised();

            Assert.IsTrue(DgtDriverGate.IsInitialised);

            // Marking again must not undo it - a proxied client calling Init repeatedly
            // must never be able to send the driver back to "uninitialised".
            DgtDriverGate.MarkInitialised();

            Assert.IsTrue(DgtDriverGate.IsInitialised);
        }

        [TestMethod(DisplayName = "A claim can be taken once and not twice")]
        public void ClaimIsExclusive()
        {
            DgtDriverGate.ResetForTests();

            Assert.IsTrue(DgtDriverGate.TryBeginInitialising(), "the first claim should be granted");
            Assert.IsFalse(
                DgtDriverGate.TryBeginInitialising(),
                "a second claim must not be granted while the first is still running");
        }

        [TestMethod(DisplayName = "A claim reads as initialising, not as initialised")]
        public void ClaimIsObservable()
        {
            DgtDriverGate.ResetForTests();

            Assert.IsTrue(DgtDriverGate.TryBeginInitialising());
            Assert.IsTrue(DgtDriverGate.IsInitialising);
            Assert.IsFalse(DgtDriverGate.IsInitialised, "a driver that is still starting up is not ready");
        }

        /// <summary>
        /// The claim exists to stop a nested Init re-entering the driver while the outer one
        /// holds the init mutex. That only works if the claim is visible to the nested call
        /// for the whole of the native initialisation, which is what this pins.
        /// </summary>
        [TestMethod(DisplayName = "An abandoned claim lets a later attempt through")]
        public void AbandonedClaimCanBeRetaken()
        {
            DgtDriverGate.ResetForTests();

            Assert.IsTrue(DgtDriverGate.TryBeginInitialising());
            DgtDriverGate.MarkInitialisationAbandoned();

            Assert.IsFalse(DgtDriverGate.IsInitialising, "the claim should be released");
            Assert.IsTrue(
                DgtDriverGate.TryBeginInitialising(),
                "a driver that failed to initialise must not block the next attempt forever");
        }

        [TestMethod(DisplayName = "Abandoning cannot undo a completed initialisation")]
        public void AbandonDoesNotUndoInitialisation()
        {
            DgtDriverGate.ResetForTests();

            Assert.IsTrue(DgtDriverGate.TryBeginInitialising());
            DgtDriverGate.MarkInitialised();
            DgtDriverGate.MarkInitialisationAbandoned();

            Assert.IsTrue(DgtDriverGate.IsInitialised, "a completed initialisation is permanent");
            Assert.IsFalse(
                DgtDriverGate.TryBeginInitialising(),
                "an initialised driver must never be claimed for a second initialisation");
        }

        /// <summary>
        /// The facade, the adapter and the gRPC service all reach the driver through this
        /// class, which only means anything if they share one lock. A call that returns a
        /// value - the facade reads a version string - must queue behind a plain call, not
        /// slip past it onto the driver.
        /// </summary>
        [TestMethod(DisplayName = "A returning call waits for the same lock as the others")]
        public void GenericRunSharesTheLock()
        {
            using ManualResetEventSlim holding = new();
            using ManualResetEventSlim release = new();
            using ManualResetEventSlim returningCallEntered = new();

            Task holder = Task.Run(() => DgtDriverGate.Run(() =>
            {
                holding.Set();
                _ = release.Wait(TimeSpan.FromSeconds(10));
            }));

            Assert.IsTrue(holding.Wait(TimeSpan.FromSeconds(5)), "the holder should have taken the lock");

            Task<bool> queued = Task.Run(() => DgtDriverGate.Run(() =>
            {
                returningCallEntered.Set();
                return true;
            }));

            Assert.IsFalse(
                returningCallEntered.Wait(TimeSpan.FromMilliseconds(250)),
                "a call that returns a value must still wait for the driver lock");

            release.Set();

            Assert.IsTrue(queued.Wait(TimeSpan.FromSeconds(10)), "the queued call should complete once the lock is free");
            Assert.IsTrue(returningCallEntered.IsSet);
            Assert.IsTrue(holder.Wait(TimeSpan.FromSeconds(10)));
        }
    }
}
