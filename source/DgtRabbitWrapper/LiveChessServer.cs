using DgtRabbitWrapper.DgtEbDll;
using Fleck;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;

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
        private string broadcastFEN = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR";
        private readonly ConcurrentQueue<int> closedPortQueue = new();

        private readonly int randomSerialNo = 20000 + Random.Shared.Next(9999);

        public LiveChessServer(IDgtEbDllFacade dgtEbDllFacade, int boardSerialNo, int comPort, int batteryPct, string initialFEN)
        {
            _dgtEbDllFacade = dgtEbDllFacade;

            BoardSerialNo = boardSerialNo;
            ComPort = comPort;
            BatteryPct = batteryPct;
            InitialFEN = initialFEN;
        }

        private void SendToSocket(IWebSocketConnection clientSocket, string message)
        {
            try
            {
                OnLiveChessSrvMessage?.Invoke(this, $"OUT::{clientSocket.ConnectionInfo.ClientPort}::{message}");
                clientSocket.Send(message);
            }
            catch (Exception ex)
            {
                OnLiveChessSrvMessage?.Invoke(this, $"ERR::{ex.Message}");
            }
        }

        public void RunLiveChessServer()
        {
            Task.Run(() => { RunLiveChessServerInternal(); });
        }

        private void RunLiveChessServerInternal()
        {
            server = new("ws://0.0.0.0:1982") { RestartAfterListenError = true };
            server.ListenerSocket.NoDelay = true;

            _dgtEbDllFacade.OnStableFenChanged += (object sender, FenChangedEventArgs e) =>
            {
                if (LastFenSeen != e.FEN)
                {
                    LastFenSeen = e.FEN;

                    // LastUpdateTime = e.TimeChangedTicks;
                    //  WhiteCount = e.FEN.Where(c => char.IsUpper(c)).Count();
                    //  BlackCount = e.FEN.Where(c => char.IsLower(c)).Count();
                    KingCount = e.FEN.Where(c => (c == 'k' || c == 'K')).Count();

                    //Don't send boards with no kings - always invalid
                    if (KingCount == 2)
                    {
                        broadcastFEN = LastFenSeen;
                    }
                    else
                    {
                        OnLiveChessSrvMessage?.Invoke(this, "Fen dropped - missing king(s)");
                        return;
                    }
                }
            };

            server.Start(socket =>
            {
                socket.OnOpen = () => OnLiveChessSrvMessage?.Invoke(this, $"Session START from port {socket.ConnectionInfo.ClientPort}");
                socket.OnError = (error) => OnLiveChessSrvMessage?.Invoke(this, $"Session ERROR from port {socket.ConnectionInfo.ClientPort} >> {error}");

                socket.OnClose = () =>
                {
                    //The port closing does not close the session thread so queue the port number
                    //(always unique) on the clossing session.  Check in the session loop and if
                    //a session spots its own port it can dequeue it and break. 
                    closedPortQueue.Enqueue(socket.ConnectionInfo.ClientPort);
                    OnLiveChessSrvMessage?.Invoke(this, $"Session STOPPED from port {socket.ConnectionInfo.ClientPort}");
                };

                socket.OnMessage = message =>
                {
                    OnLiveChessSrvMessage?.Invoke(this, $"IN ::{socket.ConnectionInfo.ClientPort}::{message}");

                    if (message != null && message.Contains("call"))
                    {
                        if (message.Contains("eboards"))
                        {
                            SendToSocket(socket, "{\"response\":\"call\",\"id\":1,\"param\":[{\"serialnr\":\"BOARDNO\",\"source\":\"COM1\",\"state\":\"ACTIVE\",\"battery\":\"100%\",\"comment\":null,\"board\":\"FENFEN\",\"flipped\":false,\"clock\":null}],\"time\":TIMETIME}".Replace("BOARDNO", randomSerialNo.ToString()).Replace("FENFEN", broadcastFEN).Replace("TIMETIME", DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString()));
                        }
                        else if (message.Contains("subscribe"))
                        {
                            SendToSocket(socket, "{\"response\":\"call\",\"id\":2,\"param\":null,\"time\":TIMETIME}".Replace("TIMETIME", DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString()));
                            SendToSocket(socket, "{\"response\":\"feed\",\"id\":1,\"param\":{\"serialnr\":\"BOARDNO\",\"flipped\":false,\"board\":\"FENFEN\",\"clock\":null},\"time\":TIMETIME}".Replace("BOARDNO", randomSerialNo.ToString()).Replace("FENFEN", broadcastFEN).Replace("TIMETIME", DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString()));

                            string lastSend = "";
                            while (true)
                            {
                                _ = closedPortQueue.TryPeek(out var port);
                                if (port == socket.ConnectionInfo.ClientPort)
                                {
                                    _ = closedPortQueue.TryDequeue(out int result);
                                    break;
                                }
                                else
                                {
                                    if (broadcastFEN != lastSend)
                                    {
                                        lastSend = broadcastFEN;
                                        SendToSocket(socket, "{\"response\":\"feed\",\"id\":1,\"param\":{\"serialnr\":\"BOARDNO\",\"flipped\":false,\"board\":\"FENFENFEN\"},\"time\":TIMETIME}".Replace("BOARDNO",randomSerialNo.ToString()).Replace("FENFENFEN", broadcastFEN).Replace("TIMETIME", DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString()));
                                    }
                                    Thread.Sleep(200);
                                }
                            }
                        }
                    }
                };
            });
        }
    }
}
