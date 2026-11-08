using DgtGrpcService;
using Grpc.Core;
using System.Text;
using System.Threading.Channels;

namespace DgtCherub.Services
{
    public class DgtService : DGTDLL.DGTDLLBase
    {
        private const int StreamBufferSize = 64;

        private readonly IBoardEventSource _boardEvents;
        private readonly IAngelHubService _hub;

        public DgtService(IBoardEventSource boardEvents, IAngelHubService hub) : base()
        {
            // Registering the native callbacks belongs to the IBoardEventSource
            // singleton, not to this service: gRPC constructs one DgtService per
            // call, so the old constructor re-registered every callback for every
            // request and kept every previous instance's delegates alive.
            _boardEvents = boardEvents;
            _hub = hub;
        }

        /// <summary>
        /// Whether a clock call from this client should be forwarded to the shared driver.
        /// With desktop client mode off, Cherub owns the clock and these calls are accepted
        /// but ignored, so a client cannot fight Cherub for the display.
        /// </summary>
        private bool ShouldForwardClockCalls => _hub.DesktopClientMode;

        public override Task<IntResponse> Init(Empty request, ServerCallContext context)
        {
            return Task.FromResult(new IntResponse
            {
                Value = DgtRabbitWrapper.DgtEbDll.DgtDriverGate.Run(() => DgtRabbitWrapper.DgtEbDll.DgtEbDllImport.Init())
            });
        }

        public override Task<IntResponse> GetVersion(Empty request, ServerCallContext context)
        {
            return Task.FromResult(new IntResponse
            {
                Value = DgtRabbitWrapper.DgtEbDll.DgtDriverGate.Run(() => DgtRabbitWrapper.DgtEbDll.DgtEbDllImport.GetVersion())
            });
        }

        public override Task<IntResponse> UseFEN(BoolRequest request, ServerCallContext context)
        {
            return Task.FromResult(new IntResponse
            {
                Value = DgtRabbitWrapper.DgtEbDll.DgtDriverGate.Run(() => DgtRabbitWrapper.DgtEbDll.DgtEbDllImport.UseFEN(request.Value))
            });
        }

        public override Task<IntResponse> WriteDebug(BoolRequest request, ServerCallContext context)
        {
            return Task.FromResult(new IntResponse
            {
                Value = DgtRabbitWrapper.DgtEbDll.DgtDriverGate.Run(() => DgtRabbitWrapper.DgtEbDll.DgtEbDllImport.WriteDebug(request.Value))
            }); ;
        }
        public override Task<IntResponse> SetAutoRotation(BoolRequest request, ServerCallContext context)
        {
            return Task.FromResult(new IntResponse
            {
                Value = DgtRabbitWrapper.DgtEbDll.DgtDriverGate.Run(() => DgtRabbitWrapper.DgtEbDll.DgtEbDllImport.SetAutoRotation(request.Value))
            });
        }

        public override Task<IntResponse> UseSAN(BoolRequest request, ServerCallContext context)
        {
            return Task.FromResult(new IntResponse
            {
                Value = DgtRabbitWrapper.DgtEbDll.DgtDriverGate.Run(() => DgtRabbitWrapper.DgtEbDll.DgtEbDllImport.UseSAN(request.Value))
            });
        }
        public override Task<IntResponse> SetGameType(IntRequest request, ServerCallContext context)
        {
            return Task.FromResult(new IntResponse
            {
                Value = DgtRabbitWrapper.DgtEbDll.DgtDriverGate.Run(() => DgtRabbitWrapper.DgtEbDll.DgtEbDllImport.SetGameType(request.Value))
            });
        }
        public override Task<IntResponse> HideDialog(IntRequest request, ServerCallContext context)
        {
            return Task.FromResult(new IntResponse
            {
                Value = DgtRabbitWrapper.DgtEbDll.DgtDriverGate.Run(() => DgtRabbitWrapper.DgtEbDll.DgtEbDllImport.HideDialog(request.Value))
            });
        }
        public override Task<IntResponse> ShowDialog(IntRequest request, ServerCallContext context)
        {
            return Task.FromResult(new IntResponse
            {
                Value = DgtRabbitWrapper.DgtEbDll.DgtDriverGate.Run(() => DgtRabbitWrapper.DgtEbDll.DgtEbDllImport.ShowDialog(request.Value))
            });
        }

