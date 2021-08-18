using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using System;
using System.Reflection;
using System.Threading.Tasks;

namespace DgtAngel.Services
{
    public interface IScriptWrapper
    {
        Task AddIndexToContextMenu();
        string GetAudioFileId(ScriptWrapper.AudioClip audioClip);
        string GetAudioFileSrc(ScriptWrapper.AudioClip audioClip);
        Task<string> GetChessDotComBoardJson();
        Task PlayAudioFile(ScriptWrapper.AudioClip audioClip);
        Task WriteToConsole(ScriptWrapper.LogLevel logLevel, string source, string message);
    }

    public class ScriptWrapper : IScriptWrapper
    {
        //Switch to true for full console trace
        //TODO:Move to parameter
        private readonly bool isConsoleDebugEnabled = false;

        public enum LogLevel { DEBUG, INFO, WARN, ERR };
        public enum AudioClip { MISMATCH = 0, MATCH, DGT_LC_CONNECTED, DGT_LC_DISCONNECTED, DGT_CONNECTED, DGT_DISCONNECTED, CDC_WATCHING, CDC_NOTWATCHING };
        private enum AudioFileIdx { ID = 0, FILENAME };

        private const string AUDIO_BASE = "Audio/Speech-en-01/";
        private readonly string[,] AudioFiles = { { "audio-mismatch", "Mismatch.wav" },
                                                  { "audio-match","Match.wav" },
                                                  { "audio-lcconnected","DgtLcConnected.wav" },
                                                  { "audio-lcdisconnected","DgtLcDisconnected.wav" },
                                                  { "audio-dgtconnected","DgtConnected.wav" },
                                                  { "audio-dgtdisconnected","DgtDisconnected.wav" },
                                                  { "audio-cdcwatching","CdcWatching.wav" },
                                                  { "audio-cdcnotwatching","CdcStoppedWatching.wav" },
                                                };

        private readonly ILogger _logger;
        private readonly IJSRuntime _jSRuntime;

        public ScriptWrapper(ILogger<ScriptWrapper> logger, IJSRuntime jSRuntime)
        {
            _logger = logger;
            _jSRuntime = jSRuntime;
        }

        // HELPER METHODS

        public string GetAudioFileId(AudioClip audioClip)
        {
            return AudioFiles[(int)audioClip, (int)AudioFileIdx.ID];
        }

        public string GetAudioFileSrc(AudioClip audioClip)
        {
            return $"{AUDIO_BASE}{AudioFiles[(int)audioClip, (int)AudioFileIdx.FILENAME]}";
        }

        // JAVASCRIPT CALLS BELOW HERE >>>>>>

        public async Task<string> GetChessDotComBoardJson()
        {
            _logger?.LogTrace($"Calling JS from {MethodBase.GetCurrentMethod().ReflectedType.Name}");
            return await _jSRuntime.InvokeAsync<string>("getGetRemoteBoardStateJson");
        }

        public async Task AddIndexToContextMenu()
        {
            _logger?.LogTrace($"Calling JS from {MethodBase.GetCurrentMethod().ReflectedType.Name}");
            await _jSRuntime.InvokeVoidAsync("addIndexToContextMenu");
        }

        public async Task PlayAudioFile(AudioClip audioClip)
        {
            _logger?.LogTrace($"Calling JS from {MethodBase.GetCurrentMethod().ReflectedType.Name}");
            await _jSRuntime.InvokeVoidAsync("playAudioFromBkg", AudioFiles[(int)audioClip, (int)ScriptWrapper.AudioFileIdx.ID]);
        }

        public async Task WriteToConsole(LogLevel logLevel, string source, string message)
        {
            _logger?.LogTrace($"Calling JS from {MethodBase.GetCurrentMethod().ReflectedType.Name}");

            if (isConsoleDebugEnabled || logLevel != LogLevel.DEBUG)
            {
                string logMethod = logLevel switch
                {
                    LogLevel.DEBUG => "writeDebugToConsole",
                    LogLevel.INFO => "writeInfoToConsole",
                    LogLevel.WARN => "writeWarningToConsole",
                    LogLevel.ERR => "writeErrorToConsole",
                    _ => throw new Exception("Unknown Log Type")
                };

                await _jSRuntime.InvokeVoidAsync(logMethod, $"{source,10}-->{message}");
            }
        }
    }



}
