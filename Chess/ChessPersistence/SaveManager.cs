using System.Text.Json;
using ChessLogic;

namespace ChessPersistence
{
    public static class SaveManager
    {
        private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
        };

        public static void Save(GameState state, string filePath)
        {
            var boardCells = new List<string>(64);
            var hasMovedList = new List<bool>(64);

            for (int r = 0; r < 8; r++)
            {
                for (int c = 0; c < 8; c++)
                {
                    Piece piece = state.Board[r, c];
                    if (piece == null)
                    {
                        boardCells.Add("");
                        hasMovedList.Add(false);
                    }
                    else
                    {
                        boardCells.Add(EncodePiece(piece));
                        hasMovedList.Add(piece.HasMoved);
                    }
                }
            }

            Position skipW = state.Board.GetPawnSkipPosition(Player.White);
            Position skipB = state.Board.GetPawnSkipPosition(Player.Black);

            var data = new SaveData
            {
                CurrentPlayer = state.CurrentPlayer.ToString(),
                NoCaptureOrPawnMoves = state.NoCaptureOrPawnMoves,
                Board = boardCells,
                HasMoved = hasMovedList,
                PawnSkipWhite = skipW == null ? null : $"{skipW.Row},{skipW.Column}",
                PawnSkipBlack = skipB == null ? null : $"{skipB.Row},{skipB.Column}",
                MoveNotations = state.MoveHistory.Select(r => r.Notation).ToList(),
                MovePlayers   = state.MoveHistory.Select(r => r.MovedBy.ToString()).ToList(),
            };

            File.WriteAllText(filePath, JsonSerializer.Serialize(data, JsonOptions));
        }

        public static GameState Load(string filePath)
        {
            SaveData data = JsonSerializer.Deserialize<SaveData>(File.ReadAllText(filePath))
                ?? throw new InvalidDataException("Save file is empty or corrupt.");

            Board board = new Board();

            for (int r = 0; r < 8; r++)
            {
                for (int c = 0; c < 8; c++)
                {
                    int idx = r * 8 + c;
                    string code = data.Board[idx];

                    if (!string.IsNullOrEmpty(code))
                    {
                        Piece piece = DecodePiece(code);
                        piece.HasMoved = data.HasMoved[idx];
                        board[r, c] = piece;
                    }
                }
            }

            if (data.PawnSkipWhite != null)
                board.SetPawnSkipPosition(Player.White, ParsePosition(data.PawnSkipWhite));

            if (data.PawnSkipBlack != null)
                board.SetPawnSkipPosition(Player.Black, ParsePosition(data.PawnSkipBlack));

            Player player = data.CurrentPlayer == "Black" ? Player.Black : Player.White;

            var notations = data.MoveNotations ?? new List<string>();
            var players   = data.MovePlayers   ?? new List<string>();

            var history = notations.Zip(players,
                (n, p) => (n, p == "Black" ? Player.Black : Player.White));

            return GameState.Restore(player, board, data.NoCaptureOrPawnMoves, history);
        }

        private static string EncodePiece(Piece p)
        {
            char color = p.Color == Player.White ? 'W' : 'B';
            char type = p.Type switch
            {
                PieceType.Pawn   => 'P',
                PieceType.Knight => 'N',
                PieceType.Bishop => 'B',
                PieceType.Rook   => 'R',
                PieceType.Queen  => 'Q',
                PieceType.King   => 'K',
                _                => throw new ArgumentException($"Unknown piece type: {p.Type}"),
            };
            return $"{color}{type}";
        }

        private static Piece DecodePiece(string code)
        {
            Player color = code[0] == 'W' ? Player.White : Player.Black;
            return code[1] switch
            {
                'P' => new Pawn(color),
                'N' => new Knight(color),
                'B' => new Bishop(color),
                'R' => new Rook(color),
                'Q' => new Queen(color),
                'K' => new King(color),
                _   => throw new InvalidDataException($"Unknown piece code: {code}"),
            };
        }

        private static Position ParsePosition(string s)
        {
            string[] parts = s.Split(',');
            return new Position(int.Parse(parts[0]), int.Parse(parts[1]));
        }
    }
}
