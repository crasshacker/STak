using System;
using System.IO;
using System.Linq;
using System.Timers;
using System.Threading;
using System.Diagnostics;
using System.Collections.Generic;
using NLog;
using STak.TakEngine.Trackers;

using Timer = System.Timers.Timer;

namespace STak.TakEngine
{
    public class Game : BasicGame, IGame
    {
        private enum MoveType
        {
            Abort,
            Move,
            Undo,
            Redo
        }

        private static readonly NLog.Logger s_logger = LogManager.GetCurrentClassLogger();

        // Setting this to true changes the way moves/undos/redos initiated by a call to one
        // of the InitiateMove/Undo/Redo methods are completed in a significant way; it takes
        // responsibility for calling the appropriate CompleteMove/Undo/Redo method away from
        // the move/undo/redo initiator and puts it in the hands of the game itself.  Beware!
        private static readonly bool s_commenceMoves = false;

        private IMove        m_initiatedAbort;
        private IMove        m_initiatedMove;
        private IMove        m_initiatedUndo;
        private IMove        m_initiatedRedo;

        public IGameActivityTracker Tracker   { get; private set; }
        public StoneMove            StoneMove { get; private set; }
        public StackMove            StackMove { get; private set; }

        public Stone DrawnStone   => StoneMove?.Stone;
        public Stack GrabbedStack => StackMove?.GrabbedStack;

        public bool  IsStoneMoving => StoneMove != null;
        public bool  IsStackMoving => StackMove != null;

        public override bool IsMoveInProgress => IsStoneMoving || IsStackMoving || IsAnyTypeOfMoveInitiated;

        public void CancelOperation() => throw new Exception("CancelOperation not yet supported.");

        private IMove InitiatedAbort { set => SetInitialized(MoveType.Abort, value); get => m_initiatedAbort; }
        private IMove InitiatedMove  { set => SetInitialized(MoveType.Move,  value); get => m_initiatedMove;  }
        private IMove InitiatedUndo  { set => SetInitialized(MoveType.Undo,  value); get => m_initiatedUndo;  }
        private IMove InitiatedRedo  { set => SetInitialized(MoveType.Redo,  value); get => m_initiatedRedo;  }

        private bool IsAbortInitiated => m_initiatedAbort != null;
        private bool IsMoveInitiated  => m_initiatedMove  != null;
        private bool IsUndoInitiated  => m_initiatedUndo  != null;
        private bool IsRedoInitiated  => m_initiatedRedo  != null;


        public bool IsAnyTypeOfMoveInitiated => IsAbortInitiated
                                             || IsMoveInitiated
                                             || IsUndoInitiated
                                             || IsRedoInitiated;


        public Game(GamePrototype prototype, IGameActivityTracker tracker = null)
            : base(prototype)
        {
            Tracker = tracker;
        }


        public Game(IBasicGame game, IGameActivityTracker tracker = null)
            : base(game)
        {
            Tracker = tracker;
        }


        public override void Initialize()
        {
            if (IsInitialized)
            {
                throw new InvalidOperationException("Game has already been initialized.");
            }

            base.Initialize();
            Tracker?.OnGameCreated(Prototype);
        }


        public override void Start()
        {
            if (IsStarted)
            {
                throw new InvalidOperationException("Game has already been started.");
            }

            base.Start();
            Tracker?.OnGameStarted(Id);
        }


        public bool CanDrawStone(int playerId, StoneType stoneType)
        {
            bool canDrawStone = false;

            if (CheckActivePlayer(playerId) && IsStarted && ! IsMoveInProgress)
            {
                playerId = (ActiveTurn == 1) ? (1 - ActivePlayer.Id) : ActivePlayer.Id;
                canDrawStone = Reserves[playerId].GetAvailableStoneCount(stoneType) > 0;
            }

            return canDrawStone;
        }


        public bool CanReturnStone(int playerId)
        {
            return CheckActivePlayer(playerId) && IsStarted && IsStoneMoving;
        }


        public bool CanPlaceStone(int playerId, Cell cell)
        {
            return CheckActivePlayer(playerId) && IsStarted && IsStoneMoving && Board[cell].IsEmpty;
        }


        public bool CanGrabStack(int playerId, Cell cell, int stoneCount)
        {
            Stack stack = Board[cell];
            return CheckActivePlayer(playerId) && ! IsMoveInProgress
                                               && (ActiveTurn > 1)
                                               && (stoneCount <= stack.Count)
                                               && (stoneCount <= Board.Size)
                                               && (stack.TopStone.PlayerId == ActivePlayer.Id);
        }


