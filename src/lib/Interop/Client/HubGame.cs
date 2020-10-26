using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.AspNetCore.SignalR.Client;
using STak.TakEngine;
using STak.TakEngine.Trackers;
using STak.TakHub.Interop;
using STak.TakHub.Interop.Dto;

namespace STak.TakHub.Client
{
    public class HubGame : IGame, IGameState
    {
        private readonly IGameState    m_gameState;
        private readonly HubConnection m_connection;


        public HubGame(GameHubClient hubClient, GamePrototype prototype, IGameActivityTracker tracker)
        {
            HubClient = hubClient;
            Prototype = prototype;
            Tracker   = tracker;

            var mirror = FindMirroringGame();
            m_gameState = mirror as IGameState;

            //
            // This is necessary in the case where a local player is an AI, so that when that player
            // receives a TurnStarted event and calls Game.MakeMove after choosing a move (locally),
            // the call is forwarded to the game running on the hub.  Associating each of the players
            // with the top-level game rather than the local tracking game is appropriate in any case.
            //
            mirror.PlayerOne.Join(this, Player.One);
            mirror.PlayerTwo.Join(this, Player.Two);

            m_connection = hubClient.Connection;
        }

        public GameHubClient        HubClient { get; }
        public GamePrototype        Prototype { get; }
        public IGameActivityTracker Tracker   { get; set; }

        public IGame BaseGame => m_gameState as IGame;

        // Pass-through properties and methods for local game state queries.

        public Board           Board            => m_gameState.Board;
        public BitBoard        BitBoard         => m_gameState.BitBoard;
        public List<IMove>     ExecutedMoves    => m_gameState.ExecutedMoves;
        public List<IMove>     RevertedMoves    => m_gameState.RevertedMoves;
        public Player[]        Players          => m_gameState.Players;
        public PlayerReserve[] Reserves         => m_gameState.Reserves;
        public Player          ActivePlayer     => m_gameState.ActivePlayer;
        public GameResult      Result           => m_gameState.Result;

        public Guid            Id               => m_gameState.Id;
        public Player          PlayerOne        => m_gameState.PlayerOne;
        public Player          PlayerTwo        => m_gameState.PlayerTwo;
        public Player          LastPlayer       => m_gameState.LastPlayer;
        public int             ActiveReserve    => m_gameState.ActiveReserve;
        public int             LastReserve      => m_gameState.LastReserve;
        public int             ActivePly        => m_gameState.ActivePly;
        public int             ActiveTurn       => m_gameState.ActiveTurn;
        public int             LastTurn         => m_gameState.LastTurn;
        public IMove           LastMove         => m_gameState.LastMove;

        public bool            IsInitialized    => m_gameState.IsInitialized;
        public bool            IsStarted        => m_gameState.IsStarted;
        public bool            IsInProgress     => m_gameState.IsInProgress;
        public bool            IsCompleted      => m_gameState.IsCompleted;
        public bool            WasCompleted     => m_gameState.WasCompleted;

        public StoneMove       StoneMove        => m_gameState.StoneMove;
        public StackMove       StackMove        => m_gameState.StackMove;
        public Stone           DrawnStone       => m_gameState.DrawnStone;
        public Stack           GrabbedStack     => m_gameState.GrabbedStack;
        public bool            IsStoneMoving    => m_gameState.IsStoneMoving;
        public bool            IsStackMoving    => m_gameState.IsStackMoving;
        public bool            IsMoveInProgress => m_gameState.IsMoveInProgress;

        //
        // Currently the TakHub server is responsible for initializing and starting the game,
        // so we do nothing here.  This is ugly and should probably be revisited to determine
        // whether a cleaner alternative (where the client is responsible for calling these
        // methods) could be implemented.
        //
        public void Initialize() { }
        public void Start()      { }

        public bool CanUndoMove(int playerId)                                   => m_gameState.CanUndoMove(playerId);
        public bool CanRedoMove(int playerId)                                   => m_gameState.CanRedoMove(playerId);
        public bool CanMakeMove(int playerId, IMove move)                       => m_gameState.CanMakeMove(playerId, move);

