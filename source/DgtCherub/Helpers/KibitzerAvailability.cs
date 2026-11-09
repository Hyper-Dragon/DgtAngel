namespace DgtCherub.Helpers
{
    /// <summary>
    /// Whether the kibitzer switch can still be used.
    /// </summary>
    /// <remarks>
    /// The switch only goes one way, so "may this be used" is not a UI preference that different
    /// events can each have an opinion about - it is a consequence of the kibitzer already being
    /// on. Anything that ends up enabling the switch after it has been used hands back the ability
    /// to hide the engine again, which is the one thing the switch exists to prevent. Every caller
    /// asks here instead of deciding for itself.
    /// </remarks>
    public static class KibitzerAvailability
    {
        /// <summary>
        /// True when there is an engine to run the kibitzer and it has never been switched on.
        /// </summary>
        /// <param name="engineReady">Whether an engine is loaded and usable.</param>
        /// <param name="kibitzerRunning">Whether the kibitzer has already been switched on.</param>
        public static bool SwitchEnabled(bool engineReady, bool kibitzerRunning)
        {
            return engineReady && !kibitzerRunning;
        }
    }
}
