﻿using DgtRabbitWrapper.DgtEbDll;
using Grpc.Core;
using DgtGrpcService;
using NAudio.Gui;
using System.Text;
using static DgtRabbitWrapper.DgtEbDll.DgtEbDllImport;
using Microsoft.AspNetCore.Rewrite;
using Google.Protobuf.WellKnownTypes;
using System.Collections.Concurrent;

namespace DgtGrpcService.Services
{
    public class DgtService : DGTDLL.DGTDLLBase
    {
        //DgtRabbitWrapper.DgtEbDll.DgtEbDllImport dgtEbDllImport = new DgtRabbitWrapper.DgtEbDll.DgtEbDllImport();

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


        private static ConcurrentQueue<string> queue = new();
        private static readonly CallbackStableBoardFunc _callbackStableBoardInstance = new(CallbackStableBoardInstanceMethod);
        private static void CallbackStableBoardInstanceMethod(string fenString) 
        {
            queue.Enqueue(fenString[..]);
        }
        
        public override async Task RegisterStableBoardFunc(DgtGrpcService.Empty request, IServerStreamWriter<DgtGrpcService.StringResponse> responseStream, ServerCallContext context)
        {
            //Register the callback function with the DgtRabbitWrapper
            _ = DgtRabbitWrapper.DgtEbDll.DgtEbDllImport.RegisterStableBoardFunc(
                _callbackStableBoardInstance,
                IntPtr.Zero
            );

            while (!context.CancellationToken.IsCancellationRequested)
            {
                while (queue.TryDequeue(out string val))
                {
                    await responseStream.WriteAsync(new DgtGrpcService.StringResponse { Value = val });
                }
                
                await Task.Delay(1000); // Gotta look busy
            }
        }
    }
}