        public bool CanDrawStone(int playerId, StoneType stoneType)             => m_gameState.CanDrawStone(playerId, stoneType);
        public bool CanReturnStone(int playerId)                                => m_gameState.CanReturnStone(playerId);
        public bool CanPlaceStone(int playerId, Cell cell)                      => m_gameState.CanPlaceStone(playerId, cell);
        public bool CanGrabStack(int playerId, Cell cell, int stoneCount)       => m_gameState.CanGrabStack(playerId, cell, stoneCount);
        public bool CanDropStack(int playerId, Cell cell, int stoneCount)       => m_gameState.CanDropStack(playerId, cell, stoneCount);
        public bool CanAbortMove(int playerId)                                  => m_gameState.CanAbortMove(playerId);

        // TODO - Not yet supported.
        public void ChangePlayer(Player player)                                 => throw new Exception("ChangePlayer not yet supported.");

        public async void HumanizePlayer(int playerId, string name)
            => await HubCallMediator.InvokeCommandAsync(GameCommand.HumanizePlayer, async () =>
               await m_connection.InvokeAsync("HumanizePlayer", Prototype.Id, playerId, name));
        public async void CancelOperation()
            => await HubCallMediator.InvokeCommandAsync(GameCommand.CancelOperation, async () =>
               await m_connection.InvokeAsync("CancelOperation", Prototype.Id));

        public async void DrawStone(int playerId, StoneType stoneType, int stoneId = -1)
            => await HubCallMediator.InvokeCommandAsync(GameCommand.DrawStone, async () =>
               await m_connection.InvokeAsync("DrawStone", Prototype.Id, playerId, stoneType, stoneId));
        public async void ReturnStone(int playerId)
            => await HubCallMediator.InvokeCommandAsync(GameCommand.ReturnStone, async () =>
               await m_connection.InvokeAsync("ReturnStone", Prototype.Id, playerId));
        public async void PlaceStone(int playerId, Cell cell, StoneType stoneType)
            => await HubCallMediator.InvokeCommandAsync(GameCommand.PlaceStone, async () =>
               await m_connection.InvokeAsync("PlaceStone", Prototype.Id, playerId, Mapper.Map<CellDto>(cell), stoneType));
        public async void GrabStack(int playerId, Cell cell, int stoneCount)
            => await HubCallMediator.InvokeCommandAsync(GameCommand.GrabStack, async () =>
               await m_connection.InvokeAsync("GrabStack", Prototype.Id, playerId, Mapper.Map<CellDto>(cell), stoneCount));
        public async void DropStack(int playerId, Cell cell, int stoneCount)
            => await HubCallMediator.InvokeCommandAsync(GameCommand.DropStack, async () =>
               await m_connection.InvokeAsync("DropStack", Prototype.Id, playerId, Mapper.Map<CellDto>(cell), stoneCount));
        public async void AbortMove(int playerId)
            => await HubCallMediator.InvokeCommandAsync(GameCommand.AbortMove, async () =>
               await m_connection.InvokeAsync("AbortMove", Prototype.Id, playerId));
        public async void UndoMove(int playerId)
            => await HubCallMediator.InvokeCommandAsync(GameCommand.UndoMove, async () =>
               await m_connection.InvokeAsync("UndoMove", Prototype.Id, playerId));
        public async void RedoMove(int playerId)
            => await HubCallMediator.InvokeCommandAsync(GameCommand.RedoMove, async () =>
               await m_connection.InvokeAsync("RedoMove", Prototype.Id, playerId));
        public async void MakeMove(int playerId, IMove move)
            => await HubCallMediator.InvokeCommandAsync(GameCommand.MakeMove, async () =>
               await m_connection.InvokeAsync("MakeMove", Prototype.Id, playerId, Mapper.Map<MoveDto>(move)));
        public async void InitiateAbort(int playerId, IMove move, int duration)
            => await HubCallMediator.InvokeCommandAsync(GameCommand.InitiateAbort, async () =>
               await m_connection.InvokeAsync("InitiateAbort", Prototype.Id, playerId, Mapper.Map<MoveDto>(move), duration));
        public async void CompleteAbort(int playerId, IMove move)
            => await HubCallMediator.InvokeCommandAsync(GameCommand.CompleteAbort, async () =>
               await m_connection.InvokeAsync("CompleteAbort", Prototype.Id, playerId, Mapper.Map<MoveDto>(move)));
        public async void InitiateMove(int playerId, IMove move, int duration)
            => await HubCallMediator.InvokeCommandAsync(GameCommand.InitiateMove, async () =>
               await m_connection.InvokeAsync("InitiateMove", Prototype.Id, playerId, Mapper.Map<MoveDto>(move), duration));
        public async void CompleteMove(int playerId, IMove move)
            => await HubCallMediator.InvokeCommandAsync(GameCommand.CompleteMove, async () =>
               await m_connection.InvokeAsync("CompleteMove", Prototype.Id, playerId, Mapper.Map<MoveDto>(move)));
        public async void InitiateUndo(int playerId, int duration)
            => await HubCallMediator.InvokeCommandAsync(GameCommand.InitiateUndo, async () =>
               await m_connection.InvokeAsync("InitiateUndo", Prototype.Id, playerId, duration));
        public async void CompleteUndo(int playerId)
            => await HubCallMediator.InvokeCommandAsync(GameCommand.CompleteUndo, async () =>
               await m_connection.InvokeAsync("CompleteUndo", Prototype.Id, playerId));
        public async void InitiateRedo(int playerId, int duration)
            => await HubCallMediator.InvokeCommandAsync(GameCommand.InitiateRedo, async () =>
               await m_connection.InvokeAsync("InitiateRedo", Prototype.Id, playerId, duration));
        public async void CompleteRedo(int playerId)
            => await HubCallMediator.InvokeCommandAsync(GameCommand.CompleteRedo, async () =>
               await m_connection.InvokeAsync("CompleteRedo", Prototype.Id, playerId));
        public async void SetCurrentTurn(int playerId, int turn)
            => await HubCallMediator.InvokeCommandAsync(GameCommand.SetCurrentTurn, async () =>
               await m_connection.InvokeAsync("SetCurrentTurn", Prototype.Id, playerId, turn));
        public async void TrackMove(int playerId, BoardPosition position)
            => await HubCallMediator.InvokeCommandAsync(GameCommand.TrackMove, async () =>
               await m_connection.InvokeAsync("TrackMove", Prototype.Id, playerId, Mapper.Map<BoardPositionDto>(position)));


