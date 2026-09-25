using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace Pleno.Sim.Editor
{
    /// <summary>
    /// Um lote rodando em segundo plano (threads do .NET) enquanto o Editor segue respondendo. Os
    /// CSVs são gravados na thread principal quando o lote termina. Só um lote por vez.
    /// </summary>
    public static class SimJob
    {
        static Task<List<BotResults>> _task;
        static CancellationTokenSource _cancel;

        public static SimConfig Config { get; private set; }
        public static SimProgress Progress { get; private set; }
        public static string OutputDirectory { get; private set; }

        /// <summary>Tabela do último lote terminado (ou a mensagem de erro).</summary>
        public static string LastSummary { get; private set; }

        public static bool Running => _task != null;

        public static string ProjectRoot => Directory.GetParent(Application.dataPath).FullName;

        /// <summary>SimOutput/&lt;seed&gt;-&lt;runs&gt;: o mesmo lote grava sempre na mesma pasta (e os mesmos números).</summary>
        public static string DefaultOutput(SimConfig config)
        {
            string name = $"{config.MasterSeed}-{config.Runs}";
            if (config.BalanceLabel != new SimConfig().BalanceLabel)
                name += "-" + config.BalanceLabel;
            return Path.Combine(ProjectRoot, "SimOutput", name);
        }

        public static void Start(SimConfig config, string outputDirectory = null)
        {
            if (Running)
                throw new InvalidOperationException("Já há um lote rodando.");
            Config = config;
            Progress = new SimProgress();
            OutputDirectory = outputDirectory ?? DefaultOutput(config);
            _cancel = new CancellationTokenSource();
            CancellationToken token = _cancel.Token;
            SimProgress progress = Progress;
            _task = Task.Run(() => SimBatch.Run(config, progress, token));
            EditorApplication.update += Poll;
        }

        public static void Cancel() => _cancel?.Cancel();

        static void Poll()
        {
            if (_task == null || !_task.IsCompleted)
                return;
            EditorApplication.update -= Poll;
            Task<List<BotResults>> task = _task;
            _task = null;

            if (task.IsCanceled || (task.Exception?.InnerException is OperationCanceledException))
            {
                LastSummary = "Lote cancelado.";
                Debug.Log("[Pleno.Sim] " + LastSummary);
                return;
            }
            if (task.IsFaulted)
            {
                LastSummary = "Erro: " + task.Exception?.InnerException;
                Debug.LogError("[Pleno.Sim] " + LastSummary);
                return;
            }

            SimReport.WriteAll(task.Result, Config, OutputDirectory);
            LastSummary = SimReport.Text(task.Result, Config) + "\nCSVs em " + OutputDirectory;
            Debug.Log("[Pleno.Sim]\n" + LastSummary);
        }

        /// <summary>Menu para o lote padrão da Fase 3 (800 runs × 4 bots). Usado também pelo Unity MCP.</summary>
        [MenuItem("Pleno/Simulador/Rodar lote padrão (800 runs × 4 bots)")]
        public static void RunDefault()
        {
            if (Running)
            {
                Debug.LogWarning("[Pleno.Sim] Já há um lote rodando.");
                return;
            }
            Start(new SimConfig());
            Debug.Log($"[Pleno.Sim] Lote iniciado; CSVs vão para {OutputDirectory}");
        }
    }
}
