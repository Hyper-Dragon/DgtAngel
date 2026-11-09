using Grpc.Core;
using DgtGrpcService;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace DgtCherubTest.TestSupport
{
    /// <summary>
    /// Minimal <see cref="ServerCallContext"/>. Grpc.Core ships no public implementation,
    /// and the service only ever reads CancellationToken from it.
    /// </summary>
    internal sealed class TestServerCallContext : ServerCallContext
    {
        private readonly CancellationToken _cancellationToken;

        private TestServerCallContext(CancellationToken cancellationToken) => _cancellationToken = cancellationToken;

        public static TestServerCallContext Create(CancellationToken cancellationToken) => new(cancellationToken);

        protected override CancellationToken CancellationTokenCore => _cancellationToken;

        protected override IDictionary<object, object> UserStateCore { get; } = new Dictionary<object, object>();

        protected override string MethodCore => "test";

        protected override string HostCore => "localhost";

        protected override string PeerCore => "test-peer";

        protected override DateTime DeadlineCore => DateTime.MaxValue;

        protected override Metadata RequestHeadersCore => new();

        protected override Metadata ResponseTrailersCore { get; } = new();

        protected override Status StatusCore { get; set; }

        protected override WriteOptions? WriteOptionsCore { get; set; }

        protected override AuthContext AuthContextCore { get; } = new(null, new Dictionary<string, List<AuthProperty>>());

        protected override ContextPropagationToken CreatePropagationTokenCore(ContextPropagationOptions? options) => throw new NotSupportedException();

        protected override Task WriteResponseHeadersAsyncCore(Metadata responseHeaders) => Task.CompletedTask;
    }

    /// <summary>
    /// Captures everything written to a server stream, and can hold the first write so a
    /// test can build a backlog behind it.
    /// </summary>
    internal sealed class CollectingStreamWriter<T> : IServerStreamWriter<T>
    {
        private readonly List<T> _received = new();
        private readonly SemaphoreSlim _writeSignal = new(0);
        private readonly TaskCompletionSource _firstWriteGate = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource _firstWriteStarted = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly bool _holdFirstWrite;
        private int _writeCount;

        public CollectingStreamWriter(bool holdFirstWrite = false) => _holdFirstWrite = holdFirstWrite;

        public WriteOptions? WriteOptions { get; set; }

        /// <summary>Completes when the writer is first entered, before the hold is applied.</summary>
        public Task FirstWriteStarted => _firstWriteStarted.Task;

        public IReadOnlyList<T> Received
        {
            get
            {
                lock (_received)
                {
                    return _received.ToArray();
                }
            }
        }

        public async Task WriteAsync(T message)
        {
            if (Interlocked.Increment(ref _writeCount) == 1)
            {
                _firstWriteStarted.TrySetResult();

                if (_holdFirstWrite)
                {
                    await _firstWriteGate.Task.ConfigureAwait(false);
                }
            }

            lock (_received)
            {
                _received.Add(message);
            }

            _ = _writeSignal.Release();
        }

        /// <summary>Lets the held first write through.</summary>
        public void ReleaseFirstWrite() => _firstWriteGate.TrySetResult();

        /// <summary>Waits until at least <paramref name="count"/> messages have been written.</summary>
        public async Task WaitForCountAsync(int count, TimeSpan timeout)
        {
            using CancellationTokenSource timeoutSource = new(timeout);
            try
            {
                while (Received.Count < count)
                {
                    await _writeSignal.WaitAsync(timeoutSource.Token).ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException)
            {
                throw new AssertFailedException($"expected at least {count} writes within {timeout.TotalMilliseconds} ms, saw {Received.Count}");
            }
        }
    }

    /// <summary>Stream writer that fails every write, standing in for a dropped connection.</summary>
    internal sealed class FailingStreamWriter<T> : IServerStreamWriter<T>
    {
        public WriteOptions? WriteOptions { get; set; }

        public int Attempts { get; private set; }

        public Task WriteAsync(T message)
        {
            Attempts++;
            throw new InvalidOperationException("synthetic write failure");
        }
    }

    internal sealed class AssertFailedException : Exception
    {
        public AssertFailedException(string message) : base(message) { }
    }
}
