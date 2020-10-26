using System;
using System.IO;
using System.Reflection;
using Microsoft.Extensions.Configuration;
using Akka.Actor;
using NLog.Extensions.Logging;
using NLog;

namespace STak.ActorHost
{
    class Program
    {
        private const string ActorSystemName = "takgamehost";
        private const string JsonConfigFile  = "appsettings.json";
        private const string HoconConfigFile = "appsettings.hocon";

        static int Main()
        {
            string configFile = Path.Combine(GetApplicationDirectory(), HoconConfigFile);

            if (! File.Exists(configFile))
            {
                Console.WriteLine($"File not found: {configFile}");
                return 1;
            }

            InitializeLogging();
            var actorSystem = ActorSystem.Create(ActorSystemName, File.ReadAllText(configFile));

            Console.WriteLine("Actor system initiated.  Hit Enter to quit.");
            Console.ReadLine();
            return 0;
        }


        private static void InitializeLogging()
        {
            string configFile = Path.Combine(GetApplicationDirectory(), JsonConfigFile);
            var config = new ConfigurationBuilder().AddJsonFile(configFile, true, true).Build();
            LogManager.Configuration = new NLogLoggingConfiguration(config.GetSection("nlog"));
        }


        private static string GetApplicationDirectory()
        {
            return Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        }
    }
}
