// dllmain.cpp : Defines the entry point for the DLL application.
#include "pch.h"
#include <memory>
#include <grpcpp/grpcpp.h>
#include <google/protobuf/empty.pb.h>
#include "dgtdll.pb.h"
#include "dgtdll.grpc.pb.h"
#include <fstream> // Include for file I/O
#include <cstdlib> // Include for getenv()
#include <windows.h>
#include <iostream>
#include <chrono>
#include <ctime>
#include <iomanip>
#include <sstream>

typedef int __stdcall FC(const char*);
typedef int __stdcall FI(int);
typedef int __stdcall FB(bool);
typedef int __stdcall F();
typedef int __stdcall FIIC(int, int, const char*);


struct CallbackInfo {
	std::string grpc_event_name;
	void* func;
};

std::vector<CallbackInfo> g_callbacks;
std::mutex g_callback_mutex;



// Global variables for the channel and stub
const char* CHERUB_GRPC_LISTEN_PORT = "localhost:37965";

std::shared_ptr<grpc::Channel> g_channel;
std::unique_ptr<dgt::DGTDLL::Stub> g_stub;

// Declare a mutex to synchronize access to g_log_file
std::mutex g_log_mutex;

static bool isInitComplete = false;

void BlockUntilInitComplete() {
	//while (!isInitComplete) {
	//	Sleep(100);
	//}
}


// Global variables for logging
std::ofstream g_log_file;
const char* LOG_ENV_VAR = "DGTDLL_LOG_PATH";
bool g_logging_enabled = false;



// Function to log messages to the file
void LogMessage(const std::string& message) {
	if (g_logging_enabled) {
		// Lock the mutex to ensure exclusive access to g_log_file
		std::lock_guard<std::mutex> lock(g_log_mutex);
		g_log_file << message + "\n" << std::flush;
	}
}

// Function to initialize the log file
void InitializeLogFile() {
	char* log_path = nullptr;
	std::size_t len;
	if (_dupenv_s(&log_path, &len, LOG_ENV_VAR) == 0 && log_path != nullptr) {
		g_log_file.open(log_path, std::ios::app);
		g_logging_enabled = g_log_file.is_open();
		std::free(log_path);
	}

	std::time_t now = std::chrono::system_clock::to_time_t(std::chrono::system_clock::now());

	struct std::tm timeinfo;
	localtime_s(&timeinfo, &now);

	char buffer[80];
	std::strftime(buffer, 80, "%Y-%m-%d %H:%M:%S", &timeinfo);
	std::string datetime_str(buffer);

	LogMessage("------------------------------------");
	LogMessage("ANGEL NATIVE - [" + datetime_str + "]");
	LogMessage("------------------------------------");
}

void ReconnectChannelIfNecessary() {
	//LogMessage("Called " + std::string(__func__));

	if (g_channel->GetState(true) == GRPC_CHANNEL_READY) {
		g_channel = grpc::CreateChannel(CHERUB_GRPC_LISTEN_PORT, grpc::InsecureChannelCredentials());
		g_stub = dgt::DGTDLL::NewStub(g_channel);
	}
}

template <typename RequestType>
int PerformGrpcCall(const std::function<grpc::Status(grpc::ClientContext&, RequestType&, dgt::IntResponse&)>& call) {
	LogMessage(">>>>Called " + std::string(__func__));

	try {
		ReconnectChannelIfNecessary();
		RequestType request;
		dgt::IntResponse response;
		grpc::ClientContext context;
		call(context, request, response);
		return response.value();
	}
	catch (const std::exception& e) {
		LogMessage("  Exception: " + std::string(e.what())); // Log exception message
		return 1;
	}
}

