using ChessLogic;

namespace ChessAI
{
    // Converts raw centipawn scores into normalized [0,1] values and display strings.
    // 1.0 = decisive White advantage, 0.5 = equal, 0.0 = decisive Black advantage.
    public static class EvaluationService
    {
        private const int CapCentipawns = 1000; // ±10 pawns cap for bar normalization

        public static double GetNormalizedScore(GameState state)
        {
            if (state.IsGameOver())
            {
                return state.Result.Winner switch
                {
                    Player.White => 1.0,
                    Player.Black => 0.0,
                    _            => 0.5,
                };
            }

            int raw = Evaluator.Evaluate(state.Board, Player.White);
            int capped = Math.Clamp(raw, -CapCentipawns, CapCentipawns);
            return (capped + CapCentipawns) / (2.0 * CapCentipawns);
        }

        public static string GetScoreText(GameState state)
        {
            if (state.IsGameOver())
            {
                return state.Result.Winner switch
                {
                    Player.White => "+M",
                    Player.Black => "-M",
                    _            => "=",
                };
            }

            double pawns = Evaluator.Evaluate(state.Board, Player.White) / 100.0;
            return pawns >= 0 ? $"+{pawns:F1}" : $"{pawns:F1}";
        }
    }
}