        // ***** Game Callback Methods

        internal static void OnTurnStarted(IGame game, int turn, int playerId)
        {
            HubCallMediator.ProcessNotification(GameNotification.TurnStarted, () =>
            {
                game.Tracker.OnTurnStarted(game.Id, turn, playerId);
            });
        }

        internal static void OnTurnCompleted(IGame game, int turn, int playerId)
        {
            HubCallMediator.ProcessNotification(GameNotification.TurnCompleted, () =>
            {
                game.Tracker.OnTurnCompleted(game.Id, turn, playerId);
            });
        }

        internal static void OnStoneDrawn(IGame game, StoneMove stoneMove, int playerId)
        {
            HubCallMediator.ProcessNotification(GameNotification.StoneDrawn, () =>
            {
                game.Tracker.OnStoneDrawn(game.Id, stoneMove, playerId);
            });
        }


        internal static void OnStoneReturned(IGame game, StoneMove stoneMove, int playerId)
        {
            HubCallMediator.ProcessNotification(GameNotification.StoneReturned, () =>
            {
                game.Tracker.OnStoneReturned(game.Id, stoneMove, playerId);
            });
        }


        internal static void OnStonePlaced(IGame game, StoneMove stoneMove, int playerId)
        {
            HubCallMediator.ProcessNotification(GameNotification.StonePlaced, () =>
            {
                game.Tracker.OnStonePlaced(game.Id, stoneMove, playerId);
            });
        }


