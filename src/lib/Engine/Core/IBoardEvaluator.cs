using System;

namespace STak.TakEngine
{
    public interface IBoardEvaluator
    {
        int Evaluate(IBasicGame game, int playerId);
    }
}
