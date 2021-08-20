using DgtAngelShared.Json;
using DgtCherub.Services;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace DgtCherub.Controllers
{
    public sealed class CherubWebSocketApiController : ControllerBase
    {
        private const int RECIEVE_BUFFER_SIZE_BYTES = 1024*10;

        private bool isAcceptingConnections = true;

        private readonly ILogger _logger;
        private readonly IAppDataService _appDataService;

        public CherubWebSocketApiController(ILogger<CherubWebSocketApiController> logger, IAppDataService appData)
        {
            _logger = logger;
            _appDataService = appData;
        }

        [HttpGet("/ws")]
        public async Task Get()
        {
            if (HttpContext.WebSockets.IsWebSocketRequest && isAcceptingConnections)
            {
                using WebSocket webSocket = await HttpContext.WebSockets.AcceptWebSocketAsync();
                _appDataService.UserMessageArrived("INTERNAL", "DGT Angel Connected");

                while (webSocket.State == WebSocketState.Open)
                {
                    try
                    {
                        ArraySegment<byte> buffer = new(new byte[RECIEVE_BUFFER_SIZE_BYTES]);
                        WebSocketReceiveResult result;
                        List<byte> allBytes = new();

                        do
                        {
                            //TODO: Make read more efficient and use the canx token on shutdown
                            result = await webSocket.ReceiveAsync(buffer, CancellationToken.None);
                            for (int i = 0; i < result.Count; i++)
                            {
                                allBytes.Add(buffer.Array[i]);
                            }
                        }
                        while (!result.EndOfMessage);

                        try
                        {
                            // Convert to a string (UTF-8 encoding).
                            CherubApiMessage messageIn = JsonSerializer.Deserialize<CherubApiMessage>(Encoding.UTF8.GetString(allBytes.ToArray(), 0, allBytes.Count));
                            _logger.LogDebug($"Cherub Keep-Alive from {messageIn.Source}");

                            switch (messageIn.MessageType)
                            {
                                case CherubApiMessage.MessageTypeCode.STATE_UPDATED:
                                    //_appDataService.UserMessageArrived("INGEST", messageIn.RemoteBoard.Board.FenString);
                                    _appDataService.RemoteBoardUpdated(messageIn.RemoteBoard);
                                    break;
                                case CherubApiMessage.MessageTypeCode.KEEP_ALIVE:
                                    //_appDataService.UserMessageArrived("INGEST", "Keep Alive PING Arrived");
                                    await webSocket.SendAsync(Encoding.UTF8.GetBytes("PONG"),
                                                              WebSocketMessageType.Text,
                                                              true,
                                                              CancellationToken.None);
                                    //_appDataService.UserMessageArrived("INGEST", "Keep Alive PONG Sent");
                                    break;
                                case CherubApiMessage.MessageTypeCode.MESSAGE:
                                    _appDataService.UserMessageArrived(messageIn.Source, messageIn.Message);
                                    break;
                                case CherubApiMessage.MessageTypeCode.WATCH_STOPPED:
                                    //_appDataService.UserMessageArrived("INGEST", "DGT Angel stopped watching the remote board");
                                    _appDataService.ResetRemoteBoardState();
                                    _appDataService.UserMessageArrived(messageIn.Source, $"DGT Angel stopped watching the remote board.");
                                    break;
                                default:
                                    _appDataService.UserMessageArrived("INTERNAL", "ERROR: Unknown MESSAGE TYPE recieved on the API!");
                                    _logger.LogError($"Unknown MESSAGE TYPE recieved on the API!");
                                    break;
                            }
                        }
                        catch (Exception ex)
                        {
                            _appDataService.UserMessageArrived("INTERNAL", $"ERROR: Unknown DATA recieved on the API :: {ex.Message}");
                            _logger.LogError($"Unknown DATA recieved on the API :: {ex.Message}");
                        }
                    }
                    catch (ConnectionAbortedException)
                    {
                        isAcceptingConnections = false;
                        _logger.LogInformation("Web Socket API Stopped...Cherub is shutting down.");
                    }
                }

                _appDataService.ResetRemoteBoardState();
                _appDataService.UserMessageArrived("INTERNAL", "DGT Angel has Disconnected");
            }
            else
            {
                HttpContext.Response.StatusCode = (int)HttpStatusCode.BadRequest;
            }
        }
    }
}
