using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using System.Diagnostics;
using NLog;
using STak.TakEngine;

namespace STak.TakEngine.AI
{
    public struct EvaluationResult
    {
        public IMove Move;
        public int   Value;


        public EvaluationResult(IMove move, int value)
        {
            Move  = move;
            Value = value;
        }
    }
}
