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

int version_major = 2;
int version_minor = 0;
int version_release = 7;



BOOL APIENTRY DllMain(HMODULE hModule,
	DWORD  ul_reason_for_call,
	LPVOID lpReserved
)
{
	switch (ul_reason_for_call)
	{
	case DLL_PROCESS_ATTACH:
	case DLL_THREAD_ATTACH:
	case DLL_THREAD_DETACH:
	case DLL_PROCESS_DETACH:
		break;
	}
	return TRUE;
}


extern "C" {
	__declspec(dllexport) int _DGTDLL_GetVersion() { 
		
		// create a channel to a gRPC server
		std::shared_ptr<grpc::Channel> channel =
			grpc::CreateChannel("localhost:5105", grpc::InsecureChannelCredentials());

		// Create a stub for the service.
		std::unique_ptr<dgt::DGTDLL::Stub> stub = dgt::DGTDLL::NewStub(channel);
		
		// Create an Empty request message.
		dgt::Empty request;

		// Create a context for the RPC.
		grpc::ClientContext context;

		// Call the GetVersion RPC method.
		dgt::IntResponse response;

		grpc::Status status = stub->GetVersion(&context, request, &response);

		// Check if the call was successful.
		if (status.ok()) {
			// Use the response message.
			//std::cout << "Version: " << response.value() << std::endl;
			//return response.value();
			return 0;
		}
		else {
			// Handle the error.
			//std::cerr << "RPC failed: " << status.error_message() << std::endl;
			return 1;
		}


		//return (version_major * 10000) + (version_minor * 100) + version_release; 
	}
	__declspec(dllexport) int _DGTDLL_GetWxWidgetsVersion() { return 0; }
	__declspec(dllexport) int _DGTDLL_Init() { return 0; }
	__declspec(dllexport) int _DGTDLL_Exit() { return 0; }
	__declspec(dllexport) int _DGTDLL_ShowDialog(int) { return 0; }
	__declspec(dllexport) int _DGTDLL_HideDialog(int) { return 0; }
	__declspec(dllexport) int _DGTDLL_WriteCOMPort(int) { return 0; }
	__declspec(dllexport) int _DGTDLL_WriteCOMPortString(const char*) { return 0; }
	__declspec(dllexport) int _DGTDLL_WritePosition(const char*) { return 0; }
	__declspec(dllexport) int _DGTDLL_PlayWhiteMove(const char*) { return 0; }
	__declspec(dllexport) int _DGTDLL_PlayBlackMove(const char*) { return 0; }
	__declspec(dllexport) int _DGTDLL_WriteDebug(bool) { return 0; }
	__declspec(dllexport) int _DGTDLL_DisplayClockMessage(const char*, int) { return 0; }
	__declspec(dllexport) int _DGTDLL_EndDisplay(int) { return 0; }
	__declspec(dllexport) int _DGTDLL_SetNRun(const char*, const char*, int) { return 0; }
	__declspec(dllexport) int _DGTDLL_ClockMode(int) { return 0; }
	__declspec(dllexport) int _DGTDLL_SetAutoRotation(bool) { return 0; }
	__declspec(dllexport) int _DGTDLL_UseFEN(bool) { return 0; }
	__declspec(dllexport) int _DGTDLL_UseSAN(bool) { return 0; }
	__declspec(dllexport) int _DGTDLL_SetGameType(int) { return 0; }
	__declspec(dllexport) int _DGTDLL_AllowTakebacks(bool) { return 0; }
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
