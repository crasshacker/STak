using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Threading;
using NLog;
using STak.TakEngine;

namespace STak.TakEngine.Trackers
{
    public class EventRaisingGameActivityTracker : IEventBasedGameActivityTracker
    {
        private static readonly NLog.Logger s_logger = LogManager.GetCurrentClassLogger();

        public event EventHandler<GameCreatedEventArgs>    GameCreated;
        public event EventHandler<GameStartedEventArgs>    GameStarted;
        public event EventHandler<GameCompletedEventArgs>  GameCompleted;
        public event EventHandler<TurnStartedEventArgs>    TurnStarted;
        public event EventHandler<TurnCompletedEventArgs>  TurnCompleted;
        public event EventHandler<MoveAbortedEventArgs>    MoveAborted;
        public event EventHandler<MoveMadeEventArgs>       MoveMade;
        public event EventHandler<AbortInitiatedEventArgs> AbortInitiated;
        public event EventHandler<AbortCompletedEventArgs> AbortCompleted;
        public event EventHandler<MoveInitiatedEventArgs>  MoveInitiated;
        public event EventHandler<MoveCompletedEventArgs>  MoveCompleted;
        public event EventHandler<UndoInitiatedEventArgs>  UndoInitiated;
        public event EventHandler<UndoCompletedEventArgs>  UndoCompleted;
        public event EventHandler<RedoInitiatedEventArgs>  RedoInitiated;
        public event EventHandler<RedoCompletedEventArgs>  RedoCompleted;
        public event EventHandler<MoveCommencingEventArgs> MoveCommencing;
        public event EventHandler<StoneDrawnEventArgs>     StoneDrawn;
        public event EventHandler<StonePlacedEventArgs>    StonePlaced;
        public event EventHandler<StoneReturnedEventArgs>  StoneReturned;
        public event EventHandler<StackGrabbedEventArgs>   StackGrabbed;
        public event EventHandler<StackDroppedEventArgs>   StackDropped;
        public event EventHandler<StackModifiedEventArgs>  StackModified;
        public event EventHandler<CurrentTurnSetEventArgs> CurrentTurnSet;
        public event EventHandler<MoveTrackedEventArgs>    MoveTracked;

        public IGame Game { get; set; }


        public EventRaisingGameActivityTracker(IGame game = null)
        {
            Game = game;
        }


        public virtual void OnGameCreated(GamePrototype prototype)
        {
            s_logger.Debug(">= OnGameCreated");
            GameCreated?.Invoke(this, new GameCreatedEventArgs(prototype));
            s_logger.Debug("<= OnGameCreated");
        }


        public virtual void OnGameStarted(Guid gameId)
        {
            s_logger.Debug("=> OnGameStarted");
            GameStarted?.Invoke(this, new GameStartedEventArgs(gameId));
            TurnStarted?.Invoke(this, new TurnStartedEventArgs(1, Player.One));
            s_logger.Debug("<= OnGameStarted");
        }


        public virtual void OnGameCompleted(Guid gameId)
        {
            s_logger.Debug("=> OnGameCompleted");
            GameCompleted?.Invoke(this, new GameCompletedEventArgs(gameId));
            s_logger.Debug("<= OnGameCompleted");
        }


        public virtual void OnTurnStarted(Guid gameId, int turn, int playerId)
        {
            s_logger.Debug("=> OnTurnStarted");
            TurnStarted?.Invoke(this, new TurnStartedEventArgs(turn, playerId));
            s_logger.Debug("<= OnTurnStarted");
        }


        public virtual void OnTurnCompleted(Guid gameId, int turn, int playerId)
        {
            s_logger.Debug("=> OnTurnCompleted");
            TurnCompleted?.Invoke(this, new TurnCompletedEventArgs(turn, playerId));
            s_logger.Debug("<= OnTurnCompleted");
        }


        public virtual void OnStoneDrawn(Guid gameId, StoneMove stoneMove, int playerId)
        {
            s_logger.Debug("=> OnStoneDrawn [{0}-{1}]", Game.ActiveTurn, Game.ActivePlayer.Id);
            StoneDrawn?.Invoke(this, new StoneDrawnEventArgs(stoneMove.Stone, playerId));
            s_logger.Debug("<= OnStoneDrawn");
        }


