using DgtCherub.Services;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace DgtCherubTest.TestSupport
{
    /// <summary>
    /// TimeProvider whose timers fire immediately, so the processors' post-event delays
    /// cost nothing in a test. GetUtcNow only moves when the test says so.
    /// </summary>
    internal sealed class InstantTimeProvider : TimeProvider
    {
        /// <summary>The instant every fake clock starts at, so fixtures can capture "now".</summary>
        public static readonly DateTimeOffset DefaultStart = new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);

        private DateTimeOffset _now;

        public InstantTimeProvider() : this(DefaultStart) { }

        public InstantTimeProvider(DateTimeOffset start) => _now = start;

        public override DateTimeOffset GetUtcNow() => _now;

        /// <summary>Current fake time in Unix milliseconds - what a capture time should be.</summary>
        public long NowMilliseconds => _now.ToUnixTimeMilliseconds();

        /// <summary>Moves the clock forward without waiting.</summary>
        public void Advance(TimeSpan by) => _now = _now.Add(by);

        public override ITimer CreateTimer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period)
        {
            // Fire off the calling thread so the delay completes without re-entering the
            // continuation that created it.
            _ = ThreadPool.QueueUserWorkItem(_ =>
            {
                try
                {
                    callback(state);
                }
                catch
                {
                    // A thrown callback is the timer owner's problem, not the timer's.
                }
            });

            return new ImmediateTimer();
        }

        private sealed class ImmediateTimer : ITimer
        {
            public bool Change(TimeSpan dueTime, TimeSpan period) => true;

            public void Dispose() { }

            public ValueTask DisposeAsync() => ValueTask.CompletedTask;
        }
    }

    /// <summary>
    /// Board events under the test's control. DgtService only subscribes, so this exists
    /// purely so a test can decide what the driver "reports" and in what order.
    /// </summary>
#pragma warning disable CS0067 // Events are raised through the Raise* helpers only.
    internal sealed class FakeBoardEventSource : IBoardEventSource
    {
        public event EventHandler<string>? StableBoard;
        public event EventHandler<string>? Status;
        public event EventHandler<string>? Scan;
        public event EventHandler<string>? WClock;
        public event EventHandler<string>? BClock;
        public event EventHandler<string>? Result;
        public event EventHandler<string>? NewGame;
        public event EventHandler<string>? WhiteMoveInput;
        public event EventHandler<string>? BlackMoveInput;
        public event EventHandler<string>? WhiteMoveNow;
        public event EventHandler<string>? BlackMoveNow;
        public event EventHandler<string>? StartSetup;
        public event EventHandler<string>? StopSetupWTM;
        public event EventHandler<string>? StopSetupBTM;
        public event EventHandler<int>? GameTypeChanged;
        public event EventHandler<bool>? AllowTakebacksChanged;
        public event EventHandler? WhiteTakeback;
        public event EventHandler? BlackTakeback;

        /// <summary>How many handlers are currently attached - lets tests prove unsubscribe.</summary>
        public int StableBoardSubscriberCount => StableBoard?.GetInvocationList().Length ?? 0;

        public int StatusSubscriberCount => Status?.GetInvocationList().Length ?? 0;

        public void RaiseStableBoard(string board) => StableBoard?.Invoke(this, board);

        public void RaiseStatus(string message) => Status?.Invoke(this, message);

        public void RaiseScan(string board) => Scan?.Invoke(this, board);

        public void RaiseWClock(string clock) => WClock?.Invoke(this, clock);

        public void RaiseBClock(string clock) => BClock?.Invoke(this, clock);

        public void RaiseNewGame(string message) => NewGame?.Invoke(this, message);

        public void RaiseResult(string message) => Result?.Invoke(this, message);
    }
#pragma warning restore CS0067
}
