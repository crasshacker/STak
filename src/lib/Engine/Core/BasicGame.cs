using System;
using System.IO;
using System.Linq;
using System.Timers;
using System.Threading;
using System.Diagnostics;
using System.Collections.Generic;

namespace STak.TakEngine
{
    /// <summary>
    /// Provides a basic implementation of a Tak game with atomic moves.
    /// </summary>
    /// <remarks>
    /// This class represents a Tak game at a point in time, including the complete state of the board
    /// (the location of each stone in each stack on a board cell) and the reserves (which stones have
    /// not yet been put into play).  It also maintains a list of moves that have been made as well as
    /// any moves that have been undone/rewound.  In addition the BaseGame enforces the game rules and
    /// ensures that the game is always in a consistent state.
    /// </remarks>
    public class BasicGame : IBasicGame
    {
        private const bool ValidateAIMoves = false;

        private const BitBoardMoveExecutor.Algorithm BitBoardMoveExecutionAlgorithm
                                    = BitBoardMoveExecutor.Algorithm.ApplyUndoMoves;

        private readonly IMoveExecutor<Board>    m_boardExecutor;
        private readonly IMoveExecutor<BitBoard> m_bitBoardExecutor;

        public GamePrototype   Prototype       { get; }
        public List<IMove>     ExecutedMoves   { get; }
        public List<IMove>     RevertedMoves   { get; }
        public Player[]        Players         { get; }
        public PlayerReserve[] Reserves        { get; }
        public Board           Board           { get; private   set; }
        public BitBoard        BitBoard        { get; private   set; }
        public Player          ActivePlayer    { get; private   set; }
        public GameResult      Result          { get; private   set; }
        public bool            IsInitialized   { get; private   set; }
        public bool            IsStarted       { get; private   set; }
        public bool            WasCompleted    { get; private   set; }
        public Stone[]         Stones          { get; private   set; }
        public GameTimer       GameTimer       { get; protected set; }

        public Guid            Id            => Prototype.Id;
        public Player          PlayerOne     => Players[0];
        public Player          PlayerTwo     => Players[1];
        public Player          LastPlayer    => Players[1-ActivePlayer.Id];
        public int             ActiveReserve => (ActiveTurn > 1) ? ActivePlayer.Id : LastPlayer.Id;
        public int             LastReserve   => (ActiveTurn == 2 && ActivePlayer.Id == Player.One)
                                                             ? ActiveReserve : (1 - ActiveReserve);
        public int             ActivePly     => (ExecutedMoves.Count + 1);
        public int             ActiveTurn    => (ExecutedMoves.Count / 2) + 1;
        public int             LastTurn      => (ExecutedMoves.Count / 2);
        public IMove           LastMove      => (ExecutedMoves.Count > 0) ? ExecutedMoves[^1] : null;
        public bool            IsInProgress  => (ExecutedMoves.Count > 0) && ! IsCompleted;
        public bool            IsCompleted   => Result.WinType != WinType.None;

        public virtual bool IsMoveInProgress => false;


        public BasicGame(GamePrototype prototype, bool sync = false)
        {
            ArgumentValidator.EnsureNotNull(prototype, nameof(prototype));

            Prototype = prototype;
            GameTimer = prototype.GameTimer.Clone();
            GameTimer.GameTimerExpired += GameTimerHandler;

            Player player1 = prototype.PlayerOne;
            Player player2 = prototype.PlayerTwo;
            int boardSize  = prototype.BoardSize;

            player1.Join(this, Player.One);
            player2.Join(this, Player.Two);

            Board = new Board(boardSize);
            BitBoard = new BitBoard(boardSize);

            m_boardExecutor    = new BoardMoveExecutor();
            m_bitBoardExecutor = new BitBoardMoveExecutor(BitBoardMoveExecutionAlgorithm);

            ExecutedMoves = new List<IMove>();
            RevertedMoves = new List<IMove>();

            Reserves = new PlayerReserve[2];
            Reserves[Player.One] = new PlayerReserve(Board.Size, player1);
            Reserves[Player.Two] = new PlayerReserve(Board.Size, player2);

            Stones = Reserves[Player.One].FlatStoneReserve.AvailableStones
             .Concat(Reserves[Player.One].CapstoneReserve.AvailableStones)
             .Concat(Reserves[Player.Two].FlatStoneReserve.AvailableStones)
             .Concat(Reserves[Player.Two].CapstoneReserve.AvailableStones)
             .ToArray();

            Players = new Player[2];
            Players[Player.One] = player1;
            Players[Player.Two] = player2;
            ActivePlayer = player1;

            Result = new GameResult(Player.None, WinType.None);
            IsStarted = false;

            if (sync)
            {
                // We must initialize and start the game prior to making moves.
                Initialize();
                Start();

                foreach (var move in prototype.Moves)
                {
                    MakeMove(ActivePlayer.Id, move);
                }
            }
        }


