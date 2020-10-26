using System;
using System.Collections.Generic;
using System.Globalization;
using Microsoft.Extensions.Configuration;
using STak.TakEngine.AI;

namespace STak.TakEngine.AI
{
    public static class AIConfiguration<TOptions> where TOptions : class, new()
    {
        public  static TOptions Get(string key) => s_aiOptions.ContainsKey(key) ? s_aiOptions[key] : null;
        private static Dictionary<string, TOptions> s_aiOptions = new Dictionary<string, TOptions>();


        public static void Initialize(IConfiguration config)
        {
            TakAI.LoadPlugins();
            s_aiOptions = new Dictionary<string, TOptions>();
            foreach (string aiName in TakAI.GetAINames())
            {
                string key = $"aiBehavior:{aiName}";
                key = Char.ToLower(key[0], CultureInfo.CurrentCulture) + key[1..];
                var aiSettings = config.GetSection(key);
                if (aiSettings.Exists())
                {
                    var aiOptions = new TOptions();
                    aiSettings.Bind(aiOptions);
                    s_aiOptions[aiName] = aiOptions;
                }
            }
        }
    }
}
