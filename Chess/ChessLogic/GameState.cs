namespace ChessLogic
{
    public class GameState
    {
        public Board Board { get; }
        public Player CurrentPlayer { get; private set; }
        public Result Result { get; private set; } = null;
        public IReadOnlyList<MoveRecord> MoveHistory => moveHistory;
        public bool CanUndo => undoStack.Count > 0;
        public bool CanRedo => redoStack.Count > 0;

        internal bool RecordMoveHistory { get; set; } = true;

        private int noCaptureOrPawnMoves = 0;
        private string stateString;
        private Dictionary<string, int> stateHistory = new Dictionary<string, int>();

        private readonly List<MoveRecord> moveHistory = new List<MoveRecord>();
        private readonly Stack<UndoEntry> undoStack = new Stack<UndoEntry>();
        private readonly Stack<Move> redoStack = new Stack<Move>();
        private bool isRedoing = false;

        public int NoCaptureOrPawnMoves => noCaptureOrPawnMoves;

        public GameState(Player player, Board board)
        {
            CurrentPlayer = player;
            Board = board;
            stateString = new StateString(CurrentPlayer, board).ToString();
            stateHistory[stateString] = 1;
            CheckForGameOver();
        }

        private GameState(Player player, Board board, int noCaptureOrPawnMoves,
            Dictionary<string, int> stateHistory, string stateString, Result result)
        {
            CurrentPlayer = player;
            Board = board;
            this.noCaptureOrPawnMoves = noCaptureOrPawnMoves;
            this.stateHistory = new Dictionary<string, int>(stateHistory);
            this.stateString = stateString;
            Result = result;
            RecordMoveHistory = false;
        }

        // Reconstructs a GameState from a saved board snapshot without replaying moves.
        public static GameState Restore(Player player, Board board, int noCaptureOrPawnMoves,
            IEnumerable<(string notation, Player movedBy)> moveHistory)
        {
            string stateStr = new StateString(player, board).ToString();
            var stateHist = new Dictionary<string, int> { { stateStr, 1 } };

            var state = new GameState(player, board, noCaptureOrPawnMoves, stateHist, stateStr, null);
            state.RecordMoveHistory = true;
            state.CheckForGameOver();

            foreach ((string notation, Player movedBy) in moveHistory)
                state.moveHistory.Add(new MoveRecord(null, notation, movedBy));

            return state;
        }

        public GameState CreateSearchInstance()
        {
            return new GameState(CurrentPlayer, Board.Copy(),
                noCaptureOrPawnMoves, stateHistory, stateString, Result);
        }

        public IEnumerable<Move> LegalMovesForPiece(Position pos)
        {
            if (Board.IsEmpty(pos) || Board[pos].Color != CurrentPlayer)
                return Enumerable.Empty<Move>();

            Piece piece = Board[pos];
            return piece.GetMoves(pos, Board).Where(move => move.IsLegal(Board));
        }

        public void MakeMove(Move move)
        {
            var snapshot = TakeSnapshot();

            Board.SetPawnSkipPosition(CurrentPlayer, null);
            bool captureOrPawn = move.Execute(Board);

            if (captureOrPawn)
            {
                noCaptureOrPawnMoves = 0;
                stateHistory.Clear();
            }
            else
            {
                noCaptureOrPawnMoves++;
            }

            Player movingPlayer = snapshot.CurrentPlayer;
            CurrentPlayer = CurrentPlayer.Opponent();
            UpdateStateString();
            CheckForGameOver();

            if (!isRedoing) redoStack.Clear();

            if (RecordMoveHistory)
            {
                string notation = MoveNotation.GetNotation(move, snapshot.Board, Board, movingPlayer);
                moveHistory.Add(new MoveRecord(move, notation, movingPlayer));
                undoStack.Push(new UndoEntry(snapshot, move, notation));
            }
            else
            {
                undoStack.Push(new UndoEntry(snapshot, move, null));
            }
        }

        public void UndoMove()
        {
            if (!CanUndo) return;

            UndoEntry entry = undoStack.Pop();
            redoStack.Push(entry.Move);
            RestoreFromSnapshot(entry.Snapshot);

            if (RecordMoveHistory && moveHistory.Count > 0)
                moveHistory.RemoveAt(moveHistory.Count - 1);
        }

        public void RedoMove()
        {
            if (!CanRedo) return;

            isRedoing = true;
            MakeMove(redoStack.Pop());
            isRedoing = false;
        }

        public IEnumerable<Move> AllLegalMovesFor(Player player)
        {
            IEnumerable<Move> candidates = Board.PiecePositionsFor(player).SelectMany(pos =>
            {
                Piece piece = Board[pos];
                return piece.GetMoves(pos, Board);
            });

            return candidates.Where(move => move.IsLegal(Board));
        }

        public bool IsGameOver() => Result != null;

        private GameSnapshot TakeSnapshot()
        {
            return new GameSnapshot(Board, CurrentPlayer, noCaptureOrPawnMoves,
                stateHistory, stateString, Result);
        }

        private void RestoreFromSnapshot(GameSnapshot snapshot)
        {
            Board.RestoreFrom(snapshot.Board);
            CurrentPlayer = snapshot.CurrentPlayer;
            noCaptureOrPawnMoves = snapshot.NoCaptureOrPawnMoves;
            stateHistory = new Dictionary<string, int>(snapshot.StateHistory);
            stateString = snapshot.StateString;
            Result = snapshot.Result;
        }

        private void CheckForGameOver()
        {
            if (!AllLegalMovesFor(CurrentPlayer).Any())
            {
                Result = Board.IsInCheck(CurrentPlayer)
                    ? Result.Win(CurrentPlayer.Opponent())
                    : Result.Draw(EndReason.Stalemate);
            }
            else if (Board.InsufficientMaterial())
            {
                Result = Result.Draw(EndReason.InsufficientMaterial);
            }
            else if (FiftyMoveRule())
            {
                Result = Result.Draw(EndReason.FiftyMoveRule);
            }
            else if (ThreefoldRepetition())
            {
                Result = Result.Draw(EndReason.ThreefoldRepetition);
            }
        }

        private bool FiftyMoveRule()
        {
            int fullMoves = noCaptureOrPawnMoves / 2;
            return fullMoves == 50;
        }

        private void UpdateStateString()
        {
            stateString = new StateString(CurrentPlayer, Board).ToString();

            if (!stateHistory.ContainsKey(stateString))
                stateHistory[stateString] = 1;
            else
                stateHistory[stateString]++;
        }

        private bool ThreefoldRepetition() => stateHistory[stateString] == 3;

        private class GameSnapshot
        {
            public Board Board { get; }
            public Player CurrentPlayer { get; }
            public int NoCaptureOrPawnMoves { get; }
            public Dictionary<string, int> StateHistory { get; }
            public string StateString { get; }
            public Result Result { get; }

            public GameSnapshot(Board board, Player player, int noCaptureOrPawnMoves,
                Dictionary<string, int> stateHistory, string stateString, Result result)
            {
                Board = board.Copy();
                CurrentPlayer = player;
                NoCaptureOrPawnMoves = noCaptureOrPawnMoves;
                StateHistory = new Dictionary<string, int>(stateHistory);
                StateString = stateString;
                Result = result;
            }
        }

        private class UndoEntry
        {
            public GameSnapshot Snapshot { get; }
            public Move Move { get; }
            public string Notation { get; }

            public UndoEntry(GameSnapshot snapshot, Move move, string notation)
            {
                Snapshot = snapshot;
                Move = move;
                Notation = notation;
            }
        }
    }
}
