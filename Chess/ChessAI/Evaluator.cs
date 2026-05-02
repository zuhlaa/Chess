using ChessLogic;

namespace ChessAI
{
    public static class Evaluator
    {
        public static int Evaluate(Board board, Player perspective)
        {
            int score = 0;

            foreach (Position pos in board.PiecePositions())
            {
                Piece piece = board[pos];
                int value = PieceValue(piece.Type) + PieceSquareTables.Get(piece.Type, piece.Color, pos);
                score += piece.Color == perspective ? value : -value;
            }

            return score;
        }

        public static int PieceValue(PieceType type) => type switch
        {
            PieceType.Pawn   => 100,
            PieceType.Knight => 320,
            PieceType.Bishop => 330,
            PieceType.Rook   => 500,
            PieceType.Queen  => 900,
            PieceType.King   => 20000,
            _                => 0,
        };
    }
}
