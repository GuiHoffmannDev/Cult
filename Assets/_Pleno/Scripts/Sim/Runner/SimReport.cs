using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using Pleno.Core.Content;

namespace Pleno.Sim
{
    /// <summary>
    /// Métricas do GDD 12 em CSV (vírgula como separador, ponto decimal, taxas de 0 a 1):
    /// summary.csv (vitória por Ante), mesas.csv (média e p90 por mesa), relics.csv (compra por
    /// amuleto e presença nas runs vencedoras), bosses.csv e runs.csv (uma linha por run).
    /// </summary>
    public static class SimReport
    {
        /// <summary>Meta do GDD 12: nenhum amuleto em mais de 60% das runs vencedoras.</summary>
        public const double RelicWinShareLimit = 0.60;

        static readonly string[] MesaNames = { "pequena", "grande", "chefe" };

        public static void WriteAll(IReadOnlyList<BotResults> results, SimConfig config, string directory)
        {
            Directory.CreateDirectory(directory);
            File.WriteAllText(Path.Combine(directory, "summary.csv"), Summary(results, config));
            File.WriteAllText(Path.Combine(directory, "mesas.csv"), Mesas(results));
            File.WriteAllText(Path.Combine(directory, "relics.csv"), Relics(results));
            File.WriteAllText(Path.Combine(directory, "bosses.csv"), Bosses(results));
            File.WriteAllText(Path.Combine(directory, "runs.csv"), Runs(results));
            File.WriteAllText(Path.Combine(directory, "summary.txt"), Text(results, config));
        }

        // ── CSVs ───────────────────────────────────────────────────────

        public static string Summary(IReadOnlyList<BotResults> results, SimConfig config)
        {
            var csv = new Csv("bot", "runs", "errors", "master_seed", "balance", "win_rate", "ante1_rate", "ante2_rate", "ante3_rate",
                "mesas_won_avg", "shop_money_avg", "nudges_avg", "house_cards_avg", "tabuas_avg", "rerolls_avg",
                "relics_end_avg", "best_spin_avg", "seconds");
            foreach (BotResults bot in results)
            {
                RunRecord[] runs = bot.Runs;
                csv.Row(bot.BotId, runs.Length, runs.Count(r => r.Error != null), config.MasterSeed, config.BalanceLabel,
                    Rate(runs, r => r.Won), Rate(runs, r => r.AntesCleared >= 1), Rate(runs, r => r.AntesCleared >= 2),
                    Rate(runs, r => r.AntesCleared >= 3),
                    Avg(runs.Select(r => (double)r.Mesas.Count(m => m.Won))),
                    Avg(runs.SelectMany(r => r.ShopMoney).Select(m => (double)m)),
                    Avg(runs.Select(r => (double)r.NudgesUsed)), Avg(runs.Select(r => (double)r.HouseCardsUsed)),
                    Avg(runs.Select(r => (double)r.TabuasUsed)), Avg(runs.Select(r => (double)r.Rerolls)),
                    Avg(runs.Select(r => (double)r.FinalRelics.Count)), Avg(runs.Select(r => r.BestSpin)),
                    bot.Elapsed.TotalSeconds);
            }
            return csv.ToString();
        }

        public static string Mesas(IReadOnlyList<BotResults> results)
        {
            var csv = new Csv("bot", "ante", "mesa", "target", "reached", "won", "win_rate",
                "score_avg", "score_p90", "spin_avg", "spin_p90", "spins_used_avg");
            foreach (BotResults bot in results)
            {
                foreach (var group in bot.Runs.SelectMany(r => r.Mesas).GroupBy(m => (m.Ante, m.Index)).OrderBy(g => g.Key))
                {
                    List<MesaRecord> mesas = group.ToList();
                    List<double> spins = mesas.SelectMany(m => m.Spins).ToList();
                    csv.Row(bot.BotId, group.Key.Ante, MesaNames[group.Key.Index], mesas[0].Target, mesas.Count,
                        mesas.Count(m => m.Won), (double)mesas.Count(m => m.Won) / mesas.Count,
                        Avg(mesas.Select(m => m.Score)), P90(mesas.Select(m => m.Score)),
                        Avg(spins), P90(spins), Avg(mesas.Select(m => (double)m.Spins.Count)));
                }
            }
            return csv.ToString();
        }

        public static string Relics(IReadOnlyList<BotResults> results)
        {
            var csv = new Csv("bot", "relic", "rarity", "offered_shops", "bought", "buy_rate", "pack_picks",
                "runs_held", "win_rate_when_held", "winning_runs", "share_of_winning_runs", "over_60");
            foreach (BotResults bot in results)
            {
                RunRecord[] runs = bot.Runs;
                List<RunRecord> winners = runs.Where(r => r.Won).ToList();
                foreach (string id in RelicCatalog.Rarities.Keys.OrderBy(id => id, StringComparer.Ordinal))
                {
                    int offered = runs.Sum(r => Get(r.RelicsOffered, id));
                    int bought = runs.Sum(r => Get(r.RelicsBought, id));
                    List<RunRecord> held = runs.Where(r => r.RelicsEverHeld.Contains(id)).ToList();
                    int winsWith = winners.Count(r => r.FinalRelics.Contains(id));
                    double share = winners.Count == 0 ? 0 : (double)winsWith / winners.Count;
                    csv.Row(bot.BotId, id, RelicCatalog.Rarities[id].ToString().ToLowerInvariant(), offered, bought,
                        offered == 0 ? 0 : (double)bought / offered, runs.Sum(r => Get(r.RelicsPicked, id)),
                        held.Count, held.Count == 0 ? 0 : (double)held.Count(r => r.Won) / held.Count,
                        winners.Count, share, share > RelicWinShareLimit ? "sim" : "nao");
                }
            }
            return csv.ToString();
        }

