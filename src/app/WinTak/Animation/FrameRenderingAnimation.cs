using System;
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
    public enum AnimationStateUpdateType
    {
        Updated,
        Finished,
        Aborted
    }


    public class AnimationStateUpdatedEventArgs : EventArgs
    {
        public AnimationStateUpdateType UpdateType { get; private set; }

        public AnimationStateUpdatedEventArgs(AnimationStateUpdateType updateType = AnimationStateUpdateType.Updated)
        {
            UpdateType = updateType;
        }
    }


    public class FrameRenderingAnimation
    {
        protected static readonly NLog.Logger s_logger = LogManager.GetCurrentClassLogger();
        private   static          int         s_activeAnimationCount;

        public static bool IsActive => s_activeAnimationCount > 0;

        protected DateTime                   m_startTime;
        protected bool                       m_isActive;
        private   TaskCompletionSource<bool> m_taskSource;

        public event EventHandler<AnimationStateUpdatedEventArgs> AnimationStateUpdated;


        public FrameRenderingAnimation()
        {
            m_startTime = DateTime.MinValue;
        }


        public virtual async Task Start()
        {
            if (m_isActive)
            {
                throw new Exception("Cannot start animation that is already running.");
            }

            Interlocked.Increment(ref s_activeAnimationCount);
            m_isActive = true;
            m_startTime = DateTime.Now;
            m_taskSource = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            CompositionTarget.Rendering += UpdateAnimationState;
            await m_taskSource.Task;
        }


        public virtual void Abort()
        {
            Finish();
            AnimationStateUpdated?.Invoke(this, new AnimationStateUpdatedEventArgs(AnimationStateUpdateType.Aborted));
        }


        protected virtual void Finish()
        {
            if (m_isActive)
            {
                CompositionTarget.Rendering -= UpdateAnimationState;
                Interlocked.Decrement(ref s_activeAnimationCount);
                m_isActive = false;
                AnimationStateUpdated?.Invoke(this, new AnimationStateUpdatedEventArgs(
                                                    AnimationStateUpdateType.Finished));

                // TODO - Ignoe exceptions until the bug causing this to fail in some cases is resolved.
                try { m_taskSource.SetResult(true); } catch { }
            }
        }


        protected virtual void UpdateAnimationState(object sender, EventArgs e)
        {
            AnimationStateUpdated?.Invoke(this, new AnimationStateUpdatedEventArgs(AnimationStateUpdateType.Updated));
        }
    }
}
