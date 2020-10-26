using System;
using System.Linq;
using System.Reflection;
using System.Diagnostics;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using System.Windows.Input;
using System.Windows.Controls;
using System.Windows.Media.Animation;
using System.Windows.Media.Media3D;
using System.Windows.Media;
using NodaTime;
using NLog;
using STak.TakEngine;
using STak.TakEngine.Trackers;
using STak.WinTak.Properties;

namespace STak.WinTak
{
    public partial class TableModel
    {
        private readonly Board          m_board;
        private          BoardModel     m_boardModel;
        private          ReserveModel[] m_reserveModels;

        public StoneModel[]   StoneModels   { get; private set; }
        public ReserveModel[] ReserveModels => m_reserveModels;
        public BoardModel     BoardModel    => m_boardModel;


        public TableModel(IGame game)
        {
            m_board = game.Board;
            BuildModel();
        }


        public void BuildModel()
        {
            m_boardModel    = BuildBoardModel(); // Must be built prior to building reserve/stone models.
            m_reserveModels = new ReserveModel[] { BuildReserveModel(Player.One), BuildReserveModel(Player.Two) };
            StoneModels     = m_reserveModels.SelectMany(rm => rm.StoneModels).ToArray();
        }


        public void UpdateGame(IGame game)
        {
            foreach (var stoneModel in StoneModels)
            {
                if (stoneModel.Stone != null)
                {
                    if (game is BasicGame basicGame)
                    {
                        try
                        {
                            // FIXIT - Should we add Stones property to IBasicGame?  Seems wrong, but necessary.
                            stoneModel.Stone = basicGame.Stones.Where(s => s.Id == stoneModel.Stone.Id).Single();
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debugger.Break();
                            throw new InvalidOperationException($"No stone for model {stoneModel.Stone.Id}: {ex}");
                        }
                    }
                    else
                    {
                        throw new InvalidOperationException("TableModel can only update BasicGame games.");
                    }
                }
            }
        }


        public void Clear()
        {
            foreach (var stoneModel in StoneModels)
            {
                if (stoneModel.IsHighlighted)
                {
                    stoneModel.Highlight(false);
                }
            }
        }


        public void ApplyScheme(Scheme scheme)
        {
            m_boardModel.ApplyScheme(scheme);
            m_reserveModels[Player.One].ApplyScheme(scheme);
            m_reserveModels[Player.Two].ApplyScheme(scheme);
        }


        public StoneModel GetStoneModel(int stoneId)
        {
            return StoneModels.Where(m => m.Stone != null && m.Stone.Id == stoneId).SingleOrDefault();
        }


        public StoneModel GetStoneModel(StoneType stoneType)
        {
            return StoneModels.FirstOrDefault(sm => sm.Type == stoneType);
        }


        public StoneModel GetStoneModel(Model3D model)
        {
            return StoneModels.Where(m => m.Model == model).SingleOrDefault();
        }


        public double GetStackHeight(IEnumerable<Stone> stones, Stone stone = null)
        {
            double stackHeight = 0;
            bool foundStone = false;

            foreach (Stone stackedStone in stones)
            {
                if (stone != null && stone.Id == stackedStone.Id)
                {
                    foundStone = true;
                    break;
                }
                StoneModel stoneModel = GetStoneModel(stackedStone.Id);
                stackHeight += stoneModel.Height;
            }

            if (stone != null && ! foundStone)
            {
                throw new Exception($"Stone Id={stone.Id} not present in stack.");
            }

            return stackHeight;
        }


        public double GetStackHeight(Stack stack, Stone stone = null)
        {
            return GetStackHeight(stack.Stones, stone);
        }


        public Rect GetStoneRectangleCenteredAtPoint(Point point)
        {
            double extent = m_boardModel.StoneExtent;
            double x = point.X  - (extent / 2);
            double y = point.Y  - (extent / 2);
            return new Rect(x, y, extent, extent);
        }


        public void UpdateStoneModelPosition(Stone stone)
        {
            // Note: This affects only stones that are in play (on the board).  Stones in reserves
            //       always reside in their proper locations (see ReserveModel.ReturnStoneModel).

            var stoneModel = GetStoneModel(stone.Id);
            var boardCell  = BoardModel.GetCellContainingStone(stone);

            if (boardCell != null && stoneModel != null)
            {
                double height = BoardModel.Height + GetStackHeight(boardCell.Stack, stone);
                var position = new Point3D(boardCell.Center.X, height, boardCell.Center.Y);
                stoneModel.SetPosition(position);
                stoneModel.AlignToStone();
            }
        }


