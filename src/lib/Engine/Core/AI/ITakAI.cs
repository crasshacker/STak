using System;
using System.Threading;
using STak.TakEngine;

namespace STak.TakEngine.AI
{
    public interface ITakAI
    {
        string       Name    { get; }
        TakAIOptions Options { get; set; }

        void OnGameInitiated();
        void OnGameCompleted();

        IMove ChooseNextMove(IBasicGame game, CancellationToken token);
    }
}
