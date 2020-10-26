using System;
using System.Linq;
using System.Collections.Generic;

namespace STak.WinTak
{
    public class ForwardingModelHighlighter : IModelHighlighter
    {
        private readonly List<IModelHighlighter> m_highlighters;

        public bool IsHighlighted => (bool) m_highlighters?[0].IsHighlighted;


        public ForwardingModelHighlighter(params IModelHighlighter[] highlighters)
        {
            m_highlighters = new List<IModelHighlighter>(highlighters);
        }


        public void Add(IModelHighlighter highlighter)
        {
            m_highlighters.Add(highlighter);
        }


        public void Remove(IModelHighlighter highlighter)
        {
            m_highlighters.Remove(highlighter);
        }


        public void Highlight(bool highlight)
        {
            foreach (var highlighter in m_highlighters)
            {
                highlighter.Highlight(highlight);
            }
        }
    }
}