        public void UpdateStoneModelPositions(IEnumerable<Stone> stones)
        {
            foreach (var stone in stones)
            {
                UpdateStoneModelPosition(stone);
            }
        }


        public void UpdateStandingStoneModels(IEnumerable<Stone> flattenedStones, List<IMove> moves,
                                                                           int fromMove, int toMove)
        {
            int movesAffected = Math.Abs(fromMove - toMove);
            StoneModel stoneModel;

            // IMPORTANT: Avoid modifying existing moves and stones by using copies.
            flattenedStones = new List<Stone>(flattenedStones.Select(s => s.Clone()));
            moves = new List<IMove>(moves.Select(m => m.Clone()));

            if (toMove < fromMove)
            {
                int start = moves.Count - movesAffected;

                for (int i = 0; i < movesAffected; ++i)
                {
                    var move = moves[start+i];

                    if (move is StoneMove stoneMove)
                    {
                        stoneModel = GetStoneModel(stoneMove.Stone.Id);
                        ReserveModels[stoneMove.Stone.PlayerId].ReturnStoneModel(stoneModel, true);
                    }
                    else if (move is StackMove stackMove)
                    {
                        if (stackMove.FlattenedStone != null)
                        {
                            stoneModel = GetStoneModel(stackMove.FlattenedStone.Id);
                            stoneModel.Stone.Type = StoneType.Standing;
                            stoneModel.AlignToStone();
                        }
                    }
                }
            }
            else if (toMove > fromMove)
            {
                int start = moves.Count - movesAffected;

                for (int i = 0; i < movesAffected; ++i)
                {
                    var move = moves[start+i];

                    if (move is StoneMove stoneMove)
                    {
                        var stone = stoneMove.Stone;
                        var type = flattenedStones.Where(s => s.Id == stone.Id).Any() ? StoneType.Standing : stone.Type;
                        stoneModel = ReserveModels[stone.PlayerId].DrawAnAccessibleModel(type);
                        stoneModel.Stone = stone;
                    }
                    else if (move is StackMove stackMove)
                    {
                        if (stackMove.FlattenedStone != null)
                        {
                            stoneModel = GetStoneModel(stackMove.FlattenedStone.Id);
                            stoneModel.Stone.Type = StoneType.Flat;
                            stoneModel.AlignToStone();
                        }
                    }
                }
            }
        }


        public double GetMinimumHeightForStoneCenteredAt(Point point)
        {
            Rect rect = GetStoneRectangleCenteredAtPoint(point);

            double height = m_boardModel.GetCellsIntersectedBy(rect)
                                        .Select(bc => GetStackHeight(bc.Stack) + BoardModel.Height)
                                        .Concat(m_reserveModels.Select(rm => rm.GetHeightAtLocation(rect)))
                                        .Max();
            if (height == 0)
            {
                // The point is outside the 2D space covered by the board and reserves.  To maintain smooth
                // movement across the board's boundary we set the height to the board model's height.
                height = BoardModel.Height;
            }
            else if (m_boardModel.IsIntersectedByRect(rect))
            {
                // We might or might not have intersected a cell, but we're over the board (perhaps just at
                // one of the edges/bevels) so we need to ensure we set the height to at least board height.
                height = Math.Max(height, BoardModel.Height);
            }

            return height;
        }


        private BoardModel BuildBoardModel()
        {
            return new BoardModel(m_board, UIAppConfig.Appearance.Models.StoneToCellWidthRatio);
        }


        private ReserveModel BuildReserveModel(int reserveId)
        {
            return new ReserveModel(reserveId, m_board.Size, BoardModel.Front, BoardModel.Left, BoardModel.Width,
                                                                                        m_boardModel.StoneExtent);
        }


        public Stack GetStackContainingStone(Stone stone)
        {
            return GetStacks().SingleOrDefault(s => s.Contains(stone));
        }


        public IEnumerable<Stack> GetStacks()
        {
            for (int file = 0; file < m_boardModel.Board.Size; ++file)
            {
                for (int rank = 0; rank < m_boardModel.Board.Size; ++rank)
                {
                    yield return m_board[file, rank];
                }
            }
        }
    }
}