void CallRegisteredCallbacks(const std::string& grpc_event_name, const google::protobuf::Any& event_data) {
	std::lock_guard<std::mutex> lock(g_callback_mutex);

	for (const auto& callback_info : g_callbacks) {
		if (callback_info.grpc_event_name == grpc_event_name) {
			// Call the appropriate function pointer based on the event data type
			if (event_data.Is<dgt::StringResponse>()) {
				dgt::StringResponse string_response;
				event_data.UnpackTo(&string_response);
				reinterpret_cast<FC*>(callback_info.func)(string_response.value().c_str());
			}
			else if (event_data.Is<dgt::IntResponse>()) {
				dgt::IntResponse int_response;
				event_data.UnpackTo(&int_response);
				reinterpret_cast<FI*>(callback_info.func)(int_response.value());
			}
			else if (event_data.Is<dgt::BoolResponse>()) {
				dgt::BoolResponse bool_response;
				event_data.UnpackTo(&bool_response);
				reinterpret_cast<FB*>(callback_info.func)(bool_response.value());
			}
			else if (event_data.Is<dgt::Empty>()) {
				reinterpret_cast<F*>(callback_info.func)();
			}
			else if (event_data.Is<dgt::CallbackIICResponse>()) {
				dgt::CallbackIICResponse iic_response;
				event_data.UnpackTo(&iic_response);
				reinterpret_cast<FIIC*>(callback_info.func)(iic_response.param1(), iic_response.param2(), iic_response.param3().c_str());
			}
		}
	}
}

void DllInitThread()
{
	// Initialize the channel and stub
	//g_channel = grpc::CreateChannel(CHERUB_GRPC_LISTEN_PORT, grpc::InsecureChannelCredentials());
	//g_stub = dgt::DGTDLL::NewStub(g_channel);

	// Initialize the log file
	InitializeLogFile();
	LogMessage(std::string(__func__) + " COMPLETE...");
	isInitComplete = true;
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
		g_channel = grpc::CreateChannel(CHERUB_GRPC_LISTEN_PORT, grpc::InsecureChannelCredentials());
		g_stub = dgt::DGTDLL::NewStub(g_channel);

		// Start a separate thread to initialize the DLL
		std::thread(DllInitThread).detach();
		BlockUntilInitComplete();

		LogMessage(">>STARTED   :: DLL_PROCESS_ATTACH"); // Log the event
		LogMessage(">>COMPLETED :: DLL_PROCESS_ATTACH"); // Log the event
		break;
	case DLL_THREAD_ATTACH:
		LogMessage(">>DLL_THREAD_ATTACH"); // Log the event
		// Initialize the critical section
		break;
	case DLL_THREAD_DETACH:
		LogMessage(">>DLL_THREAD_DETACH"); // Log the event
		// Delete the critical section
		break;
	case DLL_PROCESS_DETACH:
		LogMessage(">>DLL_PROCESS_DETACH"); // Log the event

		if (lpReserved == NULL) {
			// This means the DLL is being unloaded due to the process exiting
			LogMessage(">>>>DLL unloaded due to process exit");
		}
		else {
			// This means the DLL is being unloaded due to FreeLibrary being called
			LogMessage(">>>>DLL unloaded due to FreeLibrary");
		}

		if (g_logging_enabled) {
			g_log_file.close();
		}

		break;
	}
	return TRUE;
}