        internal BasicGame(IBasicGame game, bool useBitBoardOnly = false)
        {
            ArgumentValidator.EnsureNotNull(game, nameof(game));

            Prototype     = new GamePrototype(game.Prototype);
            GameTimer     = GameTimer.Unlimited;
            Board         = useBitBoardOnly ? null : game.Board.Clone();
            BitBoard      = game.BitBoard.Clone();
            Result        = game.Result;
            IsInitialized = game.IsInitialized;
            IsStarted     = game.IsStarted;

            Players = new Player[2];

            Players[Player.One] = new Player(game.PlayerOne);
            Players[Player.Two] = new Player(game.PlayerTwo);

            Players[Player.One].Join(this, Player.One);
            Players[Player.Two].Join(this, Player.Two);

            ActivePlayer = Players[game.ActivePlayer.Id];

            Reserves = new PlayerReserve[2];
            Reserves[Player.One] = game.Reserves[Player.One].Clone();
            Reserves[Player.Two] = game.Reserves[Player.Two].Clone();

            // For bitboard-only games we'll use the (non-unique) stones associated with the bitboard,
            // because we don't have access to those stones with unique Id's associated with a Board.
            // NOTE: Currently the Stones property of bitboard-only games isn't accessed by anyone.
            IBoard board = useBitBoardOnly ? (IBoard) BitBoard : (IBoard) Board;

            Stones = Reserves[Player.One].FlatStoneReserve.AvailableStones
             .Concat(Reserves[Player.One].CapstoneReserve .AvailableStones)
             .Concat(Reserves[Player.Two].FlatStoneReserve.AvailableStones)
             .Concat(Reserves[Player.Two].CapstoneReserve .AvailableStones)
             .Concat(board.Stacks.SelectMany(s => s.Stones))
             .ToArray();

            ExecutedMoves = game.ExecutedMoves.Select(m => m.Clone()).ToList();
            RevertedMoves = game.RevertedMoves.Select(m => m.Clone()).ToList();

            m_boardExecutor    = useBitBoardOnly ? null : new BoardMoveExecutor();
            m_bitBoardExecutor = new BitBoardMoveExecutor(BitBoardMoveExecutionAlgorithm);
        }


        public virtual void Initialize()
        {
            IsInitialized = true;
        }


        public virtual void Start()
        {
            if (! IsStarted)
            {
                IsStarted = true;
                GameTimer.Start();
            }
        }


        public bool CanMakeMove(int playerId, IMove move)
        {
            // TODO - Validate the move.  WinTak uses the more granular methods (CanDrawStone et al), so this
            //        method isn't yet needed by any application.

            return move switch
            {
                StoneMove stoneMove => CanMakeStoneMove(playerId, stoneMove),
                StackMove stackMove => CanMakeStackMove(playerId, stackMove),
                _                   => false
            };
        }


        public bool CanUndoMove(int playerId)
        {
            // Note that we validate using the opponent's Id because if we're undoing
            // a move, it must be his/her/its turn to play.

            int verifyId = (playerId == Player.None) ? Player.None : 1-playerId;
            return IsStarted && ! IsMoveInProgress && ExecutedMoves.Count > 0
                             && (WasCompleted || CheckActivePlayer(verifyId));
        }


        public bool CanRedoMove(int playerId)
        {
            return CheckActivePlayer(playerId) && (IsStarted && ! IsMoveInProgress && RevertedMoves.Count > 0);
        }


        public void MakeMove(int playerId, IMove move)
        {
            // Validate if we're either using a Board and thus not making the move on behalf of an AI,
            // or if AI move validation is explicitly enabled.
            if (Board != null || ValidateAIMoves)
            {
                ValidateMove(CanMakeMove(playerId, move));
            }
            MakeValidatedMove(playerId, move);
        }


        public void UndoMove(int playerId)
        {
            ValidateActivePlayer(playerId, true);
            ValidateMove(ExecutedMoves.Count > 0);

            // NOTE: The ordering of execution is very important here.  Be careful!

            SwitchPlayer();
            IMove move = ExecutedMoves[^1];
            ExecutedMoves.RemoveAt(ExecutedMoves.Count-1);
            RevertedMoves.Add(move);

            Board    =    m_boardExecutor?.UndoMove(   Board, move);
            BitBoard = m_bitBoardExecutor?.UndoMove(BitBoard, move);

            ReturnStoneIfAppropriate(move, ActiveReserve);
            ValidateBoard();

            // If the board executor is null this game is being used by an AI to evaluate moves.  In this case
            // no one will be looking at the game result after the move is undone prior to another move being
            // made, so we skip this step to reduce overall AI thinking time.
            if (m_boardExecutor != null)
            {
                SetGameResult();
            }
        }


