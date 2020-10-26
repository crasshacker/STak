using System;
using System.Threading.Tasks;
using System.Threading;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using System.Windows.Media.Animation;
using NLog;
using STak.TakEngine;

namespace STak.WinTak
{
    public class PulsatingStoneModelHighlighter : IModelHighlighter
    {
        private static readonly NLog.Logger s_logger = LogManager.GetCurrentClassLogger();

        private readonly PulsatingAnimation m_animation;
        private readonly Func<double>       m_getCurrentValue;
        private readonly Action<double>     m_setCurrentValue;
        private readonly double             m_minimumValue;
        private readonly double             m_maximumValue;
        private readonly int                m_modelId;
        private readonly int                m_pulseCount;
        private readonly TimeSpan           m_pulseDuration;
        private readonly double             m_endingValue;
        private readonly object             m_syncLock;
        private          double             m_startingValue;
        private          bool               m_highlighted;

        public bool IsHighlighted => m_highlighted;

        public event EventHandler HighlightingComplete;


        public PulsatingStoneModelHighlighter(int modelId, int pulseCount, TimeSpan pulseDuration, double minimumValue,
                                                          double maximumValue, double endingValue, double stoppingRate,
                                                                                          Func<double> getCurrentValue,
                                                                                        Action<double> setCurrentValue)
        {
            m_modelId         = modelId;
            m_getCurrentValue = getCurrentValue;
            m_setCurrentValue = setCurrentValue;
            m_pulseCount      = pulseCount;
            m_pulseDuration   = pulseDuration;
            m_minimumValue    = minimumValue;
            m_maximumValue    = maximumValue;
            m_endingValue     = endingValue;
            m_syncLock        = new object();

            m_animation = new PulsatingAnimation(m_pulseCount, m_pulseDuration, m_minimumValue, m_maximumValue,
                                                                                                  stoppingRate);
            m_animation.AnimationStateUpdated += UpdateAnimationState;
        }


        public void Highlight(bool highlight)
        {
            lock (m_syncLock)
            {
                if (highlight != m_highlighted)
                {
                    m_highlighted = highlight;

                    if (highlight)
                    {
                        m_startingValue = m_getCurrentValue();
                        var endingValue = (m_pulseCount == 0 && m_startingValue == m_minimumValue) ? m_maximumValue
                                        : (m_pulseCount == 0 && m_startingValue == m_maximumValue) ? m_minimumValue
                                                                                                   : m_endingValue;
                        s_logger.Debug($"Starting pulse for stone model Id={m_modelId}:\n"
                                     + $"highlight     = {highlight}\n"
                                     + $"pulseCount    = {m_pulseCount}\n"
                                     + $"pulseDuration = {m_pulseDuration}\n"
                                     + $"minimumValue  = {m_minimumValue}\n"
                                     + $"maximumValue  = {m_maximumValue}\n"
                                     + $"startingValue = {m_startingValue}\n"
                                     + $"endingValue   = {endingValue}\n");

                        m_animation.Start(m_startingValue, endingValue);
                    }
                    else
                    {
                        s_logger.Debug($"Stopping pulse for stone model Id={m_modelId}.");
                        m_animation.Stop();
                    }
                }
            }
        }


        private void UpdateAnimationState(object sender, AnimationStateUpdatedEventArgs e)
        {
            if (e.UpdateType == AnimationStateUpdateType.Updated)
            {
                m_setCurrentValue(m_animation.CurrentValue);
            }
            else if (e.UpdateType == AnimationStateUpdateType.Aborted
                  || e.UpdateType == AnimationStateUpdateType.Finished)
            {
                HighlightingComplete?.Invoke(this, new EventArgs());
            }
        }
    }
}
