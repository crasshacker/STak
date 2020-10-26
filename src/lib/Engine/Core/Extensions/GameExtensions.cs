using System;

namespace STak.TakEngine.Extensions
{
    public static class GameExtensions
    {
        public static bool CanDrawStone(this IGame game, StoneType stoneType)                => game.CanDrawStone(Player.None, stoneType);
        public static bool CanReturnStone(this IGame game)                                   => game.CanReturnStone(Player.None);
        public static bool CanPlaceStone(this IGame game, Cell cell)                         => game.CanPlaceStone(Player.None, cell);
        public static bool CanGrabStack(this IGame game, Cell cell, int stoneCount)          => game.CanGrabStack(Player.None, cell, stoneCount);
        public static bool CanDropStack(this IGame game, Cell cell, int stoneCount)          => game.CanDropStack(Player.None, cell, stoneCount);
        public static bool CanAbortMove(this IGame game)                                     => game.CanAbortMove(Player.None);

        public static void DrawStone(this IGame game, StoneType stoneType, int stoneId = -1) => game.DrawStone(Player.None, stoneType, stoneId);
        public static void ReturnStone(this IGame game)                                      => game.ReturnStone(Player.None);
        public static void PlaceStone(this IGame game, Cell cell)                            => game.PlaceStone(Player.None, cell, StoneType.None);
        public static void PlaceStone(this IGame game, Cell cell, StoneType stoneType)       => game.PlaceStone(Player.None, cell, stoneType);
        public static void GrabStack(this IGame game, Cell cell, int stoneCount)             => game.GrabStack(Player.None, cell, stoneCount);
        public static void DropStack(this IGame game, Cell cell, int stoneCount)             => game.DropStack(Player.None, cell, stoneCount);
        public static void AbortMove(this IGame game)                                        => game.AbortMove(Player.None);

        public static void TrackMove(this IGame game, BoardPosition position)                => game.TrackMove(Player.None, position);
        public static void SetCurrentTurn(this IGame game, int turn)                         => game.SetCurrentTurn(Player.None, turn);

        public static void InitiateAbort(this IGame game, IMove move, int duration)          => game.InitiateAbort(Player.None, move, duration);
        public static void CompleteAbort(this IGame game, IMove move)                        => game.CompleteAbort(Player.None, move);
        public static void InitiateMove(this IGame game, IMove move, int duration)           => game.InitiateMove(Player.None, move, duration);
        public static void CompleteMove(this IGame game, IMove move)                         => game.CompleteMove(Player.None, move);
        public static void InitiateUndo(this IGame game, int duration)                       => game.InitiateUndo(Player.None, duration);
        public static void CompleteUndo(this IGame game)                                     => game.CompleteUndo(Player.None);
        public static void InitiateRedo(this IGame game, int duration)                       => game.InitiateRedo(Player.None, duration);
        public static void CompleteRedo(this IGame game)                                     => game.CompleteRedo(Player.None);
    }
}



