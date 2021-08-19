using System.Text.Json.Serialization;

namespace DgtAngelShared.Json
{
    public class CherubApiMessage
    {
        public enum MessageTypeCode
        {
            KEEP_ALIVE, FEN_UPDATE, MESSAGE
        }

        public string Source { get; set; }
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public MessageTypeCode MessageType { get; set; }
        public string Message { get; set; }
        public BoardState RemoteBoard  { get; set; }
    }
}
