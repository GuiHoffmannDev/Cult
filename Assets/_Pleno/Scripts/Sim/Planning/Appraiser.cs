using System;
using System.Collections.Generic;
using System.Linq;
using Pleno.Core.Content;
using Pleno.Core.Effects.Relics;
using Pleno.Core.Flow;
using Pleno.Core.Model;
using Pleno.Core.Rng;
using Pleno.Core.Rules;

namespace Pleno.Sim.Planning
{
    /// <summary>Melhor forma de usar uma Carta da Casa agora.</summary>
    public sealed class CardChoice
    {
        public static readonly CardChoice Useless = new CardChoice(false, 0, null);

        /// <summary>Vale usar (ganho positivo, ou carta de dinheiro/sorte que o bot sempre usa).</summary>
        public bool Useful { get; }

        /// <summary>Ganho no objetivo do giro de referência. 0 para cartas que não mudam a pontuação.</summary>
        public double Gain { get; }

        public CardTargets Targets { get; }

        public CardChoice(bool useful, double gain, CardTargets targets)
        {
            Useful = useful;
            Gain = gain;
            Targets = targets ?? CardTargets.None;
        }
    }

    /// <summary>
    /// Mede quanto uma mudança na run (amuleto, Tábua, Carta da Casa, ordem dos amuletos) muda o
    /// objetivo de um giro de referência: as apostas que o bot faria com o teto de uma meta à frente
    /// (o que se compra agora fica para a run toda). Tudo é aplicado em cópias (<see cref="SimClone"/>);
    /// a run real não muda.
    /// </summary>
    public sealed class Appraiser
    {
        readonly RunEngine _run;
        readonly IReadOnlyList<PlacedBet> _bets;
        readonly double _cap;
        readonly int _reach;
        readonly Pcg32 _luck;
        readonly double[] _importance;
        readonly double[] _odds;

        /// <summary>Os amuletos da run como o bot os avalia (ver <see cref="Effective"/>), na mesma ordem.</summary>
        readonly List<RelicInstance> _relics;

        /// <summary>Objetivo do giro de referência com a run como está.</summary>
        public double Base { get; }

        /// <summary>Quanto um empurrão disponível sobe o objetivo de um giro.</summary>
        public double NudgeValue { get; }

        public IReadOnlyList<PlacedBet> Bets => _bets;

        public Appraiser(RunEngine run, IReadOnlyList<PlacedBet> referenceBets, double cap, int reach, Pcg32 luck)
        {
            _run = run;
            _bets = referenceBets;
            _cap = cap;
            _reach = reach;
            _luck = luck.Clone();

            RunState state = run.State;
            _relics = state.Relics.Select(Effective).ToList();
            var ev = new SpinEvaluator(state.Wheel, _relics, state.Levels, run.Balance, null, luck);
            double[] scores = ev.Scores(_bets);
            Base = BetPlanner.Expected(ev.Odds, scores, cap, reach);
            NudgeValue = BetPlanner.Expected(ev.Odds, scores, cap, 1) - BetPlanner.Expected(ev.Odds, scores, cap, 0);
            _odds = ev.Odds;
            _importance = new double[scores.Length];
            for (int i = 0; i < scores.Length; i++)
                _importance[i] = ev.Odds[i] * BetPlanner.Utility(scores[i], cap);
        }

        Balance Balance => _run.Balance;
        RunState State => _run.State;

        public double Value(Wheel wheel, IReadOnlyList<RelicInstance> relics, BetLevels levels)
        {
            var ev = new SpinEvaluator(wheel, relics, levels, Balance, null, _luck);
            return BetPlanner.Expected(ev.Odds, ev.Scores(_bets), _cap, _reach);
        }

        // ── Amuletos ───────────────────────────────────────────────────

        /// <summary>Ganho de pôr o amuleto no último espaço.</summary>
        public double RelicGain(string id)
        {
            var relics = _relics.ToList();
            relics.Add(Effective(new RelicInstance(id)));
            return Value(State.Wheel, relics, State.Levels) - Base + ResourceGain(_relics, relics);
        }

