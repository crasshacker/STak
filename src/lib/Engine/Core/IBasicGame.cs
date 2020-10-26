using System;
using System.Threading;
using System.Collections.Generic;

namespace STak.TakEngine
{
    public interface IBasicGame : IBasicGameState
    {
        void Initialize();
        void Start();

        void UndoMove(int playerId);
        void RedoMove(int playerId);
        void MakeMove(int playerId, IMove move);

        void HumanizePlayer(int playerId, string name);
        void ChangePlayer(Player player);
    }
}
