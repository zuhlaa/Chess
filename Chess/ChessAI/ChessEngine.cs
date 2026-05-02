using ChessLogic;

namespace ChessAI
{
    public class ChessEngine
    {
        public int SearchDepth { get; set; } = 3;

        // Returns the best move for the current player, or null if none exist.
        public Move GetBestMove(GameState state)
        {
            if (state.IsGameOver()) return null;

            GameState search = state.CreateSearchInstance();
            Player aiPlayer = search.CurrentPlayer;

            List<Move> moves = OrderMoves(
                search.AllLegalMovesFor(aiPlayer).ToList(), search.Board);

            if (moves.Count == 0) return null;

            Move bestMove = null;
            int bestScore = int.MinValue + 1;
            int alpha = int.MinValue + 1;
            int beta = int.MaxValue;

            foreach (Move move in moves)
            {
                search.MakeMove(move);
                int score = -Negamax(search, SearchDepth - 1, -beta, -alpha);
                search.UndoMove();

                if (score > bestScore)
                {
                    bestScore = score;
                    bestMove = move;
                }

                alpha = Math.Max(alpha, score);
            }

            return bestMove;
        }

        private int Negamax(GameState state, int depth, int alpha, int beta)
        {
            if (state.IsGameOver())
                return TerminalScore(state, depth);

            if (depth == 0)
                return Evaluator.Evaluate(state.Board, state.CurrentPlayer);

            List<Move> moves = OrderMoves(
                state.AllLegalMovesFor(state.CurrentPlayer).ToList(), state.Board);

            int maxScore = int.MinValue + 1;

            foreach (Move move in moves)
            {
                state.MakeMove(move);
                int score = -Negamax(state, depth - 1, -beta, -alpha);
                state.UndoMove();

                maxScore = Math.Max(maxScore, score);
                alpha = Math.Max(alpha, score);

                if (alpha >= beta) break;
            }

            return maxScore;
        }

        // Faster checkmates score higher by using the remaining depth.
        private static int TerminalScore(GameState state, int depth)
        {
            if (state.Result.Winner == Player.None) return 0;
            return -100000 - depth;
        }

        private static List<Move> OrderMoves(List<Move> moves, Board board)
        {
            return moves.OrderByDescending(m =>
            {
                if (m.Type == MoveType.EnPassant) return Evaluator.PieceValue(PieceType.Pawn);
                if (!board.IsEmpty(m.ToPos)) return Evaluator.PieceValue(board[m.ToPos].Type);
                if (m.Type == MoveType.PawnPromotion) return Evaluator.PieceValue(PieceType.Queen);
                return 0;
            }).ToList();
        }
    }
}
