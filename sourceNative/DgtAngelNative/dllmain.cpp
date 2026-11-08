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
#include <cstring>

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

// _DGTDLL_Init is forwarded once; every later caller gets the same answer.
std::mutex g_init_mutex;
bool g_init_done = false;
int g_init_result = 0;

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

// One message per callback type so the window procedure can tell them apart.
const UINT WM_DGT_CALLBACK_BASE = WM_APP + 0x0100;

enum CallbackId {
	CB_STATUS = 0,
	CB_SCAN,
	CB_STABLE_BOARD,
	CB_WCLOCK,
	CB_BCLOCK,
	CB_RESULT,
	CB_NEWGAME,
	CB_WHITE_MOVE_INPUT,
	CB_BLACK_MOVE_INPUT,
	CB_WHITE_MOVE_NOW,
	CB_BLACK_MOVE_NOW,
	CB_START_SETUP,
	CB_STOP_SETUP_WTM,
	CB_STOP_SETUP_BTM,
	CB_GAME_TYPE,
	CB_ALLOW_TAKEBACKS,
	CB_WHITE_TAKEBACK,
	CB_BLACK_TAKEBACK,
	CB_MAGIC_PIECE,
	CB_COUNT
};

const char* CALLBACK_NAMES[CB_COUNT] = {
	"Status",
	"Scan",
	"StableBoard",
	"WClock",
	"BClock",
	"Result",
	"NewGame",
	"WhiteMoveInput",
	"BlackMoveInput",
	"WhiteMoveNow",
	"BlackMoveNow",
	"StartSetup",
	"StopSetupWTM",
	"StopSetupBTM",
	"GameTypeChanged",
	"AllowTakebacksChanged",
	"WhiteTakeback",
	"BlackTakeback",
	"MagicPiece"
};

// Everything the driver pushes for one registration.  The value is a state, not a
// queue, so the newest value always wins.
struct CallbackChannel {
	void* func = nullptr;
	std::string pendingString;
	int pendingParam1 = 0;
	int pendingParam2 = 0;
	bool pendingValue = false;
	bool postQueued = false;
	std::thread thread;
	std::shared_ptr<grpc::ClientContext> context;
};

std::mutex g_callback_mutex;
HWND g_callback_window = nullptr;
DWORD g_callback_thread_id = 0;
CallbackChannel g_channels[CB_COUNT];

void InvokeCallback(int id, void* func, const std::string& text, int param1, int param2, bool value) {
	if (func == nullptr) {
		return;
	}

	try {
		switch (id) {
		case CB_GAME_TYPE:
			reinterpret_cast<FI*>(func)(param1);
			break;
		case CB_ALLOW_TAKEBACKS:
			reinterpret_cast<FB*>(func)(value);
			break;
		case CB_WHITE_TAKEBACK:
		case CB_BLACK_TAKEBACK:
			reinterpret_cast<F*>(func)();
			break;
		case CB_MAGIC_PIECE:
			reinterpret_cast<FIIC*>(func)(param1, param2, text.c_str());
			break;
		default:
			// Everything else carries a single string.
			reinterpret_cast<FC*>(func)(text.c_str());
			break;
		}
	}
	catch (...) {
		LogMessage(std::string("Exception in ") + CALLBACK_NAMES[id] + " callback");
	}
}

