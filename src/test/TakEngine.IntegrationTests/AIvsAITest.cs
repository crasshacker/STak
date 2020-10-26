using System;
using System.Threading;
using Xunit;
using Xunit.Abstractions;
using STak.TakEngine;
using STak.TakEngine.AI;
using STak.TakEngine.Extensions;

namespace STak.TakEngine.IntegrationTests
{
    public class AIvsAITest
    {
        private readonly ITestOutputHelper m_output;


        public AIvsAITest(ITestOutputHelper output)
        {
            m_output = output;
        }


        //
        // NOTE: - This test assumes no changes to Dinkum Thinkum's board evaluator.
        //
        [Fact]
        public void PlayerOneShouldWinIn82Moves()
        {
            TakAI.LoadPlugins();

            int randomSeed = 1;
            int boardSize  = 5;

            string ai1Name = "Dinkum Thinkum";
            string ai2Name = "Dinkum Thinkum";

            var ai1 = TakAI.GetAI(ai1Name);
            var ai2 = TakAI.GetAI(ai2Name);

            ai1.Options = AIConfiguration<TakAIOptions>.Get(ai1Name);
            ai2.Options = AIConfiguration<TakAIOptions>.Get(ai2Name);

            ai1.Options.TreeEvaluationDepth = 3;
            ai2.Options.TreeEvaluationDepth = 3;

            ai1.Options.MaximumThinkingTime = 0;
            ai2.Options.MaximumThinkingTime = 0;

            ai1.Options.RandomizationSeed = randomSeed;
            ai2.Options.RandomizationSeed = randomSeed;

            ai1.Options.EvaluateMovesInParallel = false;
            ai2.Options.EvaluateMovesInParallel = false;

            ai1.Options.EvaluateCellsRandomly   = false;
            ai2.Options.EvaluateCellsRandomly   = false;

            var player1 = new Player($"AI 1 - {ai1Name}", ai1);
            var player2 = new Player($"AI 2 - {ai2Name}", ai2);

            var prototype = new GamePrototype(player1, player2, boardSize);
            var game      = new BasicGame(prototype);

            for (int ply = 0; ! game.IsCompleted; ply++)
            {
                var ai = ply % 2 == 0 ? ai1 : ai2;
                var move = ai.ChooseNextMove(game, CancellationToken.None);
                game.MakeMove(move);
            }

            WinType winType    = game.Result.WinType;
            int     winner     = game.Result.Winner;
            string  winnerName = $"Player {winner+1}";
            int     moveCount  = game.ExecutedMoves.Count;

            m_output.WriteLine($"Winner:     {winnerName}\n"
                             + $"Win Type:   {winType}\n"
                             + $"Move Count: {moveCount}");

            Assert.Equal(WinType.Road, winType);
            Assert.Equal(Player.Two, winner);
            Assert.Equal(82, moveCount);
        }
    }
}