        public bool CanDropStack(int playerId, Cell cell, int stoneCount)
        {
            return (CheckActivePlayer(playerId) && (IsStarted && StackMove != null))
                                  && StackMove.CanDropStack(Board, cell, stoneCount);
        }


        public bool CanAbortMove(int playerId)
        {
            return CheckActivePlayer(playerId) && (IsStarted && IsMoveInProgress);
        }


        public void DrawStone(int playerId, StoneType stoneType, int stoneId = -1)
        {
            playerId = ValidateActivePlayer(playerId);
            ValidateMove(CanDrawStone(playerId, stoneType));

            StoneMove = new StoneMove(Reserves[ActiveReserve].DrawStone(stoneType, stoneId));
            s_logger.Debug("=> DrawStone [{0}-{1}] StoneId={2}", ActiveTurn, ActivePlayer.Id, stoneId);
            Tracker?.OnStoneDrawn(Id, StoneMove, playerId);
            s_logger.Debug("<= DrawStone");
        }


        public void ReturnStone(int playerId)
        {
            playerId = ValidateActivePlayer(playerId);
            ValidateMove(CanReturnStone(playerId));

            s_logger.Debug("=> ReturnStone [{0}-{1}]", ActiveTurn, ActivePlayer.Id);
            s_logger.Debug("=> ReturnStone [{0}-{1}] StoneId={2}, Cell={3}", ActiveTurn, ActivePlayer.Id,
                                                               StoneMove.Stone.Id, StoneMove.TargetCell);
            StoneMove stoneMove = StoneMove;
            Reserves[ActiveReserve].ReturnStone(StoneMove.Stone);
            StoneMove = null;
            Tracker?.OnStoneReturned(Id, stoneMove, playerId);
            s_logger.Debug("<= ReturnStone");
        }


        public void PlaceStone(int playerId, Cell cell, StoneType stoneType)
        {
            playerId = ValidateActivePlayer(playerId);
            ValidateMove(CanPlaceStone(playerId, cell));

            s_logger.Debug("=> PlaceStone [{0}-{1}] StoneId={2}, Cell={3}", ActiveTurn, ActivePlayer.Id,
                                                                              StoneMove.Stone.Id, cell);

            StoneMove.TargetCell = cell;
            StoneMove stoneMove = StoneMove;
            StoneType currentType = stoneMove.Stone.Type;

            if (stoneType != StoneType.None)
            {
                if ((currentType == StoneType.Cap && stoneType != StoneType.Cap)
                 || (currentType != StoneType.Cap && stoneType == StoneType.Cap))
                {
                    throw new ArgumentException("Invalid stone type conversion.", nameof(stoneType));
                }
                stoneMove.Stone.Type = stoneType;
            }
            if (playerId != Player.None)
            {
                s_logger.Debug($"Player {playerId} making move: {stoneMove}");
            }
            // Return the stone because base.MakeMove will draw it again.
            Reserves[ActiveReserve].ReturnStone(StoneMove.Stone);
            MakeValidatedMove(playerId, StoneMove);
            PrepareToFinishMove();
            Tracker?.OnStonePlaced(Id, stoneMove, playerId);
            FinishMove(ActivePlayer.Id);

            s_logger.Debug("<= PlaceStone");
        }


        public void GrabStack(int playerId, Cell cell, int stoneCount)
        {
            playerId = ValidateActivePlayer(playerId);
            ValidateMove(CanGrabStack(playerId, cell, stoneCount));

            s_logger.Debug("=> GrabStack [{0}-{1}] Cell={2}, Count={3}", ActiveTurn, ActivePlayer.Id, cell, stoneCount);

            StackMove = new StackMove(cell, stoneCount, Direction.None, null);
            StackMove.GrabStack(Board);
            Tracker?.OnStackGrabbed(Id, StackMove, playerId);

            s_logger.Debug("<= GrabStack");
        }


        public void DropStack(int playerId, Cell cell, int stoneCount)
        {
            playerId = ValidateActivePlayer(playerId);
            ValidateMove(CanDropStack(playerId, cell, stoneCount));

            s_logger.Debug("=> DropStack [{0}-{1}] Cell={2}, Count={3}", ActiveTurn, ActivePlayer.Id, cell, stoneCount);

            StackMove stackMove = StackMove;
            if (StackMove.DropStack(Board, cell, stoneCount))
            {
                if (playerId != Player.None)
                {
                    s_logger.Debug($"Player {playerId} making move: {StackMove}");
                }
                MakeValidatedMove(playerId, StackMove);
                PrepareToFinishMove();
            }
            Tracker?.OnStackDropped(Id, stackMove, playerId);

            if (stackMove.HasExecuted)
            {
                FinishMove(ActivePlayer.Id);
            }

            s_logger.Debug("<= DropStack");
        }


