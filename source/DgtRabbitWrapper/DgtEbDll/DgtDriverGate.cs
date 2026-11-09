using System;
using System.Threading;

namespace DgtRabbitWrapper.DgtEbDll
{
    /// <summary>
    /// Serialises every call into the DGT driver.
    /// </summary>
    /// <remarks>
    /// dgtebdll.dll is not thread-safe - it is a wxWidgets application that also drives
    /// hardware, and overlapping calls corrupt its state (seen as SEHExceptions and as
    /// writes that return success but are silently dropped).
    ///
    /// The driver is reached from two places: Cherub's own threads through
    /// <see cref="DgtEbDllAdapter"/> and gRPC server threads through
    /// <c>DgtService</c>.  The adapter's own
    /// <see cref="System.Runtime.CompilerServices.MethodImplAttribute"/> synchronisation
    /// only serialises its own callers, so it cannot protect against a gRPC call landing
    /// at the same moment.  Both paths therefore take this one lock.
    /// </remarks>
    public static class DgtDriverGate
    {
        /// <summary>
        /// The lock every path into the driver takes.
        /// </summary>
        /// <remarks>
        /// The facade, the adapter and the gRPC service all reach the driver through this
        /// class. There used to be two locks - this one for the gRPC path and the facade's
        /// own instance lock, from MethodImplOptions.Synchronized - which meant a facade call
        /// and a gRPC call could land on the driver at the same instant and corrupt it, the
        /// very thing this class exists to prevent.
        /// </remarks>
        private static readonly object Sync = typeof(DgtEbDllAdapter);

        private const int NOT_STARTED = 0;
        private const int INITIALISING = 1;
        private const int INITIALISED = 2;

        private static int _state;

        /// <summary>
        /// Whether this process has finished initialising the shared driver.
        /// </summary>
        /// <remarks>
        /// The driver is a single instance owned by this process.  A remote client's
        /// <c>_DGTDLL_Init</c> asks whether it is ready, not for a second
        /// initialisation: re-running it re-registers every callback on a live driver
        /// and comes back FAIL, which told every proxied client that init had failed.
        /// </remarks>
        public static bool IsInitialised => Volatile.Read(ref _state) == INITIALISED;

        /// <summary>
        /// Whether an initialisation has been claimed and has not yet finished.
        /// </summary>
        public static bool IsInitialising => Volatile.Read(ref _state) == INITIALISING;

        /// <summary>
        /// Claims the right to initialise the driver.
        /// </summary>
        /// <remarks>
        /// Claim <em>before</em> the first native call, not after: the claim is only worth
        /// anything while the initialisation is still running.
        ///
        /// This exists because of the shim. When a shim takes dgtebdll.dll's place it
        /// answers <c>_DGTDLL_Init</c> by calling back into this process over gRPC, so a
        /// second Init arrives on another thread while the first is still inside the
        /// native call, holding the shim's init mutex. Without a claim the nested call
        /// runs the native Init again, blocks on that mutex, and both threads wait
        /// forever - the startup deadlock of 2026-11-09, which showed as "Not Responding"
        /// for five seconds per call with no exception and nothing in the log.
        /// </remarks>
        /// <returns><c>true</c> if this caller now owns initialisation.</returns>
        public static bool TryBeginInitialising()
            => Interlocked.CompareExchange(ref _state, INITIALISING, NOT_STARTED) == NOT_STARTED;

        /// <summary>Records that the driver has been initialised in this process.</summary>
        public static void MarkInitialised() => Interlocked.Exchange(ref _state, INITIALISED);

        /// <summary>
        /// Releases a claim that did not produce a working driver, so a later attempt can be
        /// made. Never undoes a completed initialisation.
        /// </summary>
        public static void MarkInitialisationAbandoned()
            => Interlocked.CompareExchange(ref _state, NOT_STARTED, INITIALISING);

        /// <summary>Returns the gate to its unclaimed state. For tests only.</summary>
        internal static void ResetForTests() => Interlocked.Exchange(ref _state, NOT_STARTED);

        /// <summary>
        /// Runs a driver call that returns the driver's result code.
        /// </summary>
        public static int Run(Func<int> call)
        {
            ArgumentNullException.ThrowIfNull(call);

            lock (Sync)
            {
                return call();
            }
        }

        /// <summary>
        /// Runs a driver call whose result the caller needs, holding the same lock as every
        /// other call into the driver.
        /// </summary>
        public static T Run<T>(Func<T> call)
        {
            ArgumentNullException.ThrowIfNull(call);

            lock (Sync)
            {
                return call();
            }
        }

        /// <summary>
        /// Runs a driver call whose result the caller does not need.
        /// </summary>
        public static void Run(Action call)
        {
            ArgumentNullException.ThrowIfNull(call);

            lock (Sync)
            {
                call();
            }
        }
    }
}