LRESULT CALLBACK CallbackWindowProc(HWND hwnd, UINT message, WPARAM wParam, LPARAM lParam) {
	if (message >= WM_DGT_CALLBACK_BASE && message < WM_DGT_CALLBACK_BASE + CB_COUNT) {
		const int id = static_cast<int>(message - WM_DGT_CALLBACK_BASE);
		void* func = nullptr;
		std::string text;
		int param1 = 0;
		int param2 = 0;
		bool value = false;

		{
			std::lock_guard<std::mutex> lock(g_callback_mutex);
			g_channels[id].postQueued = false;
			func = g_channels[id].func;
			text.swap(g_channels[id].pendingString);
			param1 = g_channels[id].pendingParam1;
			param2 = g_channels[id].pendingParam2;
			value = g_channels[id].pendingValue;
		}

		InvokeCallback(id, func, text, param1, param2, value);
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

void QueueCallbackValue(int id, const std::string& text, int param1 = 0, int param2 = 0, bool value = false) {
	HWND window = nullptr;
	bool post = false;

	{
		std::lock_guard<std::mutex> lock(g_callback_mutex);
		if (g_channels[id].func == nullptr) {
			return;
		}

		window = g_callback_window;
		if (window != nullptr) {
			// A callback value is a state, not a queue - the newest value
			// always wins, so coalesce anything the host has not pumped yet.
			g_channels[id].pendingString = text;
			g_channels[id].pendingParam1 = param1;
			g_channels[id].pendingParam2 = param2;
			g_channels[id].pendingValue = value;

			if (!g_channels[id].postQueued) {
				g_channels[id].postQueued = true;
				post = true;
			}
		}
	}

	if (window == nullptr) {
		// The host gave us no thread to marshal to; fall back to the reader
		// thread (best effort, and what the previous version always did).
		void* func = nullptr;
		{
			std::lock_guard<std::mutex> lock(g_callback_mutex);
			func = g_channels[id].func;
		}

		InvokeCallback(id, func, text, param1, param2, value);
		return;
	}

	if (post && !PostMessageW(window, WM_DGT_CALLBACK_BASE + id, 0, 0)) {
		std::lock_guard<std::mutex> lock(g_callback_mutex);
		g_channels[id].postQueued = false;
	}
}

// A cancelled context makes Read() return false, so TryCancel() is enough to end
// the loop - this gRPC build has no ClientContext::IsCancelled().
void QueueResponse(int id, const dgt::StringResponse& response) {
	LogMessage(std::string("Received ") + CALLBACK_NAMES[id] + ": " + response.value());
	QueueCallbackValue(id, response.value());
}

void QueueResponse(int id, const dgt::IntResponse& response) {
	LogMessage(std::string("Received ") + CALLBACK_NAMES[id] + ": " + std::to_string(response.value()));
	QueueCallbackValue(id, "", response.value());
}

void QueueResponse(int id, const dgt::BoolResponse& response) {
	LogMessage(std::string("Received ") + CALLBACK_NAMES[id] + ": " + (response.value() ? "true" : "false"));
	QueueCallbackValue(id, "", 0, 0, response.value());
}

void QueueResponse(int id, const dgt::CallbackIICResponse& response) {
	LogMessage(std::string("Received ") + CALLBACK_NAMES[id] + ": " + response.param3());
	QueueCallbackValue(id, response.param3(), response.param1(), response.param2());
}

void QueueResponse(int id, const dgt::EmptyResponse&) {
	LogMessage(std::string("Received ") + CALLBACK_NAMES[id]);
	QueueCallbackValue(id, "");
}

template <typename ResponseType, typename StartStream>
void ReadCallbackStream(int id, std::shared_ptr<grpc::ClientContext> context, StartStream startStream) {
	auto stub = GetStubSnapshot();
	if (!stub) {
		return;
	}

	dgt::Empty request;
	auto reader = startStream(*stub, context.get(), request);

	ResponseType response;
	while (reader->Read(&response)) {
		QueueResponse(id, response);
	}

	reader->Finish();
	LogMessage(std::string(CALLBACK_NAMES[id]) + " stream ended");
}

void StopCallbackStream(int id) {
	std::thread worker;
	std::shared_ptr<grpc::ClientContext> context;

	{
		std::lock_guard<std::mutex> lock(g_callback_mutex);
		worker = std::move(g_channels[id].thread);
		context = std::move(g_channels[id].context);
		g_channels[id].func = nullptr;
		g_channels[id].pendingString.clear();
		g_channels[id].postQueued = false;
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

void StopAllCallbackStreams() {
	for (int id = 0; id < CB_COUNT; ++id) {
		StopCallbackStream(id);
	}
}

// Used from DllMain, where joining is not allowed (loader lock).  Cancel every
// stream, give the readers a moment to notice, then let them go.
void AbandonAllCallbackStreams() {
	HANDLE natives[CB_COUNT] = {};

	for (int id = 0; id < CB_COUNT; ++id) {
		std::lock_guard<std::mutex> lock(g_callback_mutex);
		if (g_channels[id].context) {
			g_channels[id].context->TryCancel();
		}

		if (g_channels[id].thread.joinable()) {
			natives[id] = static_cast<HANDLE>(g_channels[id].thread.native_handle());
		}
	}

	for (int id = 0; id < CB_COUNT; ++id) {
		if (natives[id] != nullptr) {
			WaitForSingleObject(natives[id], 2000);
		}
	}

	for (int id = 0; id < CB_COUNT; ++id) {
		std::lock_guard<std::mutex> lock(g_callback_mutex);
		if (g_channels[id].thread.joinable()) {
			g_channels[id].thread.detach();
		}

		g_channels[id].context.reset();
	}
}

template <typename FnPtr, typename ResponseType, typename StartStream>
int RegisterCallbackStream(int id, FnPtr func, StartStream startStream) {
	// Re-registration (or NULL to stop the events) always ends the previous
	// stream first - the old version started a reader thread per call and never
	// stopped one.
	StopCallbackStream(id);

	if (func == nullptr) {
		return 0;
	}

	{
		std::lock_guard<std::mutex> lock(g_callback_mutex);
		g_channels[id].func = reinterpret_cast<void*>(func);
		g_channels[id].pendingString.clear();
		g_channels[id].pendingParam1 = 0;
		g_channels[id].pendingParam2 = 0;
		g_channels[id].pendingValue = false;
		g_channels[id].postQueued = false;

		// Created on the host's thread so the host's own message pump dispatches
		// the events and the callback runs on that thread.
		GetOrCreateCallbackWindowLocked();
	}

	auto context = std::make_shared<grpc::ClientContext>();
	{
		std::lock_guard<std::mutex> lock(g_callback_mutex);
		g_channels[id].context = context;
		g_channels[id].thread = std::thread(ReadCallbackStream<ResponseType, StartStream>, id, context, startStream);
	}

	return 0;
}

void ReleaseCallbackWindow() {
	HWND window = nullptr;

	{
		std::lock_guard<std::mutex> lock(g_callback_mutex);
		window = g_callback_window;
		g_callback_window = nullptr;

		for (int id = 0; id < CB_COUNT; ++id) {
			g_channels[id].func = nullptr;
			g_channels[id].pendingString.clear();
			g_channels[id].postQueued = false;
		}
	}

	// A window can only be destroyed by the thread that created it.  If that is
	// not us, leave it to go with the process.
	if (window != nullptr && GetCurrentThreadId() == g_callback_thread_id) {
		DestroyWindow(window);
	}
}

template <typename RequestType, typename Call>
int PerformGrpcCall(RequestType request, Call call) {	LogMessage(">>>>Called " + std::string(__func__));

	try {
		// Serialise onto the single stub: g_stub must not be replaced while a
		// call is in flight, and the host's calls are already sequential.
		std::lock_guard<std::mutex> lock(g_stub_mutex);
		EnsureStubLocked();

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


// Writes a driver-supplied clock string back into the buffer the host handed us, but only
// when it fits: the host sized that buffer for the string it passed in, so copying anything
// longer would overflow it.  Dropping the write is the safe failure - a host that reads the
// buffer just keeps what it sent.
void CopyClockStringBack(const char* target, const std::string& value) {
	if (target == nullptr || value.empty()) {
		return;
	}

	char* buffer = const_cast<char*>(target);
	const std::size_t capacity = std::strlen(buffer);

	if (value.size() > capacity) {
		LogMessage("  write-back dropped - value longer than the host buffer");
		return;
	}

	std::memcpy(buffer, value.c_str(), value.size() + 1);
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
		// Stop the readers before the host unloads the module, otherwise they can
		// call back into code that no longer exists.  No logging here - the
		// loader lock is held.
		AbandonAllCallbackStreams();
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

		dgt::Empty request;

		auto call = [](grpc::ClientContext& context, dgt::Empty& request, dgt::IntResponse& response) {
			return g_stub->GetVersion(&context, request, &response);
		};

		return PerformGrpcCall(request, call);
	}

	__declspec(dllexport) int __stdcall _DGTDLL_GetWxWidgetsVersion() {
		BlockUntilInitComplete();
		LogMessage(">>Called " + std::string(__func__));

		dgt::Empty request;

		auto call = [](grpc::ClientContext& context, dgt::Empty& request, dgt::IntResponse& response) {
			return g_stub->GetWxWidgetsVersion(&context, request, &response);
		};

		return PerformGrpcCall(request, call);
	}

	__declspec(dllexport) int __stdcall __stdcall _DGTDLL_Init() {
		BlockUntilInitComplete();
		LogMessage(">>Called " + std::string(__func__));

		std::lock_guard<std::mutex> lock(g_init_mutex);

		// The driver is shared: Cherub owns it and has already initialised it.  A
		// second Init would re-initialise it underneath every other client, so the
		// first result is remembered and reported to later callers - which is what a
		// host would see from a driver that was already up.
		if (g_init_done) {
			return g_init_result;
		}

		dgt::Empty request;

		auto call = [](grpc::ClientContext& context, dgt::Empty& request, dgt::IntResponse& response) {
			return g_stub->Init(&context, request, &response);
		};

		int result = PerformGrpcCall(request, call);

		// Only a success is cached: a transport failure must be retried rather than
		// reported to every later caller.
		if (result == 0) {
			g_init_done = true;
			g_init_result = result;
		}

		return result;
	}

	__declspec(dllexport) int __stdcall _DGTDLL_Exit() {
		LogMessage(">>Called " + std::string(__func__));

		// Only tear down this client's own streams.  Forwarding Exit() to the
		// service would de-initialise the shared board driver and take the
		// board away from every other client (and from Cherub itself).
		StopAllCallbackStreams();
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

		return PerformGrpcCall(request, call);
	}

	__declspec(dllexport) int __stdcall _DGTDLL_HideDialog(int val) {
		BlockUntilInitComplete();
		LogMessage(">>Called " + std::string(__func__));

		dgt::IntRequest request;
		request.set_value(val);

		auto call = [](grpc::ClientContext& context, const dgt::IntRequest& request, dgt::IntResponse& response) {
			return g_stub->HideDialog(&context, request, &response);
		};

		return PerformGrpcCall(request, call);
	}

	__declspec(dllexport) int __stdcall _DGTDLL_WriteCOMPort(int val) {
		BlockUntilInitComplete();
		LogMessage(">>Called " + std::string(__func__));

		dgt::IntRequest request;
		request.set_value(val);

		auto call = [](grpc::ClientContext& context, const dgt::IntRequest& request, dgt::IntResponse& response) {
			return g_stub->WriteCOMPort(&context, request, &response);
		};

		return PerformGrpcCall(request, call);
	}

	__declspec(dllexport) int __stdcall _DGTDLL_WriteCOMPortString(const char* val) {
		BlockUntilInitComplete();
		LogMessage(">>Called " + std::string(__func__));

		dgt::StringRequest request;
		request.set_value(val);

		auto call = [](grpc::ClientContext& context, const dgt::StringRequest& request, dgt::IntResponse& response) {
			return g_stub->WriteCOMPortString(&context, request, &response);
		};

		return PerformGrpcCall(request, call);
	}

	__declspec(dllexport) int __stdcall _DGTDLL_WritePosition(const char* val) {
		BlockUntilInitComplete();
		LogMessage(">>Called " + std::string(__func__));

		dgt::StringRequest request;
		request.set_value(val);

		auto call = [](grpc::ClientContext& context, const dgt::StringRequest& request, dgt::IntResponse& response) {
			return g_stub->WritePosition(&context, request, &response);
		};

		return PerformGrpcCall(request, call);
	}

	__declspec(dllexport) int __stdcall _DGTDLL_PlayWhiteMove(const char* val) {
		BlockUntilInitComplete();
		LogMessage(">>Called " + std::string(__func__));

		dgt::StringRequest request;
		request.set_value(val);

		auto call = [](grpc::ClientContext& context, const dgt::StringRequest& request, dgt::IntResponse& response) {
			return g_stub->PlayWhiteMove(&context, request, &response);
		};

		return PerformGrpcCall(request, call);
	}

	__declspec(dllexport) int __stdcall _DGTDLL_PlayBlackMove(const char* val) {
		BlockUntilInitComplete();
		LogMessage(">>Called " + std::string(__func__));

		dgt::StringRequest request;
		request.set_value(val);

		auto call = [](grpc::ClientContext& context, const dgt::StringRequest& request, dgt::IntResponse& response) {
			return g_stub->PlayBlackMove(&context, request, &response);
		};

		return PerformGrpcCall(request, call);
	}

	__declspec(dllexport) int __stdcall _DGTDLL_WriteDebug(bool val) {
		BlockUntilInitComplete();
		LogMessage(">>Called " + std::string(__func__));

		dgt::BoolRequest request;
		request.set_value(val);

		auto call = [](grpc::ClientContext& context, const dgt::BoolRequest& request, dgt::IntResponse& response) {
			return g_stub->WriteDebug(&context, request, &response);
		};

		return PerformGrpcCall(request, call);
	}

	__declspec(dllexport) int __stdcall _DGTDLL_DisplayClockMessage(const char* val1, int val2) {
		BlockUntilInitComplete();
		LogMessage(">>Called " + std::string(__func__) + " [" + (val1 == nullptr ? "<null>" : val1) + "] [" + std::to_string(val2) + "]");

		dgt::ClockMessageRequest request;
		request.set_message(val1);
		request.set_time(val2);

		auto call = [](grpc::ClientContext& context, const dgt::ClockMessageRequest& request, dgt::IntResponse& response) {
			return g_stub->DisplayClockMessage(&context, request, &response);
		};

		return PerformGrpcCall(request, call);
	}

	__declspec(dllexport) int __stdcall _DGTDLL_EndDisplay(int val) {
		BlockUntilInitComplete();
		LogMessage(">>Called " + std::string(__func__));

		dgt::IntRequest request;
		request.set_value(val);

		auto call = [](grpc::ClientContext& context, const dgt::IntRequest& request, dgt::IntResponse& response) {
			return g_stub->EndDisplay(&context, request, &response);
		};

		return PerformGrpcCall(request, call);
	}

	__declspec(dllexport) int __stdcall _DGTDLL_SetNRun(const char* val1, const char* val2, int val3) {
		BlockUntilInitComplete();
		LogMessage(">>Called " + std::string(__func__) + " [" + (val1 == nullptr ? "<null>" : val1) + "] [" + (val2 == nullptr ? "<null>" : val2) + "] [" + std::to_string(val3) + "]");

		dgt::SetNRunRequest request;
		request.set_param1(val1 == nullptr ? "" : val1);
		request.set_param2(val2 == nullptr ? "" : val2);
		request.set_time(val3);

		try {
			std::lock_guard<std::mutex> lock(g_stub_mutex);
			EnsureStubLocked();

			dgt::SetNRunResponse response;
			grpc::ClientContext context;
			context.set_deadline(std::chrono::system_clock::now() + std::chrono::seconds(GRPC_CALL_TIMEOUT_SECONDS));

			grpc::Status status = g_stub->SetNRun(&context, request, &response);
			if (!status.ok()) {
				LogMessage("  gRPC call failed: " + status.error_message());
				return 1;
			}

			// The real DLL writes the current clock strings back into the caller's buffers,
			// so hand them back the same way.  CopyClockStringBack only writes when the value
			// fits the buffer the host gave us.
			CopyClockStringBack(val1, response.white());
			CopyClockStringBack(val2, response.black());

			return response.value();
		}
		catch (const std::exception& e) {
			LogMessage("  Exception: " + std::string(e.what()));
			return 1;
		}
	}

	__declspec(dllexport) int __stdcall _DGTDLL_ClockMode(int val) {
		BlockUntilInitComplete();
		LogMessage(">>Called " + std::string(__func__));

		dgt::IntRequest request;
		request.set_value(val);

		auto call = [](grpc::ClientContext& context, const dgt::IntRequest& request, dgt::IntResponse& response) {
			return g_stub->ClockMode(&context, request, &response);
		};

		return PerformGrpcCall(request, call);
	}

	__declspec(dllexport) int __stdcall _DGTDLL_SetAutoRotation(bool val) {
		BlockUntilInitComplete();
		LogMessage(">>Called " + std::string(__func__));

		dgt::BoolRequest request;
		request.set_value(val);

		auto call = [](grpc::ClientContext& context, const dgt::BoolRequest& request, dgt::IntResponse& response) {
			return g_stub->SetAutoRotation(&context, request, &response);
		};

		return PerformGrpcCall(request, call);
	}

	__declspec(dllexport) int __stdcall _DGTDLL_UseFEN(bool val) {
		BlockUntilInitComplete();
		LogMessage(">>Called " + std::string(__func__));

		dgt::BoolRequest request;
		request.set_value(val);

		auto call = [](grpc::ClientContext& context, const dgt::BoolRequest& request, dgt::IntResponse& response) {
			return g_stub->UseFEN(&context, request, &response);
		};

		return PerformGrpcCall(request, call);
	}

	__declspec(dllexport) int __stdcall _DGTDLL_UseSAN(bool val) {
		BlockUntilInitComplete();
		LogMessage(">>Called " + std::string(__func__));

		dgt::BoolRequest request;
		request.set_value(val);

		auto call = [](grpc::ClientContext& context, const dgt::BoolRequest& request, dgt::IntResponse& response) {
			return g_stub->UseSAN(&context, request, &response);
		};

		return PerformGrpcCall(request, call);
	}

	__declspec(dllexport) int __stdcall _DGTDLL_SetGameType(int val) {
		BlockUntilInitComplete();
		LogMessage(">>Called " + std::string(__func__));

		dgt::IntRequest request;
		request.set_value(val);

		auto call = [](grpc::ClientContext& context, const dgt::IntRequest& request, dgt::IntResponse& response) {
			return g_stub->SetGameType(&context, request, &response);
		};

		return PerformGrpcCall(request, call);
	}

	__declspec(dllexport) int __stdcall _DGTDLL_AllowTakebacks(bool val) {
		BlockUntilInitComplete();
		LogMessage(">>Called " + std::string(__func__));

		dgt::BoolRequest request;
		request.set_value(val);

		auto call = [](grpc::ClientContext& context, const dgt::BoolRequest& request, dgt::IntResponse& response) {
			return g_stub->AllowTakebacks(&context, request, &response);
		};

		return PerformGrpcCall(request, call);
	}



	__declspec(dllexport) int __stdcall _DGTDLL_RegisterStatusFunc(FC* func) {
		LogMessage(">>Called " + std::string(__func__));
		return RegisterCallbackStream<FC*, dgt::StringResponse>(CB_STATUS, func,
			[](dgt::DGTDLL::Stub& stub, grpc::ClientContext* context, dgt::Empty& request) {
				return stub.RegisterStatusFunc(context, request);
			});
	}

	__declspec(dllexport) int __stdcall _DGTDLL_RegisterScanFunc(FC* func) {
		LogMessage(">>Called " + std::string(__func__));
		return RegisterCallbackStream<FC*, dgt::StringResponse>(CB_SCAN, func,
			[](dgt::DGTDLL::Stub& stub, grpc::ClientContext* context, dgt::Empty& request) {
				return stub.RegisterScanFunc(context, request);
			});
	}

	__declspec(dllexport) int __stdcall _DGTDLL_RegisterStableBoardFunc(FC* func) {
		LogMessage(">>Called " + std::string(__func__));

// Deliberately no _DGTDLL_UseFEN here.  Registering a callback must not change
	// the shared driver's output format behind the host's back - the host asks for
	// what it wants with _DGTDLL_UseFEN, exactly as it would against the real DLL.
	return RegisterCallbackStream<FC*, dgt::StringResponse>(CB_STABLE_BOARD, func,
		[](dgt::DGTDLL::Stub& stub, grpc::ClientContext* context, dgt::Empty& request) {
			return stub.RegisterStableBoardFunc(context, request);
		});
	}

	__declspec(dllexport) int __stdcall _DGTDLL_RegisterWClockFunc(FC* func) {
		LogMessage(">>Called " + std::string(__func__));
		return RegisterCallbackStream<FC*, dgt::StringResponse>(CB_WCLOCK, func,
			[](dgt::DGTDLL::Stub& stub, grpc::ClientContext* context, dgt::Empty& request) {
				return stub.RegisterWClockFunc(context, request);
			});
	}

	__declspec(dllexport) int __stdcall _DGTDLL_RegisterBClockFunc(FC* func) {
		LogMessage(">>Called " + std::string(__func__));
		return RegisterCallbackStream<FC*, dgt::StringResponse>(CB_BCLOCK, func,
			[](dgt::DGTDLL::Stub& stub, grpc::ClientContext* context, dgt::Empty& request) {
				return stub.RegisterBClockFunc(context, request);
			});
	}

	__declspec(dllexport) int __stdcall _DGTDLL_RegisterResultFunc(FC* func) {
		LogMessage(">>Called " + std::string(__func__));
		return RegisterCallbackStream<FC*, dgt::StringResponse>(CB_RESULT, func,
			[](dgt::DGTDLL::Stub& stub, grpc::ClientContext* context, dgt::Empty& request) {
				return stub.RegisterResultFunc(context, request);
			});
	}

	__declspec(dllexport) int __stdcall _DGTDLL_RegisterNewGameFunc(FC* func) {
		LogMessage(">>Called " + std::string(__func__));
		return RegisterCallbackStream<FC*, dgt::StringResponse>(CB_NEWGAME, func,
			[](dgt::DGTDLL::Stub& stub, grpc::ClientContext* context, dgt::Empty& request) {
				return stub.RegisterNewGameFunc(context, request);
			});
	}

	__declspec(dllexport) int __stdcall _DGTDLL_RegisterWhiteMoveInputFunc(FC* func) {
		LogMessage(">>Called " + std::string(__func__));
		return RegisterCallbackStream<FC*, dgt::StringResponse>(CB_WHITE_MOVE_INPUT, func,
			[](dgt::DGTDLL::Stub& stub, grpc::ClientContext* context, dgt::Empty& request) {
				return stub.RegisterWhiteMoveInputFunc(context, request);
			});
	}

	__declspec(dllexport) int __stdcall _DGTDLL_RegisterBlackMoveInputFunc(FC* func) {
		LogMessage(">>Called " + std::string(__func__));
		return RegisterCallbackStream<FC*, dgt::StringResponse>(CB_BLACK_MOVE_INPUT, func,
			[](dgt::DGTDLL::Stub& stub, grpc::ClientContext* context, dgt::Empty& request) {
				return stub.RegisterBlackMoveInputFunc(context, request);
			});
	}

	__declspec(dllexport) int __stdcall _DGTDLL_RegisterWhiteMoveNowFunc(FC* func) {
		LogMessage(">>Called " + std::string(__func__));
		return RegisterCallbackStream<FC*, dgt::StringResponse>(CB_WHITE_MOVE_NOW, func,
			[](dgt::DGTDLL::Stub& stub, grpc::ClientContext* context, dgt::Empty& request) {
				return stub.RegisterWhiteMoveNowFunc(context, request);
			});
	}

	__declspec(dllexport) int __stdcall _DGTDLL_RegisterBlackMoveNowFunc(FC* func) {
		LogMessage(">>Called " + std::string(__func__));
		return RegisterCallbackStream<FC*, dgt::StringResponse>(CB_BLACK_MOVE_NOW, func,
			[](dgt::DGTDLL::Stub& stub, grpc::ClientContext* context, dgt::Empty& request) {
				return stub.RegisterBlackMoveNowFunc(context, request);
			});
	}

	__declspec(dllexport) int __stdcall _DGTDLL_RegisterStartSetupFunc(FC* func) {
		LogMessage(">>Called " + std::string(__func__));
		return RegisterCallbackStream<FC*, dgt::StringResponse>(CB_START_SETUP, func,
			[](dgt::DGTDLL::Stub& stub, grpc::ClientContext* context, dgt::Empty& request) {
				return stub.RegisterStartSetupFunc(context, request);
			});
	}

	__declspec(dllexport) int __stdcall _DGTDLL_RegisterStopSetupWTMFunc(FC* func) {
		LogMessage(">>Called " + std::string(__func__));
		return RegisterCallbackStream<FC*, dgt::StringResponse>(CB_STOP_SETUP_WTM, func,
			[](dgt::DGTDLL::Stub& stub, grpc::ClientContext* context, dgt::Empty& request) {
				return stub.RegisterStopSetupWTMFunc(context, request);
			});
	}

	__declspec(dllexport) int __stdcall _DGTDLL_RegisterStopSetupBTMFunc(FC* func) {
		LogMessage(">>Called " + std::string(__func__));
		return RegisterCallbackStream<FC*, dgt::StringResponse>(CB_STOP_SETUP_BTM, func,
			[](dgt::DGTDLL::Stub& stub, grpc::ClientContext* context, dgt::Empty& request) {
				return stub.RegisterStopSetupBTMFunc(context, request);
			});
	}

	__declspec(dllexport) int __stdcall _DGTDLL_RegisterWhiteTakebackFunc(F* func) {
		LogMessage(">>Called " + std::string(__func__));
		return RegisterCallbackStream<F*, dgt::EmptyResponse>(CB_WHITE_TAKEBACK, func,
			[](dgt::DGTDLL::Stub& stub, grpc::ClientContext* context, dgt::Empty& request) {
				return stub.RegisterWhiteTakebackFunc(context, request);
			});
	}

	__declspec(dllexport) int __stdcall _DGTDLL_RegisterBlackTakebackFunc(F* func) {
		LogMessage(">>Called " + std::string(__func__));
		return RegisterCallbackStream<F*, dgt::EmptyResponse>(CB_BLACK_TAKEBACK, func,
			[](dgt::DGTDLL::Stub& stub, grpc::ClientContext* context, dgt::Empty& request) {
				return stub.RegisterBlackTakebackFunc(context, request);
			});
	}

	__declspec(dllexport) int __stdcall _DGTDLL_RegisterGameTypeChangedFunc(FI* func) {
		LogMessage(">>Called " + std::string(__func__));
		return RegisterCallbackStream<FI*, dgt::IntResponse>(CB_GAME_TYPE, func,
			[](dgt::DGTDLL::Stub& stub, grpc::ClientContext* context, dgt::Empty& request) {
				return stub.RegisterGameTypeChangedFunc(context, request);
			});
	}

	__declspec(dllexport) int __stdcall _DGTDLL_RegisterAllowTakebacksChangedFunc(FB* func) {
		LogMessage(">>Called " + std::string(__func__));
		return RegisterCallbackStream<FB*, dgt::BoolResponse>(CB_ALLOW_TAKEBACKS, func,
			[](dgt::DGTDLL::Stub& stub, grpc::ClientContext* context, dgt::Empty& request) {
				return stub.RegisterAllowTakebacksChangedFunc(context, request);
			});
	}

	__declspec(dllexport) int __stdcall _DGTDLL_RegisterMagicPieceFunc(FIIC* func) {
		LogMessage(">>Called " + std::string(__func__));
		return RegisterCallbackStream<FIIC*, dgt::CallbackIICResponse>(CB_MAGIC_PIECE, func,
			[](dgt::DGTDLL::Stub& stub, grpc::ClientContext* context, dgt::Empty& request) {
				return stub.RegisterMagicPieceFunc(context, request);
			});
	}
}


