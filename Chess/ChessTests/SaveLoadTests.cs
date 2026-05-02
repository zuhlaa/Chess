using ChessLogic;
using ChessPersistence;
using Xunit;

namespace ChessTests
{
    public class SaveLoadTests
    {
        [Fact]
        public void SaveLoad_EmptyGame_RoundTrips()
        {
            var state = new GameState(Player.White, Board.Initial());
            string path = Path.GetTempFileName();

            try
            {
                SaveManager.Save(state, path);
                GameState loaded = SaveManager.Load(path);

                Assert.Equal(state.CurrentPlayer, loaded.CurrentPlayer);
                Assert.Equal(0, loaded.MoveHistory.Count);
                Assert.False(loaded.IsGameOver());
            }
            finally { File.Delete(path); }
        }

        [Fact]
        public void SaveLoad_WithMoves_RestoresBoardState()
        {
            var state = new GameState(Player.White, Board.Initial());

            // 1.e4 e5 2.Nf3
            state.MakeMove(FindMove(state, new Position(6, 4), new Position(4, 4)));
            state.MakeMove(FindMove(state, new Position(1, 4), new Position(3, 4)));
            state.MakeMove(FindMove(state, new Position(7, 6), new Position(5, 5)));

            string path = Path.GetTempFileName();

            try
            {
                SaveManager.Save(state, path);
                GameState loaded = SaveManager.Load(path);

                Assert.Equal(state.CurrentPlayer, loaded.CurrentPlayer);
                Assert.Equal(state.MoveHistory.Count, loaded.MoveHistory.Count);
                AssertBoardsEqual(state.Board, loaded.Board);
            }
            finally { File.Delete(path); }
        }

        [Fact]
        public void SaveLoad_WithPawnPromotion_PreservesPromotedPiece()
        {
            // Custom board: White pawn about to promote
            var board = new Board();
            board[1, 0] = new Pawn(Player.White);
            board[7, 4] = new King(Player.White);
            board[0, 4] = new King(Player.Black);

            var state = new GameState(Player.White, board);
            state.MakeMove(new PawnPromotion(new Position(1, 0), new Position(0, 0), PieceType.Queen));

            string path = Path.GetTempFileName();

            try
            {
                SaveManager.Save(state, path);
                GameState loaded = SaveManager.Load(path);

                Piece promoted = loaded.Board[0, 0];
                Assert.NotNull(promoted);
                Assert.Equal(PieceType.Queen, promoted.Type);
                Assert.Equal(Player.White, promoted.Color);
            }
            finally { File.Delete(path); }
        }

        [Fact]
        public void SaveLoad_PreservesHasMovedFlags()
        {
            var state = new GameState(Player.White, Board.Initial());
            state.MakeMove(FindMove(state, new Position(6, 4), new Position(4, 4))); // e4

            string path = Path.GetTempFileName();

            try
            {
                SaveManager.Save(state, path);
                GameState loaded = SaveManager.Load(path);

                // The e-pawn moved to row 4 — HasMoved should be true
                Assert.True(loaded.Board[4, 4].HasMoved);
                // The a-pawn never moved — HasMoved should be false
                Assert.False(loaded.Board[1, 0].HasMoved);
            }
            finally { File.Delete(path); }
        }

        [Fact]
        public void MoveNotation_FoolsMate_CorrectNotation()
        {
            var state = new GameState(Player.White, Board.Initial());

            state.MakeMove(FindMove(state, new Position(6, 5), new Position(5, 5))); // f3
            state.MakeMove(FindMove(state, new Position(1, 4), new Position(3, 4))); // e5
            state.MakeMove(FindMove(state, new Position(6, 6), new Position(4, 6))); // g4
            state.MakeMove(FindMove(state, new Position(0, 3), new Position(4, 7))); // Qh4#

            var history = state.MoveHistory;
            Assert.Equal(4, history.Count);
            Assert.EndsWith("#", history[3].Notation);
        }

        [Fact]
        public void UndoRedo_RestoresFullMoveHistory()
        {
            var state = new GameState(Player.White, Board.Initial());

            state.MakeMove(FindMove(state, new Position(6, 4), new Position(4, 4))); // e4
            state.MakeMove(FindMove(state, new Position(1, 4), new Position(3, 4))); // e5

            Assert.Equal(2, state.MoveHistory.Count);

            state.UndoMove();
            Assert.Equal(1, state.MoveHistory.Count);
            Assert.True(state.CanRedo);

            state.RedoMove();
            Assert.Equal(2, state.MoveHistory.Count);
            Assert.False(state.CanRedo);
        }

        private static void AssertBoardsEqual(Board a, Board b)
        {
            for (int r = 0; r < 8; r++)
            {
                for (int c = 0; c < 8; c++)
                {
                    Piece ap = a[r, c];
                    Piece bp = b[r, c];

                    if (ap == null)
                    {
                        Assert.Null(bp);
                    }
                    else
                    {
                        Assert.NotNull(bp);
                        Assert.Equal(ap.Type, bp.Type);
                        Assert.Equal(ap.Color, bp.Color);
                    }
                }
            }
        }

        private static Move FindMove(GameState state, Position from, Position to)
        {
            return state.LegalMovesForPiece(from).First(m => m.ToPos == to);
        }
    }
}
