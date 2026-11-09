using Microsoft.VisualStudio.TestTools.UnitTesting;
using UciComms;

namespace ChessHelpersTest
{
    /// <summary>
    /// The kibitzer switch.
    /// </summary>
    /// <remarks>
    /// The kibitzer switch is a one-way control, not a preference. It exists so a player can
    /// declare that a session is for analysis, and it must not be possible to take that
    /// declaration back without restarting - otherwise the engine could be used and then hidden
    /// again before a move was made. These tests hold the switch to that.
    /// </remarks>
    [TestClass]
    public class UciEngineManagerTests
    {
        [TestMethod(DisplayName = "Switching the kibitzer on raises Activated and records the state")]
        public void SwitchOnRaisesActivated()
        {
            UciEngineManager manager = new();
            int activated = 0;
            manager.OnKibitzerActivated += () => activated++;

            manager.SwitchKibitzer(true);

            Assert.AreEqual(1, activated, "switching on must raise OnKibitzerActivated once");
            Assert.IsTrue(manager.IsKibitzerRunning, "the manager must record that the kibitzer is on");
        }

        [TestMethod(DisplayName = "The kibitzer cannot be switched off once it has been switched on")]
        public void SwitchOffIsRefusedOnceOn()
        {
            UciEngineManager manager = new();
            int deactivated = 0;
            manager.OnKibitzerDeactivated += () => deactivated++;

            manager.SwitchKibitzer(true);
            manager.SwitchKibitzer(false);

            // This is the anti-cheat latch: an engine that could be hidden again before a move
            // would be no friction at all. Playing online again means restarting Cherub.
            Assert.AreEqual(0, deactivated, "switching off must not raise OnKibitzerDeactivated");
            Assert.IsTrue(manager.IsKibitzerRunning,
                          "once on, the kibitzer stays on until a new manager is created");
        }

        [TestMethod(DisplayName = "Switching a kibitzer off that was never on does nothing")]
        public void SwitchOffBeforeOnIsHarmless()
        {
            UciEngineManager manager = new();
            int deactivated = 0;
            manager.OnKibitzerDeactivated += () => deactivated++;

            manager.SwitchKibitzer(false);

            Assert.AreEqual(1, deactivated, "there was nothing to latch, so the caller is told it is off");
            Assert.IsFalse(manager.IsKibitzerRunning);
        }

        [TestMethod(DisplayName = "Switching off after the latch is reported as a broken latch")]
        public void SwitchOffAfterOnRaisesLatchBroken()
        {
            UciEngineManager manager = new();
            int broken = 0;
            manager.OnKibitzerLatchBroken += () => broken++;

            manager.SwitchKibitzer(true);
            manager.SwitchKibitzer(false);

            // The application ends the session when this fires: a switch-off that got past the
            // latch means something has gone round the control the kibitzer exists to enforce.
            Assert.AreEqual(1, broken, "bypassing the latch must be reported, not ignored");
            Assert.IsTrue(manager.IsKibitzerRunning, "and the kibitzer is still on");
        }

        [TestMethod(DisplayName = "Switching off a kibitzer that was never on is not a broken latch")]
        public void SwitchOffBeforeOnDoesNotRaiseLatchBroken()
        {
            UciEngineManager manager = new();
            int broken = 0;
            manager.OnKibitzerLatchBroken += () => broken++;

            manager.SwitchKibitzer(false);

            Assert.AreEqual(0, broken, "there was no latch to break");
        }

        [TestMethod(DisplayName = "A fresh manager has the kibitzer off")]
        public void KibitzerStartsOff()
        {
            Assert.IsFalse(new UciEngineManager().IsKibitzerRunning);
        }
    }
}
