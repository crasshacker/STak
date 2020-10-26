using System;
using System.Linq;
using System.Threading;
using STak.TakEngine;

namespace STak.TakEngine.AI
{
    public class DefaultTakAI : ITakAI
    {
        private static TakAIOptions s_defaultOptions { get; set; } = new TakAIOptions();
        private        TakAIOptions s_options;

        public string       Name    => "Dinkum Thinkum"; 
        public TakAIOptions Options { get => s_options ?? s_defaultOptions;
                                      set => s_options = value; }


        public DefaultTakAI()
        {
        }


        public IMove ChooseNextMove(IBasicGame game, CancellationToken token)
        {
            Minimax minimax = new Minimax(game, new BoardEvaluator());
            return minimax.Analyze(Options.TreeEvaluationDepth,
                                   Options.EvaluateCellsRandomly,
                                   Options.RandomizationSeed,
                                   Options.EvaluateMovesInParallel,
                                   Options.CpuCoreUsagePercentage,
                                   token);
        }


        private class BoardEvaluator : IBoardEvaluator
        {
            public BoardEvaluator()
            {
            }


            public int Evaluate(IBasicGame game, int playerId)
            {
                int score = 0;

                if (game.Result.WinType != WinType.None)
                {
                    if (game.Result.Winner == playerId)
                    {
                        score = Minimax.MaxValue-1;
                    }
                    else if (game.Result.Winner == 1-playerId)
                    {
                        score = Minimax.MinValue+1;
                    }
                }
                else
                {
                    BitBoard bitBoard = game.BitBoard;

                    int whiteExtent = game.Result.Extents[0].Max();
                    int blackExtent = game.Result.Extents[1].Max();
                    score += (whiteExtent - blackExtent) * 100;

                    int whiteRoadCount = bitBoard.WhiteRoadCount;
                    int blackRoadCount = bitBoard.BlackRoadCount;
                    score += (whiteRoadCount - blackRoadCount) * 50;

                    int[] controls = { 0, 0 };

                    for (int file = 0; file < bitBoard.Size; ++file)
                    {
                        for (int rank = 0; rank < bitBoard.Size; ++rank)
                        {
                            int height = bitBoard.GetStackHeight(file, rank);

                            if (height > 0)
                            {
                                controls[bitBoard.GetStackControl(file, rank)]++;
                                ulong stack  = bitBoard.GetStack(file, rank);
                                int blackStoneCount = BitBoard.PopCount(stack);
                                int whiteStoneCount = height - blackStoneCount;
                                score += (whiteStoneCount - blackStoneCount) * 10;
                            }
                        }
                    }

                    score += (controls[0] - controls[1]) * 25;

                    if (playerId == Player.Two)
                    {
                        score *= -1;
                    }
                }

                return score;
            }
        }
    }
}
