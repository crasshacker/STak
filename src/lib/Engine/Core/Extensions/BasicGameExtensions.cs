using System;

namespace STak.TakEngine.Extensions
{
    public static class BasicGameExtensions
    {
        public static bool CanUndoMove(this IBasicGame game) => game.CanUndoMove(Player.None);
        public static bool CanRedoMove(this IBasicGame game) => game.CanRedoMove(Player.None);
        public static bool CanMakeMove(this IBasicGame game, IMove move) => game.CanMakeMove(Player.None, move);

        public static void UndoMove(this IBasicGame game) => game.UndoMove(Player.None);
        public static void RedoMove(this IBasicGame game) => game.RedoMove(Player.None);
        public static void MakeMove(this IBasicGame game, IMove move) => game.MakeMove(Player.None, move);
    }
}


