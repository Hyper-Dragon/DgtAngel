using Microsoft.VisualStudio.TestTools.UnitTesting;
using UciComms;

namespace ChessHelpersTest
{
    /// <summary>
    /// The kibitzer switch.
    /// </summary>
    /// <remarks>
    /// The kibitzer path is unfinished: nothing publishes a kibitzer FEN yet, so OnKibitzerFenChange
    /// still has no producer. What can be held down today is the switch itself - that it raises the
    /// events the UI listens for, and that it records the state it was switched to, so the remaining
    /// work has something stable to build on.
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

        [TestMethod(DisplayName = "Switching the kibitzer off raises Deactivated and clears the state")]
        public void SwitchOffRaisesDeactivated()
        {
            UciEngineManager manager = new();
            int deactivated = 0;
            manager.OnKibitzerDeactivated += () => deactivated++;

            manager.SwitchKibitzer(true);
            manager.SwitchKibitzer(false);

            Assert.AreEqual(1, deactivated, "switching off must raise OnKibitzerDeactivated once");
            Assert.IsFalse(manager.IsKibitzerRunning, "the manager must record that the kibitzer is off");
        }

        [TestMethod(DisplayName = "A fresh manager has the kibitzer off")]
        public void KibitzerStartsOff()
        {
            Assert.IsFalse(new UciEngineManager().IsKibitzerRunning);
        }
    }
}
