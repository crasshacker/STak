using System;
using System.Collections.Generic;
using Akka.Actor;
using STak.TakEngine;

namespace STak.TakEngine.Actors
{
    public class ActorGameState : IGameState
    {
        // Fields

        private IActorRef m_gameActor { get; }

        // Property Getters.

        public Guid            Id               { get => Ask<Guid>(new GetGameIdMessage());                 }
        public GamePrototype   Prototype        { get => Ask<GamePrototype>(new GetGamePrototypeMessage()); }

        public Board           Board            { get => Ask<Board>(new GetBoardMessage());                 }
        public BitBoard        BitBoard         { get => Ask<BitBoard>(new GetBitBoardMessage());           }
        public List<IMove>     ExecutedMoves    { get => Ask<List<IMove>>(new GetExecutedMovesMessage());   }
        public List<IMove>     RevertedMoves    { get => Ask<List<IMove>>(new GetRevertedMovesMessage());   }
        public Player[]        Players          { get => Ask<Player[]>(new GetPlayersMessage());            }
        public PlayerReserve[] Reserves         { get => Ask<PlayerReserve[]>(new GetReservesMessage());    }
        public Player          ActivePlayer     { get => Ask<Player>(new GetActivePlayerMessage());         }
        public GameResult      Result           { get => Ask<GameResult>(new GetResultMessage());           }

        public Player          PlayerOne        { get => Ask<Player>(new GetPlayerOneMessage());            }
        public Player          PlayerTwo        { get => Ask<Player>(new GetPlayerTwoMessage());            }
        public Player          LastPlayer       { get => Ask<Player>(new GetLastPlayerMessage());           }
        public int             ActiveReserve    { get => Ask<int>(new GetActiveReserveMessage());           }
        public int             LastReserve      { get => Ask<int>(new GetLastReserveMessage());             }
        public int             ActivePly        { get => Ask<int>(new GetActivePlyMessage());               }
        public int             ActiveTurn       { get => Ask<int>(new GetActiveTurnMessage());              }
        public int             LastTurn         { get => Ask<int>(new GetLastTurnMessage());                }
        public IMove           LastMove         { get => Ask<IMove>(new GetLastMoveMessage());              }

        public StoneMove       StoneMove        { get => Ask<StoneMove>(new GetStoneMoveMessage());         }
        public StackMove       StackMove        { get => Ask<StackMove>(new GetStackMoveMessage());         }
        public Stone           DrawnStone       { get => Ask<Stone>(new GetDrawnStoneMessage());            }
        public Stack           GrabbedStack     { get => Ask<Stack>(new GetGrabbedStackMessage());          }
        public bool            IsStoneMoving    { get => Ask<bool>(new IsStoneMovingMessage());             }
        public bool            IsStackMoving    { get => Ask<bool>(new IsStackMovingMessage());             }
        public bool            IsMoveInProgress { get => Ask<bool>(new IsMoveInProgressMessage());          }

        public bool            IsInitialized    { get => Ask<bool>(new IsInitializedMessage());             }
        public bool            IsStarted        { get => Ask<bool>(new IsStartedMessage());                 }
        public bool            IsInProgress     { get => Ask<bool>(new IsInProgressMessage());              }
        public bool            IsCompleted      { get => Ask<bool>(new IsCompletedMessage());               }
        public bool            WasCompleted     { get => Ask<bool>(new WasCompletedMessage());              }

        // Methods requiring a playerId argument.

        public bool CanUndoMove(int playerId)                                => Ask<bool>(new CanUndoMoveMessage(playerId));
        public bool CanRedoMove(int playerId)                                => Ask<bool>(new CanRedoMoveMessage(playerId));
        public bool CanMakeMove(int playerId, IMove move)                    => Ask<bool>(new CanMakeMoveMessage(playerId, move));
        public bool CanDrawStone(int playerId, StoneType stoneType)          => Ask<bool>(new CanDrawStoneMessage(playerId, stoneType));
        public bool CanReturnStone(int playerId)                             => Ask<bool>(new CanReturnStoneMessage(playerId));
        public bool CanPlaceStone(int playerId, Cell cell)                   => Ask<bool>(new CanPlaceStoneMessage(playerId, cell));
        public bool CanGrabStack(int playerId, Cell cell, int stoneCount)    => Ask<bool>(new CanGrabStackMessage(playerId, cell, stoneCount));
        public bool CanDropStack(int playerId, Cell cell, int stoneCount)    => Ask<bool>(new CanDropStackMessage(playerId, cell, stoneCount));
        public bool CanAbortMove(int playerId)                               => Ask<bool>(new CanAbortMoveMessage(playerId));

        // Duplicate methods that don't accept a playerId argument.

        public bool CanUndoMove()                                            => Ask<bool>(new CanUndoMoveMessage(Player.None));
        public bool CanRedoMove()                                            => Ask<bool>(new CanRedoMoveMessage(Player.None));
        public bool CanMakeMove(IMove move)                                  => Ask<bool>(new CanMakeMoveMessage(Player.None, move));
        public bool CanDrawStone(StoneType stoneType)                        => Ask<bool>(new CanDrawStoneMessage(Player.None, stoneType));
        public bool CanReturnStone()                                         => Ask<bool>(new CanReturnStoneMessage(Player.None));
        public bool CanPlaceStone(Cell cell)                                 => Ask<bool>(new CanPlaceStoneMessage(Player.None, cell));
        public bool CanGrabStack(Cell cell, int stoneCount)                  => Ask<bool>(new CanGrabStackMessage(Player.None, cell, stoneCount));
        public bool CanDropStack(Cell cell, int stoneCount)                  => Ask<bool>(new CanDropStackMessage(Player.None, cell, stoneCount));
        public bool CanAbortMove()                                           => Ask<bool>(new CanAbortMoveMessage(Player.None));

        private T Ask<T>(GameActorMessage message) => m_gameActor.Ask<T>(message).Result;


        public ActorGameState(IActorRef gameActor)
        {
            m_gameActor = gameActor;
        }
    }
}