        public new void MakeMove(int playerId, IMove move)
        {
            playerId = ValidateActivePlayer(playerId);

            // TODO - We need to validate the move before executing it.
            s_logger.Debug("=> MakeMove [{0}-{1}]", ActiveTurn, ActivePlayer.Id);

            if (move is StoneMove stoneMove)
            {
                DrawStone(playerId, stoneMove.Stone.Type, stoneMove.Stone.Id);
                stoneMove.Stone = DrawnStone;
                if (stoneMove.TargetCell != Cell.None)
                {
                    PlaceStone(playerId, stoneMove.TargetCell, stoneMove.Stone.Type);
                }
            }
            else
            {
                StackMove stackMove = move as StackMove;
                GrabStack(playerId, stackMove.StartingCell, stackMove.StoneCount);

                Cell currentCell = stackMove.StartingCell;
                Direction direction = stackMove.Direction;

                foreach (int dropCount in stackMove.DropCounts)
                {
                    currentCell = currentCell.Move(direction);
                    DropStack(playerId, currentCell, dropCount);
                }
            }

            move = move.Clone();
            (move as StackMove)?.RestoreOriginalGrabbedStack(Board);

            Tracker?.OnMoveMade(Id, move, playerId);

            s_logger.Debug("<= MakeMove");
        }


        public void AbortMove(int playerId)
        {
            playerId = ValidateActivePlayer(playerId);
            ValidateMove(IsMoveInProgress);

            IMove move;
            if (IsStoneMoving)
            {
                move = StoneMove;
                // Return the stone directly rather than calling ReturnStone, because calling that
                // method would result in Tracker.OnStoneReturned being called, and it's already
                // being called by OnMoveAborted and we don't want it to be called twice.
                Reserves[ActiveReserve].ReturnStone(StoneMove.Stone);
                StoneMove = null;
            }
            else
            {
                move = StackMove;
                StackMove.Abort(Board);
                StackMove = null;
            }

            InitiatedAbort = null;
            Tracker?.OnMoveAborted(Id, move, playerId);
        }


        public void InitiateMove(int playerId, IMove move, int duration)
        {
            playerId = ValidateActivePlayer(playerId);
            s_logger.Debug("=> InitiateMove [{0}-{1}]", ActiveTurn, ActivePlayer.Id);
            InitiatedMove = move;
            Tracker?.OnMoveInitiated(Id, move, playerId, duration);

            if (s_commenceMoves)
            {
                s_logger.Debug($"Deferring completion of move for {duration} milliseconds.");
                DeferMoveExecution(playerId, move, () => CompleteMoveOperation(playerId, move), duration);
            }

            s_logger.Debug("<= InitiateMove [{0}-{1}]", ActiveTurn, ActivePlayer.Id);
        }


        public void CompleteMove(int playerId, IMove move)
        {
            if (! s_commenceMoves)
            {
                CompleteMoveOperation(playerId, move);
            }
        }


        public void InitiateUndo(int playerId, int duration)
        {
            playerId = ValidateActivePlayer(playerId, true);
            s_logger.Debug("=> InitiateUndo [{0}-{1}]", LastTurn, LastPlayer.Id);

            var move = ExecutedMoves[^1];
            InitiatedUndo = move;
            Tracker?.OnUndoInitiated(Id, move, playerId, duration);

            if (s_commenceMoves)
            {
                s_logger.Debug($"Deferring completion of undo for {duration} milliseconds.");
                DeferMoveExecution(playerId, move, () => CompleteUndoOperation(playerId), duration);
            }

            s_logger.Debug("<= InitiateUndo [{0}-{1}]", LastTurn, LastPlayer.Id);
        }


        public void CompleteUndo(int playerId)
        {
            if (! s_commenceMoves)
            {
                CompleteUndoOperation(playerId);
            }
        }


        public void InitiateRedo(int playerId, int duration)
        {
            playerId = ValidateActivePlayer(playerId);
            s_logger.Debug("=> InitiateRedo [{0}-{1}]", ActiveTurn, ActivePlayer.Id);
            var move = RevertedMoves[^1];
            InitiatedRedo = move;
            Tracker?.OnRedoInitiated(Id, move, playerId, duration);

            if (s_commenceMoves)
            {
                s_logger.Debug($"Deferring completion of redo for {duration} milliseconds.");
                DeferMoveExecution(playerId, move, () => CompleteRedoOperation(playerId), duration);
            }

            s_logger.Debug("<= InitiateRedo [{0}-{1}]", ActiveTurn, ActivePlayer.Id);
        }


