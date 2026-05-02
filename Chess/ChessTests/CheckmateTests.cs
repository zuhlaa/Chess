using ChessLogic;
using Xunit;

namespace ChessTests
{
    public class CheckmateTests
    {
        [Fact]
        public void FoolsMate_BlackWins()
        {
            // 1.f3 e5 2.g4 Qh4#
            var state = new GameState(Player.White, Board.Initial());

            state.MakeMove(FindMove(state, new Position(6, 5), new Position(5, 5))); // f3
            state.MakeMove(FindMove(state, new Position(1, 4), new Position(3, 4))); // e5
            state.MakeMove(FindMove(state, new Position(6, 6), new Position(4, 6))); // g4
            state.MakeMove(FindMove(state, new Position(0, 3), new Position(4, 7))); // Qh4#

            Assert.True(state.IsGameOver());
            Assert.Equal(EndReason.Checkmate, state.Result.Reason);
            Assert.Equal(Player.Black, state.Result.Winner);
        }

        [Fact]
        public void ScholarsMate_WhiteWins()
        {
            // 1.e4 e5 2.Bc4 Nc6 3.Qh5 Nf6?? 4.Qxf7#
            var state = new GameState(Player.White, Board.Initial());

            state.MakeMove(FindMove(state, new Position(6, 4), new Position(4, 4))); // e4
            state.MakeMove(FindMove(state, new Position(1, 4), new Position(3, 4))); // e5
            state.MakeMove(FindMove(state, new Position(7, 5), new Position(4, 2))); // Bc4
            state.MakeMove(FindMove(state, new Position(0, 1), new Position(2, 2))); // Nc6
            state.MakeMove(FindMove(state, new Position(7, 3), new Position(3, 7))); // Qh5
            state.MakeMove(FindMove(state, new Position(0, 6), new Position(2, 5))); // Nf6?? (allows mate)
            state.MakeMove(FindMove(state, new Position(3, 7), new Position(1, 5))); // Qxf7#

            Assert.True(state.IsGameOver());
            Assert.Equal(EndReason.Checkmate, state.Result.Reason);
            Assert.Equal(Player.White, state.Result.Winner);
        }

        [Fact]
        public void Stalemate_IsDraw()
        {
            // Classic stalemate: Black king cornered with no legal moves, not in check
            var board = new Board();
            board[0, 0] = new King(Player.Black);
            board[1, 2] = new Queen(Player.White);
            board[2, 1] = new King(Player.White);

            var state = new GameState(Player.Black, board);

            Assert.True(state.IsGameOver());
            Assert.Equal(EndReason.Stalemate, state.Result.Reason);
            Assert.Equal(Player.None, state.Result.Winner);
        }

        [Fact]
        public void AfterUndo_GameIsNoLongerOver()
        {
            var state = new GameState(Player.White, Board.Initial());

            state.MakeMove(FindMove(state, new Position(6, 5), new Position(5, 5))); // f3
            state.MakeMove(FindMove(state, new Position(1, 4), new Position(3, 4))); // e5
            state.MakeMove(FindMove(state, new Position(6, 6), new Position(4, 6))); // g4
            state.MakeMove(FindMove(state, new Position(0, 3), new Position(4, 7))); // Qh4# — game over

            Assert.True(state.IsGameOver());

            state.UndoMove(); // Undo Qh4#

            Assert.False(state.IsGameOver());
            Assert.Null(state.Result);
        }

        private static Move FindMove(GameState state, Position from, Position to)
        {
            return state.LegalMovesForPiece(from).First(m => m.ToPos == to);
        }
    }
}
