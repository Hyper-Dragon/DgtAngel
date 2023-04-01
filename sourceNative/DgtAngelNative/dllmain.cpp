// dllmain.cpp : Defines the entry point for the DLL application.
#include "pch.h"
#include <memory>
#include <grpcpp/grpcpp.h>
#include <google/protobuf/empty.pb.h>
#include "dgtdll.pb.h"
#include "dgtdll.grpc.pb.h"

typedef int __stdcall FC(const char*);
typedef int __stdcall FI(int);
typedef int __stdcall FB(bool);
typedef int __stdcall F();
typedef int __stdcall FIIC(int, int, const char*);

// Global variables for the channel and stub
const char* CHERUB_GRPC_LISTEN_PORT = "localhost:37965";

std::shared_ptr<grpc::Channel> g_channel;
std::unique_ptr<dgt::DGTDLL::Stub> g_stub;

// Function to reconnect the channel if needed
void ReconnectChannelIfNecessary() {
	if (g_channel->GetState(true) == GRPC_CHANNEL_READY) {
		g_channel = grpc::CreateChannel(CHERUB_GRPC_LISTEN_PORT, grpc::InsecureChannelCredentials());
		g_stub = dgt::DGTDLL::NewStub(g_channel);
	}
}

template <typename RequestType>
int PerformGrpcCall(const std::function<grpc::Status(grpc::ClientContext&, RequestType&, dgt::IntResponse&)>& call) {
	try {
		ReconnectChannelIfNecessary();
		RequestType request;
		dgt::IntResponse response;
		grpc::ClientContext context;
		call(context, request, response);
		return response.value();
	}
	catch (const std::exception& e) {
		return 1;
		//return grpc::Status(grpc::StatusCode::UNKNOWN, e.what());
	}
}

BOOL APIENTRY DllMain(HMODULE hModule,
	DWORD  ul_reason_for_call,
	LPVOID lpReserved
)
{
	switch (ul_reason_for_call)
	{
	case DLL_PROCESS_ATTACH:
		// Initialize the channel and stub
		g_channel = grpc::CreateChannel("localhost:5105", grpc::InsecureChannelCredentials());
		g_stub = dgt::DGTDLL::NewStub(g_channel);
		break;
	case DLL_THREAD_ATTACH:
	case DLL_THREAD_DETACH:
	case DLL_PROCESS_DETACH:
		break;
	}
	return TRUE;
}

