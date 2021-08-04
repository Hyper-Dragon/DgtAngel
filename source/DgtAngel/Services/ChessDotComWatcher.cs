using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DgtAngel.Services
{
    public class ChessDotComWatcher
    {
        private readonly ScriptWrapper scriptWrapper;

        public ChessDotComWatcher(ScriptWrapper scriptWrapper)
        {
            this.scriptWrapper = scriptWrapper;
        }

        public async Task Testonlydelme() 
        {
            await scriptWrapper.WriteToConsole(ScriptWrapper.LogLevel.INFO,"Cdc Watch", "CHESS DOT COM WATCHER OK");
        }


    }
}
