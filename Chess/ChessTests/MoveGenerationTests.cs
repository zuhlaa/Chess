using ChessLogic;
using Xunit;

namespace ChessTests
{
    public class MoveGenerationTests
    {
        [Fact]
        public void InitialPosition_WhiteHas20LegalMoves()
        {
            var state = new GameState(Player.White, Board.Initial());
            int count = state.AllLegalMovesFor(Player.White).Count();
            Assert.Equal(20, count);
        }

        [Fact]
        public void AfterE4_BlackHas20LegalMoves()
        {
            var state = new GameState(Player.White, Board.Initial());
            Move e4 = state.LegalMovesForPiece(new Position(6, 4)).First(m => m.ToPos == new Position(4, 4));
            state.MakeMove(e4);

            int count = state.AllLegalMovesFor(Player.Black).Count();
            Assert.Equal(20, count);
        }

        [Fact]
        public void PinnedPiece_CannotMoveIfItExposesKing()
        {
            // White King at g1 (7,6), White Pawn at f2 (6,5) pinned diagonally
            // by Black Queen at h4 (4,7) — if pawn moves forward it exposes the king.
            // Diagonal from (7,6) through (6,5) to (5,4)... hmm, different diagonal.
            // Use: White King (7,4), White Pawn (5,2), Black Queen (3,0) — same SW diagonal.
            var board = new Board();
            board[7, 4] = new King(Player.White);
            board[5, 2] = new Pawn(Player.White); // pinned on the SW diagonal
            board[3, 0] = new Queen(Player.Black); // pins through the pawn
            board[0, 7] = new King(Player.Black);

            var state = new GameState(Player.White, board);
            var pawnMoves = state.LegalMovesForPiece(new Position(5, 2));
            Assert.Empty(pawnMoves);
        }

        [Fact]
        public void EnPassant_IsLegalAfterDoublePawnPush()
        {
            var state = new GameState(Player.White, Board.Initial());

            // 1.e4 d5 2.e5 f5 — now exf6 en passant is legal
            state.MakeMove(FindMove(state, new Position(6, 4), new Position(4, 4)));
            state.MakeMove(FindMove(state, new Position(1, 3), new Position(3, 3)));
            state.MakeMove(FindMove(state, new Position(4, 4), new Position(3, 4)));
            state.MakeMove(FindMove(state, new Position(1, 5), new Position(3, 5)));

            IEnumerable<Move> ePMoves = state.LegalMovesForPiece(new Position(3, 4))
                .Where(m => m.Type == MoveType.EnPassant);
            Assert.Single(ePMoves);
        }

        [Fact]
        public void Castling_KingsideIsLegalWhenClear()
        {
            var board = new Board();
            board[7, 4] = new King(Player.White);
            board[7, 7] = new Rook(Player.White);
            board[0, 4] = new King(Player.Black);

            var state = new GameState(Player.White, board);
            bool canCastle = state.LegalMovesForPiece(new Position(7, 4))
                .Any(m => m.Type == MoveType.CastleKS);
            Assert.True(canCastle);
        }

        [Fact]
        public void Castling_NotLegalThroughOccupiedSquare()
        {
            var board = new Board();
            board[7, 4] = new King(Player.White);
            board[7, 7] = new Rook(Player.White);
            board[7, 6] = new Knight(Player.White); // blocks castling path
            board[0, 4] = new King(Player.Black);

            var state = new GameState(Player.White, board);
            bool canCastle = state.LegalMovesForPiece(new Position(7, 4))
                .Any(m => m.Type == MoveType.CastleKS);
            Assert.False(canCastle);
        }

        private static Move FindMove(GameState state, Position from, Position to)
        {
            return state.LegalMovesForPiece(from).First(m => m.ToPos == to);
        }
    }
}
