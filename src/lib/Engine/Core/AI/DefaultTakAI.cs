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

        private Minimaxer m_minimaxer;

        public string       Name    => "Dinkum Thinkum"; 
        public TakAIOptions Options { get => s_options ?? s_defaultOptions;
                                      set => s_options = value; }


        public DefaultTakAI()
        {
        }


        public IMove ChooseNextMove(IBasicGame game, CancellationToken token)
        {
            return m_minimaxer.ChooseMove(game, Options, token);
        }


        public void OnGameInitiated()
        {
            string name = Options.Minimaxer ?? "basic";

            // NOTE: Don't raise an error if name is unknown; default to the basic minimaxer.
            m_minimaxer = String.Equals(name, "experimental", StringComparison.OrdinalIgnoreCase)
                        ? new IterativeDeepeningMinimaxer(new MoveEnumerator(), new BoardEvaluator())
                        : new BasicMinimaxer(new MoveEnumerator(), new BoardEvaluator());
            m_minimaxer.Initialize();
        }


        public void OnGameCompleted()
        {
            m_minimaxer.LogResults();
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
                        score = TakAI.WinValue;
                    }
                    else if (game.Result.Winner == 1-playerId)
                    {
                        score = TakAI.LossValue;
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
