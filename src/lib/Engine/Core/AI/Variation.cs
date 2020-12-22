using System;
using STak.TakEngine;

namespace STak.TakEngine.AI
{
    internal class Variation
    {
        public int                Count   { get; set; }
        public EvaluationResult[] Results { get; }


        public Variation(int maxResults)
        {
            Results = new EvaluationResult[maxResults];
            Count   = 0;
        }


        public Variation Clone()
        {
            var variation = new Variation(Results.Length);
            Results.CopyTo(variation.Results, 0);
            variation.Count = Count;
            return variation;
        }


        public void Clear()
        {
            Count = 0;
        }


        public void Add(EvaluationResult result)
        {
            result = new EvaluationResult(result.Move.Clone(), result.Value);

            if (result.Move is StoneMove stoneMove && stoneMove.Stone.Id != -1)
            {
                // Replace the move with one that uses a generic stone (with Id=-1).
                var stone = new Stone(stoneMove.Stone.PlayerId, stoneMove.Stone.Type);
                result.Move = new StoneMove(stoneMove.TargetCell, stone);
            }

            Results[Count++] = result;
        }


        public void Add(Variation variation)
        {
            for (int i = 0; i < variation.Count; ++i)
            {
                Add(variation.Results[i]);
            }
        }


        public void Set(Variation variation)
        {
            Count = 0;
            Add(variation.Clone());
        }


        public void Set(EvaluationResult result, Variation variation = null)
        {
            Count = 0;
            Add(result);
            if (variation != null)
            {
                Add(variation.Clone());
            }
        }


        public override string ToString()
        {
            string str = String.Empty;

            for (int i = 0; i < Count; ++i)
            {
                string value = Results[i].Value switch
                {
                    TakAI.LossValue => "Lose",
                    TakAI.WinValue  => "Win",
                    _               => Results[i].Value.ToString()
                };
                str += $"{Results[i].Move} ({value})";
                if (i < Count-1) { str += ", "; }
            }

            return str;
        }
    }
}
