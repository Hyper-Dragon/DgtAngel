using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UciComms.Data
{
    public abstract class UciResponse
    {
        public string RawData { get; set; } = string.Empty;
    }

    public class IdResponse : UciResponse
    {
        public string Name { get; set; } = string.Empty;
        public string Author { get; set; } = string.Empty;
    }

    public class UciOkResponse : UciResponse
    {

    }

    public class ReadyOkResponse : UciResponse
    {

    }

    public class BestMoveResponse : UciResponse
    {
        public string BestMove { get; set; } = string.Empty;
        public string PonderMove { get; set; } = string.Empty;
    }

    public class InfoResponse : UciResponse
    {
        public int Depth { get; set; } = 0;
        public int SelDepth { get; set; } = 0;
        public int Time { get; set; } = 0;
        public int Nodes { get; set; } = 0;
        public string Pv { get; set; } = string.Empty;
        public int MultiPv { get; set; } = 0;
        public int ScoreCp { get; set; } = 0;
        public string ScoreBound { get; set; } = string.Empty;
        public int ScoreMate { get; set; } = 0;
        public string CurrMove { get; set; } = string.Empty;
        public int CurrMoveNumber { get; set; } = 0;
        public int CurrLineCpuNr { get; set; } = 0;
        public int HashFull { get; set; } = 0;
        public int Nps { get; set; } = 0;
        public int TbHits { get; set; } = 0 ;
        public  int SbHits { get; set; } = 0;
        public int CpuLoad { get; set; } = 0;
        public string Refutation { get; set; } = string.Empty;
        public string CurrLine { get; set; } = string.Empty;
        public string StringInfo { get; set; } = string.Empty;
    }

}
