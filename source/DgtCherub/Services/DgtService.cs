using Grpc.Core;
using GrpcServiceDgtTest;

namespace GrpcServiceDgtTest.Services
{
    public class DgtService : DGTDLL.DGTDLLBase
    {

        public override Task<IntResponse> Init(Empty request, ServerCallContext context)
        {
            return base.Init(request, context);
        }


        public override Task<IntResponse> GetVersion(Empty request, ServerCallContext context)
        {
            return Task.FromResult(new IntResponse { Value = 123 });
        }

    }
}
