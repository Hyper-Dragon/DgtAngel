using System.Text.Json;
using System.Text.Json.Serialization;

namespace DgtLiveChessWrapper.DgtLiveChessJson.FeedResponse
{
    internal sealed record LiveChessFeedResponse
    {
        internal static (string JsonString, LiveChessFeedResponse Response) Deserialize(string jsonString)
        {
            return (jsonString, JsonSerializer.Deserialize<LiveChessFeedResponse>(jsonString));
        }

        [JsonPropertyName("response")]
        public string Response { get; init; }
        [JsonPropertyName("id")]
        public int Id { get; init; }
        [JsonPropertyName("param")]
        public LiveChessResponseParams Param { get; init; }
        [JsonPropertyName("time")]
        public long Time { get; init; }
    }

    internal sealed record LiveChessResponseParams
    {
        [JsonPropertyName("serialnr")]
        public string Serialnr { get; init; }
        [JsonPropertyName("flipped")]
        public bool Flipped { get; init; }
        [JsonPropertyName("board")]
        public string Board { get; init; }
        [JsonPropertyName("clock")]
        public object Clock { get; init; }
    }
}
