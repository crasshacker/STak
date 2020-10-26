using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using System.Windows.Media.Animation;
using NLog;
using STak.TakEngine;

namespace STak.WinTak
{
    public class AnimationTimeChangedEventArgs
    {
        public int Time { get; private set; }

        public AnimationTimeChangedEventArgs(int time)
        {
            Time = time;
        }
    }


    public abstract class MoveAnimation : PathAnimation
    {
        // TODO - We should use Board.Width to define this value.
        public static readonly int DefaultMoveTime = 1000;

        public new static bool IsActive => s_activeMoveAnimationCount > 0;
        public     static bool IsEnabled { get; set; } = true;

        public static event EventHandler<AnimationTimeChangedEventArgs> AnimationTimeChanged;

        private static double AnimationAirGap => UIAppConfig.Appearance.Animation.AnimationAirGap;

        private static int    s_activeMoveAnimationCount;
        private static double s_animationSpeed;

        protected IMove         m_move;
        protected AnimationType m_moveType;
        protected int           m_playerId;
        protected bool          m_highlight;


        public MoveAnimation(TableModel tableModel, IMove move, int playerId, TimeSpan duration,
                                                         AnimationType moveType, bool highlight)
            : base(tableModel, duration)
        {
            m_move      = move;
            m_moveType  = moveType;
            m_playerId  = playerId;
            m_highlight = highlight;
        }


        public override async Task Start()
        {
            if (m_isActive)
            {
                throw new Exception("Cannot start animation that is already running.");
            }

            Interlocked.Increment(ref s_activeMoveAnimationCount);
            await base.Start();
        }


        protected override void Finish()
        {
            if (m_isActive)
            {
                Interlocked.Decrement(ref s_activeMoveAnimationCount);
            }
            base.Finish();
        }


        public static void SetAnimationSpeed(double speedRatio)
        {
            s_animationSpeed = Math.Max(speedRatio, 0.0001);
            var speedFactor = UIAppConfig.Appearance.Animation.AnimationSpeedFactor;
            var duration = (int) (s_animationSpeed * DefaultMoveTime / speedFactor);
            UIAppConfig.Appearance.Animation.MoveAnimationTime = duration;
            AnimationTimeChanged?.Invoke(null, new AnimationTimeChangedEventArgs(duration));
        }


        public static double GetAnimationSpeed()
        {
            return s_animationSpeed;
        }


        protected double GetAirGap()
        {
            var flatModel = m_tableModel.GetStoneModel(StoneType.Flat);
            return (double)flatModel.Height * AnimationAirGap;
        }


        protected List<PathSegment> GetRaisedPath(Point3D startPoint, Point3D endPoint)
        {
            var sampleFactor = 0.5;

            var vector = endPoint - startPoint;
            var length = vector.Length;
            vector.Normalize();

            int samples = Math.Min(20, (int)(length * sampleFactor));
            var pathCells = new Dictionary<BoardCell, Point3D>();
            double maxHeight = Math.Max(startPoint.Y, endPoint.Y) + GetAirGap();

            for (int i = 0; i < samples; ++i)
            {
                double progress = (i+1) / (double)samples;
                var point = startPoint + (length * (vector * progress));
                Rect rect = m_tableModel.GetStoneRectangleCenteredAtPoint(new Point(point.X, point.Z));

                foreach (var boardCell in BoardModel.GetCellsIntersectedBy(rect)
                                      .Where(bc => ! pathCells.ContainsKey(bc)))
                {
                    var height = BoardModel.Height + m_tableModel.GetStackHeight(boardCell.Stack);
                    pathCells[boardCell] = new Point3D(boardCell.Center.X, height, boardCell.Center.Y);
                    maxHeight = Math.Max(maxHeight, height);
                }

                foreach (var reserveModel in m_tableModel.ReserveModels)
                {
                    var height = reserveModel.GetHeightAtLocation(rect);
                    maxHeight = Math.Max(maxHeight, height);
                }
            }

            List<Point3D> path = new List<Point3D>(new Point3D[] { startPoint, endPoint });

            if (startPoint.Y < maxHeight)
            {
                path.Insert(1, new Point3D(startPoint.X, maxHeight, startPoint.Z));
            }
            if (endPoint.Y < maxHeight)
            {
                path.Insert(path.Count-1, new Point3D(endPoint.X, maxHeight, endPoint.Z));
            }

            var segments = new List<PathSegment>();

            for (int i = 0; i < path.Count-1; ++i)
            {
                var segment = new PathSegment
                {
                    StartPoint = path[i],
                    EndPoint   = path[i+1],
                };

                segment.Compile();
                segments.Add(segment);
            }

            return segments;
        }
    }
}
