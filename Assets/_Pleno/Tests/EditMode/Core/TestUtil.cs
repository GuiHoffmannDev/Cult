using System;
using System.Collections.Generic;
using System.Linq;
using Pleno.Core.Content;
using Pleno.Core.Events;
using Pleno.Core.Model;
using Pleno.Core.Rng;
using Pleno.Core.Rules;

namespace Pleno.Tests.Core
{
    /// <summary>IRandom roteirizado: devolve os valores enfileirados, na ordem.</summary>
    sealed class ScriptedRandom : IRandom
    {
        readonly Queue<int> _ints = new Queue<int>();
        readonly Queue<double> _doubles = new Queue<double>();

        public ScriptedRandom Ints(params int[] values)
        {
            foreach (int v in values) _ints.Enqueue(v);
            return this;
        }

        public ScriptedRandom Doubles(params double[] values)
        {
            foreach (double v in values) _doubles.Enqueue(v);
            return this;
        }

        public int Remaining => _ints.Count + _doubles.Count;

        public uint NextUInt() => throw new InvalidOperationException("ScriptedRandom não fornece NextUInt.");

        public int NextInt(int maxExclusive)
        {
            if (_ints.Count == 0)
                throw new InvalidOperationException("ScriptedRandom sem inteiros enfileirados.");
            int v = _ints.Dequeue();
            if (v < 0 || v >= maxExclusive)
                throw new InvalidOperationException($"Valor roteirizado {v} fora de [0, {maxExclusive}).");
            return v;
        }

        public double NextDouble()
        {
            if (_doubles.Count == 0)
                throw new InvalidOperationException("ScriptedRandom sem doubles enfileirados.");
            return _doubles.Dequeue();
        }
    }

    static class Spin
    {
        public static int IndexOfNumber(Wheel wheel, int number) =>
            Enumerable.Range(0, wheel.Count).First(i => wheel[i].Number == number);

        /// <summary>Pontua um giro na roda europeia (ou na roda dada) caindo em <paramref name="number"/>.</summary>
        public static SpinResult On(int number, IEnumerable<PlacedBet> bets, IEnumerable<RelicInstance> relics = null,
            Wheel wheel = null, IRandom luck = null, BetLevels levels = null, Balance balance = null)
        {
            wheel = wheel ?? WheelLayouts.European();
            return At(wheel, IndexOfNumber(wheel, number), bets, relics, luck, levels, balance);
        }

        public static SpinResult At(Wheel wheel, int index, IEnumerable<PlacedBet> bets,
            IEnumerable<RelicInstance> relics = null, IRandom luck = null, BetLevels levels = null, Balance balance = null)
        {
            return ScoringPipeline.Resolve(new SpinInput
            {
                Wheel = wheel,
                LandingIndex = index,
                Bets = bets.ToList(),
                Relics = (relics ?? Enumerable.Empty<RelicInstance>()).ToList(),
                Levels = levels ?? new BetLevels(),
                Balance = balance ?? Balance.Default,
                Luck = luck ?? new ScriptedRandom(),
            });
        }

        public static RelicInstance[] Relics(params string[] ids) => ids.Select(id => new RelicInstance(id)).ToArray();
    }
}
