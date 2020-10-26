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
    public class PulsatingAnimation : FrameRenderingAnimation
    {
        public int      PulseCount    { get; protected set; }
        public TimeSpan PulseDuration { get; protected set; }
        public double   MinimumValue  { get; protected set; }
        public double   MaximumValue  { get; protected set; }
        public double   InitialValue  { get; protected set; }
        public double   EndingValue   { get; protected set; }
        public double   CurrentValue  { get; protected set; }
        public double   HoldingValue  { get; protected set; }
        public double   StoppingRate  { get; protected set; }

        private          TimeSpan     m_firstDuration;
        private          TimeSpan     m_lastDuration;
        private          int          m_pulseIndex;
        private          DateTime     m_pulseStart;
        private          bool         m_initialized;
        private          PulsingState m_state;
        private readonly int          m_originalPulseCount;
        private readonly TimeSpan     m_originalPulseDuration;
        private readonly object       m_syncLock;

        private double RangeExtent  => MaximumValue - MinimumValue;
        private bool   IsRising     => ((PulseCount > 0) && (m_pulseIndex % 2 == 0))
                                    || ((PulseCount < 0) && (m_pulseIndex % 2 == 1));
        private bool   IsPartial    => m_pulseIndex == 0 || m_pulseIndex == Math.Abs(PulseCount);

        private enum PulsingState { Holding, Pulsing, Stopping, Stopped }


        private PulsatingAnimation()
        {
        }


        public PulsatingAnimation(int pulseCount, TimeSpan pulseDuration, double minimumValue, double maximumValue,
                                                                                               double stoppingRate)
        {
            if (pulseDuration.Ticks == 0)
            {
                pulseDuration = TimeSpan.FromMilliseconds(1);
            }

            PulseCount    = pulseCount;
            PulseDuration = pulseDuration;
            MinimumValue  = minimumValue;
            MaximumValue  = maximumValue;
            HoldingValue  = Double.MinValue; // arbitrary/unnecessary
            CurrentValue  = Double.MinValue; // arbitrary/unnecessary
            InitialValue  = Double.MinValue; // arbitrary/unnecessary
            EndingValue   = Double.MinValue; // arbitrary/unnecessary
            StoppingRate  = stoppingRate;

            m_state                 = PulsingState.Stopped;
            m_originalPulseCount    = pulseCount;
            m_originalPulseDuration = pulseDuration;
            m_firstDuration         = TimeSpan.MinValue;
            m_lastDuration          = TimeSpan.MinValue;
            m_syncLock              = new object();
        }


        public Task Start(double initialValue, double endingValue)
        {
            Task task = null;

            if (m_state == PulsingState.Stopped)
            {
                // Attach to the frame rendering event.
                task = StartAnimation(initialValue, endingValue);
            }
            else if (m_state == PulsingState.Pulsing)
            {
                // Do nothing.
            }
            else if (m_state == PulsingState.Holding)
            {
                // Do nothing.
            }
            else if (m_state == PulsingState.Stopping)
            {
                task = ContinueAnimation();
            }

            return task;
        }


        public void Stop()
        {
            lock (m_syncLock)
            {
                if (m_state == PulsingState.Stopped)
                {
                    StopAnimation(); // TODO - Is this needed/correct?
                }
                else if (m_state == PulsingState.Pulsing)
                {
                    StopAnimation();
                }
                else if (m_state == PulsingState.Holding)
                {
                    StopAnimation();
                }
                else if (m_state == PulsingState.Stopping)
                {
                    // Do nothing.
                }
            }
        }


        protected override void Finish()
        {
            if (m_state != PulsingState.Holding)
            {
                m_state = PulsingState.Stopped;
            }
            base.Finish();
        }


        protected override void UpdateAnimationState(object sender, EventArgs e)
        {
            lock (m_syncLock)
            {
                var now = DateTime.Now;

                if (! m_initialized)
                {
                    m_pulseStart   = now;
                    m_initialized  = true;
                    m_firstDuration = (RangeExtent == 0) ? TimeSpan.FromMilliseconds(0)
                                    : (PulseCount  >= 0) ? (MaximumValue-InitialValue) / RangeExtent * PulseDuration
                                                         : (InitialValue-MinimumValue) / RangeExtent * PulseDuration;

                    // s_logger.Debug($"First render: partDuration is {m_firstDuration}.");
                }
                else
                {
                    var duration = IsPartial ? m_firstDuration : PulseDuration;
                    var progress = 1.0; // temporary lie

                    if (duration.TotalMilliseconds > 0)
                    {
                        progress  = Math.Min(1.0, (now - m_pulseStart) / duration);

                        var baseValue = (m_pulseIndex == 0) ? InitialValue
                                  : IsRising ? MinimumValue : MaximumValue;

                        var endValue = (PulseCount == 1) ? MaximumValue : (PulseCount == -1) ? MinimumValue
                                     : (m_pulseIndex == Math.Abs(PulseCount)) ? EndingValue
                                                    : IsRising ? MaximumValue : MinimumValue;

                        if (Math.Abs(PulseCount) == 1)
                        {
                            HoldingValue = endValue;
                            m_state = PulsingState.Holding;
                        }

                        CurrentValue = IsRising ? baseValue + (progress * (endValue - baseValue))
                                                : baseValue - (progress * (baseValue - endValue));

                        // s_logger.Debug($"Update[1]: base={baseValue}, min={MinimumValue}, max={MaximumValue}, "
                        //      + $" starting={InitialValue}, endingValue={EndingValue}, current={CurrentValue}, "
                        //                                          + $"progress={progress}, isRising={IsRising}.");
                    }

                    if (progress >= 1.0)
                    {
                        m_pulseStart = now;
                        m_pulseIndex++;

                        // s_logger.Debug($"Now at pulse index {m_pulseIndex}.");

                        if (m_pulseIndex >= Math.Abs(PulseCount))
                        {
                            if ((m_lastDuration == TimeSpan.MinValue) && (EndingValue != CurrentValue)
                                      && ((m_pulseIndex == Math.Abs(PulseCount)) || (PulseCount == 0)))
                            {
                                var range = IsRising ? (EndingValue-MinimumValue)
                                                     : (MaximumValue-EndingValue);
                                m_lastDuration = PulseDuration * (range / RangeExtent);

                                // s_logger.Debug($"New partDuration is {m_lastDuration}.");
                            }
                            else
                            {
                                CurrentValue = (m_state == PulsingState.Holding) ? HoldingValue : EndingValue;
                                // Shut everything down.
                                Finish();
                            }
                        }
                    }
                }
            }

            base.UpdateAnimationState(sender, e);
        }


        private Task StartAnimation(double initialValue, double endingValue)
        {
            lock (m_syncLock)
            {
                HoldingValue   = Double.MinValue;
                CurrentValue   = initialValue;
                InitialValue   = initialValue;
                EndingValue    = endingValue;
                PulseCount     = m_originalPulseCount;
                PulseDuration  = m_originalPulseDuration;
                m_lastDuration = TimeSpan.MinValue;
                m_initialized  = false;
                m_pulseIndex   = 0;

                // Reverse initial direction if we're on the wrong side of the range.
                if ((PulseCount > 0 && InitialValue > MaximumValue)
                 || (PulseCount < 0 && InitialValue < MinimumValue))
                {
                    PulseCount *= -1;
                }

                m_state = PulsingState.Pulsing;
                return base.Start();
            }
        }


        private Task ContinueAnimation()
        {
            return StartAnimation(CurrentValue, EndingValue);
        }


        private void StopAnimation()
        {
            lock (m_syncLock)
            {
                if (m_state == PulsingState.Holding)
                {
                    _ = base.Start();
                }

                // In case things would go badly with a pulse duration of zero.
                double stoppingRate = Math.Max(0.001, StoppingRate);

                HoldingValue   = Double.MinValue;
                InitialValue   = CurrentValue;
                PulseCount     = 0;
                PulseDuration  *= stoppingRate;
                m_lastDuration = TimeSpan.MinValue;
                m_pulseIndex   = 0;
                m_initialized  = false;

                m_state = PulsingState.Stopping;
            }
        }
    }
}
