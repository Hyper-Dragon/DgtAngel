using DgtAngelLib;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DgtAngel.Services
{
    public interface IChessDotComWatcher
    {
        Task PollChessDotComBoard();
    }

    public class ChessDotComWatcher : IChessDotComWatcher
    {
        private readonly IScriptWrapper scriptWrapper;
        private readonly IAppData appData;
        private readonly IChessDotComHelpers chessDotComHelpers;

        public ChessDotComWatcher(IScriptWrapper scriptWrapper, IAppData appData, IChessDotComHelpers chessDotComHelpers)
        {
            this.scriptWrapper = scriptWrapper;
            this.appData = appData;
            this.chessDotComHelpers = chessDotComHelpers;
        }


        public async Task PollChessDotComBoard()
        {
            for (; ; )
            {
                await scriptWrapper.WriteToConsole(ScriptWrapper.LogLevel.DEBUG, "Background", $"Background loop {appData.Age}");

                var chessDotComBoardString = await scriptWrapper.GetChessDotComBoardString();
                await scriptWrapper.WriteToConsole(ScriptWrapper.LogLevel.DEBUG, "Background", $"Background loop Result is {chessDotComBoardString}");



                //0:05.8|0:07.8|bk35,wb56,wk77
                if (chessDotComBoardString != "-")
                {
                    try
                    {
                        string fen = chessDotComHelpers.ConvertHtmlToFenT2(chessDotComBoardString);
                        //await Scripts.PlayAudioFile(ScriptWrapper.AudioClip.CDC_WATCHING);
                        await scriptWrapper.WriteToConsole(ScriptWrapper.LogLevel.INFO, "Background", $"ChessDotCom Fen is {fen}");
                    }
                    catch (Exception ex)
                    {
                        await scriptWrapper.WriteToConsole(ScriptWrapper.LogLevel.ERR, "Background", $"ChessDotCom Fen is unavailable [{ex.Message}]");
                    }
                }

                await Task.Delay(2000);

                appData.Age += 1;
            }
        }

    }
}
