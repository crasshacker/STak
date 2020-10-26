using System;
using System.Threading;
using System.Collections.Generic;

namespace STak.TakEngine
{
    public interface IBasicGameState
    {
        Guid            Id            { get; }
        GamePrototype   Prototype     { get; }

        Board           Board         { get; }
        BitBoard        BitBoard      { get; }
        List<IMove>     ExecutedMoves { get; }
        List<IMove>     RevertedMoves { get; }
        Player[]        Players       { get; }
        PlayerReserve[] Reserves      { get; }
        Player          ActivePlayer  { get; }
        GameResult      Result        { get; }

        Player          PlayerOne     { get; }
        Player          PlayerTwo     { get; }
        Player          LastPlayer    { get; }
        int             ActiveReserve { get; }
        int             LastReserve   { get; }
        int             ActivePly     { get; }
        int             ActiveTurn    { get; }
        int             LastTurn      { get; }
        IMove           LastMove      { get; }

        bool            IsInitialized { get; }
        bool            IsStarted     { get; }
        bool            IsInProgress  { get; }
        bool            IsCompleted   { get; }
        bool            WasCompleted  { get; }

        bool CanUndoMove(int playerId);
        bool CanRedoMove(int playerId);
        bool CanMakeMove(int playerId, IMove move);
    }
}