        public override Task<IntResponse> ClockMode(IntRequest request, ServerCallContext context)
        {
            return Task.FromResult(new IntResponse
            {
                Value = DgtRabbitWrapper.DgtEbDll.DgtDriverGate.Run(() => DgtRabbitWrapper.DgtEbDll.DgtEbDllImport.ClockMode(request.Value))
            });
        }

        public override Task<IntResponse> SetNRun(SetNRunRequest request, ServerCallContext context)
        {
            if (!ShouldForwardClockCalls)
            {
                return Task.FromResult(new IntResponse { Value = 0 });
            }

            return Task.FromResult(new IntResponse
            {
                Value = DgtRabbitWrapper.DgtEbDll.DgtDriverGate.Run(() =>
                    DgtRabbitWrapper.DgtEbDll.DgtEbDllImport.SetNRun(
                        new StringBuilder(request.Param1),
                        new StringBuilder(request.Param2),
                        request.Time))
            });
        }

        public override Task<IntResponse> DisplayClockMessage(ClockMessageRequest request, ServerCallContext context)
        {
            if (!ShouldForwardClockCalls)
            {
                return Task.FromResult(new IntResponse { Value = 0 });
            }

            return Task.FromResult(new IntResponse
            {
                Value = DgtRabbitWrapper.DgtEbDll.DgtDriverGate.Run(() => DgtRabbitWrapper.DgtEbDll.DgtEbDllImport.DisplayClockMessage(new StringBuilder(request.Message), request.Time))
            });
        }

        public override Task<IntResponse> EndDisplay(IntRequest request, ServerCallContext context)
        {
            if (!ShouldForwardClockCalls)
            {
                return Task.FromResult(new IntResponse { Value = 0 });
            }

            return Task.FromResult(new IntResponse
            {
                Value = DgtRabbitWrapper.DgtEbDll.DgtDriverGate.Run(() => DgtRabbitWrapper.DgtEbDll.DgtEbDllImport.EndDisplay(request.Value))
            });
        }

        public override Task<IntResponse> AllowTakebacks(BoolRequest request, ServerCallContext context)
        {
            return Task.FromResult(new IntResponse
            {
                Value = DgtRabbitWrapper.DgtEbDll.DgtDriverGate.Run(() => DgtRabbitWrapper.DgtEbDll.DgtEbDllImport.AllowTakebacks(request.Value))
            });
        }

        public override Task<IntResponse> Exit(Empty request, ServerCallContext context)
        {
            // Deliberately does NOT call the driver's Exit(): Cherub owns the shared
            // board connection, and a remote client disconnecting must not take the
            // board away from it (or from any other client).  There is no per-client
            // driver state to release on this side.
            return Task.FromResult(new IntResponse { Value = 0 });
        }

        public override Task<IntResponse> GetWxWidgetsVersion(Empty request, ServerCallContext context)
        {
            return Task.FromResult(new IntResponse
            {
                Value = DgtRabbitWrapper.DgtEbDll.DgtEbDllImport.GetWxWidgetsVersion()
            });
        }

        public override Task<IntResponse> WriteCOMPort(IntRequest request, ServerCallContext context)
        {
            return Task.FromResult(new IntResponse
            {
                Value = DgtRabbitWrapper.DgtEbDll.DgtEbDllImport.WriteCOMPort(request.Value)
            });
        }

        public override Task<IntResponse> PlayBlackMove(StringRequest request, ServerCallContext context)
        {
            return Task.FromResult(new IntResponse
            {
                Value = DgtRabbitWrapper.DgtEbDll.DgtEbDllImport.PlayBlackMove(new StringBuilder(request.Value))
            });
        }

        public override Task<IntResponse> PlayWhiteMove(StringRequest request, ServerCallContext context)
        {
            return Task.FromResult(new IntResponse
            {
                Value = DgtRabbitWrapper.DgtEbDll.DgtEbDllImport.PlayWhiteMove(new StringBuilder(request.Value))
            });
        }

