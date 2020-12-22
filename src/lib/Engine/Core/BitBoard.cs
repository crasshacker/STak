//
// Compiling for any framework older than netcoreapp3.0 (including all current versions of netstandard,
// up to and including netstandard2.1) requires that these two lines be commented out.
//
#define USE_BITOPERATIONS_INTRINSICS
using static System.Numerics.BitOperations;

using System;
using System.Text;
using System.Linq;
using System.Numerics;
using System.Diagnostics;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace STak.TakEngine
{
    /// <summary>
    /// Provides an efficient mechanism for compactly storing board state and quickly analyzing board positions.
    /// </summary>
    /// <remarks>
    /// A bitboard stores that state of the game board in a compact representation that allows for fast evaluation
    /// of its quality, where quality means "how good it looks for a particular player" relative to other possible
    /// board states achievable in the same number of moves.  It carries all of the information that a full Board
    /// instance does, except that it doesn't treat stones as unique instances with their own unique IDs, but as
    /// generic stones with only a type (flat/standing/cap) and a player assignment (white/black).  In fact, when
    /// a stack is is returned by bitboard[cell] it is comprised solely of stones taken from a set of six stones
    /// that are reused whenever a stone of the matching type and player (e.g., a white capstone) is required.
    /// </remarks>
    public class BitBoard : IBoard
    {
        // NOTE: We might(?) improve performance by applying [MethodImpl(MethodImplOptions.AggressiveInlining)] to
        //       some of the smaller methods called often during move evaluation.  We'd need to profile to be sure
        //       that this improved rather than degraded performance.

        private static readonly BitBoardMaskSet[] s_bitBoardMaskSets =
        {
            new BitBoardMaskSet(3),
            new BitBoardMaskSet(4),
            new BitBoardMaskSet(5),
            new BitBoardMaskSet(6),
            new BitBoardMaskSet(7),
            new BitBoardMaskSet(8)
        };

        // When a bitboard stack is accessed we create a new Stack and fill it with pre-built stones that are
        // shared between all stacks that are accessed from the bitboard.  So the stones in a bitboard Stack
        // have the proper stone type and color (player Id), but do not have unique Id's and thus can't be
        // used to identify specific stones.
        private static readonly Stone[] FlatStones     = new Stone[] { new Stone(Player.One, StoneType.Flat),
                                                                       new Stone(Player.Two, StoneType.Flat) };
        private static readonly Stone[] StandingStones = new Stone[] { new Stone(Player.One, StoneType.Standing),
                                                                       new Stone(Player.Two, StoneType.Standing) };
        private static readonly Stone[] Capstones      = new Stone[] { new Stone(Player.One, StoneType.Cap),
                                                                       new Stone(Player.Two, StoneType.Cap) };

        //
        // For an 8x8 board, cell h1 is represented by the low order bit, a1 is the eighth lowest bit,
        // h2 is at the ninth lowest bit, and so on, putting cell a8 in the high order bit.
        //
        private readonly int    m_size;      // Board size.
        private byte[]          m_heights;   // Height of each stack, including the top stone.
        private ulong[]         m_stacks;    // LSB of the ulong is the bottom of the stack.
        private ulong           m_white;     // Stacks topped by a white stone.
        private ulong           m_black;     // Stacks topped by a black stone.
        private ulong           m_standing;  // Stacks topped by a standing stone.
        private ulong           m_cap;       // Stacks topped by a capstone.
        private BitBoardMaskSet m_masks;     // Generally useful masks.

        public int   Size                               => m_size;
        public int   WhiteControlCount                  => PopCount(m_white);
        public int   BlackControlCount                  => PopCount(m_black);
        public int   OccupancyCount                     => PopCount(m_white | m_black);
        public int   WhiteRoadCount                     => PopCount(m_white &  ~m_standing);
        public int   BlackRoadCount                     => PopCount(m_black &  ~m_standing);
        public int   WhiteFlatCount                     => PopCount(m_white & ~(m_standing | m_cap));
        public int   BlackFlatCount                     => PopCount(m_black & ~(m_standing | m_cap));

        public bool  IsOnBoard(int file, int rank)      => (m_masks.Mask & GetBoardForCell(file, rank)) != 0;
        public bool  IsEdgeCell(int file, int rank)     => (m_masks.Edge & GetBoardForCell(file, rank)) != 0;

        public ulong GetStack(int file, int rank)       => m_stacks[GetCellIndex(file, rank)];
        public int   GetStackHeight(int file, int rank) => m_heights[GetCellIndex(file, rank)];


        public BitBoard(int boardSize)
        {
            if (boardSize < 3 || boardSize > 8)
            {
                throw new ArgumentException($"Invalid board size: {boardSize}.");
            }

            m_size    = boardSize;
            m_heights = new byte[64];
            m_stacks  = new ulong[64];
            m_masks   = s_bitBoardMaskSets[boardSize - Board.Sizes.Min()];
        }


        public BitBoard Clone()
        {
            BitBoard bitBoard = (BitBoard) this.MemberwiseClone();

            bitBoard.m_heights = new byte[64];
            m_heights.CopyTo(bitBoard.m_heights, 0);

            bitBoard.m_stacks = new ulong[64];
            m_stacks.CopyTo(bitBoard.m_stacks, 0);

            return bitBoard;
        }


        public Stack this[Cell cell]
        {
                    get => this[cell.File, cell.Rank];
            private set => this[cell.File, cell.Rank] = value;
        }


        // NOTE: When a bitboard stack is accessed we create a new Stack and fill it with pre-built stones that
        //       have the appropriate player Id and stone type.  These same pre-built stones are used in all of
        //       stacks returned by this method over time.  Caller should be aware of this and not assume that
        //       stones are unique; they are only useful for determining the player Id and stone type.  At present
        //       this method is called from only once location - BasicGame.ValidateBoard - and that code is only
        //       enabled when DEBUG_BOARD is set, so this method currently isn't generally used at all.
        public Stack this[int file, int rank]
        {
            get
            {
                Stack stack = new Stack(file, rank);

                int   bitIndex    = GetCellIndex(file, rank);
                ulong bitBoard    = GetBoardForCell(file, rank);
                ulong stoneStack  = m_stacks[bitIndex];
                byte  stackHeight = m_heights[bitIndex];
                int   playerId;

                if (stackHeight > 0)
                {
                    for (int i = 0; i < stackHeight-1; ++i)
                    {
                        playerId = (int) ((stoneStack >> i) & 1);
                        stack.Stones.Add(FlatStones[playerId]);
                    }

                    playerId = (int) ((stoneStack >> (stackHeight-1)) & 1);

                    if (IsBitSet(m_standing, bitBoard))
                    {
                        stack.Stones.Add(StandingStones[playerId]);
                    }
                    else if (IsBitSet(m_cap, bitBoard))
                    {
                        stack.Stones.Add(Capstones[playerId]);
                    }
                    else
                    {
                        stack.Stones.Add(FlatStones[playerId]);
                    }

                    if (playerId == 0)
                    {
                        Debug.Assert(((m_white >> bitIndex) & 1UL) == 1);
                        Debug.Assert(((m_black >> bitIndex) & 1UL) == 0);
                    }
                    else
                    {
                        Debug.Assert(((m_black >> bitIndex) & 1UL) == 1);
                        Debug.Assert(((m_white >> bitIndex) & 1UL) == 0);
                    }
                }

                return stack;
            }

            private set
            {
                Stack stack = value;
                ulong bitStack = 0;

                for (int i = stack.Count-2; i >= 0; --i)
                {
                    bitStack = (bitStack << 1) | (ulong)(uint)stack.Stones[i].PlayerId;
                }

                int bitIndex = GetCellIndex(file, rank);
                m_heights[bitIndex] = (byte)stack.Count;
                m_stacks[bitIndex]  = bitStack;

                if (stack.TopStone != null)
                {
                    ulong resetMask = ~ (1UL << bitIndex);

                    ulong white    = (ulong) stack.TopStone.PlayerId;
                    ulong black    = 1 - white;
                    ulong standing = stack.TopStone.Type == StoneType.Standing ? 1UL : 0UL;
                    ulong cap      = stack.TopStone.Type == StoneType.Cap      ? 1UL : 0UL;

                    m_white &= resetMask;
                    m_white |= (white << bitIndex);

                    m_black &= resetMask;
                    m_black |= (black << bitIndex);

                    m_standing &= resetMask;
                    m_standing |= (standing << bitIndex);

                    m_cap &= resetMask;
                    m_cap |= (cap << bitIndex);
                }
            }
        }


        public void ApplyMove(IMove move)
        {
            if (move is StoneMove)
            {
                StoneMove stoneMove = move as StoneMove;
                Stone stone = stoneMove.Stone;

                int bitIndex = GetCellIndex(stoneMove.TargetCell);
                ulong resetMask = ~ (1UL << bitIndex);

                ulong black = (ulong) stone.PlayerId;
                ulong white = 1UL - black;

                //
                // Because this is a StoneMove, we must be placing the stone on an empty call, thus adding
                // the stone to the cell's (empty) stack.  However, rather than using the general form for
                // adding a stone to a stack we just set the color and count values explicitly.  If we were
                // adding the stone to a non-empty stack we would need to use the more expensive code:
                //
                // m_stacks[bitIndex] &= ~ (1UL << m_heights[bitIndex]);
                // m_stacks[bitIndex] |= (black << m_heights[bitIndex]);
                // m_heights[bitIndex]++;
                //
                m_stacks [bitIndex] = black;
                m_heights[bitIndex] = 1;

                m_white &= resetMask;
                m_white |= (white << bitIndex);

                m_black &= resetMask;
                m_black |= (black << bitIndex);

                m_standing &= resetMask;
                m_cap      &= resetMask;

                if (stone.Type == StoneType.Standing) { m_standing |= 1UL << bitIndex; }
                if (stone.Type == StoneType.Cap     ) { m_cap      |= 1UL << bitIndex; }
            }
            else
            {
                StackMove stackMove = move as StackMove;
                Cell currentCell = stackMove.StartingCell;

                int bitIndex = GetCellIndex(currentCell);
                ulong resetMask = ~ (1UL << bitIndex);

                ulong finalStand = 0UL;
                ulong finalBlack = 0UL;

                byte grabbedHeight = (byte) stackMove.StoneCount;
                m_heights[bitIndex] -= grabbedHeight;
                byte stackHeight = m_heights[bitIndex];
                ulong stack = m_stacks[bitIndex];

                ulong grabbedStack = (stack >> stackHeight) & ((1UL << grabbedHeight) - 1);
                m_stacks[bitIndex] = (stackHeight > 0) ? (stack & (UInt64.MaxValue >> (64-stackHeight))) : 0;

                // Save away the stone type of the top stone of the stack.
                ulong standing = (m_standing >> bitIndex) & 1;
                ulong cap      = (m_cap      >> bitIndex) & 1;

                // Get color of the top of the stack after stones removed.
                ulong black = ((stackHeight != 0) && (((stack >> (stackHeight-1)) & 1UL) != 0)) ? 1UL : 0UL;
                ulong white =  (stackHeight != 0) ? 1 - black : 0UL;

                m_white = (m_white & resetMask) | (white << bitIndex);
                m_black = (m_black & resetMask) | (black << bitIndex);

                // The new top stone cannot be anything other than flat.
                m_standing &= resetMask;
                m_cap      &= resetMask;

                int droppedCount = 0;
                foreach (int dropCount in stackMove.DropCounts)
                {
                    currentCell = currentCell.Move(stackMove.Direction);
                    bitIndex = GetCellIndex(currentCell);
                    resetMask = ~ (1UL << bitIndex);

                    finalStand = ((m_standing >> bitIndex) & 1);
                    finalBlack = ((m_black    >> bitIndex) & 1);

                    for (int i = 0; i < dropCount; ++i)
                    {
                        ulong color = (grabbedStack >> droppedCount) & 1UL;
                        m_stacks[bitIndex] |= (color << m_heights[bitIndex]++);
                        droppedCount++;
                    }

                    black = (m_stacks[bitIndex] >> (m_heights[bitIndex]-1)) & 1;
                    white = 1 - black;

                    m_white = (m_white & resetMask) | (white << bitIndex);
                    m_black = (m_black & resetMask) | (black << bitIndex);

                    // We'll update for the final location below.
                    // Top stones for all other stacks are flat.
                    m_standing &= resetMask;
                    m_cap      &= resetMask;
                }

                m_standing = (m_standing & resetMask) | (standing << bitIndex);
                m_cap      = (m_cap      & resetMask) | (cap      << bitIndex);

                if (stackMove.FlattenedStone == null && cap == 1 && finalStand == 1)
                {
                    // It's a flattening move that hasn't previously been applied/executed.  This implies that the
                    // move is being applied as part of an AI's move evaluation procedure using a BasicGame that's
                    // marked as UseBitBoardOnly, because otherwise BasicGame.MakeMove would have already set the
                    // FlattenedStone property while performing the move on the main Board (not BitBoard).  Thus,
                    // we assume that it's safe to set the move's FlattenedStone to a generic stone instance with
                    // Id == -1 because the main Board won't see the stone and get confused due to its lack of Id.
                    stackMove.FlattenStone(FlatStones[finalBlack]);
                }
            }
        }


        public void UnapplyMove(IMove move)
        {
            if (move is StoneMove)
            {
                StoneMove stoneMove = move as StoneMove;
                int bitIndex = GetCellIndex(stoneMove.TargetCell);

                //
                // Because this is a StoneMove, we must be removing the lone stone on a call, leaving it empty.
                // So, rather than using the general form we would otherwise need to use to remove a stone from
                // the top of a stack, we just set the values directly.  The general form is:
                //
                // m_stacks[bitIndex] &= ~ (1UL << m_heights[bitIndex]);
                // m_heights[bitIndex]--;
                //
                m_stacks [bitIndex] = 0;
                m_heights[bitIndex] = 0;

                ulong resetMask = ~ (1UL << bitIndex);

                m_white    &= resetMask;
                m_black    &= resetMask;
                m_standing &= resetMask;
                m_cap      &= resetMask;
            }
            else
            {
                StackMove stackMove = move as StackMove;
                Cell currentCell = stackMove.StartingCell;
                int startIndex = GetCellIndex(currentCell);

                int   bitIndex   = 0;
                ulong standing   = 0;
                ulong cap        = 0;
                ulong resetMask  = 0;
                ulong black, white;

                foreach (int dropCount in stackMove.DropCounts)
                {
                    currentCell = currentCell.Move(stackMove.Direction);
                    bitIndex = GetCellIndex(currentCell);
                    resetMask = ~ (1UL << bitIndex);

                    m_heights[bitIndex] -= (byte) dropCount;
                    int height = m_heights[bitIndex];
                    ulong stack = m_stacks[bitIndex];
                    m_stacks[bitIndex] = (height > 0) ? (stack & (UInt64.MaxValue >> (64-height))) : 0;

                    ulong stones = (stack >> height) & ((1UL << dropCount) - 1);
                    m_stacks[startIndex] |= stones << m_heights[startIndex];
                    m_heights[startIndex] += (byte) dropCount;

                    black = (height > 0) ? ((stack >> (height-1)) & 1) : 0UL;
                    white = (height > 0) ? (1UL - black) : 0UL;

                    // Save status of top stone of final drop location.
                    cap      = (m_cap      >> bitIndex) & 1;
                    standing = (m_standing >> bitIndex) & 1;

                    // Update who (if anyone) now controls this stack.
                    m_white = (m_white & resetMask) | (white << bitIndex);
                    m_black = (m_black & resetMask) | (black << bitIndex);

                    // Top stones on cells that stones were moved onto must have been flat (or there were no
                    // stones on the cell prior to the move), except for the final cell, which might have had
                    // a standing stone on it (but in no case could a capstone have topped any cell dropped on).
                    m_standing = (m_standing & resetMask);
                    m_cap      = (m_cap      & resetMask);
                }

                //
                // This is not an ideal solution because stackMove.IsFlattening is only valid after the move
                // has been applied to a board in a particular state.  Applying the move to the board just to
                // determine whether it's a flattening move wastes valuable time during AI move enumeration.
                //
                // Ideally we'd recognize whether a move is flattening in the ApplyMove method and make a note
                // of it, but maintaining a single Boolean value indicating whether a standing stone in a cell
                // was flattened is insufficient, because a capstone might flatten a stone, then later move to
                // another cell, then later again move back to the original cell where it flattened a stone.
                // Now if we undo moves we don't know whether to change the flattened stone to a standing stone
                // when the capstone undoes the first stack move to land on the cell, or the second one.  So we
                // need to use the stack move's knowledge of whether it flattened a stone, since each StackMove
                // maintains its own state.
                //
                if (stackMove.IsFlattening)
                {
                    m_standing |= (1UL << bitIndex);
                }

                // Set properties of the original stack that has now been reverted.

                black = (m_stacks[startIndex] >> (m_heights[startIndex]-1)) & 1;
                white = 1UL - black;

                resetMask = ~ (1UL << startIndex);

                m_white    = (m_white    & resetMask) | (white    << startIndex);
                m_black    = (m_black    & resetMask) | (black    << startIndex);
                m_standing = (m_standing & resetMask) | (standing << startIndex);
                m_cap      = (m_cap      & resetMask) | (cap      << startIndex);
            }
        }


        public ulong GetFlatWinCells(int playerId)
        {
            return (playerId == Player.One) ? (m_white & ~(m_standing | m_cap))
                 : (playerId == Player.Two) ? (m_black & ~(m_standing | m_cap))
                                            : 0;
        }


        public int GetStackControl(int file, int rank)
        {
            int bitIndex = GetCellIndex(file, rank);
            return (int) (m_stacks[bitIndex] >> (m_heights[bitIndex]-1));
        }


        public int[] GetRoadExtents(int playerId)
        {
            ulong board = (playerId == 0) ? m_white : m_black;

            List<ulong> islands = GetIslands(board &~ m_standing);

            int fileExtent = 0;
            int rankExtent = 0;

            foreach (ulong island in islands)
            {
                int fileMin = m_size+1;
                int fileMax = 0;
                int rankMin = m_size+1;
                int rankMax = 0;

                for (int file = 0; file < m_size; ++file)
                {
                    for (int rank = 0; rank < m_size; ++rank)
                    {
                        if (IsBitSet(island, file, rank))
                        {
                            fileMin = Math.Min(fileMin, file);
                            fileMax = Math.Max(fileMax, file);
                            rankMin = Math.Min(rankMin, rank);
                            rankMax = Math.Max(rankMax, rank);
                        }
                    }
                }

                if (1 + fileMax - fileMin > fileExtent)
                {
                    fileExtent = 1 + fileMax - fileMin;
                }
                if (1 + rankMax - rankMin > rankExtent)
                {
                    rankExtent = 1 + rankMax - rankMin;
                }
            }

            return new int[] { fileExtent, rankExtent };
        }


        public bool HasRoad(int playerId)
        {
            return GetLongIsland(playerId) != 0;
        }


        public ulong GetRoad(int playerId)
        {
            return Minimize(GetLongIsland(playerId));
        }


        public bool IsBitSet(ulong board, int file, int rank)
        {
            return (board & GetBoardForCell(file, rank)) != 0;
        }


#if USE_BITOPERATIONS_INTRINSICS
        public static int PopCount(ulong value)
        {
            return BitOperations.PopCount(value);
        }
#else
        public static int PopCount(ulong value)
        {
            const ulong c1 = 0x_55555555_55555555ul;
            const ulong c2 = 0x_33333333_33333333ul;
            const ulong c3 = 0x_0F0F0F0F_0F0F0F0Ful;
            const ulong c4 = 0x_01010101_01010101ul;

            value = value - ((value >> 1) & c1);
            value = (value & c2) + ((value >> 2) & c2);
            value = (((value + (value >> 4)) & c3) * c4) >> 56;

            return (int) value;
        }
#endif


        public IEnumerable<Stack> Stacks
        {
            get
            {
                for (int file = 0; file < Size; ++file)
                {
                    for (int rank = 0; rank < Size; ++rank)
                    {
                        yield return this[file, rank];
                    }
                }
                yield break;
            }
        }


        public override bool Equals(object obj)
        {
            bool isEqual = false;

            if (obj != null && obj.GetType() == this.GetType())
            {
                BitBoard bitBoard = obj as BitBoard;

                isEqual = m_size     == bitBoard.m_size
                       && m_white    == bitBoard.m_white
                       && m_black    == bitBoard.m_black
                       && m_standing == bitBoard.m_standing
                       && m_cap      == bitBoard.m_cap
                       && m_heights  .SequenceEqual(bitBoard.m_heights)
                       && m_stacks   .SequenceEqual(bitBoard.m_stacks);
            }

            return isEqual;
        }


        public override int GetHashCode()
        {
            unchecked // Overflow is fine, just wrap.
            {
                ulong hash = (ulong)m_size + m_white * 3 + m_black * 7 + m_standing * 11 + m_cap * 13;

                foreach (byte height in m_heights)
                {
                    hash += height;
                }
                foreach (ulong stack in m_stacks)
                {
                    hash += stack;
                }

                return (int)hash;
            }
        }


        public string ToString(ulong board, int size = -1)
        {
            if (size == -1)
            {
                size = m_size;
            }

            StringBuilder sb = new StringBuilder();

            for (int rank = size-1; rank >= 0; --rank)
            {
                for (int file = 0; file < size; ++file)
                {
                    int index = GetCellIndex(file, rank);
                    bool isSet = ((board >> index) & 1) == 1;
                    sb.Append(isSet ? 'X' : '-');
                }
                sb.Append("\n");
            }

            return sb.ToString();
        }


        public override string ToString()
        {
            List<string> controls = new List<string>();
            List<string> whites   = new List<string>();
            List<string> blacks   = new List<string>();
            List<string> heights  = new List<string>();

            StringBuilder sb;

            // Build white/black control board.
            controls.Add("Control:");
            for (int rank = m_size-1; rank >= 0; --rank)
            {
                sb = new StringBuilder();
                for (int file = 0; file < m_size; ++file)
                {
                    int index = GetCellIndex(file, rank);

                    bool white = ((m_white >> index) & 1) == 1;
                    bool black = ((m_black >> index) & 1) == 1;

                    sb.Append(white ? 'W' : black ? 'B' : '-');
                }
                controls.Add(sb.ToString());
            }

            // Build white top stone board.
            whites.Add("White:");
            for (int rank = m_size-1; rank >= 0; --rank)
            {
                sb = new StringBuilder();
                for (int file = 0; file < m_size; ++file)
                {
                    int index = GetCellIndex(file, rank);

                    bool white    =          ((m_white    >> index) & 1) == 1;
                    bool standing = white && ((m_standing >> index) & 1) == 1;
                    bool cap      = white && ((m_cap      >> index) & 1) == 1;

                    sb.Append(standing ? 'S' : cap ? 'C' : white ? 'W' : '-');
                }
                whites.Add(sb.ToString());
            }

            // Build black top stone board.
            blacks.Add("Black:");
            for (int rank = m_size-1; rank >= 0; --rank)
            {
                sb = new StringBuilder();
                for (int file = 0; file < m_size; ++file)
                {
                    int index = GetCellIndex(file, rank);

                    bool black    =          ((m_black    >> index) & 1) == 1;
                    bool standing = black && ((m_standing >> index) & 1) == 1;
                    bool cap      = black && ((m_cap      >> index) & 1) == 1;

                    sb.Append(standing ? 'S' : cap ? 'C' : black ? 'B' : '-');
                }
                blacks.Add(sb.ToString());
            }

            // Build stack height board.
            heights.Add("Height:");
            for (int rank = m_size-1; rank >= 0; --rank)
            {
                sb = new StringBuilder();
                for (int file = 0; file < m_size; ++file)
                {
                    int index = GetCellIndex(file, rank);
                    int height = m_heights[index];

                    sb.Append(height > 0 ? height.ToString() : "-");
                }
                heights.Add(sb.ToString());
            }

            sb = new StringBuilder();

            for (int i = 0; i < controls.Count; ++i)
            {
                sb.Append($"{controls[i],-10}{whites[i],-10}{blacks[i],-10}{heights[i]}\n");
            }

            return sb.ToString();
        }


        private ulong Minimize(ulong board)
        {
            // NOTE: The algorithm used here may in some cases produce a non-minimal road, but it should
            //       generally produce something at least very close to minimal, without many (or any)
            //       unneeded cells included in the road.

            ulong MinimizeOnePass(ulong board)
            {
                ulong road = board;

                for (int file = 0; file < m_size; ++file)
                {
                    for (int rank = 0; rank < m_size; ++rank)
                    {
                        if (IsBitSet(road, file, rank))
                        {
                            board = road &~ GetBoardForCell(file, rank);
                            var islands = GetIslands(board);

                            if (islands.Where(i => (((i & m_masks.Left) != 0 && (i & m_masks.Right)  != 0)
                                                 || ((i & m_masks.Top)  != 0 && (i & m_masks.Bottom) != 0))).Any())
                            {
                                return MinimizeOnePass(board);
                            }
                        }
                    }
                }

                return road;
            }

            int count;

            do
            {
                count = PopCount(board);
                board = MinimizeOnePass(board);
            }
            while (count > PopCount(board));

            return board;
        }


        private ulong GetLongIsland(int playerId)
        {
            ulong board = (playerId == 0) ? m_white : m_black;
            List<ulong> islands = GetIslands(board &~ m_standing);

            return islands.Where(i => (((i & m_masks.Left) != 0 && (i & m_masks.Right)  != 0)
                                    || ((i & m_masks.Top)  != 0 && (i & m_masks.Bottom) != 0)))
                                                                              .FirstOrDefault();
        }


        private List<ulong> GetIslands(ulong board)
        {
            List<ulong> boards = new List<ulong>();

            ulong seen = 0;
            while (board != 0)
            {
                ulong next = board & (board - 1);
                ulong bit  = board &~ next;

                if ((seen & bit) == 0)
                {
                    ulong flood = Flood(board, bit);
                    if (flood != bit)
                    {
                        boards.Add(flood);
                    }
                    seen |= flood;
                }

                board = next;
            }

            return boards;
        }


        private ulong Flood(ulong within, ulong seed)
        {
            while (true)
            {
                ulong next = Spread(within, seed);
                if (next == seed)
                {
                    return next;
                }
                seed = next;
            }
        }


        private ulong Spread(ulong within, ulong seed)
        {
            ulong next = seed;
            next |= (seed << 1) &~ m_masks.Right;
            next |= (seed >> 1) &~ m_masks.Left;
            next |= (seed >> m_size);
            next |= (seed << m_size);
            return next & within;
        }


        private int GetCellIndex(int file, int rank)
        {
            return (m_size-(file+1)) + (m_size*rank);
        }


        private int GetCellIndex(Cell cell)
        {
            return GetCellIndex(cell.File, cell.Rank);
        }


        private ulong GetBoardForCell(int file, int rank)
        {
            return 1UL << GetCellIndex(file, rank);
        }


        private bool IsBitSet(ulong board, ulong bitBoard)
        {
            return (board & bitBoard) != 0;
        }


        private class BitBoardMaskSet
        {
            public ulong Left   { get; private set; }
            public ulong Right  { get; private set; }
            public ulong Top    { get; private set; }
            public ulong Bottom { get; private set; }
            public ulong Edge   { get; private set; }
            public ulong Mask   { get; private set; }


            public BitBoardMaskSet(int boardSize)
            {
                Right = 0;
                for (int i = 0; i < boardSize; ++i)
                {
                    Right |= 1UL << (i * boardSize);
                }
                Left   = Right << (boardSize - 1);
                Top    = (ulong) (((1UL << boardSize) - 1) << (boardSize * (boardSize - 1)));
                Bottom = (ulong) ((1 << boardSize) - 1);
                Edge   = Left | Right | Top | Bottom;
                Mask   = (ulong) (1 << (boardSize * boardSize) - 1);
            }
        }
    }
}
