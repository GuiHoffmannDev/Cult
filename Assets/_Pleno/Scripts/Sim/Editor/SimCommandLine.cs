using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Pleno.Sim.Editor
{
    /// <summary>
    /// Modo headless (só com o Editor fechado):
    /// <code>
    /// Unity -batchmode -nographics -projectPath "$PWD" -executeMethod Pleno.Sim.Editor.SimCommandLine.Run \
    ///   [-simRuns 800] [-simSeed PLENSEED] [-simBots guloso,sniper] [-simThreads 8] [-simOut caminho] -logFile -
    /// </code>
    /// Sai com código 1 se alguma run quebrou (ação ilegal de bot ou exceção do Core).
    /// </summary>
    public static class SimCommandLine
    {
        public static void Run()
        {
            int exitCode = 0;
            try
            {
                string[] args = Environment.GetCommandLineArgs();
                var config = new SimConfig();
                if (TryGet(args, "-simRuns", out string runs))
                    config.Runs = int.Parse(runs);
                if (TryGet(args, "-simSeed", out string seed))
                    config.MasterSeed = seed.Trim().ToUpperInvariant();
                if (TryGet(args, "-simThreads", out string threads))
                    config.Threads = int.Parse(threads);
                if (TryGet(args, "-simBots", out string bots))
                    config.Bots = bots.Split(',').Select(ParseBot).ToList();
                string output = TryGet(args, "-simOut", out string dir) ? dir : SimJob.DefaultOutput(config);

                var results = SimBatch.Run(config);
                SimReport.WriteAll(results, config, output);
                Debug.Log("[Pleno.Sim]\n" + SimReport.Text(results, config) + "\nCSVs em " + output);
                if (results.Any(r => r.Runs.Any(run => run.Error != null)))
                    exitCode = 1;
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                exitCode = 1;
            }
            EditorApplication.Exit(exitCode);
        }

        static BotKind ParseBot(string text)
        {
            string id = text.Trim().ToLowerInvariant();
            foreach (BotKind kind in Bot.All)
                if (Bot.IdOf(kind) == id)
                    return kind;
            throw new ArgumentException($"Bot desconhecido: '{text}'. Use {string.Join(", ", Bot.All.Select(Bot.IdOf))}.");
        }

        static bool TryGet(string[] args, string name, out string value)
        {
            int i = Array.IndexOf(args, name);
            value = i >= 0 && i + 1 < args.Length ? args[i + 1] : null;
            return value != null;
        }
    }
}
