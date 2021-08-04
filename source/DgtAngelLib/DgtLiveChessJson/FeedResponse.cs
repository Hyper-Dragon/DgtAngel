using System.Text.Json;
using System.Text.Json.Serialization;

namespace DgtAngelLib.DgtLiveChessJson.FeedResponse
{
    public class Rootobject
    {
        public static (string JsonString, Rootobject Response) Deserialize(string jsonString)
        {
            return (jsonString, JsonSerializer.Deserialize<DgtAngelLib.DgtLiveChessJson.FeedResponse.Rootobject>(jsonString));
        }

        [JsonPropertyName("response")]
        public string Response { get; set; }
        [JsonPropertyName("id")]
        public int Id { get; set; }
        [JsonPropertyName("param")]
        public Param Param { get; set; }
        [JsonPropertyName("time")]
        public long Time { get; set; }
    }

    public class Param
    {
        [JsonPropertyName("serialnr")]
        public string Serialnr { get; set; }
        [JsonPropertyName("flipped")]
        public bool Flipped { get; set; }
        [JsonPropertyName("board")]
        public string Board { get; set; }
        [JsonPropertyName("clock")]
        public object Clock { get; set; }
    }
}
