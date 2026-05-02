namespace ChessPersistence
{
    public class SaveData
    {
        public string CurrentPlayer { get; set; }
        public int NoCaptureOrPawnMoves { get; set; }
        public string PawnSkipWhite { get; set; }       // "row,col" or null
        public string PawnSkipBlack { get; set; }       // "row,col" or null
        public List<string> Board { get; set; }         // 64 cells: "WP", "BK", "" etc.
        public List<bool> HasMoved { get; set; }        // 64 flags matching Board cells
        public List<string> MoveNotations { get; set; } // SAN strings for history display
        public List<string> MovePlayers { get; set; }   // "White"/"Black" per notation
    }
}
