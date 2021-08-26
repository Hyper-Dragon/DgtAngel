using System.Text.Json;
using System.Text.Json.Serialization;

namespace DgtLiveChessWrapper.DgtLiveChessJson.CallResponse
{
    internal sealed record LiveChessCallResponse
    {
        internal static (string JsonString, LiveChessCallResponse Response) Deserialize(in string jsonString)
        {
            return (jsonString, JsonSerializer.Deserialize<LiveChessCallResponse>(jsonString));
        }

        [JsonPropertyName("response")]
        public string Response { get; init; }
        [JsonPropertyName("id")]
        public int Id { get; init; }
        [JsonPropertyName("param")]
        public LiveChessCallParams[] Boards { get; init; }
        [JsonPropertyName("time")]
        public long Time { get; init; }
    }

    internal sealed record LiveChessCallParams
    {
        [JsonPropertyName("serialnr")]
        public string SerialNumber { get; init; }
        [JsonPropertyName("source")]
        public string Source { get; init; }
        [JsonPropertyName("state")]
        public string ConnectionState { get; init; }
        [JsonPropertyName("battery")]
        public string BatteryLevel { get; init; }
        [JsonPropertyName("comment")]
        public object Comment { get; init; }
        [JsonPropertyName("board")]
        public string BoardFen { get; init; }
        [JsonPropertyName("flipped")]
        public bool Flipped { get; init; }
        [JsonPropertyName("clock")]
        public object Clock { get; init; }
    }
}





