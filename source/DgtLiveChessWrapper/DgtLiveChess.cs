using System;
using System.IO;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DgtLiveChessWrapper
{
    public sealed class MessageRecievedEventArgs : EventArgs
    {
        public string ResponseOut { get; init; }
    }

    public interface IDgtLiveChess
    {
        event EventHandler<MessageRecievedEventArgs> OnBatteryOk;
        event EventHandler<MessageRecievedEventArgs> OnBatteryLow;
        event EventHandler<MessageRecievedEventArgs> OnBatteryCritical;
        event EventHandler<MessageRecievedEventArgs> OnLiveChessConnected;
        event EventHandler<MessageRecievedEventArgs> OnLiveChessDisconnected;
        event EventHandler<MessageRecievedEventArgs> OnBoardConnected;
        event EventHandler<MessageRecievedEventArgs> OnBoardDisconnected;
        event EventHandler<MessageRecievedEventArgs> OnError;
        event EventHandler<MessageRecievedEventArgs> OnFenRecieved;
        event EventHandler<MessageRecievedEventArgs> OnCantFindBoard;

        Task PollDgtBoard();
    }

    public sealed class DgtLiveChess : IDgtLiveChess
    {
        // API Docs - Live Chess bust be installed and running
        // http://localhost:1982/doc/api/feeds/eboardevent/index.html

        private const string LIVE_CHESS_URL = "ws://127.0.0.1:1982/api/v1.0";

        private const int CONNECTION_RETRY_DELAY = 10000;
        private const int BOARD_POLL_RETRY_DELAY = 5000;

        private const string BOARD_CONECTED_STATUS = "ACTIVE";
        private const int BATTERY_LOW_THRESHOLD = 40;
        private const int BATTERY_CRIT_THRESHOLD = 25;

        private const string CALL_EBAORDS = "{{\"call\": \"eboards\",\"id\": {0},\"param\": null}}";
        private const string CALL_SUBSCRIBE = "{{ \"call\": \"subscribe\", \"id\": {0}, \"param\": {{ \"feed\": \"eboardevent\",\"id\": {1},\"param\": {{\"serialnr\": \"{2}\"}}}}}}";

        public event EventHandler<MessageRecievedEventArgs> OnLiveChessConnected;
        public event EventHandler<MessageRecievedEventArgs> OnLiveChessDisconnected;
        public event EventHandler<MessageRecievedEventArgs> OnBoardConnected;
        public event EventHandler<MessageRecievedEventArgs> OnBoardDisconnected;
        public event EventHandler<MessageRecievedEventArgs> OnError;
        public event EventHandler<MessageRecievedEventArgs> OnBatteryOk;
        public event EventHandler<MessageRecievedEventArgs> OnBatteryLow;
        public event EventHandler<MessageRecievedEventArgs> OnBatteryCritical;
        public event EventHandler<MessageRecievedEventArgs> OnFenRecieved;
        public event EventHandler<MessageRecievedEventArgs> OnCantFindBoard;

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
                    await ConnectAndWatch();
                }
                catch (WebSocketException ex)
                {
                    OnLiveChessDisconnected?.Invoke(this, new MessageRecievedEventArgs() { ResponseOut = $"Connection to Live Chess Unavailable ({ex.WebSocketErrorCode})" });
                }
                catch (InvalidOperationException)
                {
                    OnLiveChessDisconnected?.Invoke(this, new MessageRecievedEventArgs() { ResponseOut = $"Connection to Live Chess Unavailable (Terminated by Invalid Opperation)" });
                }
                catch (LiveChessDisconnectedException)
                {
                    OnLiveChessDisconnected?.Invoke(this, new MessageRecievedEventArgs() { ResponseOut = $"Connection to Live Chess Closed (Terminated by Disconnection)" });
                }
                catch (BoardDisconnectedException)
                {
                    OnBoardDisconnected?.Invoke(this, new MessageRecievedEventArgs() { ResponseOut = $"Connection to DGT Board Lost" });
                }
                catch (Exception ex)
                {
                    OnError?.Invoke(this, new MessageRecievedEventArgs() { ResponseOut = $" ERROR: {ex.GetType()}{ex.Message}" });
                }

                //Wait and then try again
                await Task.Delay(CONNECTION_RETRY_DELAY);
            }
        }

        /// <summary>
        /// Establish a connection and watch for board changes
        /// </summary>
        /// <returns></returns>
        private async Task ConnectAndWatch()
        {
            int idCount = 0;
            string watchdSerialNumber;
            bool reportNoActiveBoards = true;

            //Open a websocket to DGT LiveChess (running on the local machine)
            using ClientWebSocket socket = new();
            await socket.ConnectAsync(new Uri(LIVE_CHESS_URL), CancellationToken.None).ConfigureAwait(false);

            OnLiveChessConnected?.Invoke(this, new MessageRecievedEventArgs() { ResponseOut = "Connected to Live Chess" });

            // Loop until we have a board to watch
            for (; ; )
            {
                //First get a list of eBoards...
                await Send(socket, string.Format(CALL_EBAORDS, ++idCount));
                (string eboardsJsonString, DgtLiveChessJson.CallResponse.LiveChessCallResponse eboardsResponse) = DgtLiveChessWrapper.DgtLiveChessJson.CallResponse.LiveChessCallResponse.Deserialize(await Receive(socket, false));

                //...then find the first active board if we have one...
                DgtLiveChessJson.CallResponse.LiveChessCallParams activeBoard = eboardsResponse.Boards.FirstOrDefault(x => x.ConnectionState == BOARD_CONECTED_STATUS);

                if (activeBoard == null)
                {
                    //...if no active boards are available wait and try again...
                    if (reportNoActiveBoards)
                    {
                        OnCantFindBoard?.Invoke(this, new MessageRecievedEventArgs() { ResponseOut = "No Board found - is it switched on?" });
                        reportNoActiveBoards = false;
                    }
                    await Task.Delay(BOARD_POLL_RETRY_DELAY);
                }
                else
                {
                    //...if we have a board to watch break out and start watching...
                    watchdSerialNumber = activeBoard.SerialNumber;
                    reportNoActiveBoards = true;
                    OnBoardConnected?.Invoke(this, new MessageRecievedEventArgs() { ResponseOut = $"Connected to Board {watchdSerialNumber} [State={activeBoard.ConnectionState}]" });

                    if (activeBoard.BatteryLevel != null && int.TryParse(activeBoard.BatteryLevel.Replace("%", "").Trim(), out int batteryLevel))
                    {
                        if (batteryLevel < BATTERY_CRIT_THRESHOLD) { OnBatteryCritical?.Invoke(this, new MessageRecievedEventArgs() { ResponseOut = $"Critical, your board needs to be charged! [Level={activeBoard.BatteryLevel}]" }); }
                        else if (batteryLevel < BATTERY_LOW_THRESHOLD) { OnBatteryLow?.Invoke(this, new MessageRecievedEventArgs() { ResponseOut = $"Warning, your boards battery is getting a bit low. [Level={activeBoard.BatteryLevel}]" }); }
                        else { OnBatteryOk?.Invoke(this, new MessageRecievedEventArgs() { ResponseOut = $"Your boards battery is OK. [Level={activeBoard.BatteryLevel}]" }); };
                    }
                    else
                    {
                        OnError?.Invoke(this, new MessageRecievedEventArgs() { ResponseOut = $"Warning, failed to parse the battery level!)" });
                    }

                    break;
                }
            }

            //...so set up a feed...
            await Send(socket, string.Format(CALL_SUBSCRIBE, ++idCount, ++idCount, watchdSerialNumber));
            (string feedSetupJsonString, DgtLiveChessJson.CallResponse.LiveChessCallResponse feedSetupResponse) = DgtLiveChessWrapper.DgtLiveChessJson.CallResponse.LiveChessCallResponse.Deserialize(await Receive(socket, true));

            //...and keep picking up board changes until the connection is closed
            for (; ; )
            {
                (string feedMsgJsonString, DgtLiveChessJson.FeedResponse.LiveChessFeedResponse feedMsgResponse) = DgtLiveChessWrapper.DgtLiveChessJson.FeedResponse.LiveChessFeedResponse.Deserialize(await Receive(socket, true));

                if (!string.IsNullOrWhiteSpace(feedMsgResponse.Param.Board))
                {
                    OnFenRecieved?.Invoke(this, new MessageRecievedEventArgs() { ResponseOut = $"{feedMsgResponse.Param.Board}" });
                }
            }
        }

        /// <summary>
        /// Send data
        /// </summary>
        /// <param name="socket"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        private static async Task Send(ClientWebSocket socket, string data)
        {
            await socket.SendAsync(Encoding.UTF8.GetBytes(data), WebSocketMessageType.Text, true, CancellationToken.None).ConfigureAwait(false);
        }

        /// <summary>
        /// Recieve data 
        /// </summary>
        /// <param name="socket"></param>
        /// <exception cref="DgtLiveChessWrapper.BoardDisconnectedException">Thrown is the socket is closed</exception>
        /// <returns>JSON Response String</returns>
        private static async Task<string> Receive(ClientWebSocket socket, bool isBoardConnected)
        {
            ArraySegment<byte> buffer = new(new byte[2048]);

            WebSocketReceiveResult result;
            using MemoryStream ms = new();
            do
            {
                result = await socket.ReceiveAsync(buffer, CancellationToken.None).ConfigureAwait(false);
                ms.Write(buffer.Array, buffer.Offset, result.Count);
            } while (!result.EndOfMessage);

            if (result.MessageType == WebSocketMessageType.Close)
            {
                if (isBoardConnected)
                {
                    throw new BoardDisconnectedException("WebSocket Closed");
                }
                else
                {
                    throw new LiveChessDisconnectedException("WebSocket Closed");
                }
            }
            else
            {
                ms.Seek(0, SeekOrigin.Begin);
                using StreamReader reader = new(ms, Encoding.UTF8);
                string response = await reader.ReadToEndAsync();

                return response;
            }
        }
    }
}
