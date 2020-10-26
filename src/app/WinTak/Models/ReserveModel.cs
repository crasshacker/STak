using System;
using System.Linq;
using System.Diagnostics;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Input;
using System.Windows.Controls;
using System.Windows.Media.Media3D;
using System.Windows.Media;
using NLog;
using STak.TakEngine;

namespace STak.WinTak
{
    public class ReserveModel
    {
        private static readonly NLog.Logger s_logger = LogManager.GetCurrentClassLogger();

        private readonly int                m_reserveId;
        private readonly ModelInfo[]        m_modelInfos;
        private readonly List<StoneModel>[] m_flatStoneModels;
        private readonly StoneModel[]       m_capstoneModels;
        private readonly List<StoneModel>   m_returnedModels;
        private          List<StoneModel>   m_stoneModels;
        private          StoneModel         m_highlightedModel;
        private          int                m_capCounter;
        private          int                m_flatCounter;

        public StoneModel DrawnStoneModel { get; private set; }


        public ReserveModel(int playerId, int boardSize, double boardFront,  double boardLeft,
                                                         double boardExtent, double stoneExtent)
        {
            int flatCount  = Board.GetFlatStoneCount(boardSize);
            int capCount   = Board.GetCapstoneCount(boardSize);
            int stackCount = boardSize - capCount;

            m_modelInfos      = new ModelInfo[flatCount + capCount];
            m_flatStoneModels = Enumerable.Range(0, stackCount).Select(x => new List<StoneModel>()).ToArray();
            m_capstoneModels  = new StoneModel[capCount];
            m_returnedModels  = new List<StoneModel>();
            m_flatCounter     = 0;
            m_capCounter      = 0;
            m_reserveId       = playerId;

            Build(playerId, boardSize, boardFront, boardLeft, boardExtent, stoneExtent);
        }


        public IEnumerable<StoneModel> StoneModels
        {
            get
            {
                foreach (var stoneModel in m_stoneModels)
                {
                    if (stoneModel != null)
                    {
                        yield return stoneModel;
                    }
                }
            }
        }


        public void ApplyScheme(Scheme scheme)
        {
            foreach (var stoneModel in StoneModels)
            {
                stoneModel.ApplyScheme(scheme, m_reserveId);
            }
        }


        public bool CanDrawStoneModel(StoneModel stoneModel)
        {
            return (stoneModel.Type == StoneType.Cap)
                 ? m_capstoneModels.Where(m => m == stoneModel).Any()
                 : m_flatStoneModels.Where(m => m.Count > 0 && m[^1] == stoneModel).Any();
        }


        public bool CanDrawStoneModel(int stoneId)
        {
            return m_capstoneModels.Where(m => m.Id == stoneId).Any()
                || m_flatStoneModels.Where(m => m.Count > 0 && m[^1].Id == stoneId).Any();
        }


        public StoneModel DrawAnAccessibleModel(StoneType stoneType)
        {
            StoneModel drawnStoneModel = null;
            Random random = new Random();

            //
            // If m_returnedModels is not empty we must be redoing a move, so we'll try to draw the same
            // stone that we did in the original move.  (This isn't strictly necessary, but it avoids the
            // odd appearance of a different stone model being drawn following an undo then redo operation.)
            //
            if (m_returnedModels.Count > 0)
            {
                var stoneModel = m_returnedModels[^1];

                if (((stoneModel.Type == StoneType.Cap) != (stoneType == StoneType.Cap))
                                                     || ! CanDrawStoneModel(stoneModel))
                {
                    // Something's out of whack; clean things up.
                    m_returnedModels.Clear();
                }
                else
                {
                    m_returnedModels.RemoveAt(m_returnedModels.Count-1);
                    drawnStoneModel = stoneModel;

                    if (stoneType == StoneType.Cap)
                    {
                        for (int i = 0; i < m_capstoneModels.Length; ++i)
                        {
                            if (m_capstoneModels[i] == drawnStoneModel)
                            {
                                m_capstoneModels[i] = null;
                                break;
                            }
                        }
                    }
                    else
                    {
                        foreach (var stoneList in m_flatStoneModels)
                        {
                            if (stoneList.Count > 0 && stoneList[^1] == drawnStoneModel)
                            {
                                stoneList.RemoveAt(stoneList.Count-1);
                                break;
                            }
                        }
                    }
                }
            }

            if (drawnStoneModel == null)
            {
                if (stoneType == StoneType.Cap)
                {
                    int value = random.Next(m_capstoneModels.Count(m => m != null));
                    for (int i = 0; i < m_capstoneModels.Length; ++i)
                    {
                        if (m_capstoneModels[i] != null && value-- == 0)
                        {
                            drawnStoneModel = m_capstoneModels[i];
                            m_capstoneModels[i] = null;
                            break;
                        }
                    }
                }
                else
                {
                    int value = random.Next(m_flatStoneModels.Count(m => m.Count > 0));
                    foreach (var stoneList in m_flatStoneModels)
                    {
                        if (stoneList.Count > 0 && value-- == 0)
                        {
                            drawnStoneModel = stoneList[^1];
                            stoneList.RemoveAt(stoneList.Count-1);
                            break;
                        }
                    }
                }
            }

            if (drawnStoneModel != null)
            {
                if (stoneType == StoneType.Standing)
                {
                    drawnStoneModel.Unflatten();
                }

                DrawnStoneModel = drawnStoneModel;

                string id = (drawnStoneModel.Stone != null) ? drawnStoneModel.Stone.Id.ToString() : "(null)";
                s_logger.Debug($"Drew stone of type {drawnStoneModel.Type} with model Id={drawnStoneModel.Id} "
                             + $"and stone Id={id} from reserve {m_reserveId}.");
            }
            else
            {
                s_logger.Debug($"Failed to draw a stone of type {stoneType}.");
            }

            return drawnStoneModel;
        }


