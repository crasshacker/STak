using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using System.Windows.Media.Animation;
using NLog;
using STak.TakEngine;

namespace STak.WinTak
{
    public class StoneMoveAnimation : MoveAnimation
    {
        private readonly StoneMove  m_stoneMove;
        private readonly StoneModel m_stoneModel;
        private          Point3D    m_startPoint;
        private          Point3D    m_endPoint;
        private          bool       m_isHighlighted;


        public StoneMoveAnimation(TableModel tableModel, StoneMove stoneMove, int playerId, TimeSpan moveDuration,
                                                AnimationType moveType, bool highlightMove, StoneModel stoneModel)
            : base(tableModel, stoneMove, playerId, moveDuration, moveType, highlightMove)
        {
            m_stoneMove  = stoneMove.Clone() as StoneMove;
            m_stoneModel = stoneModel;

            SetCourse(BuildSegments(), moveType);
        }


        public override async Task Start()
        {
            var operation = m_moveType switch
            {
                AnimationType.MakeMove  => "move",
                AnimationType.UndoMove  => "undo",
                AnimationType.AbortMove => "abort",
                _                       => "unknown"
            };

            var point1 = (m_moveType == AnimationType.MakeMove) ? m_startPoint : m_endPoint;
            var point2 = (m_moveType == AnimationType.MakeMove) ? m_endPoint   : m_startPoint;

            s_logger.Debug($"Animating stone {operation} '{m_move}' - {point1.RoundToInteger()} "
                                                               + $"=> {point2.RoundToInteger()}.");
            HighlightMove(m_highlight);
            await base.Start();
        }


        protected override void Finish()
        {
            s_logger.Debug($"Completed animation of stone move '{m_move}'");
            HighlightMove(m_highlight);
            base.Finish();
        }


        protected override IEnumerable<IPositionable> BeginSegment(PathSegment segment)
        {
            var point1 = (m_moveType == AnimationType.MakeMove) ? segment.StartPoint : segment.EndPoint;
            var point2 = (m_moveType == AnimationType.MakeMove) ? segment.EndPoint   : segment.StartPoint;

            s_logger.Debug($"Begin animation segment {point1.RoundToInteger()} "
                                              + $"=> {point2.RoundToInteger()}.");

            return new StoneModel[] { m_stoneModel };
        }


        protected override void EndSegment(PathSegment segment)
        {
            if (segment.SoundFile != null)
            {
                AudioPlayer.PlaySound(segment.SoundFile);
            }
        }


        private Point3D GetStoneModelReservePosition(StoneModel stoneModel)
        {
            return ReserveModels[stoneModel.Stone.PlayerId].GetStoneModelPosition(stoneModel);
        }


        private PathSegment[] BuildSegments()
        {
            switch (m_moveType)
            {
              case AnimationType.MakeMove:
                m_startPoint = m_stoneModel.GetPosition();
                m_endPoint   = GetPointForBoardCell(BoardModel[m_stoneMove.TargetCell]);
                break;

              case AnimationType.UndoMove:
                m_startPoint = GetStoneModelReservePosition(m_stoneModel);
                m_endPoint   = m_stoneModel.GetPosition();
                break;

              case AnimationType.AbortMove:
                m_startPoint = GetStoneModelReservePosition(m_stoneModel);
                m_endPoint   = m_stoneModel.GetPosition();
                break;
            }

            var soundFile = StoneModel.GetSoundFileName(m_stoneModel.Type);
            var segments = GetRaisedPath(m_startPoint, m_endPoint).ToArray();
            int index = (m_moveType == AnimationType.MakeMove) ? segments.Length-1 : 0;
            segments[index].SoundFile = soundFile;
            return segments;
        }


        private void HighlightMove(bool highlight)
        {
            if      (  highlight && ! m_isHighlighted) { m_stoneModel.Highlight(true);  }
            else if (! highlight &&   m_isHighlighted) { m_stoneModel.Highlight(false); }

            m_isHighlighted = highlight;
        }
    }
}
