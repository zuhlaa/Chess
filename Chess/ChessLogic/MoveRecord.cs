namespace ChessLogic
{
    public class MoveRecord
    {
        public Move Move { get; }
        public string Notation { get; }
        public Player MovedBy { get; }

        public MoveRecord(Move move, string notation, Player movedBy)
        {
            Move = move;
            Notation = notation;
            MovedBy = movedBy;
        }
    }
}
