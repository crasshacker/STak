using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Reflection;
using System.Collections.Generic;
using STak.TakEngine;

namespace STak.TakEngine.AI
{
    public static class TakAI
    {
        private static Dictionary<string, ITakAI> s_takAIs = new Dictionary<string, ITakAI>();


        public static void LoadPlugins()
        {
            lock (s_takAIs)
            {
                if (s_takAIs.Count == 0)
                {
                    DefaultTakAI defaultTakAI = new DefaultTakAI();
                    s_takAIs[defaultTakAI.Name] = defaultTakAI;

                    string processPath = Path.GetDirectoryName(AppContext.BaseDirectory);
                    string pluginDir   = Path.Combine(processPath, "Plugins");

                    foreach (ITakAI takAI in PluginLoader<ITakAI>.LoadPlugins(pluginDir))
                    {
                        s_takAIs[takAI.Name] = takAI;
                    }
                }
            }
        }


        public static IEnumerable<string> GetAINames()
        {
            return s_takAIs.Keys;
        }


        public static ITakAI GetAI(string name)
        {
            if (!  s_takAIs.ContainsKey(name))
            {
                throw new Exception($"AI {name} not found.  Verify the AI is present in the Plugins directory.");
            }

            return s_takAIs[name];
        }
    }
}
