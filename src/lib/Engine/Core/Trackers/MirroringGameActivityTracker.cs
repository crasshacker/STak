using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Threading;
using STak.TakEngine;

namespace STak.TakEngine.Trackers
{
    public class MirroringGameActivityTracker : IGameActivityTracker
    {
        private readonly IGameActivityTracker m_tracker;
        private readonly Game                 m_game;

        public IGame                Game    { get => m_game;    }
        public IGameActivityTracker Tracker { get => m_tracker; }


        public MirroringGameActivityTracker(GamePrototype prototype, IGameActivityTracker tracker)
        {
            prototype = prototype.Clone();
            prototype.GameTimer = GameTimer.Unlimited;

            m_game    = new Game(prototype);
            m_tracker = tracker;
        }


        public void OnGameCreated(GamePrototype prototype)
        {
            m_game.Initialize();
            m_tracker.OnGameCreated(prototype);
        }


        public void OnGameStarted(Guid gameId)
        {
            m_game.Start();
            m_tracker.OnGameStarted(m_game.Id);
        }


        public void OnGameCompleted(Guid gameId)
        {
            m_tracker.OnGameCompleted(m_game.Id);
        }


        public void OnTurnStarted(Guid gameId, int turn, int playerId)
        {
            m_tracker.OnTurnStarted(m_game.Id, turn, playerId);
        }


        public void OnTurnCompleted(Guid gameId, int turn, int playerId)
        {
            m_tracker.OnTurnCompleted(m_game.Id, turn, playerId);
        }


        public void OnStoneDrawn(Guid gameId, StoneMove stoneMove, int playerId)
        {
            m_game.DrawStone(m_game.ActivePlayer.Id, stoneMove.Stone.Type, stoneMove.Stone.Id);
            m_tracker.OnStoneDrawn(gameId, m_game.StoneMove, playerId);
        }


        public void OnStoneReturned(Guid gameId, StoneMove stoneMove, int playerId)
        {
            StoneMove thisStoneMove = m_game.StoneMove;
            m_game.ReturnStone(m_game.ActivePlayer.Id);
            m_tracker.OnStoneReturned(gameId, thisStoneMove, playerId);
        }


        public void OnStonePlaced(Guid gameId, StoneMove stoneMove, int playerId)
        {
            StoneMove thisStoneMove = m_game.StoneMove;
            m_game.PlaceStone(m_game.ActivePlayer.Id, stoneMove.TargetCell, stoneMove.Stone.Type);
            m_tracker.OnStonePlaced(gameId, thisStoneMove, playerId);
        }


        public void OnStackGrabbed(Guid gameId, StackMove stackMove, int playerId)
        {
            m_game.GrabStack(m_game.ActivePlayer.Id, stackMove.StartingCell, stackMove.StoneCount);
            m_tracker.OnStackGrabbed(gameId, m_game.StackMove, playerId);
        }


        public void OnStackDropped(Guid gameId, StackMove stackMove, int playerId)
        {
            StackMove thisStackMove = m_game.StackMove;
            int distance = stackMove.DropCounts.Count;
            Cell cell = Cell.Move(stackMove.StartingCell, stackMove.Direction, distance);
            m_game.DropStack(m_game.ActivePlayer.Id, cell, stackMove.LastDropCount);
            m_tracker.OnStackDropped(gameId, thisStackMove, playerId);
        }


        public void OnMoveAborted(Guid gameId, IMove move, int playerId)
        {
            m_game.AbortMove(m_game.ActivePlayer.Id);
            m_tracker.OnMoveAborted(gameId, move, playerId);
        }


        public void OnMoveMade(Guid gameId, IMove move, int playerId)
        {
            //
            // NOTE: We don't call MakeMove because callbacks have already been made for the individual steps
            //       (DrawStone, PlaceStone, GrabStack, DropStack), and MakeMove would try to execute those
            //       steps again, and would raise an "invalid move" exception as a result.
            //
            // TODO: This could be a problem if a client application were to rely on this callback.  Further
            //       thought on how best to handle this is needed.
            //
            // m_game.MakeMove(m_game.ActivePlayer.Id, move);
            //
            m_tracker.OnMoveMade(gameId, move, playerId);
        }


        public void OnAbortInitiated(Guid gameId, IMove move, int playerId, int duration)
        {
            m_tracker.OnAbortInitiated(gameId, move, playerId, duration);
        }


        public void OnAbortCompleted(Guid gameId, IMove move, int playerId)
        {
            m_tracker.OnAbortCompleted(gameId, move, playerId);
        }


        public void OnMoveInitiated(Guid gameId, IMove move, int playerId, int duration)
        {
            m_tracker.OnMoveInitiated(gameId, move, playerId, duration);
        }


        public void OnMoveCompleted(Guid gameId, IMove move, int playerId)
        {
            m_tracker.OnMoveCompleted(gameId, move, playerId);
        }


        public void OnUndoInitiated(Guid gameId, IMove move, int playerId, int duration)
        {
            m_game.InitiateUndo(m_game.LastPlayer.Id, duration);
            m_tracker.OnUndoInitiated(gameId, move, playerId, duration);
        }


        public void OnUndoCompleted(Guid gameId, IMove move, int playerId)
        {
            m_game.CompleteUndo(m_game.LastPlayer.Id);
            m_tracker.OnUndoCompleted(gameId, move, playerId);
        }


        public void OnRedoInitiated(Guid gameId, IMove move, int playerId, int duration)
        {
            m_game.InitiateRedo(m_game.ActivePlayer.Id, duration);
            m_tracker.OnRedoInitiated(gameId, move, playerId, duration);
        }


        public void OnRedoCompleted(Guid gameId, IMove move, int playerId)
        {
            m_game.CompleteRedo(m_game.ActivePlayer.Id);
            m_tracker.OnRedoCompleted(gameId, move, playerId);
        }


        public void OnMoveCommencing(Guid gameId, IMove move, int playerId)
        {
            m_tracker.OnMoveCommencing(gameId, move, playerId);
        }


        public void OnCurrentTurnSet(Guid gameId, int turn, Stone[] stones)
        {
            m_game.SetCurrentTurn(m_game.ActivePlayer.Id, turn);
            m_tracker.OnCurrentTurnSet(gameId, turn, stones);
        }


        public void OnMoveTracked(Guid gameId, BoardPosition position)
        {
            m_game.TrackMove(m_game.ActivePlayer.Id, position);
            m_tracker.OnMoveTracked(gameId, position);
        }
    }
}
