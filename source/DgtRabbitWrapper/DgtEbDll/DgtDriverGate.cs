using System;

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
        /// Deliberately the adapter's type object: every method on
        /// <see cref="DgtEbDllAdapter"/> is declared
        /// <see cref="System.Runtime.CompilerServices.MethodImplOptions.Synchronized"/>, and
        /// for a static method that synchronises on the declaring type.  Sharing that same
        /// object means the adapter's own callers and the gRPC path contend on one lock
        /// without having to touch the adapter at all.
        /// </summary>
        private static readonly object Sync = typeof(DgtEbDllAdapter);

        /// <summary>
        /// Runs a driver call that returns the driver's result code.
        /// </summary>
        public static int Run(Func<int> call)
        {
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
            lock (Sync)
            {
                call();
            }
        }
    }
}
