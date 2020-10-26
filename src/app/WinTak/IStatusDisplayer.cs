using System;
using System.Threading;

namespace STak.WinTak
{
    public interface IStatusDisplayer
    {
        void SetCancellerSource(CancellationTokenSource cancellerSource);
        void Show(string statusText);
        void Hide();
    }
}
