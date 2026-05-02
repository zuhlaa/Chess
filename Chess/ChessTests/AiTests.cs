using ChessAI;
using ChessLogic;
using Xunit;

namespace ChessTests
{
    public class AiTests
    {
        private static readonly ChessEngine Engine = new ChessEngine { SearchDepth = 2 };

        [Fact]
        public void GetBestMove_InitialPosition_ReturnsLegalMove()
        {
            var state = new GameState(Player.White, Board.Initial());
            Move move = Engine.GetBestMove(state);

            Assert.NotNull(move);
            Assert.True(IsLegal(state, move, Player.White), "AI returned an illegal move.");
        }

        [Fact]
        public void GetBestMove_ReturnsMoveForBlack()
        {
            var state = new GameState(Player.White, Board.Initial());
            state.MakeMove(state.LegalMovesForPiece(new Position(6, 4))
                .First(m => m.ToPos == new Position(4, 4)));

            var engine = new ChessEngine { SearchDepth = 2 };
            Move move = engine.GetBestMove(state);

            Assert.NotNull(move);
            Assert.True(IsLegal(state, move, Player.Black), "AI returned an illegal move.");
        }

        [Fact]
        public void GetBestMove_GameAlreadyOver_ReturnsNull()
        {
            // Stalemate: created via constructor triggers CheckForGameOver
            var board = new Board();
            board[0, 0] = new King(Player.Black);
            board[1, 2] = new Queen(Player.White);
            board[2, 1] = new King(Player.White);

            var state = new GameState(Player.Black, board);

            Assert.True(state.IsGameOver());
            Assert.Null(Engine.GetBestMove(state));
        }

        [Fact]
        public void GetBestMove_DoesNotMutateOriginalGameState()
        {
            var state = new GameState(Player.White, Board.Initial());
            Player playerBefore = state.CurrentPlayer;
            int historyBefore = state.MoveHistory.Count;

            Engine.GetBestMove(state);

            Assert.Equal(playerBefore, state.CurrentPlayer);
            Assert.Equal(historyBefore, state.MoveHistory.Count);
        }

        [Fact]
        public void GetBestMove_TakesFreePiece()
        {
            var board = new Board();
            board[7, 4] = new King(Player.White);
            board[7, 3] = new Queen(Player.White);
            board[3, 3] = new Queen(Player.Black); // undefended
            board[0, 4] = new King(Player.Black);

            var state = new GameState(Player.White, board);
            var engine = new ChessEngine { SearchDepth = 1 };
            Move move = engine.GetBestMove(state);

            Assert.NotNull(move);
            Assert.Equal(new Position(3, 3), move.ToPos);
        }

        private static bool IsLegal(GameState state, Move move, Player player)
        {
            return state.AllLegalMovesFor(player).Any(m =>
                m.FromPos == move.FromPos && m.ToPos == move.ToPos && m.Type == move.Type);
        }
    }
}
