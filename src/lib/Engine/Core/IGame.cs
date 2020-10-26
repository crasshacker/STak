using System;
using System.Threading;
using System.Collections.Generic;
using STak.TakEngine.Trackers;

namespace STak.TakEngine
{
    public interface IGame : IBasicGame, IGameState
    {
        IGameActivityTracker Tracker { get; }

        void CancelOperation();

        void DrawStone      (int playerId, StoneType stoneType, int stoneId = -1);
        void ReturnStone    (int playerId);
        void PlaceStone     (int playerId, Cell cell, StoneType stoneType);
        void GrabStack      (int playerId, Cell cell, int stoneCount);
        void DropStack      (int playerId, Cell cell, int stoneCount);
        void AbortMove      (int playerId);
        void TrackMove      (int playerId, BoardPosition position);
        void SetCurrentTurn (int playerId, int turn);

        void InitiateAbort  (int playerId, IMove move, int duration);
        void CompleteAbort  (int playerId, IMove move);
        void InitiateMove   (int playerId, IMove move, int duration);
        void CompleteMove   (int playerId, IMove move);
        void InitiateUndo   (int playerId, int duration);
        void CompleteUndo   (int playerId);
        void InitiateRedo   (int playerId, int duration);
        void CompleteRedo   (int playerId);
    }
}
