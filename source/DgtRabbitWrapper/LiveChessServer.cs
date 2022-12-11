using ChessHelpers;
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
        private const string FROM_INTERNAL_MSG_ID = ": 999";
        private const string SERVER_ADDR = "ws://0.0.0.0:1982";
        private const string CONNECT_01_MSG = "{\"response\":\"call\",\"id\":1,\"param\":[{\"serialnr\":\"BOARDNO\",\"source\":\"COM1\",\"state\":\"ACTIVE\",\"battery\":\"100%\",\"comment\":null,\"board\":\"FENFEN\",\"flipped\":false,\"clock\":null}],\"time\":TIMETIME}";
        private const string CONNECT_02_MSG = "{\"response\":\"call\",\"id\":2,\"param\":null,\"time\":TIMETIME}";
        private const string CONNECT_03_MSG = "{\"response\":\"feed\",\"id\":1,\"param\":{\"serialnr\":\"BOARDNO\",\"flipped\":false,\"board\":\"FENFEN\",\"clock\":null},\"time\":TIMETIME}";
        private const string NEWPOS_MSG = "{\"response\":\"feed\",\"id\":1,\"param\":{\"serialnr\":\"BOARDNO\",\"flipped\":false,\"board\":\"FENFEN\"},\"time\":TIMETIME}";


        public enum PlayDropFix { NONE, FROMWHITE, FROMBLACK };
        public event EventHandler<string> OnLiveChessSrvMessage;

        public string RemoteFEN { get; set; }
        public string SideToPlay { get; set; }
        public bool BlockSendToRemote { get; set; }
        public int BoardSerialNo { get; init; }
        public int ComPort { get; init; }
        public int BatteryPct { get; init; }
        public string InitialFEN { get; init; }
        public string LastFenSeen { get; private set; } = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR";
        public int WhiteCount { get; private set; } = 0;
        public int BlackCount { get; private set; } = 0;
        public int KingCount { get; private set; } = 0;
        public long LastUpdateTime { get; private set; } = long.MinValue;

        private readonly ConcurrentQueue<int> _closedPortQueue = new();
        private readonly int _randomSerialNo = 20000 + Random.Shared.Next(9999);
        private readonly IDgtEbDllFacade _dgtEbDllFacade;

        private WebSocketServer _server;
        private string _broadcastFenCorrected = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR";
        //private string _broadcastFEN = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR";
        private string _broadcastFEN = "8/8/8/8/8/8/8/8";
        private PlayDropFix _dropFix = PlayDropFix.NONE; //This is a fix for the play board on CDC...

        public PlayDropFix DropFix
        {
            get => _dropFix;
            set
            {
                _dropFix = value;
                OnLiveChessSrvMessage?.Invoke(this, "--------------------------------------------");
                OnLiveChessSrvMessage?.Invoke(this, $"'Play' board correction->{_dropFix}");
                OnLiveChessSrvMessage?.Invoke(this, "--------------------------------------------");
            }
        }

        public LiveChessServer(IDgtEbDllFacade dgtEbDllFacade, int boardSerialNo, int comPort, int batteryPct, string initialFEN)
        {
            _dgtEbDllFacade = dgtEbDllFacade;

            BoardSerialNo = boardSerialNo;
            ComPort = comPort;
            BatteryPct = batteryPct;
            InitialFEN = initialFEN;
        }


        private void SendToSocket(IWebSocketConnection clientSocket, string message, bool isSendToLog = true)
        {
            try
            {
                if (isSendToLog)
                {
                    OnLiveChessSrvMessage?.Invoke(this, $"OUT::{clientSocket.ConnectionInfo.ClientPort}::{message}");
                }
                    
                _ = clientSocket.Send(message);
            }
            catch (Exception ex)
            {
                OnLiveChessSrvMessage?.Invoke(this, $"ERR::{ex.Message}");
            }
        }

        public void RunLiveChessServer()
        {
            _ = Task.Run(RunLiveChessServerInternal);
        }

        private string FormatMessage(string message, string fen = "")
        {
            return message.Replace("BOARDNO", _randomSerialNo.ToString())
                          .Replace("FENFEN", fen)
                          .Replace("TIMETIME", DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
                          .ToString());
        }

        private void RunLiveChessServerInternal()
        {
            _server = new(SERVER_ADDR) { RestartAfterListenError = true };
            _server.ListenerSocket.NoDelay = true;

            //Setup connection the Rabbit
            _dgtEbDllFacade.OnStableFenChanged += (object sender, FenChangedEventArgs e) =>
            {
                if (LastFenSeen != e.FEN)
                {
                    LastFenSeen = e.FEN;
                    KingCount = e.FEN.Where(c => c is 'k' or 'K').Count();

                    //Don't send boards with missing kings - always invalid
                    //This fixes castling
                    if (KingCount == 2)
                    {
                        //if the drop fix is setup apply it - otherwise just set the fen
                        _broadcastFenCorrected = DropFix switch
                        {
                            PlayDropFix.FROMWHITE => LastFenSeen.GenrateAlwaysValidMovesFEN(true),
                            PlayDropFix.FROMBLACK => LastFenSeen.GenrateAlwaysValidMovesFEN(false),
                            _ => LastFenSeen,
                        };

                        _broadcastFEN = LastFenSeen;
                    }
                    else
                    {
                        OnLiveChessSrvMessage?.Invoke(this, "FIX:: Dropped fen   -> Missing king(s)");
                        return;
                    }
                }
            };

            //Start listening for external connections
            _server.Start(socket =>
            {
                socket.OnOpen = () => OnLiveChessSrvMessage?
                                            .Invoke(this, $"Session START from port {socket.ConnectionInfo.ClientPort}");

                socket.OnError = (error) => OnLiveChessSrvMessage?
                                            .Invoke(this, $"Session ERROR from port {socket.ConnectionInfo.ClientPort} >> {error}");

                socket.OnClose = () =>
                {
                    //The port closing does not close the session thread so queue the port number
                    //(always unique) on the clossing session.  Check in the session loop and if
                    //a session spots its own port it can dequeue it and break. 
                    _closedPortQueue.Enqueue(socket.ConnectionInfo.ClientPort);
                    OnLiveChessSrvMessage?
                            .Invoke(this, $"Session STOPPED from port {socket.ConnectionInfo.ClientPort}");
                };

                socket.OnMessage = message =>
                {
                    OnLiveChessSrvMessage?
                            .Invoke(this, $"IN ::{socket.ConnectionInfo.ClientPort}::{message}");
                    ProcessMessages(socket, message);
                };
            });
        }

        private void ProcessMessages(IWebSocketConnection socket, string message)
        {
            if (message != null &&
                message.Contains("call") &&
                message.Contains("eboards"))
            {
                SendToSocket(socket, FormatMessage(CONNECT_01_MSG, _broadcastFEN));
            }
            else if (message != null &&
                     message.Contains("call") &&
                     message.Contains("subscribe"))
            {
                SendToSocket(socket, FormatMessage(CONNECT_02_MSG));
                SendToSocket(socket, FormatMessage(CONNECT_03_MSG, _broadcastFEN));

                string lastSend = "";
                while (true)
                {
                    //Check for client disconnect and drop the stream if required
                    _ = _closedPortQueue.TryPeek(out int port);
                    if (port == socket.ConnectionInfo.ClientPort)
                    {
                        _ = _closedPortQueue.TryDequeue(out _);
                        break;
                    }
                    else
                    {
                        lastSend = SendBoardUpdate(socket, message, lastSend);
                        Thread.Sleep(800); //reduce messages out
                    }
                }
            }
        }
        

        private string SendBoardUpdate(IWebSocketConnection socket, string message, string lastSend)
        {
            //Send the remote board back to CDC to confirm the turn
            //If we don't it will randomly refuse to accept moves
            if (!message.ToString().Contains(FROM_INTERNAL_MSG_ID) && DropFix != PlayDropFix.NONE)
            {
                SendToSocket(socket, FormatMessage(NEWPOS_MSG, this.RemoteFEN), false);
            }

            if (_broadcastFEN != lastSend)
            {
                lastSend = _broadcastFEN;

                //Always send the real FEN to Cherub
                //Always send the real FEN if not in Dropfix mode
                if (message.ToString().Contains(FROM_INTERNAL_MSG_ID) || DropFix == PlayDropFix.NONE)
                {
                    if (_broadcastFenCorrected != _broadcastFEN &&
                        !message.ToString().Contains(FROM_INTERNAL_MSG_ID))
                    {
                        OnLiveChessSrvMessage?.Invoke(this, $"'Play' board fix sending [{_broadcastFenCorrected}]");
                    }

                    SendToSocket(socket, FormatMessage(NEWPOS_MSG, _broadcastFEN));
                }
                else
                {
                    //Dropfix mode so test if this is a valid move before sending...
                    string _currentRemoteFen = this.RemoteFEN;
                    string _currentSideToPlay = this.SideToPlay;

                    var (move, ending, turn) = ChessHelpers.PositionDiffCalculator
                                              .CalculateSanFromFen(_currentRemoteFen, _broadcastFEN);

                    //string invertedTurn = (turn == "") ? "" : ((turn == "WHITE") ? "BLACK" : "WHITE");
                    //string invertedTurn = turn;

                    if (string.IsNullOrEmpty(move))
                    {
                        OnLiveChessSrvMessage?.Invoke(this, $"FIX:: Dropped fen   -> Invalid Move [{move}]");
                    }
                    else if(_currentSideToPlay != turn)
                    {
                        OnLiveChessSrvMessage?.Invoke(this, $"FIX:: Dropped fen   -> Expected [{_currentSideToPlay}] but detected [{turn}]");
                    }
                    else if (string.IsNullOrEmpty(move))
                    {
                        OnLiveChessSrvMessage?.Invoke(this, $"FIX:: Dropped fen   -> No move from [{_currentRemoteFen}] to [{_broadcastFEN}]");
                    }
                    else
                    {
                        //Make sure that the remote FEN is definatly sent
                        SendToSocket(socket, FormatMessage(NEWPOS_MSG, this.RemoteFEN), false);
                        OnLiveChessSrvMessage?.Invoke(this, $"FIX:: Corrected fen ->  [{_broadcastFenCorrected}] [{move}] [{_currentSideToPlay}] [{turn}]");
                        SendToSocket(socket, FormatMessage(NEWPOS_MSG, _broadcastFenCorrected));
                    }
                }
            }
            
            return lastSend;
        }
    }
}
