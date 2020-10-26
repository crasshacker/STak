using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Security.Permissions;

namespace STak.TakEngine
{
    [Serializable]
    public class GamePrototype
    {
        public Guid        Id        { get; init; }
        public int         BoardSize { get; init; }
        public Player      PlayerOne { get; init; }
        public Player      PlayerTwo { get; init; }

        public GameTimer   GameTimer { get; set; } = GameTimer.Unlimited;
        public List<IMove> Moves     { get; set; }


        public GamePrototype()
        {
            Id        = Guid.NewGuid();
            BoardSize = Board.DefaultSize;
            Moves     = new List<IMove>();
        }


        public GamePrototype(Player player1, Player player2, int boardSize = Board.DefaultSize,
                                                               IEnumerable<IMove> moves = null)
        {
            Id        = Guid.NewGuid();
            PlayerOne = player1;
            PlayerTwo = player2;
            BoardSize = boardSize;
            Moves     = (moves != null) ? new List<IMove>(moves) : new List<IMove>();
        }


        public GamePrototype(GamePrototype prototype)
        {
            // Note that we set the IsAI property explicitly, because if a player represents a remote AI player,
            // the AI property will be null and thus can't be used to determine whether the player is an AI.
            // If we later decide to modify the code to set the AI property in cases where the player is a remote
            // AI and an AI with the same name is installed locally, this explicit setting of IsAI would still be
            // needed in cases where a plugin for the AI is not present locally,

            PlayerOne = (prototype.PlayerOne.IsHuman)
                      ? new Player(prototype.PlayerOne.Name, prototype.PlayerOne.IsRemote)
                                                       { IsAI = prototype.PlayerOne.IsAI }
                      : new Player(prototype.PlayerOne.Name, prototype.PlayerOne.AI, prototype.PlayerOne.IsRemote)
                                                                               { IsAI = prototype.PlayerOne.IsAI };

            PlayerTwo = (prototype.PlayerTwo.IsHuman)
                      ? new Player(prototype.PlayerTwo.Name, prototype.PlayerTwo.IsRemote)
                                                       { IsAI = prototype.PlayerTwo.IsAI }
                      : new Player(prototype.PlayerTwo.Name, prototype.PlayerTwo.AI, prototype.PlayerTwo.IsRemote)
                                                                               { IsAI = prototype.PlayerTwo.IsAI };

            Id        = prototype.Id;
            BoardSize = prototype.BoardSize;
            Moves     = new List<IMove>(prototype.Moves.Select(m => m.Clone()));
            GameTimer = prototype.GameTimer?.Clone() ?? GameTimer.Unlimited;
        }


        public GamePrototype(GameRecord gameRecord)
        {
            Id        = Guid.NewGuid();
            BoardSize = Int32.Parse(gameRecord.Headers["Size"]);
            PlayerOne = new Player(gameRecord.Headers["Player1"]);
            PlayerTwo = new Player(gameRecord.Headers["Player2"]);
            Moves     = new List<IMove>(gameRecord.Moves);
        }


        public GamePrototype(FileInfo fileInfo)
            : this(PtnParser.ParseFile(fileInfo.FullName))
        {
        }


        public GamePrototype(string[] textLines)
            : this(PtnParser.ParseText(textLines))
        {
        }


        public GamePrototype Clone()
        {
            return new GamePrototype(this);
        }
    }
}