        public override Task<IntResponse> WriteCOMPortString(StringRequest request, ServerCallContext context)
        {
            return Task.FromResult(new IntResponse
            {
                Value = DgtRabbitWrapper.DgtEbDll.DgtEbDllImport.WriteCOMPortString(new StringBuilder(request.Value))
            });
        }
        public override Task<IntResponse> WritePosition(StringRequest request, ServerCallContext context)
        {
            return Task.FromResult(new IntResponse
            {
                Value = DgtRabbitWrapper.DgtEbDll.DgtEbDllImport.WritePosition(new StringBuilder(request.Value))
            });
        }

        /// <summary>
        /// Streams one of the board events to a gRPC client.
        /// </summary>
        /// <remarks>
        /// The native driver raises these on its own threads, several at a time, and
        /// gRPC's server stream writer throws if two writes overlap.  A bounded
        /// channel gives the stream a single writer and stops a slow client from
        /// building an unbounded backlog - the oldest events (stale clock ticks and
        /// board positions) are dropped first.  A failed write ends the stream
        /// instead of spinning on a dead connection.
        /// </remarks>
        private static async Task StreamStringsAsync(
            IServerStreamWriter<StringResponse> responseStream,
            ServerCallContext context,
            Action<EventHandler<string>> subscribe,
            Action<EventHandler<string>> unsubscribe)
        {
            Channel<string> channel = Channel.CreateBounded<string>(
                new BoundedChannelOptions(StreamBufferSize)
                {
                    FullMode = BoundedChannelFullMode.DropOldest,
                    SingleReader = true,
                    SingleWriter = false,
                });

            void Handler(object sender, string value) => channel.Writer.TryWrite(value);

            EventHandler<string> handler = Handler;
            subscribe(handler);

            try
            {
                while (await channel.Reader.WaitToReadAsync(context.CancellationToken).ConfigureAwait(false))
                {
                    while (channel.Reader.TryRead(out string value))
                    {
                        await responseStream.WriteAsync(new StringResponse { Value = value }).ConfigureAwait(false);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // The client cancelled the stream.
            }
            catch (RpcException)
            {
                // The connection went away.
            }
            catch (IOException)
            {
                // The connection went away mid-write.
            }
            catch (InvalidOperationException)
            {
                // The request has already completed.
            }
            finally
            {
                unsubscribe(handler);
                _ = channel.Writer.TryComplete();
            }
        }

        public override Task RegisterStatusFunc(Empty request, IServerStreamWriter<StringResponse> responseStream, ServerCallContext context)
        {
            return StreamStringsAsync(responseStream, context, handler => _boardEvents.Status += handler, handler => _boardEvents.Status -= handler);
        }

        public override Task RegisterStableBoardFunc(Empty request, IServerStreamWriter<StringResponse> responseStream, ServerCallContext context)
        {
            return StreamStringsAsync(responseStream, context, handler => _boardEvents.StableBoard += handler, handler => _boardEvents.StableBoard -= handler);
        }

        public override Task RegisterScanFunc(Empty request, IServerStreamWriter<StringResponse> responseStream, ServerCallContext context)
        {
            return StreamStringsAsync(responseStream, context, handler => _boardEvents.Scan += handler, handler => _boardEvents.Scan -= handler);
        }

        public override Task RegisterWClockFunc(Empty request, IServerStreamWriter<StringResponse> responseStream, ServerCallContext context)
        {
            return StreamStringsAsync(responseStream, context, handler => _boardEvents.WClock += handler, handler => _boardEvents.WClock -= handler);
        }

        public override Task RegisterBClockFunc(Empty request, IServerStreamWriter<StringResponse> responseStream, ServerCallContext context)
        {
            return StreamStringsAsync(responseStream, context, handler => _boardEvents.BClock += handler, handler => _boardEvents.BClock -= handler);
        }

        public override Task RegisterResultFunc(Empty request, IServerStreamWriter<StringResponse> responseStream, ServerCallContext context)
        {
            return StreamStringsAsync(responseStream, context, handler => _boardEvents.Result += handler, handler => _boardEvents.Result -= handler);
        }

        public override Task RegisterNewGameFunc(Empty request, IServerStreamWriter<StringResponse> responseStream, ServerCallContext context)
        {
            return StreamStringsAsync(responseStream, context, handler => _boardEvents.NewGame += handler, handler => _boardEvents.NewGame -= handler);
        }

        public override Task RegisterWhiteMoveInputFunc(Empty request, IServerStreamWriter<StringResponse> responseStream, ServerCallContext context)
        {
            return StreamStringsAsync(responseStream, context, handler => _boardEvents.WhiteMoveInput += handler, handler => _boardEvents.WhiteMoveInput -= handler);
        }

        public override Task RegisterBlackMoveInputFunc(Empty request, IServerStreamWriter<StringResponse> responseStream, ServerCallContext context)
        {
            return StreamStringsAsync(responseStream, context, handler => _boardEvents.BlackMoveInput += handler, handler => _boardEvents.BlackMoveInput -= handler);
        }

        public override Task RegisterWhiteMoveNowFunc(Empty request, IServerStreamWriter<StringResponse> responseStream, ServerCallContext context)
        {
            return StreamStringsAsync(responseStream, context, handler => _boardEvents.WhiteMoveNow += handler, handler => _boardEvents.WhiteMoveNow -= handler);
        }

        public override Task RegisterBlackMoveNowFunc(Empty request, IServerStreamWriter<StringResponse> responseStream, ServerCallContext context)
        {
            return StreamStringsAsync(responseStream, context, handler => _boardEvents.BlackMoveNow += handler, handler => _boardEvents.BlackMoveNow -= handler);
        }

        public override Task RegisterStartSetupFunc(Empty request, IServerStreamWriter<StringResponse> responseStream, ServerCallContext context)
        {
            return StreamStringsAsync(responseStream, context, handler => _boardEvents.StartSetup += handler, handler => _boardEvents.StartSetup -= handler);
        }

        public override Task RegisterStopSetupWTMFunc(Empty request, IServerStreamWriter<StringResponse> responseStream, ServerCallContext context)
        {
            return StreamStringsAsync(responseStream, context, handler => _boardEvents.StopSetupWTM += handler, handler => _boardEvents.StopSetupWTM -= handler);
        }

        public override Task RegisterStopSetupBTMFunc(Empty request, IServerStreamWriter<StringResponse> responseStream, ServerCallContext context)
        {
            return StreamStringsAsync(responseStream, context, handler => _boardEvents.StopSetupBTM += handler, handler => _boardEvents.StopSetupBTM -= handler);
        }


        /*
              public override Task RegisterGameTypeChangedFunc(DgtGrpcService.Empty request, IServerStreamWriter<DgtGrpcService.IntResponse> responseStream, ServerCallContext context) { return StringEventWriter(responseStream, context, (sender, stringOut) => DummyEvent(sender, stringOut)); }
              public override Task RegisterAllowTakebacksChangedFunc(DgtGrpcService.Empty request, IServerStreamWriter<DgtGrpcService.BoolResponse> responseStream, ServerCallContext context) { return StringEventWriter(responseStream, context, (sender, stringOut) => DummyEvent(sender, stringOut)); }
              public override Task RegisterMagicPieceFunc(DgtGrpcService.Empty request, IServerStreamWriter<DgtGrpcService.CallbackIICResponse> responseStream, ServerCallContext context) { return StringEventWriter(responseStream, context, (sender, stringOut) => DummyEvent(sender, stringOut)); }
              public override Task RegisterWhiteTakebackFunc(DgtGrpcService.Empty request, IServerStreamWriter<DgtGrpcService.EmptyResponse> responseStream, ServerCallContext context) { return StringEventWriter(responseStream, context, (sender, stringOut) => DummyEvent(sender, stringOut)); }
              public override Task RegisterBlackTakebackFunc(DgtGrpcService.Empty request, IServerStreamWriter<DgtGrpcService.EmptyResponse> responseStream, ServerCallContext context) { return StringEventWriter(responseStream, context, (sender, stringOut) => DummyEvent(sender, stringOut)); }
        */
    }
}
