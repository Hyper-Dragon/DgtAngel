using Fleck;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DgtEbDllWrapper
{
    public class LiveChessServer
    {
        private WebSocketServer server;

        public LiveChessServer() { }


        public void RunLiveChessServer() {

            server = new("ws://0.0.0.0:1982");
            server.RestartAfterListenError = true;
            
            server.Start(socket =>
            {
                //socket.OnOpen = () => TextBoxConsole.AddLine($"OPEN");
                //socket.OnClose = () => TextBoxConsole.AddLine($"CLOSE");
                
                socket.OnMessage = message =>
                {
                    int idCount = 1;
                    string fen = "8/8/8/8/8/8/8/8";

                    if (message != null && message.Contains("call"))
                    {
                        if (message.Contains("eboards"))
                        {
                            socket.Send("{\"response\":\"call\",\"id\":1,\"param\":[{\"serialnr\":\"24958\",\"source\":\"COM3\",\"state\":\"ACTIVE\",\"battery\":\"30%\",\"comment\":null,\"board\":\"8/8/8/8/8/8/8/8\",\"flipped\":false,\"clock\":null}],\"time\":1668045228634}");
                        }
                        else if (message.Contains("subscribe"))
                        {
                            socket.Send("{\"response\":\"call\",\"id\":2,\"param\":null,\"time\":1668045228663}");
                            socket.Send("{" + $"\"response\":\"feed\",\"id\":{idCount++},\"param\":" + "{" + $"\"serialnr\":\"24958\",\"flipped\":false,\"board\":\"{fen}\",\"clock\":null" + "}" + ",\"time\":1668045228666" + "}");

                            DgtEbDllFacade.OnFenChanged += (object sender, FenChangedEventArgs e) =>
                            {
                                //TextBoxConsole.AddLine($"Local board changed [SOCKET] [{e.Fen}]");
                                socket.Send("{" + $"\"response\":\"feed\",\"id\":{idCount++},\"param\":" + "{" + $"\"serialnr\":\"24958\",\"flipped\":false,\"board\":\"{e.Fen}\",\"clock\":null" + "}" + ",\"time\":1668045228666" + "}");
                            };

                            //Thread.Sleep(60000);
                        }
                    }
                };

            });
        }
    }
}
