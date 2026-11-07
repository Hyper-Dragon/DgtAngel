// dllmain.cpp : Defines the entry point for the DLL application.
#include "pch.h"
#include <memory>
#include <mutex>
#include <thread>
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


// -----------------------------------------------------------------------------
// Logging - enabled by setting DGTDLL_LOG_PATH to a file path.
//
// Deliberately lazy: DllMain runs under the loader lock, so nothing in this
// file opens a file, starts a thread or touches gRPC until the host actually
// calls an exported function.
// -----------------------------------------------------------------------------
const char* LOG_ENV_VAR = "DGTDLL_LOG_PATH";

std::mutex g_log_mutex;
std::ofstream g_log_file;
bool g_logging_enabled = false;
std::once_flag g_log_init_flag;

void WriteLogLine(const std::string& message) {
	std::lock_guard<std::mutex> lock(g_log_mutex);
	if (g_logging_enabled) {
		g_log_file << message << "\n" << std::flush;
	}
}

void InitializeLogFile() {
	char* log_path = nullptr;
	std::size_t len = 0;
	if (_dupenv_s(&log_path, &len, LOG_ENV_VAR) == 0 && log_path != nullptr) {
		g_log_file.open(log_path, std::ios::app);
		g_logging_enabled = g_log_file.is_open();
		std::free(log_path);
	}

	if (!g_logging_enabled) {
		return;
	}

	std::time_t now = std::chrono::system_clock::to_time_t(std::chrono::system_clock::now());

	struct std::tm timeinfo {};
	localtime_s(&timeinfo, &now);

	char buffer[80];
	std::strftime(buffer, sizeof(buffer), "%Y-%m-%d %H:%M:%S", &timeinfo);

	WriteLogLine("------------------------------------");
	WriteLogLine("ANGEL NATIVE - [" + std::string(buffer) + "]");
	WriteLogLine("------------------------------------");
}

void LogMessage(const std::string& message) {
	std::call_once(g_log_init_flag, InitializeLogFile);
	WriteLogLine(message);
}

// Kept for source compatibility with the original entry points.  There is no
// background initialisation thread any more; the channel is created on the
// first call that needs it.
void BlockUntilInitComplete() {
}

// -----------------------------------------------------------------------------
// gRPC plumbing
// -----------------------------------------------------------------------------
const char* CHERUB_GRPC_LISTEN_PORT = "localhost:37965";
const int GRPC_CALL_TIMEOUT_SECONDS = 5;

std::mutex g_stub_mutex;						// guards g_channel and g_stub
std::shared_ptr<grpc::Channel> g_channel;
std::shared_ptr<dgt::DGTDLL::Stub> g_stub;

// Caller must hold g_stub_mutex.
void EnsureStubLocked() {
	if (!g_channel || g_channel->GetState(false) == GRPC_CHANNEL_SHUTDOWN) {
		g_channel = grpc::CreateChannel(CHERUB_GRPC_LISTEN_PORT, grpc::InsecureChannelCredentials());
		g_stub = dgt::DGTDLL::NewStub(g_channel);
		LogMessage("gRPC channel (re)created for " + std::string(CHERUB_GRPC_LISTEN_PORT));
	}
}

std::shared_ptr<dgt::DGTDLL::Stub> GetStubSnapshot() {
	std::lock_guard<std::mutex> lock(g_stub_mutex);
	EnsureStubLocked();
	return g_stub;
}

// -----------------------------------------------------------------------------
// Stable board callback
//
// The real dgtebdll.dll fires its callbacks from the host's own event loop
// (see "Event loop" in the DGT DLL documentation), so the host's handler is
// thread affine.  Calling it straight off the gRPC reader thread is what
// produced the access violations when this DLL was loaded into Arena.
//
// We reproduce the driver's behaviour: the position is posted to a
// message-only window created on the thread that called
// _DGTDLL_RegisterStableBoardFunc.  The host's message pump dispatches it and
// the callback runs on the host's thread, with a string that is valid for the
// duration of the call.
// -----------------------------------------------------------------------------
const wchar_t* CALLBACK_WINDOW_CLASS = L"DgtAngelNative.CallbackWindow";
const UINT WM_DGT_STABLE_BOARD = WM_APP + 0x0010;

std::mutex g_callback_mutex;
FC* g_callback_func = nullptr;
HWND g_callback_window = nullptr;
DWORD g_callback_thread_id = 0;
std::string g_pending_fen;
bool g_callback_post_queued = false;

