using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using System.Windows.Media.Animation;
using NLog;
using STak.TakEngine;

namespace STak.WinTak
{
    public abstract class PathAnimation : FrameRenderingAnimation
    {
        protected List<IPositionable> m_positionables = new List<IPositionable>();
        protected TableModel          m_tableModel;
        protected PathSegment[]       m_segments;
        protected int                 m_segmentIndex;
        protected bool                m_reverse;
        protected TimeSpan            m_duration;
        private   double              m_distance;
        private   bool                m_courseSet;

        protected TableModel     TableModel    => m_tableModel;
        protected BoardModel     BoardModel    => m_tableModel.BoardModel;
        protected ReserveModel[] ReserveModels => m_tableModel.ReserveModels;


        protected abstract IEnumerable<IPositionable> BeginSegment(PathSegment segment);
        protected abstract void                       EndSegment  (PathSegment segment);


        public PathAnimation(TableModel tableModel, TimeSpan duration)
        {
            m_tableModel = tableModel;
            m_duration   = duration;
        }


        public virtual void SetCourse(IEnumerable<PathSegment> segments, AnimationType moveType = AnimationType.MakeMove)
        {
            m_segmentIndex = 0;
            m_segments     = segments.ToArray();
            m_reverse      = moveType == AnimationType.UndoMove ||  moveType == AnimationType.AbortMove;
            m_courseSet    = true;

            CompilePath(m_segments);
        }


        public override async Task Start()
        {
            if (! m_courseSet)
            {
                throw new Exception("SetCourse must be called prior to starting the animation.");
            }

            if (! m_isActive)
            {
                // Configure start/end times for each path segment.
                SetSegmentStartAndEndTimes(DateTime.Now);

                // Initialize the initial segment positionables.
                var segmentIndex = m_reverse ? m_segments.Length-1 : 0;
                StartSegment(m_segments[segmentIndex]);

                // Attach to the frame rendering event.
                AnimationStateUpdated += AnimationStateUpdateHandler;

                // Go!
                await base.Start();
            }
        }


        protected override void Finish()
        {
            base.Finish();
            m_courseSet = false;

            // Detach from the frame rendering event.
            AnimationStateUpdated -= AnimationStateUpdateHandler;
        }


        private void AnimationStateUpdateHandler(object sender, AnimationStateUpdatedEventArgs e)
        {
            if (e.UpdateType == AnimationStateUpdateType.Aborted)
            {
                MoveToFinalDestination();
            }
        }


        private void MoveToFinalDestination()
        {
            var segmentIndex = m_reverse ? (m_segments.Length - (m_segmentIndex+1)) : m_segmentIndex;
            var segment      = m_segments[segmentIndex];

            var endPoint = m_reverse ? segment.StartPoint : segment.EndPoint;
            var offset   = endPoint - m_positionables[0].GetPosition();

            foreach (var positionable in m_positionables)
            {
                positionable.SetPosition(positionable.GetPosition() + offset);
            }
        }


        protected override void UpdateAnimationState(object sender, EventArgs e)
        {
            var segmentIndex = m_reverse ? (m_segments.Length - (m_segmentIndex+1)) : m_segmentIndex;
            var segment      = m_segments[segmentIndex];

            var startPoint = m_reverse ? segment.EndPoint   : segment.StartPoint;
            var endPoint   = m_reverse ? segment.StartPoint : segment.EndPoint;

            Vector3D vector = endPoint - startPoint;
            var length = vector.Length;
            vector.Normalize();

            var lastPosition = m_positionables[0].GetPosition();

            var currentTime  = DateTime.Now;
            var segmentTime  = segment.EndTime - segment.StartTime;
            var endOfSegment = currentTime >= segment.EndTime;
            var elapsed      = (currentTime - segment.StartTime).TotalSeconds;
            var progress     = Math.Min(1.0, elapsed / segmentTime.TotalSeconds);
            var position     = endOfSegment ? endPoint : startPoint + (length * (vector * progress));
            var offset       = position - lastPosition;

            foreach (var positionable in m_positionables)
            {
                positionable.SetPosition(positionable.GetPosition() + offset);
            }

            if (endOfSegment)
            {
                EndSegment(segment);

                m_segmentIndex++;
                if (m_segmentIndex < m_segments.Length)
                {
                    segment = m_segments[segmentIndex + (m_reverse ? -1 : 1)];
                    SetSegmentStartAndEndTimes(segment, DateTime.Now);
                    StartSegment(segment);
                }
                else
                {
                    Finish();
                }
            }

            base.UpdateAnimationState(sender, e);
        }


        protected void CompilePath(IEnumerable<PathSegment> segments)
        {
            m_distance = segments.Select(s => GeometryHelper.GetDistanceBetween(s.StartPoint, s.EndPoint)).Sum();
            m_duration *= (m_distance / BoardModel.Width);

            foreach (var segment in segments)
            {
                segment.Compile();
                segment.Fraction = (m_distance > 0) ? segment.Distance / m_distance : 0;
            }
        }


        protected Point3D GetPointForBoardCell(Cell cell)
        {
            return GetPointForBoardCell(BoardModel[cell]);
        }


        protected Point3D GetPointForBoardCell(BoardCell boardCell)
        {
            double height = BoardModel.Height + m_tableModel.GetStackHeight(boardCell.Stack);
            return new Point3D(boardCell.Center.X, height, boardCell.Center.Y);
        }


        private void SetPositionables(IEnumerable<IPositionable> positionables)
        {
            m_positionables.Clear();
            m_positionables.AddRange(positionables);
        }


        private void StartSegment(PathSegment segment)
        {
            segment.Compile();
            SetSegmentStartAndEndTimes(segment, DateTime.Now);
            SetPositionables(BeginSegment(segment));

            var message = $"Starting segment: Index={m_segmentIndex}, Position={segment.StartPoint.RoundToInteger()}, "
                        + $"Stone Count={m_positionables.Count}.";
            s_logger.Debug(message);
        }


        private void SetSegmentStartAndEndTimes(DateTime startTime)
        {
            for (int i = 0; i < m_segments.Length; ++i)
            {
                // Note that we process segments in reverse order when undoing a move.
                var segment = m_segments[m_reverse ? (m_segments.Length-i)-1 : i];

                var segmentDuration = TimeSpan.FromMilliseconds(m_duration.TotalMilliseconds * segment.Fraction);
                var endTime = startTime + segmentDuration;

                segment.StartTime = startTime;
                segment.EndTime   = endTime;

                startTime = endTime;
            }
        }


        private void SetSegmentStartAndEndTimes(PathSegment segment, DateTime startTime)
        {
            var segmentDuration = m_duration * segment.Fraction;
            var endTime = startTime + segmentDuration;

            segment.StartTime = startTime;
            segment.EndTime   = endTime;
        }
    }
}