        /// <summary>Quanto o objetivo cai sem o amuleto do espaço.</summary>
        public double RelicWorth(int slot)
        {
            var relics = _relics.Where((r, i) => i != slot).ToList();
            return Base - Value(State.Wheel, relics, State.Levels) + ResourceGain(relics, _relics);
        }

        /// <summary>
        /// Amuletos que escalam começam zerados (e o Martingale zera a cada vitória), então no giro de
        /// referência não valeriam nada. O bot os avalia com pelo menos um valor típico de meio de mesa
        /// (<see cref="BotTuning"/>), igual na compra e na venda.
        /// </summary>
        RelicInstance Effective(RelicInstance owned)
        {
            RelicInstance relic = SimClone.Relic(owned);
            if (relic.Id == RelicIds.Martingale)
                relic.SetCounter(Martingale.StackKey, Math.Max(relic.GetCounter(Martingale.StackKey),
                    Balance.Relics.MartingaleMultPerLoss * BotTuning.MartingaleLossesAssumed));
            if (relic.Id == RelicIds.Colecionador)
                relic.SetCounter(Colecionador.DistinctKey, Math.Max(relic.GetCounter(Colecionador.DistinctKey),
                    BotTuning.ColecionadorNumbersAssumed));
            return relic;
        }

        /// <summary>Empurrões extras por mesa (Luva de Seda), espalhados pelos giros da mesa.</summary>
        double ResourceGain(IReadOnlyList<RelicInstance> before, IReadOnlyList<RelicInstance> after)
        {
            MesaResources a = MesaRules.Resources(Balance, State.StartingWheel, before, null);
            MesaResources b = MesaRules.Resources(Balance, State.StartingWheel, after, null);
            return (b.Nudges - a.Nudges) * NudgeValue / Math.Max(1, a.Spins);
        }

        /// <summary>
        /// Amuletos da run (as instâncias reais) na ordem que maximiza o objetivo; a ordem é a de
        /// resolução. Empate mantém a atual.
        /// </summary>
        public List<RelicInstance> BestOrder()
        {
            List<int> current = Enumerable.Range(0, _relics.Count).ToList();
            List<int> best = current;
            if (current.Count >= 2)
            {
                double bestValue = Base;
                foreach (List<int> order in Permutations(current))
                {
                    double v = Value(State.Wheel, order.Select(i => _relics[i]).ToList(), State.Levels);
                    if (v > bestValue + 1e-9)
                    {
                        bestValue = v;
                        best = order;
                    }
                }
            }
            return best.Select(i => State.Relics[i]).ToList();
        }

        static IEnumerable<List<int>> Permutations(List<int> items)
        {
            if (items.Count <= 1)
            {
                yield return items.ToList();
                yield break;
            }
            for (int i = 0; i < items.Count; i++)
            {
                var rest = items.Where((_, k) => k != i).ToList();
                foreach (List<int> tail in Permutations(rest))
                {
                    tail.Insert(0, items[i]);
                    yield return tail;
                }
            }
        }

        // ── Tábuas ─────────────────────────────────────────────────────

        public double TabuaGain(string id)
        {
            BetLevels levels = SimClone.Levels(State.Levels);
            levels.Raise(TabuaIds.BetTypes[id], Balance.Cards.TabuaLevels);
            return Value(State.Wheel, _relics, levels) - Base;
        }

        // ── Cartas da Casa ─────────────────────────────────────────────

        public CardChoice Card(string id)
        {
            switch (id)
            {
                case HouseCardIds.Tesoureiro:
                    return new CardChoice(State.Money > 0, 0, null);
                case HouseCardIds.Oraculo:
                    return new CardChoice(true, 0, null);
                case HouseCardIds.Fortuna:
                    return new CardChoice(State.Relics.Any(r => r.Edition == PocketEdition.None), 0, null);
                case HouseCardIds.Ourives:
                    return Ourives();
                case HouseCardIds.Gemeo:
                    return Gemeo();
                case HouseCardIds.Pintor:
                    return Greedy(id, Enumerable.Range(0, State.Wheel.Count).ToList());
                case HouseCardIds.Coveiro:
                    return Greedy(id, LeastImportant(BotTuning.CardTargetPool));
                default:
                    return Greedy(id, MostImportant(BotTuning.CardTargetPool));
            }
        }

