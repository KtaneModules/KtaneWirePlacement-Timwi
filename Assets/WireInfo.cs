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

        public override string ToString()
        {
            return string.Format("{0}. ({1},{2}) {3} {4}", Index + 1, Column + 1, Row + 1, IsVertical ? "v" : "h", Color);
        }
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