void InvokeStableBoardCallback(const std::string& fen) {
	FC* func = nullptr;
	{
		std::lock_guard<std::mutex> lock(g_callback_mutex);
		func = g_callback_func;
	}

	if (func == nullptr) {
		return;
	}

	try {
		func(fen.c_str());
	}
	catch (...) {
		LogMessage("Exception in stable board callback");
	}
}

LRESULT CALLBACK CallbackWindowProc(HWND hwnd, UINT message, WPARAM wParam, LPARAM lParam) {
	if (message == WM_DGT_STABLE_BOARD) {
		std::string fen;
		{
			std::lock_guard<std::mutex> lock(g_callback_mutex);
			g_callback_post_queued = false;
			fen.swap(g_pending_fen);
		}

		if (!fen.empty()) {
			InvokeStableBoardCallback(fen);
		}

		return 0;
	}

	return DefWindowProcW(hwnd, message, wParam, lParam);
}

// Must be called from the thread that will run the callback.
// Caller must hold g_callback_mutex.
HWND GetOrCreateCallbackWindowLocked() {
	if (g_callback_window != nullptr && IsWindow(g_callback_window)) {
		return g_callback_window;
	}

	HMODULE module = nullptr;
	if (!GetModuleHandleExW(GET_MODULE_HANDLE_EX_FLAG_FROM_ADDRESS | GET_MODULE_HANDLE_EX_FLAG_UNCHANGED_REFCOUNT,
		reinterpret_cast<LPCWSTR>(&CallbackWindowProc), &module)) {
		return nullptr;
	}

	WNDCLASSEXW window_class = {};
	window_class.cbSize = sizeof(WNDCLASSEXW);
	window_class.lpfnWndProc = CallbackWindowProc;
	window_class.hInstance = module;
	window_class.lpszClassName = CALLBACK_WINDOW_CLASS;
	RegisterClassExW(&window_class); // Already registered is fine.

	HWND window = CreateWindowExW(0, CALLBACK_WINDOW_CLASS, L"", 0,
		0, 0, 0, 0, HWND_MESSAGE, nullptr, module, nullptr);

	if (window != nullptr) {
		g_callback_window = window;
		g_callback_thread_id = GetCurrentThreadId();
		LogMessage("Callback window created on thread " + std::to_string(g_callback_thread_id));
	}

	return window;
}

void QueueStableBoardPosition(const std::string& fen) {
	HWND window = nullptr;
	bool post = false;

	{
		std::lock_guard<std::mutex> lock(g_callback_mutex);
		if (g_callback_func == nullptr) {
			return;
		}

		window = g_callback_window;
		if (window != nullptr) {
			// A stable board event is a state, not a queue - the newest position
			// always wins, so coalesce anything the host has not pumped yet.
			g_pending_fen = fen;
			if (!g_callback_post_queued) {
				g_callback_post_queued = true;
				post = true;
			}
		}
	}

	if (window == nullptr) {
		// The host gave us no thread to marshal to; fall back to the reader
		// thread (best effort, and what the previous version always did).
		InvokeStableBoardCallback(fen);
		return;
	}

	if (post && !PostMessageW(window, WM_DGT_STABLE_BOARD, 0, 0)) {
		std::lock_guard<std::mutex> lock(g_callback_mutex);
		g_callback_post_queued = false;
	}
}

std::mutex g_stream_mutex;
std::thread g_stream_thread;
std::shared_ptr<grpc::ClientContext> g_stream_context;

void ReadStableBoardStream(std::shared_ptr<grpc::ClientContext> context) {
	auto stub = GetStubSnapshot();
	if (!stub) {
		return;
	}

	dgt::Empty empty_request;
	auto reader = stub->RegisterStableBoardFunc(context.get(), empty_request);

	dgt::StringResponse response;
	// A cancelled context makes Read() return false, so TryCancel() is enough to
	// end the loop - this gRPC build has no ClientContext::IsCancelled().
	while (reader->Read(&response)) {
		const std::string fen = response.value();
		LogMessage("Received stable board: " + fen);
		QueueStableBoardPosition(fen);
	}

	reader->Finish();
	LogMessage("Stable board stream ended");
}

void StopStableBoardStream() {
	std::thread worker;
	std::shared_ptr<grpc::ClientContext> context;

	{
		std::lock_guard<std::mutex> lock(g_stream_mutex);
		worker = std::move(g_stream_thread);
		context = std::move(g_stream_context);
	}

	if (context) {
		context->TryCancel();
	}

	if (worker.joinable()) {
		if (worker.get_id() == std::this_thread::get_id()) {
			worker.detach();
		}
		else {
			worker.join();
		}
	}
}

