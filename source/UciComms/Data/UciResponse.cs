namespace UciComms.Data
{
    public abstract record UciResponse
    {
        public string RawData { get; set; } = string.Empty;
    }

    public sealed record IdResponse : UciResponse
    {
        public string Name { get; set; } = string.Empty;
        public string Author { get; set; } = string.Empty;
    }

    public sealed record UciOkResponse : UciResponse
    {

    }

    public sealed record ReadyOkResponse : UciResponse
    {

    }

    public sealed record BestMoveResponse : UciResponse
    {
        public string BestMove { get; set; } = string.Empty;
        public string PonderMove { get; set; } = string.Empty;
    }


    [Serializable]
    public sealed record UciOption
    {
        public string Name { get; set; }
        public string Type { get; set; }
        public string DefaultValue { get; set; }
        public string MinValue { get; set; }
        public string MaxValue { get; set; }
        public string VarValue { get; set; }

        public UciOption(string name, string type, string defaultValue, string minValue, string maxValue, string varValue)
        {
            Name = name;
            Type = type;
            DefaultValue = defaultValue;
            MinValue = minValue;
            MaxValue = maxValue;
            VarValue = varValue ?? defaultValue;
        }
    }

    public sealed record InfoResponse : UciResponse
    {
        public int Depth { get; set; } = 0;
        public int SelDepth { get; set; } = 0;
        public int Time { get; set; } = 0;
        public decimal Nodes { get; set; } = 0;
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
        public int TbHits { get; set; } = 0;
        public int SbHits { get; set; } = 0;
        public int CpuLoad { get; set; } = 0;
        public string Refutation { get; set; } = string.Empty;
        public string CurrLine { get; set; } = string.Empty;
        public string StringInfo { get; set; } = string.Empty;
    }

}