extern "C" {
	__declspec(dllexport) int __stdcall _DGTDLL_GetVersion() {
		BlockUntilInitComplete();
		LogMessage(">>Called " + std::string(__func__));

		auto call = [](grpc::ClientContext& context, dgt::Empty& request, dgt::IntResponse& response) {
			return g_stub->GetVersion(&context, request, &response);
		};

		return PerformGrpcCall<dgt::Empty>(call);
	}

	__declspec(dllexport) int __stdcall _DGTDLL_GetWxWidgetsVersion() {
		BlockUntilInitComplete();
		LogMessage(">>Called " + std::string(__func__));

		auto call = [](grpc::ClientContext& context, dgt::Empty& request, dgt::IntResponse& response) {
			return g_stub->GetWxWidgetsVersion(&context, request, &response);
		};

		return PerformGrpcCall<dgt::Empty>(call);
	}

	__declspec(dllexport) int __stdcall __stdcall _DGTDLL_Init() {
		BlockUntilInitComplete();
		LogMessage(">>Called " + std::string(__func__));

		auto call = [](grpc::ClientContext& context, dgt::Empty& request, dgt::IntResponse& response) {
			return g_stub->Init(&context, request, &response);
		};

		return PerformGrpcCall<dgt::Empty>(call);
	}

	__declspec(dllexport) int __stdcall _DGTDLL_Exit() {
		BlockUntilInitComplete();
		LogMessage(">>Called " + std::string(__func__));

		auto call = [](grpc::ClientContext& context, dgt::Empty& request, dgt::IntResponse& response) {
			return g_stub->Exit(&context, request, &response);
		};

		return PerformGrpcCall<dgt::Empty>(call);
	}

	__declspec(dllexport) int __stdcall _DGTDLL_ShowDialog(int val) {
		BlockUntilInitComplete();
		LogMessage(">>Called " + std::string(__func__));

		dgt::IntRequest request;
		request.set_value(val);

		auto call = [](grpc::ClientContext& context, const dgt::IntRequest& request, dgt::IntResponse& response) {
			return g_stub->ShowDialog(&context, request, &response);
		};

		return PerformGrpcCall<dgt::IntRequest>(call);
	}

	__declspec(dllexport) int __stdcall _DGTDLL_HideDialog(int val) {
		BlockUntilInitComplete();
		LogMessage(">>Called " + std::string(__func__));

		dgt::IntRequest request;
		request.set_value(val);

		auto call = [](grpc::ClientContext& context, const dgt::IntRequest& request, dgt::IntResponse& response) {
			return g_stub->HideDialog(&context, request, &response);
		};

		return PerformGrpcCall<dgt::IntRequest>(call);
	}

	__declspec(dllexport) int __stdcall _DGTDLL_WriteCOMPort(int val) {
		BlockUntilInitComplete();
		LogMessage(">>Called " + std::string(__func__));

		dgt::IntRequest request;
		request.set_value(val);

		auto call = [](grpc::ClientContext& context, const dgt::IntRequest& request, dgt::IntResponse& response) {
			return g_stub->WriteCOMPort(&context, request, &response);
		};

		return PerformGrpcCall<dgt::IntRequest>(call);
	}

	__declspec(dllexport) int __stdcall _DGTDLL_WriteCOMPortString(const char* val) {
		BlockUntilInitComplete();
		LogMessage(">>Called " + std::string(__func__));

		dgt::StringRequest request;
		request.set_value(val);

		auto call = [](grpc::ClientContext& context, const dgt::StringRequest& request, dgt::IntResponse& response) {
			return g_stub->WriteCOMPortString(&context, request, &response);
		};

		return PerformGrpcCall<dgt::StringRequest>(call);
	}

	__declspec(dllexport) int __stdcall _DGTDLL_WritePosition(const char* val) {
		BlockUntilInitComplete();
		LogMessage(">>Called " + std::string(__func__));

		dgt::StringRequest request;
		request.set_value(val);

		auto call = [](grpc::ClientContext& context, const dgt::StringRequest& request, dgt::IntResponse& response) {
			return g_stub->WritePosition(&context, request, &response);
		};

		return PerformGrpcCall<dgt::StringRequest>(call);
	}

	__declspec(dllexport) int __stdcall _DGTDLL_PlayWhiteMove(const char* val) {
		BlockUntilInitComplete();
		LogMessage(">>Called " + std::string(__func__));

		dgt::StringRequest request;
		request.set_value(val);

		auto call = [](grpc::ClientContext& context, const dgt::StringRequest& request, dgt::IntResponse& response) {
			return g_stub->PlayWhiteMove(&context, request, &response);
		};

		return PerformGrpcCall<dgt::StringRequest>(call);
	}

	__declspec(dllexport) int __stdcall _DGTDLL_PlayBlackMove(const char* val) {
		BlockUntilInitComplete();
		LogMessage(">>Called " + std::string(__func__));

		dgt::StringRequest request;
		request.set_value(val);

		auto call = [](grpc::ClientContext& context, const dgt::StringRequest& request, dgt::IntResponse& response) {
			return g_stub->PlayBlackMove(&context, request, &response);
		};

		return PerformGrpcCall<dgt::StringRequest>(call);
	}

	__declspec(dllexport) int __stdcall _DGTDLL_WriteDebug(bool val) {
		BlockUntilInitComplete();
		LogMessage(">>Called " + std::string(__func__));

		dgt::BoolRequest request;
		request.set_value(val);

		auto call = [](grpc::ClientContext& context, const dgt::BoolRequest& request, dgt::IntResponse& response) {
			return g_stub->WriteDebug(&context, request, &response);
		};

		return PerformGrpcCall<dgt::BoolRequest>(call);
	}

	__declspec(dllexport) int __stdcall _DGTDLL_DisplayClockMessage(const char* val1, int val2) {
		BlockUntilInitComplete();
		LogMessage(">>Called " + std::string(__func__));

		dgt::ClockMessageRequest request;
		request.set_message(val1);
		request.set_time(val2);

		auto call = [](grpc::ClientContext& context, const dgt::ClockMessageRequest& request, dgt::IntResponse& response) {
			return g_stub->DisplayClockMessage(&context, request, &response);
		};

		return PerformGrpcCall<dgt::ClockMessageRequest>(call);
	}

	__declspec(dllexport) int __stdcall _DGTDLL_EndDisplay(int val) {
		BlockUntilInitComplete();
		LogMessage(">>Called " + std::string(__func__));

		dgt::IntRequest request;
		request.set_value(val);

		auto call = [](grpc::ClientContext& context, const dgt::IntRequest& request, dgt::IntResponse& response) {
			return g_stub->EndDisplay(&context, request, &response);
		};

		return PerformGrpcCall<dgt::IntRequest>(call);
	}

	__declspec(dllexport) int __stdcall _DGTDLL_SetNRun(const char* val1, const char* val2, int val3) {
		BlockUntilInitComplete();
		LogMessage(">>Called " + std::string(__func__));

		dgt::SetNRunRequest request;
		request.set_param1(val1);
		request.set_param2(val2);
		request.set_time(val3);

		auto call = [](grpc::ClientContext& context, const dgt::SetNRunRequest& request, dgt::IntResponse& response) {
			return g_stub->SetNRun(&context, request, &response);
		};

		return PerformGrpcCall<dgt::SetNRunRequest>(call);
	}

	__declspec(dllexport) int __stdcall _DGTDLL_ClockMode(int val) {
		BlockUntilInitComplete();
		LogMessage(">>Called " + std::string(__func__));

		dgt::IntRequest request;
		request.set_value(val);

		auto call = [](grpc::ClientContext& context, const dgt::IntRequest& request, dgt::IntResponse& response) {
			return g_stub->ClockMode(&context, request, &response);
		};

		return PerformGrpcCall<dgt::IntRequest>(call);
	}

	__declspec(dllexport) int __stdcall _DGTDLL_SetAutoRotation(bool val) {
		BlockUntilInitComplete();
		LogMessage(">>Called " + std::string(__func__));

		dgt::BoolRequest request;
		request.set_value(val);

		auto call = [](grpc::ClientContext& context, const dgt::BoolRequest& request, dgt::IntResponse& response) {
			return g_stub->SetAutoRotation(&context, request, &response);
		};

		return PerformGrpcCall<dgt::BoolRequest>(call);
	}

	__declspec(dllexport) int __stdcall _DGTDLL_UseFEN(bool val) {
		BlockUntilInitComplete();
		LogMessage(">>Called " + std::string(__func__));

		dgt::BoolRequest request;
		request.set_value(val);

		auto call = [](grpc::ClientContext& context, const dgt::BoolRequest& request, dgt::IntResponse& response) {
			return g_stub->UseFEN(&context, request, &response);
		};

		return PerformGrpcCall<dgt::BoolRequest>(call);
	}

	__declspec(dllexport) int __stdcall _DGTDLL_UseSAN(bool val) {
		BlockUntilInitComplete();
		LogMessage(">>Called " + std::string(__func__));

		dgt::BoolRequest request;
		request.set_value(val);

		auto call = [](grpc::ClientContext& context, const dgt::BoolRequest& request, dgt::IntResponse& response) {
			return g_stub->UseSAN(&context, request, &response);
		};

		return PerformGrpcCall<dgt::BoolRequest>(call);
	}

	__declspec(dllexport) int __stdcall _DGTDLL_SetGameType(int val) {
		BlockUntilInitComplete();
		LogMessage(">>Called " + std::string(__func__));

		dgt::IntRequest request;
		request.set_value(val);

		auto call = [](grpc::ClientContext& context, const dgt::IntRequest& request, dgt::IntResponse& response) {
			return g_stub->SetGameType(&context, request, &response);
		};

		return PerformGrpcCall<dgt::IntRequest>(call);
	}

	__declspec(dllexport) int __stdcall _DGTDLL_AllowTakebacks(bool val) {
		BlockUntilInitComplete();
		LogMessage(">>Called " + std::string(__func__));

		dgt::BoolRequest request;
		request.set_value(val);

		auto call = [](grpc::ClientContext& context, const dgt::BoolRequest& request, dgt::IntResponse& response) {
			return g_stub->AllowTakebacks(&context, request, &response);
		};

		return PerformGrpcCall<dgt::BoolRequest>(call);
	}



	__declspec(dllexport) int __stdcall _DGTDLL_RegisterStableBoardFunc(FC* func) {
		LogMessage("Called " + std::string(__func__));

		std::thread t([func]() {

			static FC* s_func = nullptr;
			s_func = func;

			//std::lock_guard<std::mutex> lock(g_callback_mutex);
			//g_callbacks.push_back({ "StableBoard", reinterpret_cast<void*>(func) });

			ReconnectChannelIfNecessary();

			// Create an empty request message
			dgt::Empty empty_request;

			// Create a stream context to receive the response stream
			grpc::ClientContext context;
			std::unique_ptr<grpc::ClientReader<dgt::StringResponse>> reader = g_stub->RegisterStableBoardFunc(&context, empty_request);

			// Loop through the stream to read all the responses
			dgt::StringResponse response;

			while (true) {
				if (reader->Read(&response)) {
					const char* responseChars = response.value().c_str();

					LogMessage("Received response: " + response.value());
					int callbackResponse = s_func(responseChars);
				}
				Sleep(100);
			}
			});

		t.detach();


		//while (true) {
		//	Sleep(1000);
		//}


		return 0;
	}

	__declspec(dllexport) int __stdcall _DGTDLL_RegisterStatusFunc(FC*) { LogMessage("Called " + std::string(__func__)); return 1; }
	__declspec(dllexport) int __stdcall _DGTDLL_RegisterScanFunc(FC*) { LogMessage("Called " + std::string(__func__)); return 1; }
	__declspec(dllexport) int __stdcall _DGTDLL_RegisterWClockFunc(FC*) { LogMessage("Called " + std::string(__func__)); return 1; }
	__declspec(dllexport) int __stdcall _DGTDLL_RegisterBClockFunc(FC*) { LogMessage("Called " + std::string(__func__)); return 1; }
	__declspec(dllexport) int __stdcall _DGTDLL_RegisterResultFunc(FC*) { LogMessage("Called " + std::string(__func__)); return 1; }
	__declspec(dllexport) int __stdcall _DGTDLL_RegisterNewGameFunc(FC*) { LogMessage("Called " + std::string(__func__)); return 1; }
	__declspec(dllexport) int __stdcall _DGTDLL_RegisterWhiteMoveInputFunc(FC*) { LogMessage("Called " + std::string(__func__)); return 1; }
	__declspec(dllexport) int __stdcall _DGTDLL_RegisterBlackMoveInputFunc(FC*) { LogMessage("Called " + std::string(__func__)); return 1; }
	__declspec(dllexport) int __stdcall _DGTDLL_RegisterWhiteTakebackFunc(F*) { LogMessage("Called " + std::string(__func__)); return 1; }
	__declspec(dllexport) int __stdcall _DGTDLL_RegisterBlackTakebackFunc(F*) { LogMessage("Called " + std::string(__func__)); return 1; }
	__declspec(dllexport) int __stdcall _DGTDLL_RegisterWhiteMoveNowFunc(FC*) { LogMessage("Called " + std::string(__func__)); return 1; }
	__declspec(dllexport) int __stdcall _DGTDLL_RegisterBlackMoveNowFunc(FC*) { LogMessage("Called " + std::string(__func__)); return 1; }
	__declspec(dllexport) int __stdcall _DGTDLL_RegisterStartSetupFunc(FC*) { LogMessage("Called " + std::string(__func__));  return 1; }
	__declspec(dllexport) int __stdcall _DGTDLL_RegisterStopSetupWTMFunc(FC*) { LogMessage("Called " + std::string(__func__)); return 1; }
	__declspec(dllexport) int __stdcall _DGTDLL_RegisterStopSetupBTMFunc(FC*) { LogMessage("Called " + std::string(__func__)); return 1; }
	__declspec(dllexport) int __stdcall _DGTDLL_RegisterGameTypeChangedFunc(FI*) { LogMessage("Called " + std::string(__func__)); return 1; }
	__declspec(dllexport) int __stdcall _DGTDLL_RegisterAllowTakebacksChangedFunc(FB*) { LogMessage("Called " + std::string(__func__)); return 1; }
	__declspec(dllexport) int __stdcall _DGTDLL_RegisterMagicPieceFunc(FIIC*) { LogMessage("Called " + std::string(__func__)); return 1; }
}


