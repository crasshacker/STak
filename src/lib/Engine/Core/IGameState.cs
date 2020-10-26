using System;
using System.Collections.Generic;
using STak.TakEngine;

namespace STak.TakEngine
{
    public interface IGameState : IBasicGameState
    {
        StoneMove       StoneMove        { get; }
        StackMove       StackMove        { get; }
        Stone           DrawnStone       { get; }
        Stack           GrabbedStack     { get; }

        bool            IsStoneMoving    { get; }
        bool            IsStackMoving    { get; }
        bool            IsMoveInProgress { get; }

        bool CanDrawStone   (int playerId, StoneType stoneType);
        bool CanReturnStone (int playerId);
        bool CanPlaceStone  (int playerId, Cell cell);
        bool CanGrabStack   (int playerId, Cell cell, int stoneCount);
        bool CanDropStack   (int playerId, Cell cell, int stoneCount);
        bool CanAbortMove   (int playerId);
    }
}
