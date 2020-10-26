using System;
using System.Threading;
using STak.TakEngine;

namespace STak.TakEngine.AI
{
    public interface ITakAI
    {
        string       Name    { get; }
        TakAIOptions Options { get; set; }

        IMove ChooseNextMove(IBasicGame game, CancellationToken token);
    }
}