// Used from DllMain, where joining is not allowed (loader lock).  Cancel the
// stream, give the reader a moment to notice, then let it go.
void AbandonStableBoardStream() {
	HANDLE native = nullptr;

	{
		std::lock_guard<std::mutex> lock(g_stream_mutex);
		if (g_stream_context) {
			g_stream_context->TryCancel();
		}

		if (g_stream_thread.joinable()) {
			native = static_cast<HANDLE>(g_stream_thread.native_handle());
		}
	}

	if (native != nullptr) {
		WaitForSingleObject(native, 2000);
	}

	{
		std::lock_guard<std::mutex> lock(g_stream_mutex);
		if (g_stream_thread.joinable()) {
			g_stream_thread.detach();
		}

		g_stream_context.reset();
	}
}

void ReleaseCallbackWindow() {
	HWND window = nullptr;

	{
		std::lock_guard<std::mutex> lock(g_callback_mutex);
		window = g_callback_window;
		g_callback_window = nullptr;
		g_callback_func = nullptr;
		g_pending_fen.clear();
		g_callback_post_queued = false;
	}

	// A window can only be destroyed by the thread that created it.  If that is
	// not us, leave it to go with the process.
	if (window != nullptr && GetCurrentThreadId() == g_callback_thread_id) {
		DestroyWindow(window);
	}
}

template <typename RequestType>
int PerformGrpcCall(const std::function<grpc::Status(grpc::ClientContext&, RequestType&, dgt::IntResponse&)>& call) {
	LogMessage(">>>>Called " + std::string(__func__));

	try {
		// Serialise onto the single stub: g_stub must not be replaced while a
		// call is in flight, and the host's calls are already sequential.
		std::lock_guard<std::mutex> lock(g_stub_mutex);
		EnsureStubLocked();

		RequestType request;
		dgt::IntResponse response;
		grpc::ClientContext context;
		context.set_deadline(std::chrono::system_clock::now() + std::chrono::seconds(GRPC_CALL_TIMEOUT_SECONDS));

		grpc::Status status = call(context, request, response);
		if (!status.ok()) {
			// Never report success when the service did not answer - the old
			// code returned the default 0 from an unpopulated response.
			LogMessage("  gRPC call failed: " + status.error_message());
			return 1;
		}

		return response.value();
	}
	catch (const std::exception& e) {
		LogMessage("  Exception: " + std::string(e.what()));
		return 1;
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
		// Deliberately does no work.  The host holds the loader lock here and
		// gRPC spawns threads and lazily loads libraries, so doing that from
		// DllMain deadlocks or crashes the host.  The channel, the log file and
		// the stream are all created on the first exported call instead.
		break;
	case DLL_THREAD_ATTACH:
		break;
	case DLL_THREAD_DETACH:
		break;
	case DLL_PROCESS_DETACH:
		// Stop the reader before the host unloads the module, otherwise it can
		// call back into code that no longer exists.  No logging here - the
		// loader lock is held.
		AbandonStableBoardStream();
		ReleaseCallbackWindow();

		if (g_logging_enabled) {
			std::lock_guard<std::mutex> lock(g_log_mutex);
			g_log_file.close();
			g_logging_enabled = false;
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
		LogMessage(">>Called " + std::string(__func__));

		// Only tear down this client's own stream.  Forwarding Exit() to the
		// service would de-initialise the shared board driver and take the
		// board away from every other client (and from Cherub itself).
		StopStableBoardStream();
		ReleaseCallbackWindow();

		return 0;
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
		LogMessage(">>Called " + std::string(__func__));

		// Re-registration (or NULL to stop the events) always ends the previous
		// stream first - the old version started a reader thread per call and
		// never stopped one.
		StopStableBoardStream();

		if (func == nullptr) {
			ReleaseCallbackWindow();
			return 0;
		}

		{
			std::lock_guard<std::mutex> lock(g_callback_mutex);
			g_callback_func = func;
			g_pending_fen.clear();
			g_callback_post_queued = false;

			// Created on the host's thread so the host's own message pump
			// dispatches the events and the callback runs on that thread.
			GetOrCreateCallbackWindowLocked();
		}

		// The format is stored by the shared driver, so make sure it reports FEN
		// rather than PluginMove/SAN.
		_DGTDLL_UseFEN(true);

		auto context = std::make_shared<grpc::ClientContext>();
		{
			std::lock_guard<std::mutex> lock(g_stream_mutex);
			g_stream_context = context;
			g_stream_thread = std::thread(ReadStableBoardStream, context);
		}

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