        public void RedoMove(int playerId)
        {
            ValidateActivePlayer(playerId);
            ValidateMove(RevertedMoves.Count > 0);

            // NOTE: The ordering of execution is very important here.  Be careful!

            IMove move = RevertedMoves[^1];
            DrawStoneIfAppropriate(move, ActiveReserve);
            RevertedMoves.RemoveAt(RevertedMoves.Count-1);
            ExecutedMoves.Add(move);

            Board    =    m_boardExecutor?.RedoMove(   Board, move);
            BitBoard = m_bitBoardExecutor?.RedoMove(BitBoard, move);

            ValidateBoard();
            SetGameResult();
            SwitchPlayer();
        }


        public void HumanizePlayer(int playerId, string name)
        {
            Players[playerId].Humanize(name);
        }


        public void ChangePlayer(Player player)
        {
            ArgumentValidator.EnsureNotNull(player, nameof(player));

            if (ActivePlayer.Id == player.Id)
            {
                ActivePlayer = player;
            }
            Players[player.Id] = player;
            player.Join(this, player.Id);
        }


        protected void SwitchPlayer()
        {
            ActivePlayer = LastPlayer;
        }


        protected bool CheckActivePlayer(int playerId)
        {
            return playerId == Player.None || playerId == ActivePlayer.Id;
        }


        protected int ValidateActivePlayer(int playerId, bool isUndo = false)
        {
            //
            // Verify that the specified player is either active or was specified as Player.None (in which
            // case no validation is done), except in the case of an UndoMove.  In this case, verify that
            // the other player is active or is an AI player.
            //
            if (playerId != Player.None && ((! isUndo && playerId != ActivePlayer.Id)
                || (isUndo && playerId == ActivePlayer.Id && Players[1-playerId].IsHuman)))
            {
                throw new InvalidOperationException("Cannot play out of turn.");
            }

            return (playerId != Player.None) ? playerId : isUndo ? LastPlayer.Id : ActivePlayer.Id;
        }


        protected static void ValidateMove(bool canMove)
        {
            if (! canMove)
            {
                throw new InvalidOperationException("The attempted move is invalid.");
            }
        }


        protected void MakeValidatedMove(int playerId, IMove move)
        {
            GameTimer.PunchClock(playerId);
            DrawStoneIfAppropriate(move, ActiveReserve);

            Board    =    m_boardExecutor?.MakeMove(   Board, move);
            BitBoard = m_bitBoardExecutor?.MakeMove(BitBoard, move);

            ExecutedMoves.Add(move);
            RevertedMoves.Clear();
            ValidateBoard();
            SetGameResult();
            SwitchPlayer();
        }


        private bool CanMakeStoneMove(int playerId, StoneMove move)
        {
            bool canMakeMove = false;

            if (! move.HasExecuted && CheckActivePlayer(playerId) && IsStarted && ! IsMoveInProgress)
            {
                playerId = (ActiveTurn == 1) ? (1 - ActivePlayer.Id) : ActivePlayer.Id;
                canMakeMove = (move.Stone != null && move.Stone.Id != -1)
                    || Reserves[playerId].GetAvailableStoneCount(move.Stone.Type) > 0;
            }

            return canMakeMove && BitBoard[move.TargetCell].IsEmpty;
        }


        private bool CanMakeStackMove(int playerId, StackMove move)
        {
            bool canMakeMove = false;

            if (! move.HasExecuted && CheckActivePlayer(playerId) && IsStarted && ! IsMoveInProgress && ActiveTurn > 1)
            {
                IBoard board = BitBoard;
                Cell cell = move.StartingCell;
                Stack stack = board[cell];

                if (move.StoneCount <= stack.Count && move.StoneCount <= board.Size
                                      && stack.TopStone.PlayerId == ActivePlayer.Id)
                {
                    int droppedCount = 0;
                    canMakeMove = true;     // Now we assume true until proven otherwise.

                    for (int i = 0; i < move.DropCounts.Count; ++i)
                    {
                        cell = cell.Move(move.Direction);

                        bool canDrop = board[cell].TopStone == null;
                        int dropCount = move.DropCounts[i];

                        if (! canDrop)
                        {
                            StoneType grabbedType = board[move.StartingCell].TopStone.Type;
                            StoneType stoneType   = board[cell].TopStone.Type;

                            canDrop = (stoneType == StoneType.Flat)
                                   || (stoneType == StoneType.Standing && grabbedType == StoneType.Cap
                                      && dropCount == 1 && droppedCount + dropCount == move.StoneCount);
                        }

                        if (! canDrop)
                        {
                            canMakeMove = false;
                            break;
                        }

                        droppedCount += dropCount;
                        i++;
                    }
                }
            }

            return canMakeMove;
        }


