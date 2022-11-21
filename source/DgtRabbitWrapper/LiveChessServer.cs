using DgtRabbitWrapper.DgtEbDll;
using Fleck;
using System;
using System.Linq;

namespace DgtRabbitWrapper
{
    public sealed class LiveChessServer
    {
        private readonly IDgtEbDllFacade _dgtEbDllFacade;
        private WebSocketServer server;

        public int BoardSerialNo { get; init; }
        public int ComPort { get; init; }
        public int BatteryPct { get; init; }
        public string InitialFEN { get; init; }

        private string lastFenSeen = "8/8/8/8/8/8/8/8";

        public LiveChessServer(IDgtEbDllFacade dgtEbDllFacade, int boardSerialNo, int comPort, int batteryPct, string initialFEN)
        {
            _dgtEbDllFacade = dgtEbDllFacade;

            BoardSerialNo = boardSerialNo;
            ComPort = comPort;
            BatteryPct = batteryPct;
            InitialFEN = initialFEN;
        }


        public void RunLiveChessServer()
        {
            server = new("ws://0.0.0.0:1982") { RestartAfterListenError = true };

            server.Start(socket =>
            {
                //socket.OnOpen = () => TextBoxConsole.AddLine($"OPEN");
                //socket.OnClose = () => TextBoxConsole.AddLine($"CLOSE");

                socket.OnMessage = message =>
                {
                    int idCount = 1;

                    if (message != null && message.Contains("call"))
                    {
                        if (message.Contains("eboards"))
                        {
                            socket.Send("{\"response\":\"call\",\"id\":1,\"param\":[{\"serialnr\":\"" +
                                    $"{BoardSerialNo}" +
                                    "\",\"source\":\"" +
                                    $"COM{ComPort}" +
                                    "\",\"state\":\"ACTIVE\",\"battery\":\"" +
                                    $"{BatteryPct}%" +
                                    "\",\"comment\":null,\"board\":\"" +
                                    $"{InitialFEN}" +
                                    "\",\"flipped\":false,\"clock\":null}],\"time\":"
                                    + $"{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}" +
                                    "}");
                        }
                        else if (message.Contains("subscribe"))
                        {
                            socket.Send("{\"response\":\"call\",\"id\":2,\"param\":null,\"time\":1668045228663}");
                            socket.Send("{\"response\":\"feed\",\"id\":" +
                                        $"{idCount++}" +
                                        ",\"param\":" + "{" + $"\"serialnr\":\"" +
                                        $"{BoardSerialNo}" +
                                        "\",\"flipped\":false,\"board\":\"" +
                                        $"{InitialFEN}" +
                                        "\",\"clock\":null" + "}" + ",\"time\":" +
                                        $"{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}" +
                                        "}");

                            _dgtEbDllFacade.OnFenChanged += (object sender, FenChangedEventArgs e) =>
                            {
                                var whiteCount = e.FEN.Where(c => char.IsUpper(c)).Count();
                                var blackCount = e.FEN.Where(c => char.IsLower(c)).Count();
                                var kingCount = e.FEN.Where(c => (c == 'k' || c == 'K')).Count();


                                //Don't send boards with no kings - always invalid
                                if (kingCount == 2) { lastFenSeen = e.FEN; }

                                //socket.Send("{" + $"\"response\":\"feed\",\"id\":{idCount++},\"param\":" + "{" + $"\"serialnr\":\"24958\",\"flipped\":false,\"board\":\"{lastFenSeen}\",\"clock\":null" + "}" + ",\"time\":\""+ DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + "\"}");
                            };


                            while (true)
                            {
                                //socket.Send("{" + $"\"response\":\"feed\",\"id\":{idCount++},\"param\":" + "{" + $"\"serialnr\":\"24958\",\"flipped\":false,\"board\":\"{lastFenSeen}\",\"clock\":null" + "}" + ",\"time\":\"" + DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + "\"}");
                                socket.Send("{" + $"\"response\":\"feed\",\"id\":{idCount++},\"param\":" + "{" + $"\"serialnr\":\"24958\",\"flipped\":false,\"board\":\"{lastFenSeen}\",\"clock\":null" + "}" + ",\"time\":1668045228666" + "}");
                                Thread.Sleep(250);
                            }
                        }
                    }
                };

            });
        }
    }
}