        public void CompleteRedo(int playerId)
        {
            if (! s_commenceMoves)
            {
                CompleteRedoOperation(playerId);
            }
        }


        public void InitiateAbort(int playerId, IMove move, int duration)
        {
            playerId = ValidateActivePlayer(playerId);
            s_logger.Debug("=> InitiateAbort [{0}-{1}]", ActiveTurn, ActivePlayer.Id);
            InitiatedAbort = move;
            Tracker?.OnAbortInitiated(Id, move, playerId, duration);

            if (s_commenceMoves)
            {
                s_logger.Debug($"Deferring completion of abort for {duration} milliseconds.");
                DeferMoveExecution(playerId, move, () => CompleteAbortOperation(playerId, move), duration);
            }

            s_logger.Debug("<= InitiateAbort [{0}-{1}]", ActiveTurn, ActivePlayer.Id);
        }


        public void CompleteAbort(int playerId, IMove move)
        {
            if (! s_commenceMoves)
            {
                CompleteAbortOperation(playerId, move);
            }
        }


        public void SetCurrentTurn(int playerId, int turn)
        {
            playerId = ValidateActivePlayer(playerId);
            s_logger.Debug("=> SetCurrentTurn [{0}-{1}]", playerId, turn);

            if (turn < 0 || turn > ExecutedMoves.Count + RevertedMoves.Count)
            {
                throw new ArgumentException("Invalid game position.", nameof(turn));
            }

            List<Stone>        stones = new List<Stone>();
            IEnumerable<Stack> stacks = new List<Stack>();

            IMove move;

            while (turn < ExecutedMoves.Count)
            {
                base.UndoMove(Player.None);
                move = RevertedMoves[^1];
                stacks = stacks.Union(move.GetAffectedStacks(Board));
            }
            while (turn > ExecutedMoves.Count)
            {
                base.RedoMove(Player.None);
                move = ExecutedMoves[^1];
                stacks = stacks.Union(move.GetAffectedStacks(Board));
            }

            var affectedStones = stacks.SelectMany(s => s.Stones).ToArray();
            Tracker?.OnCurrentTurnSet(Id, turn, affectedStones);

            s_logger.Debug("<= SetCurrentTurn [{0}-{1}]", playerId, turn);
        }


        public void TrackMove(int playerId, BoardPosition position)
        {
            // playerId = ValidateActivePlayer(playerId);

            // s_logger.Debug("=> TrackMove [{0}-{1}]", ActiveTurn, ActivePlayer.Id);
            Tracker?.OnMoveTracked(Id, position);
            // s_logger.Debug("<= TrackMove");
        }


        private void CompleteAbortOperation(int playerId, IMove move)
        {
            playerId = ValidateActivePlayer(playerId);
            s_logger.Debug("=> CompleteAbort [{0}-{1}]", ActiveTurn, ActivePlayer.Id);
            AbortMove(playerId); // This nullifies InitiatedAbort.
            Tracker?.OnAbortCompleted(Id, move, playerId);
            s_logger.Debug("<= CompleteAbort [{0}-{1}]", ActiveTurn, ActivePlayer.Id);
        }


        private void CompleteMoveOperation(int playerId, IMove move)
        {
            playerId = ValidateActivePlayer(playerId);
            s_logger.Debug("=> CompleteMove [{0}-{1}]", ActiveTurn, ActivePlayer.Id);

            if (InitiatedMove?.Equals(move) == false)
            {
                throw new Exception("Cannot complete move different than that which was initiated.");
            }

            // We need to set this to false bedore we call MakeMove even though the move really has been
            // initiated but not completed.  Otherwise calls to ValidateMove made directly or indirectly
            // by MakeMove will fail.  So there will be a short duration where m_initiatedMove has the
            // wrong value, but shouldn't cause a problem.  (At least I don't *think* it should...)
            InitiatedMove = null;

            MakeMove(playerId, move);
            Tracker?.OnMoveCompleted(Id, move, playerId);
            s_logger.Debug("<= CompleteMove [{0}-{1}]", LastTurn, LastPlayer.Id);
        }


