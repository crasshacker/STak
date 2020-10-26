using System;
using System.Linq;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using System.Windows.Media.Animation;
using NLog;
using STak.TakEngine;

namespace STak.WinTak
{
    public class StackMoveAnimation : MoveAnimation
    {
        private readonly StackMove m_stackMove;
        private          bool      m_isHighlighted;


        public StackMoveAnimation(TableModel tableModel, StackMove stackMove, int playerId, TimeSpan duration,
                                                                   AnimationType moveType, bool highlightMove)
            : base(tableModel, stackMove, playerId, duration, moveType, highlightMove)
        {
            m_stackMove = stackMove.Clone() as StackMove;

            if (moveType == AnimationType.MakeMove)
            {
                // Prepare the move's stack for our inspection.
                m_stackMove.GrabStack(BoardModel.Board, false);
            }
            else
            {
                // Restore the stack without removing the stones from the board.
                m_stackMove.RestoreOriginalGrabbedStack(BoardModel.Board);
            }

            SetCourse(BuildSegments(), moveType);
        }


        public override async Task Start()
        {
            s_logger.Debug($"Animating stack move '{m_move}' - {m_stackMove.GrabbedStack.Count}  stones "
                         + $"from {m_segments[0].StartPoint} to {m_segments[^1].EndPoint}");

            HighlightMove(m_highlight);
            await base.Start();
        }


        protected override void Finish()
        {
            s_logger.Debug($"Completed animation of stack move '{m_move}' - {m_stackMove.GrabbedStack.Count}  stones.");

            HighlightMove(m_highlight);
            base.Finish();
        }


        protected override IEnumerable<IPositionable> BeginSegment(PathSegment segment)
        {
            IEnumerable<Stone> stones = (segment as StackMoveSegment).Carrying;

            return stones.Select(s => m_tableModel.GetStoneModel(s.Id));
        }


        protected override void EndSegment(PathSegment segment)
        {
            if (segment.SoundFile != null)
            {
                AudioPlayer.PlaySound(segment.SoundFile);
            }
        }


