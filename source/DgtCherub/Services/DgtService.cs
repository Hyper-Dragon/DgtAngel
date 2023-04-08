using DgtGrpcService;
using Grpc.Core;
using System.Text;
using EbDllInternal = DgtRabbitWrapper.DgtEbDll.DgtEbDllImport;
using static DgtRabbitWrapper.DgtEbDll.DgtEbDllImport;

namespace DgtCherub.Services
{
    public class DgtService : DGTDLL.DGTDLLBase
    {
        //private readonly DgtRabbitWrapper.DgtEbDll.DgtEbDllImport _dgtEbDllImport;

        private event EventHandler<string> RegisteredStableBoardEvent;
        private event EventHandler<string> RegisteredStatusEvent;
        private event EventHandler<string> RegisteredScanEvent;
        private event EventHandler<string> RegisteredWClockEvent;
        private event EventHandler<string> RegisteredBClockEvent;
        private event EventHandler<string> RegisteredResultEvent;
        private event EventHandler<string> RegisteredNewGameEvent;
        private event EventHandler<string> RegisteredWhiteMoveInputEvent;
        private event EventHandler<string> RegisteredBlackMoveInputEvent;
        private event EventHandler<string> RegisteredWhiteMoveNowEvent;
        private event EventHandler<string> RegisteredBlackMoveNowEvent;
        private event EventHandler<string> RegisteredStartSetupEvent;
        private event EventHandler<string> RegisteredStopSetupWTMEvent;
        private event EventHandler<string> RegisteredStopSetupBTMEvent;

        private event EventHandler<int> RegisteredGameTypeChangedEvent;
        private event EventHandler<bool> RegisteredAllowTakebacksChangedEvent;
        //private event EventHandler<string>  RegisteredMagicPieceEvent;
        private event EventHandler RegisteredWhiteTakebackEvent;
        private event EventHandler RegisteredBlackTakebackEvent;

        public DgtService() : base()
        {
            _ = EbDllInternal.RegisterStableBoardFunc(new CallbackStableBoardFunc(x => { RegisteredStableBoardEvent?.Invoke(null, x); }), nint.Zero);
            _ = EbDllInternal.RegisterStatusFunc(new CallbackStatusFunc(x => { RegisteredScanEvent?.Invoke(null, x); }), nint.Zero);
            _ = EbDllInternal.RegisterCallbackScanFunc(new CallbackScanFunc(x => { RegisteredScanEvent?.Invoke(null, x); }), nint.Zero);
            _ = EbDllInternal.RegisterWClockFunc(new CallbackWClockFunc(x => { RegisteredWClockEvent?.Invoke(null, x); }), nint.Zero);
            _ = EbDllInternal.RegisterBClockFunc(new CallbackBClockFunc(x => { RegisteredBClockEvent?.Invoke(null, x); }), nint.Zero);
            _ = EbDllInternal.RegisterResultFunc(new CallbackResultFunc(x => { RegisteredResultEvent?.Invoke(null, x); }), nint.Zero);
            _ = EbDllInternal.RegisterNewGameFunc(new CallbackNewGameFunc(x => { RegisteredNewGameEvent?.Invoke(null, x); }), nint.Zero);
            _ = EbDllInternal.RegisterWhiteMoveInputFunc(new CallbackWhiteMoveInputFunc(x => { RegisteredWhiteMoveInputEvent?.Invoke(null, x); }), nint.Zero);
            _ = EbDllInternal.RegisterBlackMoveInputFunc(new CallbackBlackMoveInputFunc(x => { RegisteredBlackMoveInputEvent?.Invoke(null, x); }), nint.Zero);
            _ = EbDllInternal.RegisterWhiteMoveNowFunc(new CallbackWhiteMoveNowFunc(x => { RegisteredWhiteMoveNowEvent?.Invoke(null, x); }), nint.Zero);
            _ = EbDllInternal.RegisterBlackMoveNowFunc(new CallbackBlackMoveNowFunc(x => { RegisteredBlackMoveNowEvent?.Invoke(null, x); }), nint.Zero);
            _ = EbDllInternal.RegisterStartSetupFunc(new CallbackStartSetupFunc(x => { RegisteredStartSetupEvent?.Invoke(null, x); }), nint.Zero);
            _ = EbDllInternal.RegisterStopSetupWTMFunc(new CallbackStopSetupWTMFunc(x => { RegisteredStopSetupWTMEvent?.Invoke(null, x); }), nint.Zero);
            _ = EbDllInternal.RegisterStopSetupBTMFunc(new CallbackStopSetupBTMFunc(x => { RegisteredStopSetupBTMEvent?.Invoke(null, x); }), nint.Zero);

            _ = EbDllInternal.RegisterGameTypeChangedFunc(new CallbackGameTypeChangedFunc(x => { RegisteredGameTypeChangedEvent?.Invoke(null, x); }), nint.Zero);
            _ = EbDllInternal.RegisterAllowTakebacksChangedFunc(new CallbackAllowTakebacksChangedFunc(x => { RegisteredAllowTakebacksChangedEvent?.Invoke(null, x); }), nint.Zero);
            //_ = DgtRabbitWrapper.DgtEbDll.DgtEbDllImport.RegisterMagicPieceFunc           (new CallbackMagicPieceFunc(x => { RegisteredStopSetupBTMEvent?.Invoke(null, x); }), IntPtr.Zero);
            _ = EbDllInternal.RegisterWhiteTakebackFunc(new CallbackWhiteTakebackFunc(() => { RegisteredWhiteTakebackEvent?.Invoke(null, null); }), nint.Zero);
            _ = EbDllInternal.RegisterBlackTakebackFunc(new CallbackBlackTakebackFunc(() => { RegisteredBlackTakebackEvent?.Invoke(null, null); }), nint.Zero);
        }

