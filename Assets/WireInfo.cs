namespace WirePlacement
{
    sealed class WireInfo
    {
        public int Index;
        public int Column;
        public int Row;
        public bool IsVertical;
        public WireColor Color;
        public bool MustCut;
        public bool IsCut;
    }

    enum WireColor
    {
        Black,
        Blue,
        Red,
        White,
        Yellow
    }
}
