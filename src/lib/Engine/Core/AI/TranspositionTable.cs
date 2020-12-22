using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using STak.TakEngine;

namespace STak.TakEngine.AI
{
    //
    // TODO - Come up with an efficient way to do Zobrist hashing.  Difficult due to the fact that unlike chess
    //        and similar games, a cell can contain multiple game pieces, making the set of hashes used to define
    //        the complete set of game states extremely large and completely nonviable.  A partial solution might
    //        be to define hashes only for the top N stones on each stack, along with the count of stacked stones.
    //
    internal class TranspositionTable
    {
        private readonly ConcurrentDictionary<BitBoard, Transposition> m_transpositionTable;

        public int Count => m_transpositionTable.Count;


        public TranspositionTable()
        {
            m_transpositionTable = new();
        }


        public void Clear() => m_transpositionTable.Clear();


        public Transposition this[BitBoard bitBoard]
        {
            get => m_transpositionTable.TryGetValue(bitBoard, out Transposition value) ? value : null;
            set => m_transpositionTable[bitBoard] = value;
        }


        // Note: Not thread safe.
        public void AgeAndRemoveDeadEntries()
        {
            List<KeyValuePair<BitBoard, Transposition>> reapables = new();

            foreach (var entry in m_transpositionTable)
            {
                if (entry.Value.TimeToLive > 0)
                {
                    entry.Value.TimeToLive--;
                }
                else
                {
                    reapables.Add(entry);
                }
            }

            foreach (var entry in reapables)
            {
                m_transpositionTable.TryRemove(entry);
            }
        }

    }


    internal enum BoundType
    {
        Exact,
        Lower,
        Upper
    }


    internal class Transposition
    {
        public int       Depth;
        public int       Value;
        public BoundType BoundType;
        public Variation Variation;
        public int       TimeToLive;


        public Transposition()
        {
        }
    }
}
