using System;
using System.Collections.Generic;

namespace STak.TakEngine
{
    public interface IMove
    {
        bool               HasExecuted { get; }
        IEnumerable<Stack> GetAffectedStacks(IBoard board);
        void               Execute(IBoard board);
        void               Undo(IBoard board);
        void               Redo(IBoard board);
        string             ToString(bool verbose = false);
        IMove              Clone();
    }
}
