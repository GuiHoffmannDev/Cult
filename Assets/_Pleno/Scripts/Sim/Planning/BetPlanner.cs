using System;
using System.Collections.Generic;
using Pleno.Core.Model;

namespace Pleno.Sim.Planning
{
    /// <summary>Apostas escolhidas para um giro e o que elas marcam em cada casa.</summary>
    public sealed class BetPlan
    {
        /// <summary>Na ordem em que vão para o pano, já com as fichas empilhadas.</summary>
        public List<PlacedBet> Bets { get; } = new List<PlacedBet>();

        /// <summary>Total do giro em cada índice da roda.</summary>
        public double[] Scores { get; set; }

        /// <summary>Valor do objetivo com o alcance de empurrão usado no plano.</summary>
        public double Value { get; set; }
    }

    /// <summary>
    /// Distribui as fichas uma a uma, sempre na aposta que mais sobe E[min(pontos do giro, teto)]
    /// (GDD 12: consistência bate pico). O teto é quanto falta para a meta. Com alcance de empurrão
    /// r, cada casa vale a melhor casa a até r posições dela.
    /// </summary>
    public static class BetPlanner
    {
        /// <summary>Peso do total sem teto, só para desempatar planos que já batem a meta.</summary>
        public const double Tiebreak = 1e-3;

        public static double Utility(double score, double cap) => Math.Min(score, cap) + Tiebreak * score;

        /// <summary>E[utilidade] do giro, com a bola podendo ser empurrada até <paramref name="reach"/> casas.</summary>
        public static double Expected(double[] odds, double[] scores, double cap, int reach)
        {
            int n = scores.Length;
            double expected = 0;
            for (int i = 0; i < n; i++)
            {
                if (odds[i] <= 0)
                    continue;
                double best = Utility(scores[i], cap);
                for (int d = 1; d <= reach; d++)
                {
                    best = Math.Max(best, Utility(scores[Wrap(i + d, n)], cap));
                    best = Math.Max(best, Utility(scores[Wrap(i - d, n)], cap));
                }
                expected += odds[i] * best;
            }
            return expected;
        }

        /// <param name="candidates">Apostas permitidas ao bot, com 1 ficha cada.</param>
        /// <param name="maxChipsPerBet">Teto de fichas numa mesma aposta (Cobertura usa 1).</param>
        public static BetPlan Plan(SpinEvaluator ev, IReadOnlyList<PlacedBet> candidates, int chips,
            int maxChipsPerBet, double cap, int reach)
        {
            int n = ev.Count;
            var usable = new List<PlacedBet>();
            var wins = new List<List<int>>();
            foreach (PlacedBet candidate in candidates)
            {
                List<int> landings = ev.WinningLandings(candidate);
                if (landings.Count == 0)
                    continue;
                usable.Add(candidate);
                wins.Add(landings);
            }

            int m = usable.Count;
            var chipsOn = new int[m];
            var order = new List<int>();
            var stacks = new PlacedBet[m][];
            var scores = new double[n];
            var trial = new double[n];
            double value = Expected(ev.Odds, scores, cap, reach);

            for (int chip = 0; chip < chips; chip++)
            {
                int best = -1;
                double bestValue = double.NegativeInfinity;
                double[] bestScores = null;
                for (int k = 0; k < m; k++)
                {
                    if (chipsOn[k] >= maxChipsPerBet)
                        continue;
                    List<PlacedBet> bets = Build(usable, stacks, order, chipsOn, k, chips);
                    Array.Copy(scores, trial, n);
                    // Só as casas onde esta aposta vence mudam: nas outras os vencedores são os mesmos.
                    foreach (int j in wins[k])
                        trial[j] = ev.Score(bets, j);
                    double v = Expected(ev.Odds, trial, cap, reach);
                    if (v > bestValue + 1e-9)
                    {
                        best = k;
                        bestValue = v;
                        bestScores = (double[])trial.Clone();
                    }
                }
                if (best < 0)
                    break;
                if (chipsOn[best] == 0)
                    order.Add(best);
                chipsOn[best]++;
                scores = bestScores;
                value = bestValue;
            }

            var plan = new BetPlan { Scores = scores, Value = value };
            foreach (int k in order)
                plan.Bets.Add(Stack(usable, stacks, k, chipsOn[k], chips));

            // Nenhuma aposta pode vencer (chefe anulou todas): girar exige ficha no pano mesmo assim.
            if (plan.Bets.Count == 0 && candidates.Count > 0 && chips > 0)
                plan.Bets.Add(candidates[0].WithChips(Math.Min(chips, maxChipsPerBet)));
            return plan;
        }

        static List<PlacedBet> Build(List<PlacedBet> usable, PlacedBet[][] stacks, List<int> order, int[] chipsOn, int extra, int maxChips)
        {
            var bets = new List<PlacedBet>(order.Count + 1);
            foreach (int k in order)
                bets.Add(Stack(usable, stacks, k, chipsOn[k] + (k == extra ? 1 : 0), maxChips));
            if (chipsOn[extra] == 0)
                bets.Add(Stack(usable, stacks, extra, 1, maxChips));
            return bets;
        }

        static PlacedBet Stack(List<PlacedBet> usable, PlacedBet[][] stacks, int k, int chips, int maxChips)
        {
            if (stacks[k] == null)
                stacks[k] = new PlacedBet[maxChips + 1];
            return stacks[k][chips] ?? (stacks[k][chips] = usable[k].WithChips(chips));
        }

        static int Wrap(int index, int n)
        {
            int r = index % n;
            return r < 0 ? r + n : r;
        }
    }
}
