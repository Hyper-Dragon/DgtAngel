using System.Text.Json.Serialization;

namespace DgtAngelShared.Json
{
    public enum ResponseCode
    {
        UNKNOWN_PAGE, RUNNING, SCRIPT_SCRAPE_ERROR, PAGE_READ_ERROR,
        GAME_PENDING, GAME_COMPLETED, GAME_IN_PROGRESS, LOST_VISABILITY, MOVE_LIST_MISSING
    }

    public enum TurnCode
    {
        NONE, WHITE, BLACK, UNKNOWN, ERROR
    }

    public enum SiteToBoardConnectionState
    {
        UNKNOWN, ACTIVE
    }

    public sealed record BoardState
    {
        public string PageUrl { get; init; }
        public long CaptureTimeMs { get; init; }
        public State State { get; init; }
        public Boardconnection BoardConnection { get; init; }
        public Board Board { get; init; }
    }

    public sealed record State
    {
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public ResponseCode Code { get; set; }
        public string Message { get; init; }
    }

    public sealed record Boardconnection
    {
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public SiteToBoardConnectionState BoardState { get; set; }
        public string ConMessage { get; init; }
    }

    public sealed record Board
    {
        public bool IsWhiteOnBottom { get; init; }
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public TurnCode ClockTurn { get; init; }
        public string LastMove { get; set; }
        public string Ending { get; set; }
        public TurnCode FenTurn { get; set; }
        public string FenString { get; init; }
        public string LastFenString { get; set; }
        public Clocks Clocks { get; init; }
    }

    public sealed record Clocks
    {
        public long CaptureTimeMs { get; init; }
        public int WhiteClock { get; init; }
        public int BlackClock { get; init; }
    }
}
