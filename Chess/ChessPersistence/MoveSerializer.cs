using ChessLogic;

namespace ChessPersistence
{
    // Encodes moves as compact strings and reconstructs them from a live GameState.
    // Format: "r1,c1:r2,c2" for regular moves, "r1,c1:r2,c2:P" for promotions (P = Q/R/B/N).
    public static class MoveSerializer
    {
        public static string Encode(Move move)
        {
            string positions = $"{move.FromPos.Row},{move.FromPos.Column}:{move.ToPos.Row},{move.ToPos.Column}";

            if (move is PawnPromotion promo)
                return $"{positions}:{PieceLetter(promo.PromotionPieceType)}";

            return positions;
        }

        public static Move Decode(string encoded, GameState state)
        {
            string[] parts = encoded.Split(':');

            Position from = ParsePosition(parts[0]);
            Position to = ParsePosition(parts[1]);

            if (parts.Length == 3)
            {
                PieceType promotionType = ParsePieceType(parts[2]);
                return new PawnPromotion(from, to, promotionType);
            }

            return state.LegalMovesForPiece(from).FirstOrDefault(m => m.ToPos == to)
                ?? throw new InvalidDataException($"No legal move found for {encoded}");
        }

        private static Position ParsePosition(string s)
        {
            string[] coords = s.Split(',');
            return new Position(int.Parse(coords[0]), int.Parse(coords[1]));
        }

        private static char PieceLetter(PieceType type) => type switch
        {
            PieceType.Queen  => 'Q',
            PieceType.Rook   => 'R',
            PieceType.Bishop => 'B',
            PieceType.Knight => 'N',
            _                => 'Q',
        };

        private static PieceType ParsePieceType(string s) => s switch
        {
            "Q" => PieceType.Queen,
            "R" => PieceType.Rook,
            "B" => PieceType.Bishop,
            "N" => PieceType.Knight,
            _   => PieceType.Queen,
        };
    }
}
