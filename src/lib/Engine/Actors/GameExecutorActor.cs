using System;
using Akka.Actor;
using STak.TakEngine;

namespace STak.TakEngine.Actors
{
    public class GameExecutorActor : ReceiveActor
    {
        private readonly IGame m_game;


        public GameExecutorActor(GamePrototype prototype, IActorRef notifierActor)
        {
            // Configure the underlying game to forward game activity notifications to the notifier actor.
            ForwardingGameActivityTracker forwardingTracker = new ForwardingGameActivityTracker(Self, notifierActor);
            m_game = new Game(prototype, forwardingTracker);

            // Property Getters.

            Receive<GetGameIdMessage>            ((m) => Reply(m_game.Id));
            Receive<GetGamePrototypeMessage>     ((m) => Reply(m_game.Prototype));
            Receive<GetBoardMessage>             ((m) => Reply(m_game.Board));
            Receive<GetBitBoardMessage>          ((m) => Reply(m_game.BitBoard));
            Receive<GetExecutedMovesMessage>     ((m) => Reply(m_game.ExecutedMoves));
            Receive<GetRevertedMovesMessage>     ((m) => Reply(m_game.RevertedMoves));
            Receive<GetPlayersMessage>           ((m) => Reply(m_game.Players));
            Receive<GetReservesMessage>          ((m) => Reply(m_game.Reserves));
            Receive<GetActivePlayerMessage>      ((m) => Reply(m_game.ActivePlayer));
            Receive<GetResultMessage>            ((m) => Reply(m_game.Result));

            Receive<GetPlayerOneMessage>         ((m) => Reply(m_game.PlayerOne));
            Receive<GetPlayerTwoMessage>         ((m) => Reply(m_game.PlayerTwo));
            Receive<GetLastPlayerMessage>        ((m) => Reply(m_game.LastPlayer));
            Receive<GetActiveReserveMessage>     ((m) => Reply(m_game.ActiveReserve));
            Receive<GetActiveTurnMessage>        ((m) => Reply(m_game.ActiveTurn));
            Receive<GetLastTurnMessage>          ((m) => Reply(m_game.LastTurn));
            Receive<GetLastMoveMessage>          ((m) => Reply(m_game.LastMove));

            Receive<GetStoneMoveMessage>         ((m) => Reply(m_game.StoneMove));
            Receive<GetStackMoveMessage>         ((m) => Reply(m_game.StackMove));
            Receive<GetDrawnStoneMessage>        ((m) => Reply(m_game.DrawnStone));
            Receive<GetGrabbedStackMessage>      ((m) => Reply(m_game.GrabbedStack));
            Receive<IsStoneMovingMessage>        ((m) => Reply(m_game.IsStoneMoving));
            Receive<IsStackMovingMessage>        ((m) => Reply(m_game.IsStackMoving));
            Receive<IsMoveInProgressMessage>     ((m) => Reply(m_game.IsMoveInProgress));

            Receive<IsInitializedMessage>        ((m) => Reply(m_game.IsInitialized));
            Receive<IsStartedMessage>            ((m) => Reply(m_game.IsStarted));
            Receive<IsInProgressMessage>         ((m) => Reply(m_game.IsInProgress));
            Receive<IsCompletedMessage>          ((m) => Reply(m_game.IsCompleted));
            Receive<WasCompletedMessage>         ((m) => Reply(m_game.WasCompleted));

            // Methods.

            Receive<InitializeMessage>           ((m) => { m_game.Initialize();                       });
            Receive<StartMessage>                ((m) => { m_game.Start();                            });
            Receive<HumanizePlayerMessage>       ((m) => { m_game.HumanizePlayer(m.PlayerId, m.Name); });
            Receive<ChangePlayerMessage>         ((m) => { m_game.ChangePlayer(m.Player);             });

            Receive<CanUndoMoveMessage>          ((m) => Reply(m_game.CanUndoMove(m.PlayerId)));
            Receive<CanRedoMoveMessage>          ((m) => Reply(m_game.CanRedoMove(m.PlayerId)));
            Receive<CanMakeMoveMessage>          ((m) => Reply(m_game.CanMakeMove(m.PlayerId, m.Move)));
            Receive<CanDrawStoneMessage>         ((m) => Reply(m_game.CanDrawStone(m.PlayerId, m.StoneType)));
            Receive<CanReturnStoneMessage>       ((m) => Reply(m_game.CanReturnStone(m.PlayerId)));
            Receive<CanPlaceStoneMessage>        ((m) => Reply(m_game.CanPlaceStone(m.PlayerId, m.Cell)));
            Receive<CanGrabStackMessage>         ((m) => Reply(m_game.CanGrabStack(m.PlayerId, m.Cell, m.StoneCount)));
            Receive<CanDropStackMessage>         ((m) => Reply(m_game.CanDropStack(m.PlayerId, m.Cell, m.StoneCount)));
            Receive<CanAbortMoveMessage>         ((m) => Reply(m_game.CanAbortMove(m.PlayerId)));

            Receive<UndoMoveMessage>             ((m) => { m_game.UndoMove(m.PlayerId);                          });
            Receive<RedoMoveMessage>             ((m) => { m_game.RedoMove(m.PlayerId);                          });
            Receive<MakeMoveMessage>             ((m) => { m_game.MakeMove(m.PlayerId, m.Move);                  });
            Receive<DrawStoneMessage>            ((m) => { m_game.DrawStone(m.PlayerId, m.StoneType, m.StoneId); });
            Receive<ReturnStoneMessage>          ((m) => { m_game.ReturnStone(m.PlayerId);                       });
            Receive<PlaceStoneMessage>           ((m) => { m_game.PlaceStone(m.PlayerId, m.Cell, m.StoneType);   });
            Receive<GrabStackMessage>            ((m) => { m_game.GrabStack(m.PlayerId, m.Cell, m.StoneCount);   });
            Receive<DropStackMessage>            ((m) => { m_game.DropStack(m.PlayerId, m.Cell, m.StoneCount);   });
            Receive<AbortMoveMessage>            ((m) => { m_game.AbortMove(m.PlayerId);                         });
            Receive<SetCurrentTurnMessage>       ((m) => { m_game.SetCurrentTurn(m.PlayerId, m.Turn);            });
            Receive<TrackMoveMessage>            ((m) => { m_game.TrackMove(m.PlayerId, m.BoardPosition);        });
            Receive<InitiateAbortMessage>        ((m) => { m_game.InitiateAbort(m.PlayerId, m.Move, m.Duration); });
            Receive<CompleteAbortMessage>        ((m) => { m_game.CompleteAbort(m.PlayerId, m.Move);             });
            Receive<InitiateMoveMessage>         ((m) => { m_game.InitiateMove(m.PlayerId, m.Move, m.Duration);  });
            Receive<CompleteMoveMessage>         ((m) => { m_game.CompleteMove(m.PlayerId, m.Move);              });
            Receive<InitiateUndoMessage>         ((m) => { m_game.InitiateUndo(m.PlayerId, m.Duration);          });
            Receive<CompleteUndoMessage>         ((m) => { m_game.CompleteUndo(m.PlayerId);                      });
            Receive<InitiateRedoMessage>         ((m) => { m_game.InitiateRedo(m.PlayerId, m.Duration);          });
            Receive<CompleteRedoMessage>         ((m) => { m_game.CompleteRedo(m.PlayerId);                      });
        }


        public static Props Props(GamePrototype prototype, IActorRef notifierActor, string address = null)
        {
            var props = Akka.Actor.Props.Create(() => new GameExecutorActor(prototype, notifierActor));

            if (address != null)
            {
               props = props.WithDeploy(Deploy.None.WithScope(new RemoteScope(Address.Parse(address)))); 
            }

            return props;
        }


        private void Reply (object obj) => Sender.Tell(obj, Sender);
    }
}
