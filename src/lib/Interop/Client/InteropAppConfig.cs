using System;
using STak.TakEngine;
using Microsoft.Extensions.Configuration;
using STak.TakEngine.AI;

namespace STak.TakHub.Client
{
    public enum SignalRProtocol
    {
        SystemTextJson,
        NewtonsoftJson,
        MessagePack
    }


    public static class InteropAppConfig
    {
        private static readonly IConfigurationRoot s_config;
        private static          InteropSettings    s_appSettings;

        public  static InteropSettings.SignalRSettings SignalR => s_appSettings.SignalR;


        static InteropAppConfig()
        {
            s_config = new ConfigurationBuilder()
                .AddJsonFile("interopappsettings.json", true, true)
                .AddEnvironmentVariables()
                .Build();

            s_appSettings = s_config.Get<InteropSettings>();
        }


        public static void Refresh()
        {
            s_appSettings = s_config.Get<InteropSettings>();
        }


        public class InteropSettings
        {
            public SignalRSettings SignalR { get; set; }

            public class SignalRSettings
            {
                public SignalRProtocol Protocol      { get; set; }
                public TimeSpan?       ServerTimeout { get; set; }
                public bool            AutoReconnect { get; set; }
            }
        }
    }
}