        /// <summary>Ganho de aplicar a carta com esses alvos numa cópia da run; null se a carta recusa os alvos.</summary>
        double? CardGain(string id, CardTargets targets)
        {
            RunState sandbox = SimClone.ForCards(State);
            if (HouseCards.Check(id, sandbox, targets, Balance) != null)
                return null;
            HouseCards.Apply(id, sandbox, targets, Balance, _luck.Clone());
            // Nenhuma carta avaliada aqui mexe nos amuletos (a Fortuna não passa por este caminho).
            return Value(sandbox.Wheel, _relics, sandbox.Levels) - Base;
        }

        /// <summary>Acrescenta alvos um a um enquanto o ganho sobe.</summary>
        CardChoice Greedy(string id, List<int> pool)
        {
            var (min, max) = HouseCards.TargetRange(id, Balance);
            var chosen = new List<int>();
            CardTargets best = null;
            double bestGain = 0;
            for (int t = 0; t < max; t++)
            {
                int pick = -1;
                double pickGain = double.NegativeInfinity;
                foreach (int candidate in pool)
                {
                    if (chosen.Contains(candidate))
                        continue;
                    double? gain = CardGain(id, CardTargets.Of(chosen.Append(candidate).ToArray()));
                    if (gain.HasValue && gain.Value > pickGain + 1e-9)
                    {
                        pick = candidate;
                        pickGain = gain.Value;
                    }
                }
                if (pick < 0)
                    break;
                bool improves = pickGain > bestGain + 1e-9;
                if (chosen.Count + 1 >= min && !improves && best != null)
                    break;
                chosen.Add(pick);
                if (chosen.Count >= min && improves)
                {
                    best = CardTargets.Of(chosen.ToArray());
                    bestGain = pickGain;
                }
            }
            return best == null ? CardChoice.Useless : new CardChoice(true, bestGain, best);
        }

        /// <summary>Gêmeo: copia uma casa que marca muito sobre uma que não marca.</summary>
        CardChoice Gemeo()
        {
            CardTargets best = null;
            double bestGain = 0;
            foreach (int source in MostImportant(BotTuning.GemeoSources))
            {
                foreach (int target in LeastImportant(BotTuning.CardTargetPool))
                {
                    var targets = CardTargets.Copy(source, target);
                    double? gain = CardGain(HouseCardIds.Gemeo, targets);
                    if (gain.HasValue && gain.Value > bestGain + 1e-9)
                    {
                        best = targets;
                        bestGain = gain.Value;
                    }
                }
            }
            return best == null ? CardChoice.Useless : new CardChoice(true, bestGain, best);
        }

        /// <summary>Ourives só dá dinheiro: vai na casa sem aprimoramento mais provável.</summary>
        CardChoice Ourives()
        {
            var ev = new SpinEvaluator(State.Wheel, State.Relics, State.Levels, Balance, null, _luck);
            int best = -1;
            for (int i = 0; i < State.Wheel.Count; i++)
            {
                if (State.Wheel[i].Enhancement != PocketEnhancement.None || State.Wheel[i].Cracked)
                    continue;
                if (HouseCards.Check(HouseCardIds.Ourives, State, CardTargets.Of(i), Balance) != null)
                    continue;
                if (best < 0 || ev.Odds[i] > ev.Odds[best] + 1e-12)
                    best = i;
            }
            return best < 0 ? CardChoice.Useless : new CardChoice(true, 0, CardTargets.Of(best));
        }

        List<int> MostImportant(int count) => Enumerable.Range(0, _importance.Length)
            .OrderByDescending(i => _importance[i]).ThenBy(i => i).Take(count).ToList();

        /// <summary>As que menos marcam; entre as que não marcam, as mais prováveis (tirá-las rende mais).</summary>
        List<int> LeastImportant(int count) => Enumerable.Range(0, _importance.Length)
            .OrderBy(i => _importance[i]).ThenByDescending(i => _odds[i]).ThenBy(i => i).Take(count).ToList();
    }
}