        private void CompleteUndoOperation(int playerId)
        {
            playerId = ValidateActivePlayer(playerId, true);
            s_logger.Debug("=> CompleteUndo [{0}-{1}]", LastTurn, LastPlayer.Id);

            if (InitiatedUndo?.Equals(ExecutedMoves[^1]) == false)
            {
                throw new Exception("Cannot complete undo of different move than that for which undo was initiated.");
            }

            UndoMove(playerId);
            InitiatedUndo = null;
            Tracker?.OnUndoCompleted(Id, RevertedMoves[^1], playerId);
            s_logger.Debug("<= CompleteUndo [{0}-{1}]", ActiveTurn, ActivePlayer.Id);
        }


        private void CompleteRedoOperation(int playerId)
        {
            playerId = ValidateActivePlayer(playerId);
            s_logger.Debug("=> CompleteRedo [{0}-{1}]", ActiveTurn, ActivePlayer.Id);

            if (InitiatedRedo?.Equals(RevertedMoves[^1]) == false)
            {
                throw new Exception("Cannot complete redo of different move than that for which redo was initiated.");
            }

            RedoMove(playerId);
            InitiatedRedo = null;
            Tracker?.OnRedoCompleted(Id, ExecutedMoves[^1], playerId);
            s_logger.Debug("<= CompleteRedo [{0}-{1}]", LastTurn, LastPlayer.Id);
        }


        private void DeferMoveExecution(int playerId, IMove move, Action completeMovement, int duration)
        {
            if (s_commenceMoves)
            {
                var timer = new Timer(duration);
                timer.Elapsed += (s, o) =>
                {
                    Tracker?.OnMoveCommencing(Id, move, playerId);
                    completeMovement();
                    timer.Dispose();
                };
                timer.AutoReset = false;
                timer.Enabled = true;
                timer.Start();
            }
        }


        private void PrepareToFinishMove()
        {
            SwitchPlayer();
        }


        private void FinishMove(int playerId)
        {
            playerId = ValidateActivePlayer(playerId);

            int GetActiveTurn()
            {
                // The move has already been added to the ExecutedMoves list, so the ActiveTurn property
                // will be wrong if PlayerTwo is the player calling FinishMove.  To work around this we
                // remove the move from the executed list, compute the active turn, and restore the move.
                var move = ExecutedMoves[^1];
                ExecutedMoves.RemoveAt(ExecutedMoves.Count-1);
                var activeTurn = ActiveTurn;
                ExecutedMoves.Add(move);
                return activeTurn;
            }

            // Set to null BEFORE raising TurnCompleted event.
            StoneMove = null;
            StackMove = null;

            int activeTurn = GetActiveTurn();
            s_logger.Debug("=> FinishMove [{0}-{1}]", activeTurn, ActivePlayer.Id);

            Tracker?.OnTurnCompleted(Id, activeTurn, ActivePlayer.Id);
            SwitchPlayer();

            if (IsCompleted)
            {
                Tracker?.OnGameCompleted(Id);
            }
            else
            {
                Tracker?.OnTurnStarted(Id, ActiveTurn, ActivePlayer.Id);
            }

            s_logger.Debug("<= FinishMove [{0}-{1}]", LastTurn, LastPlayer.Id);
        }


        private void SetInitialized(MoveType moveType, IMove move)
        {
            if (move != null && IsAnyTypeOfMoveInitiated)
            {
                var snippet1 = moveType switch
                {
                    MoveType.Abort => "abort a move",
                    MoveType.Move  => "make a move",
                    MoveType.Undo  => "undo a move",
                    MoveType.Redo  => "redo a move",
                    _              => null
                };

                var snippet2 = IsAbortInitiated ? "a move is being aborted"
                             : IsMoveInitiated  ? "a move is being made"
                             : IsUndoInitiated  ? "a move is being undone"
                             : IsRedoInitiated  ? "a move is being redone"
                             : throw new Exception($"Invalid MoveType: moveType.");

                throw new Exception($"Cannot {snippet1} a move while {snippet2}.");
            }

            if      (moveType == MoveType.Abort) { m_initiatedAbort = move; }
            else if (moveType == MoveType.Move)  { m_initiatedMove  = move; }
            else if (moveType == MoveType.Undo)  { m_initiatedUndo  = move; }
            else if (moveType == MoveType.Redo)  { m_initiatedRedo  = move; }
            else { throw new Exception($"Invalid MoveType: moveType."); }
        }


        protected override void GameTimerHandler(object source, ElapsedEventArgs e)
        {
            base.GameTimerHandler(source, e);
            // TODO - How does event handler get the game result, and know that it's a Time win?
            GameTimer = GameTimer.Unlimited;
            Tracker?.OnGameCompleted(Id);
        }
    }
}