        private void DrawStoneIfAppropriate(IMove move, int reserve)
        {
            if (move is StoneMove stoneMove)
            {
                stoneMove.Stone = Reserves[reserve].DrawStone(stoneMove.Stone.Type, stoneMove.Stone.Id);
            }
        }


        private void ReturnStoneIfAppropriate(IMove move, int reserve)
        {
            if (move is StoneMove stoneMove)
            {
                Reserves[reserve].ReturnStone(stoneMove.Stone);
            }
        }


        private void RestoreStackIfAppropriate(IMove move)
        {
            if (move is StackMove stackMove)
            {
                stackMove.RestoreOriginalGrabbedStack(Board);
            }
        }


        private void SetGameResult()
        {
            int boardSize = BitBoard.Size;

            if (GameTimer.IsExpired)
            {
                int playerId = (GameTimer.GetRemainingTime(Player.One) > TimeSpan.Zero) ? Player.One : Player.Two;
                Result = new GameResult(playerId, WinType.Time);
            }
            else if ((Reserves[0].GetAvailableStoneCount(StoneType.Flat) == 0  &&
                      Reserves[0].GetAvailableStoneCount(StoneType.Cap)  == 0) ||
                     (Reserves[1].GetAvailableStoneCount(StoneType.Flat) == 0  &&
                      Reserves[1].GetAvailableStoneCount(StoneType.Cap)  == 0) ||
                     (BitBoard.OccupancyCount == boardSize * boardSize))
            {
                int white = BitBoard.WhiteFlatCount;
                int black = BitBoard.BlackFlatCount;

                int playerId = (white > black) ? Player.One
                             : (white < black) ? Player.Two
                                               : Player.None;

                WinType winType = (playerId == Player.None) ? WinType.Draw : WinType.Flat;
                int score = (winType == WinType.Draw) ? 0 : Reserves[playerId].GetAvailableStoneCount(StoneType.None)
                                                                                       + (int) Math.Pow(boardSize, 2);
                Result = new GameResult(playerId, winType)
                {
                    Score = score
                };
            }
            else
            {
                int thisPlayer =     ActivePlayer.Id;
                int thatPlayer = 1 - ActivePlayer.Id;

                int[]   thisExtents = BitBoard.GetRoadExtents(thisPlayer);
                int[]   thatExtents = BitBoard.GetRoadExtents(thatPlayer);

                int[][] bothExtents = thisPlayer == Player.One ? new int[][] { thisExtents, thatExtents }
                                                               : new int[][] { thatExtents, thisExtents };

                if (thisExtents[0] == boardSize || thisExtents[1] == boardSize)
                {
                    int score = (int) Math.Pow(boardSize, 2)
                              + Reserves[thisPlayer].GetAvailableStoneCount(StoneType.None);
                    Result = new GameResult(thisPlayer, WinType.Road)
                    {
                        Score   = score,
                        Extents = bothExtents
                    };
                }
                else if (thatExtents[0] == boardSize || thatExtents[1] == boardSize)
                {
                    int score = (int) Math.Pow(boardSize, 2)
                              + Reserves[thatPlayer].GetAvailableStoneCount(StoneType.None);
                    Result = new GameResult(thatPlayer, WinType.Road)
                    {
                        Score   = score,
                        Extents = bothExtents
                    };
                }
                else
                {
                    Result = new GameResult(Player.None, WinType.None)
                    {
                        Score   = 0,
                        Extents = bothExtents
                    };
                }
            }

            if (Result.WinType != WinType.None)
            {
                WasCompleted = true; // remains true forever, even after post-game undos
            }
        }


        protected virtual void GameTimerHandler(object source, ElapsedEventArgs e)
        {
            SetGameResult();
        }


        [Conditional("DEBUG_BOARD")]
        private void ValidateBoard()
        {
            if (m_boardExecutor != null)
            {
                for (int file = 0; file < Board.Size; ++file)
                {
                    for (int rank = 0; rank < Board.Size; ++rank)
                    {
                        Stack boardStack    = Board   [file, rank];
                        Stack bitBoardStack = BitBoard[file, rank];

                        if (! boardStack.MaybeEquals(bitBoardStack))
                        {
                            throw new Exception("Board and bitboard are out of sync.");
                        }
                    }
                }
            }
        }
    }
}