        public static string Bosses(IReadOnlyList<BotResults> results)
        {
            var csv = new Csv("bot", "boss", "reached", "won", "win_rate");
            foreach (BotResults bot in results)
            {
                foreach (var group in bot.Runs.SelectMany(r => r.Mesas).Where(m => m.BossId != null)
                    .GroupBy(m => m.BossId).OrderBy(g => g.Key, StringComparer.Ordinal))
                {
                    int reached = group.Count(), won = group.Count(m => m.Won);
                    csv.Row(bot.BotId, group.Key, reached, won, (double)won / reached);
                }
            }
            return csv.ToString();
        }

        public static string Runs(IReadOnlyList<BotResults> results)
        {
            var csv = new Csv("bot", "seed", "won", "antes_cleared", "mesas_played", "last_ante", "last_mesa",
                "final_money", "best_spin", "nudges", "house_cards", "tabuas", "rerolls", "relics", "error");
            foreach (BotResults bot in results)
            {
                foreach (RunRecord r in bot.Runs)
                {
                    MesaRecord last = r.Mesas.LastOrDefault();
                    csv.Row(r.Bot, r.Seed, r.Won ? 1 : 0, r.AntesCleared, r.Mesas.Count, last?.Ante ?? 0,
                        last == null ? "" : MesaNames[last.Index], r.FinalMoney, r.BestSpin, r.NudgesUsed,
                        r.HouseCardsUsed, r.TabuasUsed, r.Rerolls, string.Join("|", r.FinalRelics), r.Error ?? "");
                }
            }
            return csv.ToString();
        }

        // ── Texto ──────────────────────────────────────────────────────

        /// <summary>Tabela curta para o console e a janela do Editor.</summary>
        public static string Text(IReadOnlyList<BotResults> results, SimConfig config)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"PLENO sim: {config.Runs} runs por bot, seed mestra {config.MasterSeed}, roda Europeia, balanceamento {config.BalanceLabel}");
            sb.AppendLine(string.Format(CultureInfo.InvariantCulture, "{0,-10} {1,8} {2,7} {3,7} {4,7} {5,7} {6,7}",
                "bot", "vitória", "ante1", "ante2", "ante3", "erros", "seg"));
            foreach (BotResults bot in results)
            {
                RunRecord[] runs = bot.Runs;
                sb.AppendLine(string.Format(CultureInfo.InvariantCulture, "{0,-10} {1,8} {2,7} {3,7} {4,7} {5,7} {6,7:F1}",
                    bot.BotId, Percent(Rate(runs, r => r.Won)), Percent(Rate(runs, r => r.AntesCleared >= 1)),
                    Percent(Rate(runs, r => r.AntesCleared >= 2)), Percent(Rate(runs, r => r.AntesCleared >= 3)),
                    runs.Count(r => r.Error != null), bot.Elapsed.TotalSeconds));
            }
            return sb.ToString();
        }

        /// <summary>Uma casa decimal, arredondada igual em qualquer runtime (o "P1" do Mono e o do .NET divergem no meio).</summary>
        static string Percent(double rate) =>
            (Math.Round(rate * 1000, MidpointRounding.AwayFromZero) / 10).ToString("0.0", CultureInfo.InvariantCulture) + "%";

        // ── Estatística ────────────────────────────────────────────────

        static double Rate(RunRecord[] runs, Func<RunRecord, bool> predicate) =>
            runs.Length == 0 ? 0 : (double)runs.Count(predicate) / runs.Length;

        public static double Avg(IEnumerable<double> values)
        {
            double sum = 0;
            int n = 0;
            foreach (double v in values)
            {
                sum += v;
                n++;
            }
            return n == 0 ? 0 : sum / n;
        }

        /// <summary>Percentil 90 pelo método do posto mais próximo.</summary>
        public static double P90(IEnumerable<double> values)
        {
            double[] sorted = values.OrderBy(v => v).ToArray();
            if (sorted.Length == 0)
                return 0;
            int rank = (int)Math.Ceiling(0.9 * sorted.Length);
            return sorted[Math.Max(0, rank - 1)];
        }

        static int Get(Dictionary<string, int> counts, string id) => counts.TryGetValue(id, out int n) ? n : 0;

        sealed class Csv
        {
            readonly StringBuilder _sb = new StringBuilder();

            public Csv(params string[] header)
            {
                _sb.Append(string.Join(",", header)).Append('\n');
            }

            public void Row(params object[] cells)
            {
                _sb.Append(string.Join(",", cells.Select(Format))).Append('\n');
            }

            static string Format(object cell)
            {
                string text;
                switch (cell)
                {
                    case double d:
                        text = Math.Round(d, 4).ToString("0.####", CultureInfo.InvariantCulture);
                        break;
                    case IFormattable f:
                        text = f.ToString(null, CultureInfo.InvariantCulture);
                        break;
                    default:
                        text = cell?.ToString() ?? "";
                        break;
                }
                return text.IndexOfAny(new[] { ',', '"', '\n' }) >= 0 ? "\"" + text.Replace("\"", "\"\"") + "\"" : text;
            }

            public override string ToString() => _sb.ToString();
        }
    }
}
