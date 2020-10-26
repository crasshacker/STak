using System;
using System.Collections.Generic;

namespace STak.TakEngine
{
    public interface IBoard
    {
        int                Size   { get; }
        IEnumerable<Stack> Stacks { get; }

        Stack this[Cell cell]          { get; }
        Stack this[int file, int rank] { get; }
    }
}
