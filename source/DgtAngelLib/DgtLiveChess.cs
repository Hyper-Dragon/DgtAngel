using System;
using System.IO;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DgtAngelLib
{
    public class MessageRecievedEventArgs : EventArgs
    {
        public string ResponseOut { get; set; }
    }

    public interface IDgtLiveChess
    {
        event EventHandler<MessageRecievedEventArgs> OnBatteryLow;
        event EventHandler<MessageRecievedEventArgs> OnLiveChessConnected;
        event EventHandler<MessageRecievedEventArgs> OnLiveChessDisconnected;
        event EventHandler<MessageRecievedEventArgs> OnBoardConnected;
        event EventHandler<MessageRecievedEventArgs> OnBoardDisconnected;
        event EventHandler<MessageRecievedEventArgs> OnError;
        event EventHandler<MessageRecievedEventArgs> OnResponseRecieved;

        Task ConnectAndWatch();
        Task PollDgtBoard();
    }

    public class DgtLiveChess : IDgtLiveChess
    {
        // API Docs - Live Chess bust be installed and running
        // http://localhost:1982/doc/api/feeds/eboardevent/index.html

        private const string LIVE_CHESS_URL = "ws://127.0.0.1:1982/api/v1.0";

        private const int CONNECTION_RETRY_DELAY = 10000;
        private const int BOARD_POLL_RETRY_DELAY = 10000;

        private const string BOARD_CONECTED_STATUS = "ACTIVE";

        private const string CALL_EBAORDS = "{{\"call\": \"eboards\",\"id\": {0},\"param\": null}}";
        private const string CALL_SUBSCRIBE = "{{ \"call\": \"subscribe\", \"id\": {0}, \"param\": {{ \"feed\": \"eboardevent\",\"id\": {1},\"param\": {{\"serialnr\": \"{2}\"}}}}}}";

        public event EventHandler<MessageRecievedEventArgs> OnLiveChessConnected;
        public event EventHandler<MessageRecievedEventArgs> OnLiveChessDisconnected;
        public event EventHandler<MessageRecievedEventArgs> OnBoardConnected;
        public event EventHandler<MessageRecievedEventArgs> OnBoardDisconnected;
        public event EventHandler<MessageRecievedEventArgs> OnError;
        public event EventHandler<MessageRecievedEventArgs> OnBatteryLow;
        public event EventHandler<MessageRecievedEventArgs> OnResponseRecieved;

        /// <summary>
        /// Establish and maintain a connection to a DGT Board via Live Chess
        /// DO USE AWAIT - This method is self contained and loops forever 
        /// </summary>
        public async Task PollDgtBoard()
        {
            for (; ; )
            {
                try
                {
                    await this.ConnectAndWatch();
                }
                catch (BoardDisconnectedException)
                {
                    OnBoardDisconnected?.Invoke(this, new MessageRecievedEventArgs() { ResponseOut = $"Connection to DGT Board Cloesd" });
                    OnLiveChessDisconnected?.Invoke(this, new MessageRecievedEventArgs() { ResponseOut = $"Connected to Live Chess Closed" });
                    
                }
                catch (Exception ex)
                {
                    OnResponseRecieved?.Invoke(this, new MessageRecievedEventArgs() { ResponseOut = $"ERROR: {ex.GetType()}{ex.Message}" });
                }

                //Wait and then try again
                await Task.Delay(CONNECTION_RETRY_DELAY);
            }
        }

        /// <summary>
        /// Establish a connection and watch for board changes
        /// </summary>
        /// <returns></returns>
        public async Task ConnectAndWatch()
        {
            int idCount = 0;
            string watchdSerialNumber;

            //Open a websocket to DGT LiveChess (running on the local machine)
            using var socket = new ClientWebSocket();
            await socket.ConnectAsync(new Uri(LIVE_CHESS_URL), CancellationToken.None);

            OnLiveChessConnected.Invoke(this, new MessageRecievedEventArgs() { ResponseOut = "Connected to Live Chess" });

            // Loop until we have a board to watch
            for (; ; )
            {
                //First get a list of eBoards...
                await Send(socket, string.Format(CALL_EBAORDS, ++idCount));
                var (eboardsJsonString, eboardsResponse) = DgtAngelLib.DgtLiveChessJson.CallResponse.Rootobject.Deserialize(await Receive(socket));

                //...then find the first active board if we have one...
                var activeBoard = eboardsResponse.Boards.FirstOrDefault(x => x.ConnectionState == BOARD_CONECTED_STATUS);

                if (activeBoard == null)
                {
                    //...if no active boards are available wait and try again...
                    OnResponseRecieved?.Invoke(this, new MessageRecievedEventArgs() { ResponseOut = "Connected but no active boards found!" });
                    await Task.Delay(BOARD_POLL_RETRY_DELAY);
                }
                else
                {
                    //...if we have a board to watch break out and start watching...
                    watchdSerialNumber = activeBoard.SerialNumber;
                    OnBoardConnected?.Invoke(this, new MessageRecievedEventArgs() { ResponseOut = $"Connected to {watchdSerialNumber}:{activeBoard.ConnectionState}" });
                    break;
                }
            }

            //...so set up a feed...
            await Send(socket, string.Format(CALL_SUBSCRIBE, ++idCount, ++idCount, watchdSerialNumber));
            var (feedSetupJsonString, feedSetupResponse) = DgtAngelLib.DgtLiveChessJson.CallResponse.Rootobject.Deserialize(await Receive(socket));
            OnResponseRecieved?.Invoke(this, new MessageRecievedEventArgs() { ResponseOut = feedSetupJsonString });

            //...and keep picking up board changes until the connection is closed
            for (; ; )
            {
                var (feedMsgJsonString, feedMsgResponse) = DgtAngelLib.DgtLiveChessJson.FeedResponse.Rootobject.Deserialize(await Receive(socket));
                OnResponseRecieved?.Invoke(this, new MessageRecievedEventArgs() { ResponseOut = $"Board Fen {feedMsgResponse.Param.Board}" });
            }
        }

        /// <summary>
        /// Send data
        /// </summary>
        /// <param name="socket"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        private static async Task Send(ClientWebSocket socket, string data) =>
                               await socket.SendAsync(Encoding.UTF8.GetBytes(data), WebSocketMessageType.Text, true, CancellationToken.None);

        /// <summary>
        /// Recieve data 
        /// </summary>
        /// <param name="socket"></param>
        /// <exception cref="DgtAngelLib.BoardDisconnectedException">Thrown is the socket is closed</exception>
        /// <returns>JSON Response String</returns>
        private static async Task<string> Receive(ClientWebSocket socket)
        {
            var buffer = new ArraySegment<byte>(new byte[2048]);

            WebSocketReceiveResult result;
            using var ms = new MemoryStream();
            do
            {
                result = await socket.ReceiveAsync(buffer, CancellationToken.None);
                ms.Write(buffer.Array, buffer.Offset, result.Count);
            } while (!result.EndOfMessage);

            if (result.MessageType == WebSocketMessageType.Close)
            {
                throw new BoardDisconnectedException("WebSocket Closed");
            }
            else
            {
                ms.Seek(0, SeekOrigin.Begin);
                using var reader = new StreamReader(ms, Encoding.UTF8);
                string response = await reader.ReadToEndAsync();

                return response;
            }
        }
    }
}
