using System;

namespace STak.TakEngine.AI
{
    public class TakAIOptions
    {
        public string Minimaxer               { get; set; } = "basic";
        public int    MaximumInstanceCount    { get; set; } = 1;
        public int    TreeEvaluationDepth     { get; set; } = 3;
        public int    MaximumThinkingTime     { get; set; } = 0;
        public int    CpuCoreUsagePercentage  { get; set; } = 0;
        public int    RandomizationSeed       { get; set; } = 0;
        public bool   EvaluateCellsRandomly   { get; set; } = true;
        public bool   EvaluateMovesInParallel { get; set; } = true;
        public bool   SearchNullWindow        { get; set; } = true;
        public bool   UseTranspositionTable   { get; set; } = true;
    }
}
