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

        public event EventHandler<string> OnLiveChessSrvMessage;

        public int BoardSerialNo { get; init; }
        public int ComPort { get; init; }
        public int BatteryPct { get; init; }
        public string InitialFEN { get; init; }

        public string LastFenSeen { get; private set; } = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR";
        public int WhiteCount { get; private set; } = 0;
        public int BlackCount { get; private set; } = 0;
        public int KingCount { get; private set; } = 0;

        public long LastUpdateTime { get; private set; } = long.MinValue;

        private object _newFenLock = new object();

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
                socket.OnOpen = () => OnLiveChessSrvMessage?.Invoke(this, "Server START");
                socket.OnClose = () => OnLiveChessSrvMessage?.Invoke(this, "Server STOPPED");

                socket.OnMessage = message =>
                {
                    int idCount = 1;

                    OnLiveChessSrvMessage?.Invoke(this, "Client connection start");

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
                                //lock (_newFenLock)
                                //{
                                    if (e.TimeChangedTicks > LastUpdateTime)
                                    {
                                        LastUpdateTime = e.TimeChangedTicks;
                                        WhiteCount = e.FEN.Where(c => char.IsUpper(c)).Count();
                                        BlackCount = e.FEN.Where(c => char.IsLower(c)).Count();
                                        KingCount = e.FEN.Where(c => (c == 'k' || c == 'K')).Count();


                                        //Don't send boards with no kings - always invalid
                                        if (KingCount == 2) { 
                                            LastFenSeen = e.FEN;
                                        }
                                        else
                                        {
                                            OnLiveChessSrvMessage?.Invoke(this, "Fen dropped - missing king(s)");
                                        }

                                        //socket.Send("{" + $"\"response\":\"feed\",\"id\":{idCount++},\"param\":" + "{" + $"\"serialnr\":\"24958\",\"flipped\":false,\"board\":\"{lastFenSeen}\",\"clock\":null" + "}" + ",\"time\":\""+ DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + "\"}");
                                    }
                                    else
                                    {
                                        OnLiveChessSrvMessage?.Invoke(this, "LiveSrv: Fen dropped - message too late");
                                    }
                                //}
                            };


                            while (true)
                            {
                                //socket.Send("{" + $"\"response\":\"feed\",\"id\":{idCount++},\"param\":" + "{" + $"\"serialnr\":\"24958\",\"flipped\":false,\"board\":\"{lastFenSeen}\",\"clock\":null" + "}" + ",\"time\":\"" + DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + "\"}");
                                socket.Send("{" + $"\"response\":\"feed\",\"id\":{idCount++},\"param\":" + "{" + $"\"serialnr\":\"24958\",\"flipped\":false,\"board\":\"{LastFenSeen}\",\"clock\":null" + "}" + ",\"time\":1668045228666" + "}");
                                Thread.Sleep(250);
                            }
                        }
                    }
                };

            });
        }
    }
}
