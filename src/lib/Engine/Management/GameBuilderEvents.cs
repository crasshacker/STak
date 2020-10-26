using System;
using STak.TakEngine;

namespace STak.TakEngine.Management
{
    public class GameBuilderEventArgs : EventArgs
    {
        public IGame Game { get; set; }

        public GameBuilderEventArgs(IGame game)
        {
            Game = game;
        }
    }


    public class GameConstructedEventArgs : GameBuilderEventArgs
    {
        public GameConstructedEventArgs(IGame game)
            : base(game)
        {
        }
    }


    public class GameInitializedEventArgs : GameBuilderEventArgs
    {
        public GameInitializedEventArgs(IGame game)
            : base(game)
        {
        }
    }


    public class GameDestructedEventArgs : GameBuilderEventArgs
    {
        public GameDestructedEventArgs(IGame game)
            : base(game)
        {
        }
    }
}
