using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace DgtAngelShared.Json
{
    public class BoardState
    {
        public string PageUrl { get; set; }
        public long CaptureTimeMs { get; set; }
        public State State { get; set; }
        public Boardconnection BoardConnection { get; set; }
        public Board Board { get; set; }
    }

    public class State
    {
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public ResponseCode Code { get; set; }
        public string Message { get; set; }
    }

    public enum ResponseCode
    {
        UNKNOWN_PAGE,
        RUNNING,
        SCRIPT_SCRAPE_ERROR,
        GAME_PENDING,
        GAME_COMPLETED,
        GAME_IN_PROGRESS
    }

    public enum TurnCode
    {
        NONE,
        WHITE,
        BLACK,
        UNKNOWN
    }

    public class Boardconnection
    {
        public string BoardState { get; set; }
        public string ConMessage { get; set; }
    }

    public class Board
    {
        public bool IsWhiteOnBottom { get; set; }
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public TurnCode Turn { get; set; }
        public string LastMove { get; set; }
        public string FenString { get; set; }
        public Clocks Clocks { get; set; }
    }

    public class Clocks
    {
        public int WhiteClock { get; set; }
        public int BlackClock { get; set; }
    }

}
