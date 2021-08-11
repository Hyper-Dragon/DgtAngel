using System.Text.Json;
using System.Text.Json.Serialization;

namespace DgtLiveChessWrapper.DgtLiveChessJson.CallResponse
{
    public class Rootobject
    {
        public static (string JsonString,Rootobject Response) Deserialize(string jsonString)
        {
            return (jsonString,JsonSerializer.Deserialize<DgtLiveChessJson.CallResponse.Rootobject>(jsonString));
        }

        [JsonPropertyName("response")]
        public string Response { get; set; }
        [JsonPropertyName("id")]
        public int Id { get; set; }
        [JsonPropertyName("param")]
        public Param[] Boards { get; set; }
        [JsonPropertyName("time")]
        public long Time { get; set; }
    }

    public class Param
    {
        [JsonPropertyName("serialnr")]
        public string SerialNumber { get; set; }
        [JsonPropertyName("source")]
        public string Source { get; set; }
        [JsonPropertyName("state")]
        public string ConnectionState { get; set; }
        [JsonPropertyName("battery")]
        public string BatteryLevel { get; set; }
        [JsonPropertyName("comment")]
        public object Comment { get; set; }
        [JsonPropertyName("board")]
        public string BoardFen { get; set; }
        [JsonPropertyName("flipped")]
        public bool Flipped { get; set; }
        [JsonPropertyName("clock")]
        public object Clock { get; set; }
    }
}





