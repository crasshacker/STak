using System;

namespace STak.TakEngine
{
    public record GameResult(int Winner, WinType WinType)
    {
        public static readonly int TimeWinScore = 1;

        public int     Score    { get; init; } = WinType == WinType.Time ? TimeWinScore : 0;
        public int[][] Extents  { get; init; } = { new int[2], new int[2] };

        public bool IsCompleted => this.WinType != WinType.None;

        protected GameResult(GameResult result)
        {
            Score   = result.Score;
            Winner  = result.Winner;
            WinType = result.WinType;
            Extents = new int[][] { new [] { result.Extents[0][0], result.Extents[0][1] },
                                    new [] { result.Extents[1][0], result.Extents[1][1] } };
        }
    }
}
