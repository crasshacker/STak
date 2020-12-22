using System;
using System.Collections.Generic;
using STak.TakEngine;

namespace STak.TakEngine.AI
{
    public static class Randomizer
    {
        public static void Shuffle<T>(this IList<T> list, Random rng)
        {
            ArgumentValidator.EnsureNotNull(list, nameof(list));
            ArgumentValidator.EnsureNotNull(rng, nameof(rng));

            for(var i = 0; i < list.Count; i++)
            {
                list.Swap(i, rng.Next(i, list.Count));
            }
        }


        private static void Swap<T>(this IList<T> list, int i, int j)
        {
            var temp = list[i];
            list[i] = list[j];
            list[j] = temp;
        }
    }
}