        public override Task<IntResponse> Init(Empty request, ServerCallContext context)
        {
            return Task.FromResult(new IntResponse
            {
                Value = DgtRabbitWrapper.DgtEbDll.DgtEbDllImport.Init()
            });
        }

        public override Task<IntResponse> GetVersion(Empty request, ServerCallContext context)
        {
            return Task.FromResult(new IntResponse
            {
                Value = DgtRabbitWrapper.DgtEbDll.DgtEbDllImport.GetVersion()
            });
        }

        public override Task<IntResponse> UseFEN(BoolRequest request, ServerCallContext context)
        {
            return Task.FromResult(new IntResponse
            {
                Value = DgtRabbitWrapper.DgtEbDll.DgtEbDllImport.UseFEN(request.Value)
            });
        }

        public override Task<IntResponse> WriteDebug(BoolRequest request, ServerCallContext context)
        {
            return Task.FromResult(new IntResponse
            {
                Value = DgtRabbitWrapper.DgtEbDll.DgtEbDllImport.WriteDebug(request.Value)
            }); ;
        }
        public override Task<IntResponse> SetAutoRotation(BoolRequest request, ServerCallContext context)
        {
            return Task.FromResult(new IntResponse
            {
                Value = DgtRabbitWrapper.DgtEbDll.DgtEbDllImport.SetAutoRotation(request.Value)
            });
        }

        public override Task<IntResponse> UseSAN(BoolRequest request, ServerCallContext context)
        {
            return Task.FromResult(new IntResponse
            {
                Value = DgtRabbitWrapper.DgtEbDll.DgtEbDllImport.UseSAN(request.Value)
            });
        }
        public override Task<IntResponse> SetGameType(IntRequest request, ServerCallContext context)
        {
            return Task.FromResult(new IntResponse
            {
                Value = DgtRabbitWrapper.DgtEbDll.DgtEbDllImport.SetGameType(request.Value)
            });
        }
        public override Task<IntResponse> HideDialog(IntRequest request, ServerCallContext context)
        {
            return Task.FromResult(new IntResponse
            {
                Value = DgtRabbitWrapper.DgtEbDll.DgtEbDllImport.HideDialog(request.Value)
            });
        }
        public override Task<IntResponse> ShowDialog(IntRequest request, ServerCallContext context)
        {
            return Task.FromResult(new IntResponse
            {
                Value = DgtRabbitWrapper.DgtEbDll.DgtEbDllImport.ShowDialog(request.Value)
            });
        }

        public override Task<IntResponse> ClockMode(IntRequest request, ServerCallContext context)
        {
            return Task.FromResult(new IntResponse
            {
                Value = DgtRabbitWrapper.DgtEbDll.DgtEbDllImport.ClockMode(request.Value)
            });
        }

        public override Task<IntResponse> SetNRun(SetNRunRequest request, ServerCallContext context)
        {
            return Task.FromResult(new IntResponse
            {
                Value = DgtRabbitWrapper.DgtEbDll.DgtEbDllImport.SetNRun(
                    new StringBuilder(request.Param1),
                    new StringBuilder(request.Param2),
                    request.Time)
            });
        }

        public override Task<IntResponse> EndDisplay(IntRequest request, ServerCallContext context)
        {
            return Task.FromResult(new IntResponse
            {
                Value = DgtRabbitWrapper.DgtEbDll.DgtEbDllImport.EndDisplay(request.Value)
            });
        }

        public override Task<IntResponse> DisplayClockMessage(ClockMessageRequest request, ServerCallContext context)
        {
            return Task.FromResult(new IntResponse
            {
                Value = DgtRabbitWrapper.DgtEbDll.DgtEbDllImport.DisplayClockMessage(new StringBuilder(request.Message), request.Time)
            });
        }

        public override Task<IntResponse> AllowTakebacks(BoolRequest request, ServerCallContext context)
        {
            return Task.FromResult(new IntResponse
            {
                Value = DgtRabbitWrapper.DgtEbDll.DgtEbDllImport.AllowTakebacks(request.Value)
            });
        }


