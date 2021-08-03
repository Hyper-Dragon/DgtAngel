using System;
using System.IO;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace DgtAngelLib
{
    public class MessageRecievedEventArgs : EventArgs
    {
        public string ResponseOut { get; set; }
    }

    public class DgtLiveChess
    {
        public event EventHandler<MessageRecievedEventArgs> ResponseRecieved;


        public async Task Connect()
        {
            // http://localhost:1982/doc/api/feeds/eboardevent/index.html

            Console.WriteLine("Hello World!");

            using var socket = new ClientWebSocket();
            try
            {
                await socket.ConnectAsync(new Uri(@"ws://127.0.0.1:1982/api/v1.0"), CancellationToken.None);

                Console.WriteLine("Connected");

                await Send(socket, "{\"call\": \"eboards\",\"id\": 1,\"param\": null}");
                Console.WriteLine("Sent1");

                await Receive(socket);
                Console.WriteLine("Rec1");

                await Send(socket, "{ \"call\": \"subscribe\", \"id\": 2, \"param\": { \"feed\": \"eboardevent\",\"id\": 1,\"param\": {\"serialnr\": \"24958\"}}}");
                Console.WriteLine("Sent2");

                for (; ; )
                {
                    await Receive(socket);
                    Console.WriteLine("Rec2");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR - {ex.Message}");
            }
        }

        async Task Send(ClientWebSocket socket, string data) =>
                               await socket.SendAsync(Encoding.UTF8.GetBytes(data), WebSocketMessageType.Text, true, CancellationToken.None);

        async Task Receive(ClientWebSocket socket)
        {
            var buffer = new ArraySegment<byte>(new byte[2048]);

            WebSocketReceiveResult result;
            using var ms = new MemoryStream();
            do
            {
                result = await socket.ReceiveAsync(buffer, CancellationToken.None);
                ms.Write(buffer.Array, buffer.Offset, result.Count);
            } while (!result.EndOfMessage);

            //if (result.MessageType == WebSocketMessageType.Close)
            //    break;

            ms.Seek(0, SeekOrigin.Begin);
            using var reader = new StreamReader(ms, Encoding.UTF8);
            string response = await reader.ReadToEndAsync();


            //EboardsRoot weatherForecast = JsonSerializer.Deserialize<EboardsRoot>(response);
            ResponseRecieved?.Invoke(this, new MessageRecievedEventArgs() { ResponseOut=response });

            Console.WriteLine(response);
        }



    }
}
