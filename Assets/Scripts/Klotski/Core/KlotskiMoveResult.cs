namespace NanokaGame.Games.Klotski
{
    public enum KlotskiMoveResult
    {
        Success,
        PieceNotFound,
        InvalidDirection,
        InvalidDistance,
        OutOfBounds,
        Blocked,
        GameAlreadyCompleted
    }
}