extern "C" {
	__declspec(dllexport) int _DGTDLL_GetVersion() {
		auto call = [](grpc::ClientContext& context, dgt::Empty& request, dgt::IntResponse& response) {
			return g_stub->GetVersion(&context, request, &response);
		};

		return PerformGrpcCall<dgt::Empty>(call);
	}

	__declspec(dllexport) int _DGTDLL_GetWxWidgetsVersion() {
		auto call = [](grpc::ClientContext& context, dgt::Empty& request, dgt::IntResponse& response) {
			return g_stub->GetWxWidgetsVersion(&context, request, &response);
		};

		return PerformGrpcCall<dgt::Empty>(call);
	}

	__declspec(dllexport) int _DGTDLL_Init() {
		auto call = [](grpc::ClientContext& context, dgt::Empty& request, dgt::IntResponse& response) {
			return g_stub->Init(&context, request, &response);
		};

		return PerformGrpcCall<dgt::Empty>(call);
	}

	__declspec(dllexport) int _DGTDLL_Exit() {
		auto call = [](grpc::ClientContext& context, dgt::Empty& request, dgt::IntResponse& response) {
			return g_stub->Exit(&context, request, &response);
		};

		return PerformGrpcCall<dgt::Empty>(call);
	}

	__declspec(dllexport) int _DGTDLL_ShowDialog(int val) {
		dgt::IntRequest request;
		request.set_value(val);

		auto call = [](grpc::ClientContext& context, const dgt::IntRequest& request, dgt::IntResponse& response) {
			return g_stub->ShowDialog(&context, request, &response);
		};

		return PerformGrpcCall<dgt::IntRequest>(call);
	}

	__declspec(dllexport) int _DGTDLL_HideDialog(int val) {
		dgt::IntRequest request;
		request.set_value(val);

		auto call = [](grpc::ClientContext& context, const dgt::IntRequest& request, dgt::IntResponse& response) {
			return g_stub->HideDialog(&context, request, &response);
		};

		return PerformGrpcCall<dgt::IntRequest>(call);
	}

	__declspec(dllexport) int _DGTDLL_WriteCOMPort(int val) {
		dgt::IntRequest request;
		request.set_value(val);

		auto call = [](grpc::ClientContext& context, const dgt::IntRequest& request, dgt::IntResponse& response) {
			return g_stub->WriteCOMPort(&context, request, &response);
		};

		return PerformGrpcCall<dgt::IntRequest>(call);
	}

	__declspec(dllexport) int _DGTDLL_WriteCOMPortString(const char* val) {
		dgt::StringRequest request;
		request.set_value(val);

		auto call = [](grpc::ClientContext& context, const dgt::StringRequest& request, dgt::IntResponse& response) {
			return g_stub->WriteCOMPortString(&context, request, &response);
		};

		return PerformGrpcCall<dgt::StringRequest>(call);
	}

	__declspec(dllexport) int _DGTDLL_WritePosition(const char* val) {
		dgt::StringRequest request;
		request.set_value(val);

		auto call = [](grpc::ClientContext& context, const dgt::StringRequest& request, dgt::IntResponse& response) {
			return g_stub->WritePosition(&context, request, &response);
		};

		return PerformGrpcCall<dgt::StringRequest>(call);
	}

	__declspec(dllexport) int _DGTDLL_PlayWhiteMove(const char* val) {
		dgt::StringRequest request;
		request.set_value(val);

		auto call = [](grpc::ClientContext& context, const dgt::StringRequest& request, dgt::IntResponse& response) {
			return g_stub->PlayWhiteMove(&context, request, &response);
		};

		return PerformGrpcCall<dgt::StringRequest>(call);
	}

	__declspec(dllexport) int _DGTDLL_PlayBlackMove(const char* val) {
		dgt::StringRequest request;
		request.set_value(val);

		auto call = [](grpc::ClientContext& context, const dgt::StringRequest& request, dgt::IntResponse& response) {
			return g_stub->PlayBlackMove(&context, request, &response);
		};

		return PerformGrpcCall<dgt::StringRequest>(call);
	}

	__declspec(dllexport) int _DGTDLL_WriteDebug(bool val) {
		dgt::BoolRequest request;
		request.set_value(val);

		auto call = [](grpc::ClientContext& context, const dgt::BoolRequest& request, dgt::IntResponse& response) {
			return g_stub->WriteDebug(&context, request, &response);
		};

		return PerformGrpcCall<dgt::BoolRequest>(call);
	}

	__declspec(dllexport) int _DGTDLL_DisplayClockMessage(const char* val1, int val2) {
		dgt::ClockMessageRequest request;
		request.set_message(val1);
		request.set_time(val2);

		auto call = [](grpc::ClientContext& context, const dgt::ClockMessageRequest& request, dgt::IntResponse& response) {
			return g_stub->DisplayClockMessage(&context, request, &response);
		};

		return PerformGrpcCall<dgt::ClockMessageRequest>(call);
	}

	__declspec(dllexport) int _DGTDLL_EndDisplay(int val) {
		dgt::IntRequest request;
		request.set_value(val);

		auto call = [](grpc::ClientContext& context, const dgt::IntRequest& request, dgt::IntResponse& response) {
			return g_stub->EndDisplay(&context, request, &response);
		};

		return PerformGrpcCall<dgt::IntRequest>(call);
	}

	__declspec(dllexport) int _DGTDLL_SetNRun(const char* val1, const char* val2, int val3) {
		dgt::SetNRunRequest request;
		request.set_param1(val1);
		request.set_param2(val2);
		request.set_time(val3);

		auto call = [](grpc::ClientContext& context, const dgt::SetNRunRequest& request, dgt::IntResponse& response) {
			return g_stub->SetNRun(&context, request, &response);
		};

		return PerformGrpcCall<dgt::SetNRunRequest>(call);
	}

	__declspec(dllexport) int _DGTDLL_ClockMode(int val) {
		dgt::IntRequest request;
		request.set_value(val);

		auto call = [](grpc::ClientContext& context, const dgt::IntRequest& request, dgt::IntResponse& response) {
			return g_stub->ClockMode(&context, request, &response);
		};

		return PerformGrpcCall<dgt::IntRequest>(call);
	}

	__declspec(dllexport) int _DGTDLL_SetAutoRotation(bool val) {
		dgt::BoolRequest request;
		request.set_value(val);

		auto call = [](grpc::ClientContext& context, const dgt::BoolRequest& request, dgt::IntResponse& response) {
			return g_stub->SetAutoRotation(&context, request, &response);
		};

		return PerformGrpcCall<dgt::BoolRequest>(call);
	}

	__declspec(dllexport) int _DGTDLL_UseFEN(bool val) {
		dgt::BoolRequest request;
		request.set_value(val);

		auto call = [](grpc::ClientContext& context, const dgt::BoolRequest& request, dgt::IntResponse& response) {
			return g_stub->UseFEN(&context, request, &response);
		};

		return PerformGrpcCall<dgt::BoolRequest>(call);
	}

	__declspec(dllexport) int _DGTDLL_UseSAN(bool val) {
		dgt::BoolRequest request;
		request.set_value(val);

		auto call = [](grpc::ClientContext& context, const dgt::BoolRequest& request, dgt::IntResponse& response) {
			return g_stub->UseSAN(&context, request, &response);
		};

		return PerformGrpcCall<dgt::BoolRequest>(call);
	}

	__declspec(dllexport) int _DGTDLL_SetGameType(int val) {
		dgt::IntRequest request;
		request.set_value(val);

		auto call = [](grpc::ClientContext& context, const dgt::IntRequest& request, dgt::IntResponse& response) {
			return g_stub->SetGameType(&context, request, &response);
		};

		return PerformGrpcCall<dgt::IntRequest>(call);
	}

	__declspec(dllexport) int _DGTDLL_AllowTakebacks(bool val) {
		dgt::BoolRequest request;
		request.set_value(val);

		auto call = [](grpc::ClientContext& context, const dgt::BoolRequest& request, dgt::IntResponse& response) {
			return g_stub->AllowTakebacks(&context, request, &response);
		};

		return PerformGrpcCall<dgt::BoolRequest>(call);
	}


	//Callback methods below...
	__declspec(dllexport) int _DGTDLL_RegisterStatusFunc(FC*) { return 0; }
	__declspec(dllexport) int _DGTDLL_RegisterScanFunc(FC*) { return 0; }
	__declspec(dllexport) int _DGTDLL_RegisterStableBoardFunc(FC*) { return 0; }
	__declspec(dllexport) int _DGTDLL_RegisterWClockFunc(FC*) { return 0; }
	__declspec(dllexport) int _DGTDLL_RegisterBClockFunc(FC*) { return 0; }
	__declspec(dllexport) int _DGTDLL_RegisterResultFunc(FC*) { return 0; }
	__declspec(dllexport) int _DGTDLL_RegisterNewGameFunc(FC*) { return 0; }
	__declspec(dllexport) int _DGTDLL_RegisterWhiteMoveInputFunc(FC*) { return 0; }
	__declspec(dllexport) int _DGTDLL_RegisterBlackMoveInputFunc(FC*) { return 0; }
	__declspec(dllexport) int _DGTDLL_RegisterWhiteTakebackFunc(F*) { return 0; }
	__declspec(dllexport) int _DGTDLL_RegisterBlackTakebackFunc(F*) { return 0; }
	__declspec(dllexport) int _DGTDLL_RegisterWhiteMoveNowFunc(FC*) { return 0; }
	__declspec(dllexport) int _DGTDLL_RegisterBlackMoveNowFunc(FC*) { return 0; }
	__declspec(dllexport) int _DGTDLL_RegisterStartSetupFunc(FC*) { return 0; }
	__declspec(dllexport) int _DGTDLL_RegisterStopSetupWTMFunc(FC*) { return 0; }
	__declspec(dllexport) int _DGTDLL_RegisterStopSetupBTMFunc(FC*) { return 0; }
	__declspec(dllexport) int _DGTDLL_RegisterGameTypeChangedFunc(FI*) { return 0; }
	__declspec(dllexport) int _DGTDLL_RegisterAllowTakebacksChangedFunc(FB*) { return 0; }
	__declspec(dllexport) int _DGTDLL_RegisterMagicPieceFunc(FIIC*) { return 0; }
}
