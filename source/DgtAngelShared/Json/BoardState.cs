using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
        public string Code { get; set; }
        public string Message { get; set; }
    }

    public class Boardconnection
    {
        public string BoardState { get; set; }
        public string ConMessage { get; set; }
    }

    public class Board
    {
        public bool IsWhiteOnBottom { get; set; }
        public string Turn { get; set; }
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
