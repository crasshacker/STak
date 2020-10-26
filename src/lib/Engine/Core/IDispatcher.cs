using System;

namespace STak.TakEngine
{
    public interface IDispatcher 
    {
        bool IsDispatchNeeded { get; }

        void Invoke(Action action);
        void InvokeAsync(Action action);
    }
}