        public StoneModel DrawStoneModel(int modelId)
        {
            var stoneModel = StoneModels.Where(sm => sm.Id == modelId).Single();

            if (stoneModel != null)
            {
                if (stoneModel.Type == StoneType.Cap)
                {
                    for (int i = 0; i < m_capstoneModels.Length; ++i)
                    {
                        if (m_capstoneModels[i] == stoneModel)
                        {
                            m_capstoneModels[i] = null;
                        }
                    }
                }
                else
                {
                    foreach (var stoneList in m_flatStoneModels)
                    {
                        // Note that we always take a topmost stone.
                        if (stoneList.Count > 0 && stoneModel == stoneList[^1])
                        {
                            stoneList.RemoveAt(stoneList.Count-1);
                        }
                    }
                }

                if (stoneModel.Type == StoneType.Standing)
                {
                    stoneModel.Unflatten();
                }

                DrawnStoneModel = stoneModel;

                string id = (stoneModel.Stone != null) ? stoneModel.Stone.Id.ToString() : "(null)";
                s_logger.Debug($"Drew stone of type {stoneModel.Type} with model Id={stoneModel.Id}"
                                                 + $" and stone Id={id} from reserve {m_reserveId}.");
            }
            else
            {
                s_logger.Debug("Failed to draw the specified stone model.");
            }

            return stoneModel;
        }


        public void PlaceStoneModel(Stone stone)
        {
            if (DrawnStoneModel == null)
            {
                throw new Exception("Cannot place stone when none was drawn.");
            }

            // Important:  Associate the model with the stone.
            DrawnStoneModel.Stone = stone;

            int index = GetStoneModelIndex(DrawnStoneModel);
            m_modelInfos[index].Model   = DrawnStoneModel;
            m_modelInfos[index].StoneId = stone.Id;

            DrawnStoneModel = null;
        }


        public void ReturnStoneModel(StoneModel stoneModel, bool isUndo = false)
        {
            s_logger.Debug($"Returning stone model with model Id={stoneModel.Id}, stone Id={stoneModel.Stone.Id}.");

            if (stoneModel.Stone.PlayerId != m_reserveId)
            {
                throw new Exception("Cannot return a stone to the other player's reserve.");
            }

            if (isUndo)
            {
                m_returnedModels.Add(stoneModel);
            }

            int index = GetStoneModelIndex(stoneModel);
            ModelInfo modelInfo = m_modelInfos[index];
            stoneModel.SetPosition(modelInfo.Point);

            if (stoneModel.Type == StoneType.Cap)
            {
                m_capstoneModels[modelInfo.Index] = stoneModel;
            }
            else
            {
                stoneModel.Flatten();
                m_flatStoneModels[modelInfo.Index].Add(stoneModel);
            }

            int stoneId = stoneModel.Stone.Id;  // For the log message below.
            stoneModel.Stone = null;            // Very important!
            DrawnStoneModel = null;             // Also important!

            s_logger.Debug($"Returned stone of type {stoneModel.Type} with model Id={stoneModel.Id} "
                                                + $"and stone Id={stoneId} to reserve {m_reserveId}.");
        }