        public override Task<IntResponse> Exit(Empty request, ServerCallContext context)
        {
            return Task.FromResult(new IntResponse
            {
                Value = DgtRabbitWrapper.DgtEbDll.DgtEbDllImport.Exit()
            });
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


        private async Task StringEventWriter(IServerStreamWriter<StringResponse> responseStream, ServerCallContext context, EventHandler<string> eventHandler)
        {
            // Create a TaskCompletionSource to signal when a new event is received
            TaskCompletionSource<bool> tcs = new();

            // Define the event handler that will be called when a new event is raised
            void StringEventHandler(object sender, string stringOut)
            {
                // Write the fenString to the response stream and complete the task
                _ = responseStream.WriteAsync(new StringResponse { Value = stringOut })
                    .ContinueWith(_ => tcs.TrySetResult(true), TaskScheduler.Default);
            }

            // Subscribe to the event
            eventHandler += StringEventHandler;

            try
            {
                // Loop while the client connection is still active
                while (!context.CancellationToken.IsCancellationRequested)
                {
                    // Block until the task completion source is signaled (i.e., a new event is received)
                    _ = await tcs.Task;
                    tcs = new TaskCompletionSource<bool>();
                }
            }
            finally
            {
                // Unsubscribe from the event
                eventHandler -= StringEventHandler;
            }
        }



        public override Task RegisterStatusFunc(Empty request, IServerStreamWriter<StringResponse> responseStream, ServerCallContext context)
        {
            return StringEventWriter(responseStream, context, (sender, stringOut) => RegisteredStatusEvent(sender, stringOut));
        }

        public override Task RegisterStableBoardFunc(Empty request, IServerStreamWriter<StringResponse> responseStream, ServerCallContext context)
        {
            return StringEventWriter(responseStream, context, (sender, stringOut) => RegisteredStableBoardEvent(sender, stringOut));
        }

        public override Task RegisterScanFunc(Empty request, IServerStreamWriter<StringResponse> responseStream, ServerCallContext context)
        {
            return StringEventWriter(responseStream, context, (sender, stringOut) => RegisteredScanEvent(sender, stringOut));
        }

        public override Task RegisterWClockFunc(Empty request, IServerStreamWriter<StringResponse> responseStream, ServerCallContext context)
        {
            return StringEventWriter(responseStream, context, (sender, stringOut) => RegisteredWClockEvent(sender, stringOut));
        }

        public override Task RegisterBClockFunc(Empty request, IServerStreamWriter<StringResponse> responseStream, ServerCallContext context)
        {
            return StringEventWriter(responseStream, context, (sender, stringOut) => RegisteredBClockEvent(sender, stringOut));
        }

        public override Task RegisterResultFunc(Empty request, IServerStreamWriter<StringResponse> responseStream, ServerCallContext context)
        {
            return StringEventWriter(responseStream, context, (sender, stringOut) => RegisteredResultEvent(sender, stringOut));
        }

        public override Task RegisterNewGameFunc(Empty request, IServerStreamWriter<StringResponse> responseStream, ServerCallContext context)
        {
            return StringEventWriter(responseStream, context, (sender, stringOut) => RegisteredNewGameEvent(sender, stringOut));
        }

        public override Task RegisterWhiteMoveInputFunc(Empty request, IServerStreamWriter<StringResponse> responseStream, ServerCallContext context)
        {
            return StringEventWriter(responseStream, context, (sender, stringOut) => RegisteredWhiteMoveInputEvent(sender, stringOut));
        }

        public override Task RegisterBlackMoveInputFunc(Empty request, IServerStreamWriter<StringResponse> responseStream, ServerCallContext context)
        {
            return StringEventWriter(responseStream, context, (sender, stringOut) => RegisteredBlackMoveInputEvent(sender, stringOut));
        }

        public override Task RegisterWhiteMoveNowFunc(Empty request, IServerStreamWriter<StringResponse> responseStream, ServerCallContext context)
        {
            return StringEventWriter(responseStream, context, (sender, stringOut) => RegisteredWhiteMoveNowEvent(sender, stringOut));
        }

        public override Task RegisterBlackMoveNowFunc(Empty request, IServerStreamWriter<StringResponse> responseStream, ServerCallContext context)
        {
            return StringEventWriter(responseStream, context, (sender, stringOut) => RegisteredBlackMoveNowEvent(sender, stringOut));
        }

        public override Task RegisterStartSetupFunc(Empty request, IServerStreamWriter<StringResponse> responseStream, ServerCallContext context)
        {
            return StringEventWriter(responseStream, context, (sender, stringOut) => RegisteredStartSetupEvent(sender, stringOut));
        }

        public override Task RegisterStopSetupWTMFunc(Empty request, IServerStreamWriter<StringResponse> responseStream, ServerCallContext context)
        {
            return StringEventWriter(responseStream, context, (sender, stringOut) => RegisteredStopSetupWTMEvent(sender, stringOut));
        }

        public override Task RegisterStopSetupBTMFunc(Empty request, IServerStreamWriter<StringResponse> responseStream, ServerCallContext context)
        {
            return StringEventWriter(responseStream, context, (sender, stringOut) => RegisteredStopSetupBTMEvent(sender, stringOut));
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
