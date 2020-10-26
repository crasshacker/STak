using System;
using System.Threading;
using System.Collections.Generic;
using Akka.Actor;
using STak.TakEngine;
using STak.TakEngine.Trackers;

namespace STak.TakEngine.Actors
{
    public class GameActorFacade : IGame, IGameState
    {
        private readonly IActorRef  m_gameActor;
        private          IGameState m_gameState;


        public GameActorFacade(IActorRef gameExecutorActor, IGameActivityTracker tracker)
        {
            m_gameActor = gameExecutorActor;
            Tracker     = tracker;
        }


        private GameActorFacade()
        {
        }

        public IGameActivityTracker Tracker { get; }

        // Redirect all queries to the instance holding the current state.

        public Guid            Id               => m_gameState.Id;
        public GamePrototype   Prototype        => m_gameState.Prototype;

        public Board           Board            => m_gameState.Board;
        public BitBoard        BitBoard         => m_gameState.BitBoard;
        public List<IMove>     ExecutedMoves    => m_gameState.ExecutedMoves;
        public List<IMove>     RevertedMoves    => m_gameState.RevertedMoves;
        public Player[]        Players          => m_gameState.Players;
        public PlayerReserve[] Reserves         => m_gameState.Reserves;
        public Player          ActivePlayer     => m_gameState.ActivePlayer;
        public GameResult      Result           => m_gameState.Result;

        public Player          PlayerOne        => m_gameState.PlayerOne;
        public Player          PlayerTwo        => m_gameState.PlayerTwo;
        public Player          LastPlayer       => m_gameState.LastPlayer;
        public int             ActiveReserve    => m_gameState.ActiveReserve;
        public int             LastReserve      => m_gameState.LastReserve;
        public int             ActivePly        => m_gameState.ActivePly;
        public int             ActiveTurn       => m_gameState.ActiveTurn;
        public int             LastTurn         => m_gameState.LastTurn;
        public IMove           LastMove         => m_gameState.LastMove;

        public StoneMove       StoneMove        => m_gameState.StoneMove;
        public StackMove       StackMove        => m_gameState.StackMove;
        public Stone           DrawnStone       => m_gameState.DrawnStone;
        public Stack           GrabbedStack     => m_gameState.GrabbedStack;
        public bool            IsStoneMoving    => m_gameState.IsStoneMoving;
        public bool            IsStackMoving    => m_gameState.IsStackMoving;
        public bool            IsMoveInProgress => m_gameState.IsMoveInProgress;

        public bool            IsInitialized    => m_gameState.IsInitialized;
        public bool            IsStarted        => m_gameState.IsStarted;
        public bool            IsInProgress     => m_gameState.IsInProgress;
        public bool            IsCompleted      => m_gameState.IsCompleted;
        public bool            WasCompleted     => m_gameState.WasCompleted;

        // Query methods.

        public bool CanUndoMove(int playerId)                                      => m_gameState.CanUndoMove(playerId);
        public bool CanRedoMove(int playerId)                                      => m_gameState.CanRedoMove(playerId);
        public bool CanMakeMove(int playerId, IMove move)                          => m_gameState.CanMakeMove(playerId, move);

        public bool CanDrawStone(int playerId, StoneType stoneType)                => m_gameState.CanDrawStone(playerId, stoneType);
        public bool CanReturnStone(int playerId)                                   => m_gameState.CanReturnStone(playerId);
        public bool CanPlaceStone(int playerId, Cell cell)                         => m_gameState.CanPlaceStone(playerId, cell);
        public bool CanGrabStack(int playerId, Cell cell, int stoneCount)          => m_gameState.CanGrabStack(playerId, cell, stoneCount);
        public bool CanDropStack(int playerId, Cell cell, int stoneCount)          => m_gameState.CanDropStack(playerId, cell, stoneCount);
        public bool CanAbortMove(int playerId)                                     => m_gameState.CanAbortMove(playerId);

