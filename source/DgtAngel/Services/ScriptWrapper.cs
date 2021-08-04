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
        public enum AudioClip { MISMATCH };
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
            string clipname = audioClip switch
            {
                AudioClip.MISMATCH => "audio-mismatch",
                _ => throw new Exception("Unknown Audio File")
            };

            await jSRuntime.InvokeVoidAsync("playAudioFromBkg", clipname);
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
