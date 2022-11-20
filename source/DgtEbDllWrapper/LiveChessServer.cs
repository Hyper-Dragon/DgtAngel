using DgtRabbitWrapper.DgtEbDll;
using Fleck;

namespace DgtRabbitWrapper
{
    public class LiveChessServer
    {
        private DgtEbDllFacade testme;
        private WebSocketServer server;

        public LiveChessServer() { }


        public void RunLiveChessServer() {

            server = new("ws://0.0.0.0:1982") { RestartAfterListenError = true };

            testme = new DgtEbDllFacade();
            testme.Init();

            string lastFenSeen = "8/8/8/8/8/8/8/8";

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


                            DgtEbDllAdapter.OnFenChanged += (object sender, FenChangedEventArgs e) =>
                            {
                                lastFenSeen = e.Fen;
                                //socket.Send("{" + $"\"response\":\"feed\",\"id\":{idCount++},\"param\":" + "{" + $"\"serialnr\":\"24958\",\"flipped\":false,\"board\":\"{lastFenSeen}\",\"clock\":null" + "}" + ",\"time\":\""+ DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + "\"}");
                            };


                            while (true)
                            {
                                //socket.Send("{" + $"\"response\":\"feed\",\"id\":{idCount++},\"param\":" + "{" + $"\"serialnr\":\"24958\",\"flipped\":false,\"board\":\"{lastFenSeen}\",\"clock\":null" + "}" + ",\"time\":\"" + DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + "\"}");
                                socket.Send("{" + $"\"response\":\"feed\",\"id\":{idCount++},\"param\":" + "{" + $"\"serialnr\":\"24958\",\"flipped\":false,\"board\":\"{lastFenSeen}\",\"clock\":null" + "}" + ",\"time\":1668045228666" + "}");
                                Thread.Sleep(250);
                            }

                            //Thread.Sleep(60000);
                        }
                    }
                };

            });
        }
    }
}
