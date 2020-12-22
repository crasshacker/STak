using System;
using System.Collections.Generic;
using STak.TakEngine;

namespace STak.TakEngine.AI
{
    public interface IMoveEnumerator
    {
        List<IMove> EnumerateMoves(IBoard board, int turn, int playerId, int reserveId, PlayerReserve reserve,
                                                               IList<Cell> boardCells, IMove firstMove = null);
    }
}
