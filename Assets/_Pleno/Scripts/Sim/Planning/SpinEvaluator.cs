using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Pleno.Core.Content;
using Pleno.Core.Effects;
using Pleno.Core.Events;
using Pleno.Core.Flow;
using Pleno.Core.Model;
using Pleno.Core.Rng;
using Pleno.Core.Rules;

namespace Pleno.Sim.Planning
{
    /// <summary>
    /// Prevê um giro sem tocar na run: chance de a bola parar em cada casa e quanto um conjunto de
    /// apostas marca em cada casa, pelo próprio <see cref="ScoringPipeline"/> do Core.
    /// Trabalha sobre cópias da roda, dos amuletos e dos níveis. A sorte do pipeline (Sortuda, Vidro)
    /// vem de um stream de avaliação do bot, clonado a cada giro simulado: o bot nunca lê nem
    /// consome o stream <c>luck</c> da run, e todas as opções comparadas veem a mesma sorte.
    /// </summary>
    public sealed class SpinEvaluator
    {
        static readonly ConcurrentDictionary<string, bool> MutatesAfterSpin = new ConcurrentDictionary<string, bool>();

        readonly Wheel _wheel;
        readonly IReadOnlyList<RelicInstance> _sourceRelics;
        readonly List<RelicInstance> _relics;
        readonly bool[] _restoreCounters;
        readonly BetLevels _levels;
        readonly Balance _balance;
        readonly Func<PlacedBet, bool> _filter;
        readonly Pcg32 _luck;

        /// <summary>Chance de a bola terminar em cada índice antes de empurrar (Casa Rachada já incluída).</summary>
        public double[] Odds { get; }

        /// <summary>A cópia da roda sobre a qual a avaliação acontece (mesmos índices e Ids da real).</summary>
        public Wheel Wheel => _wheel;

        public int Count => _wheel.Count;
        public Balance Balance => _balance;

        public SpinEvaluator(Wheel wheel, IReadOnlyList<RelicInstance> relics, BetLevels levels, Balance balance,
            IBossRule boss, Pcg32 luck)
        {
            _wheel = wheel.Clone();
            _sourceRelics = SimClone.Relics(relics);
            _relics = SimClone.Relics(_sourceRelics);
            _restoreCounters = new bool[_relics.Count];
            for (int i = 0; i < _relics.Count; i++)
                _restoreCounters[i] = MutatesAfterSpin.GetOrAdd(_relics[i].Id, OverridesAfterSpin);
            _levels = SimClone.Levels(levels);
            _balance = balance;
            _filter = boss == null ? null : (Func<PlacedBet, bool>)boss.BetScores;
            _luck = luck.Clone();
            Odds = LandingOdds(_wheel, WheelRng.Weights(_wheel, balance, _relics));
        }

        /// <summary>Avaliador do estado atual da run (inclui a regra do chefe da mesa, se houver).</summary>
        public static SpinEvaluator For(RunEngine run, Pcg32 luck) =>
            new SpinEvaluator(run.State.Wheel, run.State.Relics, run.State.Levels, run.Balance, run.Boss, luck);

        /// <summary>Falso se a regra do chefe anula a aposta.</summary>
        public bool Counts(PlacedBet bet) => _filter == null || _filter(bet);

        public bool Wins(PlacedBet bet, int landing) => Counts(bet) && BetResolver.Wins(bet, _wheel, landing);

        /// <summary>Índices onde a aposta vence.</summary>
        public List<int> WinningLandings(PlacedBet bet)
        {
            var landings = new List<int>();
            if (!Counts(bet))
                return landings;
            for (int j = 0; j < _wheel.Count; j++)
                if (BetResolver.Wins(bet, _wheel, j))
                    landings.Add(j);
            return landings;
        }

        /// <summary>Total do giro (floor(Pontos × Mult)) se a bola parar em <paramref name="landing"/>.</summary>
        public double Score(IReadOnlyList<PlacedBet> bets, int landing)
        {
            bool any = false;
            for (int b = 0; b < bets.Count && !any; b++)
                any = Wins(bets[b], landing);
            if (!any)
                return 0;

            Pocket pocket = _wheel[landing];
            PocketEnhancement enhancement = pocket.Enhancement;
            bool cracked = pocket.Cracked;

            SpinResult result = ScoringPipeline.Resolve(new SpinInput
            {
                Wheel = _wheel,
                LandingIndex = landing,
                Bets = bets,
                Relics = _relics,
                Levels = _levels,
                Balance = _balance,
                Luck = _luck.Clone(),
                BetFilter = _filter,
            });

            // O pós-giro mexe na cópia (Vidro quebra, amuletos escalam); desfaz para a próxima casa.
            pocket.Enhancement = enhancement;
            pocket.Cracked = cracked;
            for (int i = 0; i < _relics.Count; i++)
                if (_restoreCounters[i])
                    _relics[i].Counters = new Dictionary<string, double>(_sourceRelics[i].Counters);
            return result.Total;
        }

        /// <summary>Total em cada casa da roda.</summary>
        public double[] Scores(IReadOnlyList<PlacedBet> bets)
        {
            var scores = new double[_wheel.Count];
            for (int j = 0; j < scores.Length; j++)
                scores[j] = Score(bets, j);
            return scores;
        }

        /// <summary>O sorteio pesado, e a Casa Rachada mandando a bola meio a meio para as vizinhas.</summary>
        public static double[] LandingOdds(Wheel wheel, double[] weights)
        {
            double total = 0;
            foreach (double w in weights)
                total += w;
            var odds = new double[wheel.Count];
            for (int i = 0; i < odds.Length; i++)
            {
                double p = weights[i] / total;
                if (wheel[i].Cracked)
                {
                    odds[wheel.Neighbor(i, -1)] += p / 2;
                    odds[wheel.Neighbor(i, 1)] += p / 2;
                }
                else
                {
                    odds[i] += p;
                }
            }
            return odds;
        }

        static bool OverridesAfterSpin(string relicId) =>
            RelicEffectRegistry.Get(relicId).GetType().GetMethod(nameof(IRelicEffect.OnAfterSpin)).DeclaringType
            != typeof(RelicEffectBase);
    }
}
