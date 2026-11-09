using DgtCherub.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DgtCherubTest
{
    /// <summary>
    /// The rule behind the kibitzer switch being greyed out.
    /// </summary>
    /// <remarks>
    /// This was a bug seen in a real session: the switch was ticked, the kibitzer ran and scored,
    /// and then the switch came back enabled - because the engine was reloaded after the switch
    /// had been used and the reload enabled it again. Unticking it was still refused, so nothing
    /// could actually be switched off, but a live control that does nothing is an invitation to
    /// try again, and the refusal is only as good as the last place that set Enabled.
    /// </remarks>
    [TestClass]
    public class KibitzerAvailabilityTests
    {
        [TestMethod(DisplayName = "The switch can be used once an engine is loaded")]
        public void EnabledWhenEngineReadyAndKibitzerOff()
        {
            Assert.IsTrue(KibitzerAvailability.SwitchEnabled(engineReady: true, kibitzerRunning: false));
        }

        [TestMethod(DisplayName = "The switch is not usable before an engine is loaded")]
        public void DisabledWithoutAnEngine()
        {
            Assert.IsFalse(KibitzerAvailability.SwitchEnabled(engineReady: false, kibitzerRunning: false));
        }

        [TestMethod(DisplayName = "The switch stays shut once the kibitzer has been switched on")]
        public void DisabledOnceRunning()
        {
            Assert.IsFalse(KibitzerAvailability.SwitchEnabled(engineReady: true, kibitzerRunning: true));
        }

        [TestMethod(DisplayName = "A reloaded engine cannot hand the switch back")]
        public void DisabledOnceRunningEvenWithAFreshEngine()
        {
            // The engine reloading is the case that was missed: loading is the event that makes
            // the switch usable in the first place, so it has to ask the same question.
            Assert.IsFalse(KibitzerAvailability.SwitchEnabled(engineReady: true, kibitzerRunning: true),
                           "a loaded engine must not re-enable a switch the session has already used");
        }
    }
}