        public void AddHighlight(StoneModel stoneModel)
        {
            if (m_highlightedModel != stoneModel)
            {
                if (m_highlightedModel != null)
                {
                    m_highlightedModel.Highlight(false);
                }
                m_highlightedModel = stoneModel;
                stoneModel.Highlight(true);
            }
        }


        public void RemoveHighlight()
        {
            if (m_highlightedModel != null)
            {
                m_highlightedModel.Highlight(false);
                m_highlightedModel = null;
            }
        }


        public Point3D GetStoneModelPosition(StoneModel stoneModel)
        {
            int index = GetStoneModelIndex(stoneModel);
            return m_modelInfos[index].Point;
        }


        private int GetStoneModelIndex(StoneModel stoneModel)
        {
            return stoneModel.Id % m_modelInfos.Length;
        }


        public double GetHeightAtLocation(Rect stoneRect)
        {
            double height = 0;

            foreach (var stoneModels in m_flatStoneModels.Where(ms => ms.Count > 0))
            {
                var stoneModel = stoneModels[0];
                var rect = stoneModel.Get2DBounds();
                if (stoneRect.IntersectsWith(rect))
                {
                    height = Math.Max(height, stoneModel.Height * stoneModels.Count);
                }
            }
            foreach (var stoneModel in m_capstoneModels)
            {
                if (stoneModel != null)
                {
                    var rect = stoneModel.Get2DBounds();
                    if (stoneRect.IntersectsWith(rect))
                    {
                        height = Math.Max(height, stoneModel.Height);
                    }
                }
            }

            return height;
        }


        private void Build(int playerId, int boardSize, double boardFront,  double boardLeft,
                                                        double boardExtent, double stoneExtent)
        {
            double cellExtent  = boardExtent / boardSize;

            int flatCount  = Board.GetFlatStoneCount(boardSize);
            int capCount   = Board.GetCapstoneCount (boardSize);
            int stoneCount = flatCount + capCount;

            // Create a temporary flatstone to use in computing how we need to scale the model.
            double scale = stoneExtent / (new StoneModel(0, Player.One, StoneType.Flat, 1.0).Extent);

            int flatCellCount = boardSize - capCount;
            int file, rank;

            m_stoneModels = new List<StoneModel>();

            for (int stoneIndex = 0; stoneIndex < stoneCount; ++stoneIndex)
            {
                StoneType stoneType = (stoneIndex < flatCount) ? StoneType.Flat : StoneType.Cap;

                int stoneId = (playerId == Player.One) ? stoneIndex : stoneIndex+stoneCount;
                StoneModel stoneModel = new StoneModel(stoneId, playerId, stoneType, scale);

                if (stoneType == StoneType.Flat)
                {
                    file = stoneIndex % flatCellCount;
                    rank = stoneIndex / flatCellCount;
                }
                else
                {
                    file = flatCellCount + stoneIndex - flatCount;
                    rank = 0;
                }

                double x = boardLeft  + (cellExtent / 2) + (cellExtent * file);
                double z = boardFront + (cellExtent / 2) + 40;
                double y = (rank * stoneModel.Height);

                x *= (playerId == Player.One) ? 1 : -1;
                z *= (playerId == Player.One) ? 1 : -1;

                var offset = new Point3D(x, y, z);
                var index = (stoneType == StoneType.Cap) ? m_capCounter++
                                                         : m_flatCounter++ % m_flatStoneModels.Length;
                ModelInfo modelInfo = new ModelInfo(stoneModel, index, offset);

                m_modelInfos[stoneIndex] = modelInfo;
                stoneModel.SetPosition(offset);
                m_stoneModels.Add(stoneModel);

                if (stoneType == StoneType.Cap)
                {
                    m_capstoneModels[index] = stoneModel;
                }
                else
                {
                    m_flatStoneModels[index].Add(stoneModel);
                }
            }
        }


        private class ModelInfo
        {
            public int        Index   { get; set; }
            public Point3D    Point   { get; set; }
            public StoneModel Model   { get; set; }
            public int        StoneId { get; set; }

            public ModelInfo(StoneModel stoneModel, int index, Point3D point)
            {
                Model = stoneModel;
                Index = index;
                Point = point;
            }
        }
    }
}
