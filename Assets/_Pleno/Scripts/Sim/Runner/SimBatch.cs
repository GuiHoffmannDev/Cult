using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Pleno.Core.Content;
using Pleno.Core.Rng;

namespace Pleno.Sim
{
    public sealed class SimConfig
    {
        /// <summary>Seed mestra padrão: as seeds das runs saem dela, então o lote inteiro se repete.</summary>
        public const string DefaultMasterSeed = "PLENSEED";

        public int Runs { get; set; } = 800;
        public string MasterSeed { get; set; } = DefaultMasterSeed;
        public List<BotKind> Bots { get; set; } = Bot.All.ToList();

        /// <summary>
        /// Balanceamento de cada run. O padrão é o do jogo; experimentos passam uma variação em
        /// memória (o arquivo de balanceamento não muda).
        /// </summary>
        public Func<Balance> MakeBalance { get; set; } = () => Balance.Default;

        /// <summary>Rótulo do balanceamento usado, para o relatório ("padrão" quando não é experimento).</summary>
        public string BalanceLabel { get; set; } = BalancePresets.DefaultId;

        /// <summary>Runs em paralelo. Cada run é independente e determinística, então o resultado não muda.</summary>
        public int Threads { get; set; } = Environment.ProcessorCount;
    }

    /// <summary>Andamento de um lote, lido pela janela do Editor enquanto as runs rodam em outras threads.</summary>
    public sealed class SimProgress
    {
        int _done;

        public int Done => Volatile.Read(ref _done);
        public int Total { get; internal set; }
        public string CurrentBot { get; internal set; }

        internal void Step() => Interlocked.Increment(ref _done);
    }

    public sealed class BotResults
    {
        public BotKind Kind { get; set; }
        public string BotId => Bot.IdOf(Kind);
        public RunRecord[] Runs { get; set; }
        public TimeSpan Elapsed { get; set; }
    }

    /// <summary>Roda N runs por bot. Todos os bots jogam as mesmas seeds (comparação pareada).</summary>
    public static class SimBatch
    {
        /// <summary>Seeds das runs, derivadas da seed mestra por um stream próprio do simulador.</summary>
        public static List<string> Seeds(string masterSeed, int count)
        {
            Pcg32 rng = RngStreams.Derive(masterSeed, "sim-seeds");
            var seeds = new List<string>(count);
            for (int i = 0; i < count; i++)
                seeds.Add(RunSeed.FromEntropy(rng.NextUInt() | ((ulong)rng.NextUInt() << 32)));
            return seeds;
        }

        public static List<BotResults> Run(SimConfig config, SimProgress progress = null,
            CancellationToken cancel = default)
        {
            List<string> seeds = Seeds(config.MasterSeed, config.Runs);
            progress = progress ?? new SimProgress();
            progress.Total = seeds.Count * config.Bots.Count;

            var results = new List<BotResults>();
            foreach (BotKind kind in config.Bots)
            {
                progress.CurrentBot = Bot.IdOf(kind);
                var records = new RunRecord[seeds.Count];
                var clock = Stopwatch.StartNew();
                Parallel.For(0, seeds.Count,
                    new ParallelOptions { MaxDegreeOfParallelism = Math.Max(1, config.Threads), CancellationToken = cancel },
                    i =>
                    {
                        records[i] = PlaySafe(kind, seeds[i], config.MakeBalance());
                        progress.Step();
                    });
                results.Add(new BotResults { Kind = kind, Runs = records, Elapsed = clock.Elapsed });
            }
            return results;
        }

        /// <summary>Uma run que quebra vira um registro com o erro, sem derrubar o lote.</summary>
        static RunRecord PlaySafe(BotKind kind, string seed, Balance balance)
        {
            try
            {
                return SimRunner.Play(kind, seed, balance);
            }
            catch (Exception e)
            {
                return new RunRecord(Bot.IdOf(kind), seed) { Error = e.GetType().Name + ": " + e.Message };
            }
        }
    }
}