        internal static void OnStackGrabbed(IGame game, StackMove stackMove, int playerId)
        {
            HubCallMediator.ProcessNotification(GameNotification.StackGrabbed, () =>
            {
                game.Tracker.OnStackGrabbed(game.Id, stackMove, playerId);
            });
        }


        internal static void OnStackDropped(IGame game, StackMove stackMove, int playerId)
        {
            HubCallMediator.ProcessNotification(GameNotification.StackDropped, () =>
            {
                game.Tracker.OnStackDropped(game.Id, stackMove, playerId);
            });
        }


        internal static void OnMoveAborted(IGame game, IMove move, int playerId)
        {
            HubCallMediator.ProcessNotification(GameNotification.MoveAborted, () =>
            {
                game.Tracker.OnMoveAborted(game.Id, move, playerId);
            });
        }


        internal static void OnMoveMade(IGame game, IMove move, int playerId)
        {
            HubCallMediator.ProcessNotification(GameNotification.MoveMade, () =>
            {
                game.Tracker.OnMoveMade(game.Id, move, playerId);
            });
        }


        internal static void OnAbortInitiated(IGame game, IMove move, int playerId, int duration)
        {
            HubCallMediator.ProcessNotification(GameNotification.AbortInitiated, () =>
            {
                game.Tracker.OnAbortInitiated(game.Id, move, playerId, duration);
            });
        }


        internal static void OnAbortCompleted(IGame game, IMove move, int playerId)
        {
            HubCallMediator.ProcessNotification(GameNotification.AbortCompleted, () =>
            {
                game.Tracker.OnAbortCompleted(game.Id, move, playerId);
            });
        }


        internal static void OnMoveInitiated(IGame game, IMove move, int playerId, int duration)
        {
            HubCallMediator.ProcessNotification(GameNotification.MoveInitiated, () =>
            {
                game.Tracker.OnMoveInitiated(game.Id, move, playerId, duration);
            });
        }


        internal static void OnMoveCompleted(IGame game, IMove move, int playerId)
        {
            HubCallMediator.ProcessNotification(GameNotification.MoveCompleted, () =>
            {
                game.Tracker.OnMoveCompleted(game.Id, move, playerId);
            });
        }


        internal static void OnUndoInitiated(IGame game, IMove move, int playerId, int duration)
        {
            HubCallMediator.ProcessNotification(GameNotification.UndoInitiated, () =>
            {
                game.Tracker.OnUndoInitiated(game.Id, move, playerId, duration);
            });
        }


        internal static void OnUndoCompleted(IGame game, IMove move, int playerId)
        {
            HubCallMediator.ProcessNotification(GameNotification.UndoCompleted, () =>
            {
                game.Tracker.OnUndoCompleted(game.Id, move, playerId);
            });
        }


        internal static void OnRedoInitiated(IGame game, IMove move, int playerId, int duration)
        {
            HubCallMediator.ProcessNotification(GameNotification.RedoInitiated, () =>
            {
                game.Tracker.OnRedoInitiated(game.Id, move, playerId, duration);
            });
        }


        internal static void OnRedoCompleted(IGame game, IMove move, int playerId)
        {
            HubCallMediator.ProcessNotification(GameNotification.RedoCompleted, () =>
            {
                game.Tracker.OnRedoCompleted(game.Id, move, playerId);
            });
        }


        internal static void OnCurrentTurnSet(IGame game, int turn, Stone[] stones)
        {
            HubCallMediator.ProcessNotification(GameNotification.CurrentTurnSet, () =>
            {
                game.Tracker.OnCurrentTurnSet(game.Id, turn, stones);
            });
        }


        internal static void OnMoveTracked(IGame game, BoardPosition position)
        {
            HubCallMediator.ProcessNotification(GameNotification.MoveTracked, () =>
            {
                game.Tracker.OnMoveTracked(game.Id, position);
            });
        }


        private IGame FindMirroringGame()
        {
            return (Tracker as DispatchingGameActivityTracker)?.Tracker switch
            {
                MirroringGameActivityTracker    mirroringTracker => mirroringTracker.Game,
                EventRaisingGameActivityTracker eventingTracker  => eventingTracker.Game,
                _ => null
            };
        }
    }
}