        // Game action methods.

        private void Tell(GameActorMessage message) => m_gameActor.Tell(message, m_gameActor);

        public void Initialize()                                                   => Tell(new InitializeMessage());
        public void Start()                                                        => Tell(new StartMessage());

        public void UndoMove(int playerId)                                         => Tell(new UndoMoveMessage(playerId));
        public void RedoMove(int playerId)                                         => Tell(new RedoMoveMessage(playerId));
        public void MakeMove(int playerId, IMove move)                             => Tell(new MakeMoveMessage(playerId, move));
        public void DrawStone(int playerId, StoneType stoneType, int stoneId = -1) => Tell(new DrawStoneMessage(playerId, stoneType, stoneId));
        public void ReturnStone(int playerId)                                      => Tell(new ReturnStoneMessage(playerId));
        public void PlaceStone(int playerId, Cell cell, StoneType stoneType)       => Tell(new PlaceStoneMessage(playerId, cell, stoneType));
        public void GrabStack(int playerId, Cell cell, int stoneCount)             => Tell(new GrabStackMessage(playerId, cell, stoneCount));
        public void DropStack(int playerId, Cell cell, int stoneCount)             => Tell(new DropStackMessage(playerId, cell, stoneCount));
        public void AbortMove(int playerId)                                        => Tell(new AbortMoveMessage(playerId));
        public void SetCurrentTurn(int playerId, int turn)                         => Tell(new SetCurrentTurnMessage(playerId, turn));
        public void TrackMove(int playerId, BoardPosition position)                => Tell(new TrackMoveMessage(playerId, position));

        public void InitiateAbort(int playerId, IMove move, int duration)          => Tell(new InitiateAbortMessage(playerId, move, duration));
        public void CompleteAbort(int playerId, IMove move)                        => Tell(new CompleteAbortMessage(playerId, move));
        public void InitiateMove(int playerId, IMove move, int duration)           => Tell(new InitiateMoveMessage(playerId, move, duration));
        public void CompleteMove(int playerId, IMove move)                         => Tell(new CompleteMoveMessage(playerId, move));
        public void InitiateUndo(int playerId, int duration)                       => Tell(new InitiateUndoMessage(playerId, duration));
        public void CompleteUndo(int playerId)                                     => Tell(new CompleteUndoMessage(playerId));
        public void InitiateRedo(int playerId, int duration)                       => Tell(new InitiateRedoMessage(playerId, duration));
        public void CompleteRedo(int playerId)                                     => Tell(new CompleteRedoMessage(playerId));

        public void HumanizePlayer(int playerId, string name)                      => Tell(new HumanizePlayerMessage(playerId, name));

        public void ChangePlayer(Player player)                                    => Tell(new ChangePlayerMessage(player));
        public void CancelOperation()                                              => throw new Exception("CancelOperation not yet supported.");


        public void InitializeGameState()
        {
            var game = FindMirroringGame(Tracker);

            if (game == null)
            {
                throw new Exception("Actor-based games must use a mirroring tracker to maintain game state.");
            }

            game.PlayerOne.Join(this, Player.One);
            game.PlayerTwo.Join(this, Player.Two);

            m_gameState = game;
        }


        private IGame FindMirroringGame(IGameActivityTracker tracker)
        {
            IGame game = null;

            if (tracker is DispatchingGameActivityTracker dispatcher)
            {
                game = dispatcher.Tracker switch
                {
                    MirroringGameActivityTracker    mirroringTracker => mirroringTracker.Game,
                    EventRaisingGameActivityTracker eventingTracker  => eventingTracker.Game,
                    _ => null
                };
            }
            else if (tracker is FanoutGameActivityTracker fanner)
            {
                game = fanner.GetTracker<MirroringGameActivityTracker>()?.Game
                    ?? fanner.GetTracker<EventRaisingGameActivityTracker>()?.Game;
            }

            return game;
        }
    }
}