        private PathSegment[] BuildSegments()
        {
            var distance     = m_stackMove.Distance;
            var direction    = m_stackMove.Direction;
            var currentCell  = m_stackMove.StartingCell;
            var stoneCount   = m_stackMove.StoneCount;
            var grabbedStack = m_stackMove.GrabbedStack;
            var segmentCount = m_stackMove.Distance * 3;

            var dropCount = 0;
            var boardCell = BoardModel[currentCell];
            var carrying  = new Stone[distance][];
            var dropping  = new Stone[distance][];

            var airGap = GetAirGap();

            for (int i = 0; i < distance; ++i)
            {
                carrying[i] = grabbedStack.Stones.Skip(dropCount).Take(stoneCount-dropCount).ToArray();
                dropping[i] = grabbedStack.Stones.Skip(dropCount).Take(m_stackMove.DropCounts[i]).ToArray();

                dropCount += m_stackMove.DropCounts[i];
                boardCell  = GetNextCell(boardCell);
            }

            double totalLength = 0.0;

            var segments  = new List<StackMoveSegment>();

            for (int i = 0; i < distance; ++i)
            {
                var thisCell = BoardModel[currentCell.Move(direction, 0)];
                var thatCell = BoardModel[currentCell.Move(direction, 1)];

                //
                // Create three segments:
                //
                // 1. A direct upwardly vertical segment starting at the bottom of the bottommost stone being
                //    carried in this segment, and ending at a height just above the top of the stack at the
                //    first cell where stones will be dropped.
                //
                // 2. A direct horizontal segment between the center of the starting cell and the center of
                //    the follow cell in the direction of the move, at the same height at the first segment.
                //
                // 3. A direct downwardly vertical segment ending at the top of either the board or the stack
                //    thereon, if any, at the cell at the end of this move step.
                //
                // NOTE: This code ASSUMES that the actual move/undo/redo hasn't yet been performed; however, in
                //       the case of a move being aborted the move is assumed to have been performed up to the
                //       point that the move was aborted.  For a StackMove this means that zero or more cells
                //       may have already been visited, and any stones dropped on those cells will be present on
                //       the actual board (and will need to be accounted for in height calculations).
                //

                var point1 = GetPointForBoardCell(thisCell);
                var point4 = GetPointForBoardCell(thatCell);

                if (i == 0 && m_moveType == AnimationType.MakeMove)
                {
                    // Adjust so that the starting point is at the bottom of the stack, rather than the top.
                    point1.Y -= m_tableModel.GetStackHeight(carrying[i]);
                }
                else if (m_moveType != AnimationType.MakeMove)
                {
                    // Account for the stones that were dropped here by the move, because we'll be
                    // grabbing those stones from the board.
                    point4.Y -= m_tableModel.GetStackHeight(dropping[i]);
                }

                // Point2 is directly above point1, slightly above the higher of (1) the top of the remainder of
                // the stack on the starting cell for this move segment, and (2) the the top of the stack in the
                // cell on which this move segments is dropping stones.
                Point3D point2 = point1;
                point2.Y = Math.Max(point1.Y, point4.Y) + airGap;

                // Point3 is horizontal move endpoint; Point4 is directly below point3, and
                // is at the level of the top of the board stack (or the board, if no stack).
                Point3D point3 = point4;
                point3.Y = point2.Y;

                if (i > 0 && m_moveType == AnimationType.MakeMove)
                {
                    // We just dropped some stones, so we when we start the next segment of the move the
                    // bottommost stone being moved will be at a higher position than the previous "anchor"
                    // stone.  To account for this we need to raise the points that make up the segment.
                    var height = m_tableModel.GetStackHeight(dropping[i-1]);

                    point1 = RaisePoint(point1, height);
                    point2 = RaisePoint(point2, height);
                    point3 = RaisePoint(point3, height);
                }

                var length1 = Math.Sqrt(Math.Pow(point2.X-point1.X, 2)
                                      + Math.Pow(point2.Y-point1.Y, 2)
                                      + Math.Pow(point2.Z-point1.Z, 2));

                var length2 = Math.Sqrt(Math.Pow(point3.X-point2.X, 2)
                                      + Math.Pow(point3.Y-point2.Y, 2)
                                      + Math.Pow(point3.Z-point2.Z, 2));

                var length3 = Math.Sqrt(Math.Pow(point4.X-point3.X, 2)
                                      + Math.Pow(point4.Y-point3.Y, 2)
                                      + Math.Pow(point4.Z-point3.Z, 2));

                string soundFile = null;

                if (m_moveType != AnimationType.MakeMove)
                {
                    var bottomModel = m_tableModel.GetStoneModel(dropping[i][0].Id);
                    soundFile = StoneModel.GetSoundFileName(bottomModel.Type);
                }
                    
                segments.Add(new StackMoveSegment
                {
                    StartPoint = point1,
                    EndPoint   = point2,
                    Distance   = length1,
                    Carrying   = carrying[i],
                    Dropping   = dropping[i],
                    SoundFile  = soundFile
                });

                segments.Add(new StackMoveSegment
                {
                    StartPoint = point2,
                    EndPoint   = point3,
                    Distance   = length2,
                    Carrying   = carrying[i],
                    Dropping   = dropping[i],
                    SoundFile  = null
                });

                segments.Add(new StackMoveSegment
                {
                    StartPoint = point3,
                    EndPoint   = point4,
                    Distance   = length3,
                    Carrying   = carrying[i],
                    Dropping   = dropping[i],
                    SoundFile  = soundFile
                });

                totalLength += length1 + length2 + length3;
                currentCell = currentCell.Move(direction);
            }

            if (m_moveType == AnimationType.AbortMove)
            {
                var holding    = m_stackMove.GrabbedStack.Stones.Skip(m_stackMove.DropCounts.Sum()).ToArray();
                var startPoint = (distance == 0) ? GetPointForBoardCell(m_stackMove.StartingCell)
                                                 : segments[^1].EndPoint;
                var endPoint   = m_tableModel.GetStoneModel(holding.First().Id).GetPosition();

                if (distance > 0)
                {
                    startPoint.Y += m_tableModel.GetStackHeight(dropping[^1]);
                }

                foreach (var pathSegment in GetRaisedPath(startPoint, endPoint))
                {
                    var segment = new StackMoveSegment(pathSegment)
                    {
                        Carrying = holding,
                        Dropping = holding
                    };
                    segments.Add(segment);
                }
            }

            CompilePath(segments);
            return segments.ToArray();
        }


        private void HighlightMove(bool highlight)
        {
            foreach (var stone in m_stackMove.GrabbedStack.Stones)
            {
                var stoneModel = m_tableModel.GetStoneModel(stone.Id);

                if      (  highlight && ! m_isHighlighted) { stoneModel.Highlight(true);  }
                else if (! highlight &&   m_isHighlighted) { stoneModel.Highlight(false); }
            }
            m_isHighlighted = highlight;
        }


        private Point3D RaisePoint(Point3D point, double height)
        {
            s_logger.Debug($"Raising point {point} by {height} units.");
            return new Point3D(point.X, point.Y + height, point.Z);
        }


        private BoardCell GetNextCell(BoardCell boardCell, bool undo = false)
        {
            var distance  = m_stackMove.Distance;
            var direction = m_stackMove.Direction;
            var startCell = m_stackMove.StartingCell;
            var endCell   = startCell.Move(direction, distance);

            if (undo)
            {
                var temp  = startCell;
                startCell = endCell;
                endCell   = temp;
                direction = Direction.Reverse(direction);
            }

            return (boardCell.Cell != endCell) ? BoardModel[boardCell.Cell.Move(direction)]
                                               : BoardModel[startCell];
        }


        private class StackMoveSegment : PathSegment
        {
            public Stone[] Carrying { get; set; } = new Stone[0];
            public Stone[] Dropping { get; set; } = new Stone[0];

            public StackMoveSegment()
            {
            }

            public StackMoveSegment(PathSegment segment)
            {
                segment.CopyTo(this);
            }
        }
    }
}
