﻿using DgtAngelShared.Json;
using Microsoft.Extensions.Logging;
using System;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using static DgtAngelShared.Json.CherubApiMessage;

namespace DgtAngel.Services
{
    public class CherubConnectionEventArgs : EventArgs
    {
        public string ResponseOut { get; set; }
    }

    public interface ICherubConnectionManager
    {
        event EventHandler<CherubConnectionEventArgs> OnCherubConnected;
        event EventHandler<CherubConnectionEventArgs> OnCherubDisconnected;
        event EventHandler<CherubConnectionEventArgs> OnError;

        Task ConnectAndWatch();
        Task SendDgtAngelDisconnectedToCherubClient();
        Task SendMessageToCherubClient(string message);
        Task SendUpdatedBoardStateCherubClient(BoardState remoteBoardState);
        Task StartCherubConnection();
    }

    public sealed class CherubConnectionManager : ICherubConnectionManager
    {
        const string CHERUB_API_WS_PATH = "/ws";
        const string CHERUB_API_WS_HOST = "ws://localhost:37964";

        private const int CONNECTION_RETRY_DELAY = 10000;
        private const int BOARD_POLL_RETRY_DELAY = 5000;

        public event EventHandler<CherubConnectionEventArgs> OnCherubConnected;
        public event EventHandler<CherubConnectionEventArgs> OnCherubDisconnected;
        public event EventHandler<CherubConnectionEventArgs> OnError;

        private readonly ILogger _logger;
        private ClientWebSocket _socket = null;

        private readonly string keepAliveMessage = JsonSerializer.Serialize<CherubApiMessage>(new CherubApiMessage()
        {
            Source = "ANGEL",
            MessageType = MessageTypeCode.KEEP_ALIVE,
            Message = "",
            RemoteBoard = null
        });

        public CherubConnectionManager(ILogger<CherubConnectionManager> logger)
        {
            _logger = logger;
        }

        public async Task StartCherubConnection()
        {
            _logger?.LogInformation($"Starting Chrub Connection Manager");

            for (; ; )
            {
                try
                {
                    await ConnectAndWatch();
                }
                catch (WebSocketException ex)
                {
                    _logger?.LogInformation($"Cherub Connection :: {ex.Message}");
                    //OnCherubDisconnected?.Invoke(this, new CherubConnectionEventArgs() { ResponseOut = $"Connection to Cherub Unavailable ({ex.WebSocketErrorCode})" });
                }
                catch (InvalidOperationException ex)
                {
                    //OnCherubDisconnected?.Invoke(this, new CherubConnectionEventArgs() { ResponseOut = $"Connection to Cherub Unavailable (Terminated by Invalid Opperation)" });
                    _logger?.LogInformation($"Cherub Connection :: {ex.Message}");
                }
                catch (Exception ex)
                {
                    //OnError?.Invoke(this, new CherubConnectionEventArgs() { ResponseOut = $" ERROR: {ex.GetType()}{ex.Message}" });
                    _logger?.LogInformation($"Cherub Connection :: {ex.Message}");
                }

                //Wait and then try again
                _socket = null;
                await Task.Delay(CONNECTION_RETRY_DELAY);
            }
        }

        public async Task ConnectAndWatch()
        {
            //Open a websocket to DGT Cherub (running on the local machine)
            _socket = new();

            do
            {
                await _socket.ConnectAsync(new Uri($"{CHERUB_API_WS_HOST}{CHERUB_API_WS_PATH}"), CancellationToken.None).ConfigureAwait(false);
            } while (_socket.State != WebSocketState.Open);

            OnCherubConnected?.Invoke(this, new CherubConnectionEventArgs() { ResponseOut = "Connected to Cherub" });

            for (; ; )
            {
                await SendJsonToCherubClient(keepAliveMessage);
                await Task.Delay(30000);
            }
        }

        // These method are fire and forget - if Cherub isn't there that's fine - just log
        private async Task SendJsonToCherubClient(string message)
        {
            try
            {
                if (_socket != null && _socket.State == WebSocketState.Open)
                {
                    await _socket.SendAsync(Encoding.UTF8.GetBytes(message),
                                            WebSocketMessageType.Text,
                                            true,
                                            CancellationToken.None);
                }
            }
            catch (Exception ex) { _logger?.LogInformation($"Failed to send to Cherub>> {_socket?.State} :: {ex.Message}"); }
        }

        public async Task SendMessageToCherubClient(string message)
        {
            await SendJsonToCherubClient(JsonSerializer.Serialize<CherubApiMessage>(new CherubApiMessage()
            {
                Source = "ANGEL",
                MessageType = MessageTypeCode.MESSAGE,
                Message = message,
                RemoteBoard = null
            }));
        }

        public async Task SendUpdatedBoardStateCherubClient(BoardState remoteBoardState)
        {
            _logger.LogInformation($"Sending FEN [{remoteBoardState.Board.FenString}]  to Cherub");
            await SendJsonToCherubClient(JsonSerializer.Serialize<CherubApiMessage>(new CherubApiMessage()
            {
                Source = "ANGEL",
                MessageType = MessageTypeCode.FEN_UPDATE,
                Message = "",
                RemoteBoard = remoteBoardState
            }));
        }


        public async Task SendDgtAngelDisconnectedToCherubClient()
        {
            _logger.LogInformation($"Sending DGT Angel disconnect to Cherub");

            await SendJsonToCherubClient(JsonSerializer.Serialize<CherubApiMessage>(new CherubApiMessage()
            {
                Source = "ANGEL",
                MessageType = MessageTypeCode.MESSAGE,
                Message = "DGT Angel Disconnected",
                RemoteBoard = null
            }));
        }
    }
}

