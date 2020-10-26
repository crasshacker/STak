using System;
using System.Collections.Generic;

namespace STak.TakEngine
{
    public record GameRecord(Dictionary<string, string> Headers, IMove[] Moves, GameResult Result);
}