        public virtual void OnStoneReturned(Guid gameId, StoneMove stoneMove, int playerId)
        {
            s_logger.Debug("=> OnStoneReturned [{0}-{1}]", Game.ActiveTurn, Game.ActivePlayer.Id);
            StoneReturned?.Invoke(this, new StoneReturnedEventArgs(stoneMove.Stone, playerId));
            s_logger.Debug("<= OnStoneReturned");
        }


        public virtual void OnStonePlaced(Guid gameId, StoneMove stoneMove, int playerId)
        {
            int turn = (Game.LastPlayer.Id == Player.One) ? Game.ActiveTurn : Game.LastTurn;
            s_logger.Debug("=> OnStonePlaced [{0}-{1}]", turn, Game.LastPlayer.Id);
            StonePlaced?.Invoke(this, new StonePlacedEventArgs(stoneMove.TargetCell, stoneMove.Stone, playerId));
            StackModified?.Invoke(this, new StackModifiedEventArgs(Game.Board[stoneMove.TargetCell]));
            s_logger.Debug("<= OnStonePlaced");
        }


        public virtual void OnStackGrabbed(Guid gameId, StackMove stackMove, int playerId)
        {
            s_logger.Debug("=> OnStackGrabbed [{0}-{1}]", Game.ActiveTurn, Game.ActivePlayer.Id);
            StackGrabbed?.Invoke(this, new StackGrabbedEventArgs(stackMove.GrabbedStack, playerId));
            s_logger.Debug("<= OnStackGrabbed");
        }


        public virtual void OnStackDropped(Guid gameId, StackMove stackMove, int playerId)
        {
            int turn = (Game.LastPlayer.Id == Player.One || ! stackMove.HasExecuted) ? Game.ActiveTurn : Game.LastTurn;
            s_logger.Debug("=> OnStackDropped [{0}-{1}]", turn, Game.LastPlayer.Id);
            StackDropped?.Invoke(this, new StackDroppedEventArgs(stackMove, playerId));
            Cell cell = Cell.Move(stackMove.StartingCell, stackMove.Direction, stackMove.DropCounts.Count);
            StackModified?.Invoke(this, new StackModifiedEventArgs(Game.Board[cell]));
            s_logger.Debug("<= OnStackDropped");
        }


        public virtual void OnMoveAborted(Guid gameId, IMove move, int playerId)
        {
            s_logger.Debug("=> OnMoveAborted");
            if (move is StoneMove stoneMove)
            {
                OnStoneReturned(gameId, stoneMove, playerId);
            }
            else
            {
                foreach (Stack stack in move.GetAffectedStacks(Game.Board))
                {
                    StackModified?.Invoke(this, new StackModifiedEventArgs(stack));
                }
                MoveAborted?.Invoke(this, new MoveAbortedEventArgs(move, playerId));
            }
            s_logger.Debug("<= OnMoveAborted");
        }


        public virtual void OnMoveMade(Guid gameId, IMove move, int playerId)
        {
            s_logger.Debug("=> OnMoveMade");
            MoveMade?.Invoke(this, new MoveMadeEventArgs(move, playerId));
            s_logger.Debug("<= OnMoveMade");
        }


        public virtual void OnAbortInitiated(Guid gameId, IMove move, int playerId, int duration)
        {
            s_logger.Debug("=> OnAbortInitiated");
            AbortInitiated?.Invoke(this, new AbortInitiatedEventArgs(move, playerId, duration));
            s_logger.Debug("<= OnAbortInitiated");
        }


        public virtual void OnAbortCompleted(Guid gameId, IMove move, int playerId)
        {
            s_logger.Debug("=> OnAbortCompleted");
            AbortCompleted?.Invoke(this, new AbortCompletedEventArgs(move, playerId));
            s_logger.Debug("<= OnAbortCompleted");
        }


        public virtual void OnMoveInitiated(Guid gameId, IMove move, int playerId, int duration)
        {
            s_logger.Debug("=> OnMoveInitiated");
            MoveInitiated?.Invoke(this, new MoveInitiatedEventArgs(move, playerId, duration));
            s_logger.Debug("<= OnMoveInitiated");
        }


