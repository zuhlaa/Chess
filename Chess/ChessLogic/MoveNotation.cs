namespace ChessLogic
{
    public static class MoveNotation
    {
        public static string GetNotation(Move move, Board before, Board after, Player movedBy)
        {
            string baseNote = GetBaseNotation(move, before, movedBy);
            string suffix = GetCheckSuffix(after, movedBy.Opponent());
            return baseNote + suffix;
        }

        private static string GetBaseNotation(Move move, Board board, Player player)
        {
            if (move.Type == MoveType.CastleKS) return "O-O";
            if (move.Type == MoveType.CastleQS) return "O-O-O";

            Piece piece = board[move.FromPos];
            bool isCapture = move.Type == MoveType.EnPassant || !board.IsEmpty(move.ToPos);
            string target = SquareName(move.ToPos);

            if (piece.Type == PieceType.Pawn)
            {
                string pawnBase = isCapture
                    ? $"{FileChar(move.FromPos.Column)}x{target}"
                    : target;

                if (move.Type == MoveType.PawnPromotion)
                    pawnBase += $"={(PieceLetter(((PawnPromotion)move).PromotionPieceType))}";

                return pawnBase;
            }

            char letter = PieceLetter(piece.Type);
            string disambig = GetDisambiguation(move, board, player);
            string capture = isCapture ? "x" : "";

            return $"{letter}{disambig}{capture}{target}";
        }

        private static string GetDisambiguation(Move move, Board board, Player player)
        {
            Piece movingPiece = board[move.FromPos];

            List<Position> ambiguous = board.PiecePositionsFor(player)
                .Where(pos => pos != move.FromPos
                           && board[pos].Type == movingPiece.Type
                           && board[pos].GetMoves(pos, board).Any(m => m.ToPos == move.ToPos && m.IsLegal(board)))
                .ToList();

            if (ambiguous.Count == 0) return "";

            bool conflictFile = ambiguous.Any(p => p.Column == move.FromPos.Column);
            bool conflictRank = ambiguous.Any(p => p.Row == move.FromPos.Row);

            if (!conflictFile) return FileChar(move.FromPos.Column).ToString();
            if (!conflictRank) return (8 - move.FromPos.Row).ToString();
            return SquareName(move.FromPos);
        }

        private static string GetCheckSuffix(Board after, Player opponent)
        {
            if (!after.IsInCheck(opponent)) return "";

            bool hasLegalMoves = after.PiecePositionsFor(opponent)
                .SelectMany(pos => after[pos].GetMoves(pos, after))
                .Any(m => m.IsLegal(after));

            return hasLegalMoves ? "+" : "#";
        }

        private static string SquareName(Position pos) =>
            $"{FileChar(pos.Column)}{8 - pos.Row}";

        private static char FileChar(int column) => (char)('a' + column);

        private static char PieceLetter(PieceType type) => type switch
        {
            PieceType.Knight => 'N',
            PieceType.Bishop => 'B',
            PieceType.Rook   => 'R',
            PieceType.Queen  => 'Q',
            PieceType.King   => 'K',
            _                => '?'
        };
    }
}
