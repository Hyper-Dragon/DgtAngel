using Microsoft.JSInterop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DgtAngel.Services
{
    public class ScriptWrapper
    {
        public enum LogLevel { DEBUG,INFO,WARN,ERR };
        public enum AudioClip { MISMATCH=0 };
        private enum AudioFileIdx { ID = 0, FILENAME };

        private readonly string[,] AudioFiles = { { "audio-mismatch", "Audio/Mismatch.wav" } };


        private readonly IJSRuntime jSRuntime;

        public ScriptWrapper(IJSRuntime jSRuntime)
        {
            this.jSRuntime = jSRuntime;
        }


        async public Task<string> GetChessDotComBoardString()
        {
            return await jSRuntime.InvokeAsync<string>("getPiecesHtml");
        }


        async public Task PlayAudioFile(AudioClip audioClip)
        {            
            await jSRuntime.InvokeVoidAsync("playAudioFromBkg", AudioFiles[(int)audioClip, (int)ScriptWrapper.AudioFileIdx.ID]);
        }

        public string GetAudioFileId(AudioClip audioClip)
        {
            return AudioFiles[(int)audioClip, (int)AudioFileIdx.ID];
        }

        public string GetAudioFileSrc(AudioClip audioClip)
        {
            return AudioFiles[(int)audioClip, (int)AudioFileIdx.FILENAME];
        }

        async public Task WriteToConsole(LogLevel logLevel, String source, String message)
        {
            string logMethod = logLevel switch
            {
                LogLevel.DEBUG => "writeDebugToConsole",
                LogLevel.INFO => "writeInfoToConsole",
                LogLevel.WARN => "writeWarningToConsole",
                LogLevel.ERR => "writeErrorToConsole",
                _ => throw new Exception("Unknown Log Type")
            };

            await jSRuntime.InvokeVoidAsync(logMethod, $"{source,10}:{message}");
        }


    }
}