        public virtual void OnMoveCompleted(Guid gameId, IMove move, int playerId)
        {
            s_logger.Debug("=> OnMoveCompleted");
            MoveCompleted?.Invoke(this, new MoveCompletedEventArgs(move, playerId));
            s_logger.Debug("<= OnMoveCompleted");
        }


        public virtual void OnUndoInitiated(Guid gameId, IMove move, int playerId, int duration)
        {
            s_logger.Debug("=> OnUndoInitiated");
            UndoInitiated?.Invoke(this, new UndoInitiatedEventArgs(move, playerId, duration));
            s_logger.Debug("<= OnUndoInitiated");
        }


        public virtual void OnUndoCompleted(Guid gameId, IMove move, int playerId)
        {
            s_logger.Debug("=> OnUndoCompleted");

            if (move is StoneMove stoneMove)
            {
                OnStoneReturned(gameId, stoneMove, playerId);
            }

            foreach (Stack stack in move.GetAffectedStacks(Game.Board))
            {
                StackModified?.Invoke(this, new StackModifiedEventArgs(stack));
            }

            // Report undoing the move *before* starting the next turn, so that AI players recognize
            // when one of their moves has been undone so that they can ignore the next TurnStarted
            // event, which would otherwise cause the AI to immediately make another move.
            UndoCompleted?.Invoke(this, new UndoCompletedEventArgs(move, playerId));
            OnTurnStarted(gameId, Game.ActiveTurn, Game.ActivePlayer.Id);

            s_logger.Debug("<= OnUndoCompleted");
        }


        public virtual void OnRedoInitiated(Guid gameId, IMove move, int playerId, int duration)
        {
            s_logger.Debug("=> OnRedoInitiated");
            RedoInitiated?.Invoke(this, new RedoInitiatedEventArgs(move, playerId, duration));
            s_logger.Debug("<= OnRedoInitiated");
        }


        public virtual void OnRedoCompleted(Guid gameId, IMove move, int playerId)
        {
            s_logger.Debug("=> OnRedoCompleted");

            StoneMove stoneMove = move as StoneMove;
            StackMove stackMove = move as StackMove;

            if (move is StoneMove)
            {
                OnStoneDrawn(gameId, stoneMove, playerId);
                // Don't call OnStonePlaced; it invokes StackModified, and we do that ourselves below.
                StonePlaced?.Invoke(this, new StonePlacedEventArgs(stoneMove.TargetCell, stoneMove.Stone, playerId));
            }

            foreach (Stack stack in move.GetAffectedStacks(Game.Board))
            {
                StackModified?.Invoke(this, new StackModifiedEventArgs(stack));
            }

            RedoCompleted?.Invoke(this, new RedoCompletedEventArgs(move, playerId));

            if (Game.IsCompleted)
            {
                OnGameCompleted(Game.Id);
            }
            else
            {
                OnTurnStarted(gameId, Game.ActiveTurn, Game.ActivePlayer.Id);
            }

            s_logger.Debug("<= OnRedoCompleted");
        }


        public virtual void OnMoveCommencing(Guid gameId, IMove move, int playerId)
        {
            s_logger.Debug("=> OnMoveCommencing");
            MoveCommencing?.Invoke(this, new MoveCommencingEventArgs(move, playerId));
            s_logger.Debug("<= OnMoveCommencing");
        }


        public virtual void OnCurrentTurnSet(Guid gameId, int turn, Stone[] stones)
        {
            CurrentTurnSet?.Invoke(this, new CurrentTurnSetEventArgs(turn, stones));

            if (Game.IsCompleted)
            {
                OnGameCompleted(Game.Id);
            }
            else
            {
                OnTurnStarted(gameId, Game.ActiveTurn, Game.ActivePlayer.Id);
            }
        }


        public virtual void OnMoveTracked(Guid gameId, BoardPosition position)
        {
            // s_logger.Debug("=> OnMoveTracked");
            MoveTracked?.Invoke(this, new MoveTrackedEventArgs(position));
            // s_logger.Debug("<= OnMoveTracked");
        }
    }
}
