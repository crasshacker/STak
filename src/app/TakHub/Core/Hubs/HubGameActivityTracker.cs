using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using STak.TakHub.Core.Interfaces.Hubs;
using STak.TakHub.Interop;
using STak.TakHub.Interop.Dto;
using STak.TakEngine;
using STak.TakEngine.Trackers;

namespace STak.TakHub.Core.Hubs
{
    // This class is for testing purposes only - It introduces a random delay before sending notifications.

    public class HubGameActivityTracker : IGameActivityTracker
    {
        private readonly IGameHubContext           m_hubContext;
        private readonly AsyncActionQueueProcessor m_processor;
        private readonly string                    m_groupName;


        public HubGameActivityTracker(IGameHubContext hubContext, GamePrototype prototype, int minLatency = 0, int maxLatency = 0)
        {
            m_hubContext = hubContext;
            m_groupName  = prototype.Id.ToString();
            m_processor  = new AsyncActionQueueProcessor(minLatency, maxLatency);

            m_processor.StartProcessing();
        }

        public void OnGameCreated    (GamePrototype prototype)
            => m_processor.Enqueue(async () => await m_hubContext.GetGroup(m_groupName).OnGameCreated    (Mapper.Map<GamePrototypeDto>(prototype)));
        public void OnGameStarted    (Guid gameId)
            => m_processor.Enqueue(async () => await m_hubContext.GetGroup(m_groupName).OnGameStarted    (gameId));
        public void OnGameCompleted  (Guid gameId)
            => m_processor.Enqueue(async () => await m_hubContext.GetGroup(m_groupName).OnGameCompleted  (gameId));
        public void OnTurnStarted    (Guid gameId, int turn, int playerId)
            => m_processor.Enqueue(async () => await m_hubContext.GetGroup(m_groupName).OnTurnStarted    (gameId, turn, playerId));
        public void OnTurnCompleted  (Guid gameId, int turn, int playerId)
            => m_processor.Enqueue(async () => await m_hubContext.GetGroup(m_groupName).OnTurnCompleted  (gameId, turn, playerId));
        public void OnStoneDrawn     (Guid gameId, StoneMove stoneMove, int playerId)
            => m_processor.Enqueue(async () => await m_hubContext.GetGroup(m_groupName).OnStoneDrawn     (gameId, Mapper.Map<StoneMoveDto>(stoneMove), playerId));
        public void OnStoneReturned  (Guid gameId, StoneMove stoneMove, int playerId)
            => m_processor.Enqueue(async () => await m_hubContext.GetGroup(m_groupName).OnStoneReturned  (gameId, Mapper.Map<StoneMoveDto>(stoneMove), playerId));
        public void OnStonePlaced    (Guid gameId, StoneMove stoneMove, int playerId)
            => m_processor.Enqueue(async () => await m_hubContext.GetGroup(m_groupName).OnStonePlaced    (gameId, Mapper.Map<StoneMoveDto>(stoneMove), playerId));
        public void OnStackGrabbed   (Guid gameId, StackMove stackMove, int playerId)
            => m_processor.Enqueue(async () => await m_hubContext.GetGroup(m_groupName).OnStackGrabbed   (gameId, Mapper.Map<StackMoveDto>(stackMove), playerId));
        public void OnStackDropped   (Guid gameId, StackMove stackMove, int playerId)
            => m_processor.Enqueue(async () => await m_hubContext.GetGroup(m_groupName).OnStackDropped   (gameId, Mapper.Map<StackMoveDto>(stackMove), playerId));
        public void OnMoveAborted    (Guid gameId, IMove move, int playerId)
            => m_processor.Enqueue(async () => await m_hubContext.GetGroup(m_groupName).OnMoveAborted    (gameId, Mapper.Map<MoveDto>(move), playerId));
        public void OnMoveMade       (Guid gameId, IMove move, int playerId)
            => m_processor.Enqueue(async () => await m_hubContext.GetGroup(m_groupName).OnMoveMade       (gameId, Mapper.Map<MoveDto>(move), playerId));
        public void OnAbortInitiated (Guid gameId, IMove move, int playerId, int duration)
            => m_processor.Enqueue(async () => await m_hubContext.GetGroup(m_groupName).OnAbortInitiated (gameId, Mapper.Map<MoveDto>(move), playerId, duration));
        public void OnAbortCompleted (Guid gameId, IMove move, int playerId)
            => m_processor.Enqueue(async () => await m_hubContext.GetGroup(m_groupName).OnAbortCompleted (gameId, Mapper.Map<MoveDto>(move), playerId));
        public void OnMoveInitiated  (Guid gameId, IMove move, int playerId, int duration)
            => m_processor.Enqueue(async () => await m_hubContext.GetGroup(m_groupName).OnMoveInitiated  (gameId, Mapper.Map<MoveDto>(move), playerId, duration));
        public void OnMoveCompleted  (Guid gameId, IMove move, int playerId)
            => m_processor.Enqueue(async () => await m_hubContext.GetGroup(m_groupName).OnMoveCompleted  (gameId, Mapper.Map<MoveDto>(move), playerId));
        public void OnUndoInitiated  (Guid gameId, IMove move, int playerId, int duration)
            => m_processor.Enqueue(async () => await m_hubContext.GetGroup(m_groupName).OnUndoInitiated  (gameId, Mapper.Map<MoveDto>(move), playerId, duration));
        public void OnUndoCompleted  (Guid gameId, IMove move, int playerId)
            => m_processor.Enqueue(async () => await m_hubContext.GetGroup(m_groupName).OnUndoCompleted  (gameId, Mapper.Map<MoveDto>(move), playerId));
        public void OnRedoInitiated  (Guid gameId, IMove move, int playerId, int duration)
            => m_processor.Enqueue(async () => await m_hubContext.GetGroup(m_groupName).OnRedoInitiated  (gameId, Mapper.Map<MoveDto>(move), playerId, duration));
        public void OnRedoCompleted  (Guid gameId, IMove move, int playerId)
            => m_processor.Enqueue(async () => await m_hubContext.GetGroup(m_groupName).OnRedoCompleted  (gameId, Mapper.Map<MoveDto>(move), playerId));
        public void OnMoveCommencing (Guid gameId, IMove move, int playerId)
            => m_processor.Enqueue(async () => await m_hubContext.GetGroup(m_groupName).OnMoveCommencing (gameId, Mapper.Map<MoveDto>(move), playerId));
        public void OnCurrentTurnSet (Guid gameId, int turn, Stone[] stones)
            => m_processor.Enqueue(async () => await m_hubContext.GetGroup(m_groupName).OnCurrentTurnSet (gameId, turn, stones.Select(s => Mapper.Map<StoneDto>(s)).ToArray()));
        public void OnMoveTracked    (Guid gameId, BoardPosition position)
            => m_processor.Enqueue(async () => await m_hubContext.GetGroup(m_groupName).OnMoveTracked    (gameId, Mapper.Map<BoardPositionDto>(position)));


        private class AsyncActionQueueProcessor
        {
            private readonly BlockingCollection<Func<Task>> m_messageQueue = new BlockingCollection<Func<Task>>();
            private readonly Random                         m_randomizer   = new Random();
            private readonly int                            m_minLatency;
            private readonly int                            m_maxLatency;


            public AsyncActionQueueProcessor(int minLatency, int maxLatency)
            {
                m_minLatency = minLatency;
                m_maxLatency = maxLatency;
            }


            public void StartProcessing()
            {
                var thread = new Thread(ProcessQueue)
                {
                    IsBackground = true // Don't prevent application from exiting if running.
                };
                thread.Start();
            }


            public void Enqueue(Func<Task> action)
            {
                m_messageQueue.Add(action);
            }


            private void ProcessQueue()
            {
                foreach (var action in m_messageQueue.GetConsumingEnumerable())
                {
                    try { ProcessItem(action).Wait(); } catch { }
                }
            }


            private async Task ProcessItem(Func<Task> action)
            {
                if (m_maxLatency > 0)
                {
                    await Task.Delay(m_randomizer.Next(m_minLatency, m_maxLatency));
                }
                await action();
            }
        }
    }
}
