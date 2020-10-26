using System;

namespace STak.WinTak
{
    public interface IModelHighlighter
    {
        bool IsHighlighted { get; }
        void Highlight(bool highlight);
    }
}
