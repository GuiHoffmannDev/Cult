using System;
using System.Collections.Generic;
using Pleno.Core.Content;
using Pleno.Core.Effects;
using Pleno.Core.Model;
using Pleno.Core.Rng;

namespace Pleno.Core.Rules
{
    /// <summary>Onde a bola parou, decidido antes da animação.</summary>
    public readonly struct SpinDraw
    {
        /// <summary>Casa sorteada pelo peso.</summary>
        public readonly int DrawnIndex;

        /// <summary>Casa onde a bola terminou (difere da sorteada se ela era Rachada).</summary>
        public readonly int LandingIndex;

        /// <summary>-1 ou +1 se a bola saltou de uma Casa Rachada; 0 se não saltou.</summary>
        public readonly int CrackBounce;

        public SpinDraw(int drawnIndex, int landingIndex, int crackBounce)
        {
            DrawnIndex = drawnIndex;
            LandingIndex = landingIndex;
            CrackBounce = crackBounce;
        }
    }

    /// <summary>Sorteio ponderado da casa (GDD 10.4). Usa o stream <c>wheel</c>.</summary>
    public static class WheelRng
    {
        /// <summary>Peso efetivo de cada casa, já com Magnética e amuletos.</summary>
        public static double[] Weights(Wheel wheel, Balance balance, IReadOnlyList<RelicInstance> relics)
        {
            var rules = new WeightRules { MagneticWeightX = balance.Pocket.MagneticWeightX };
            foreach (RelicInstance relic in relics)
                rules = RelicEffectRegistry.Get(relic.Id).ModifyWeightRules(relic, balance, rules);

            var weights = new double[wheel.Count];
            for (int i = 0; i < wheel.Count; i++)
            {
                Pocket pocket = wheel[i];
                weights[i] = pocket.Weight * (pocket.Enhancement == PocketEnhancement.Magnetic ? rules.MagneticWeightX : 1.0);
            }

            foreach (RelicInstance relic in relics)
                RelicEffectRegistry.Get(relic.Id).ModifyWeights(relic, wheel, weights);
            return weights;
        }

        /// <summary>Índice sorteado com probabilidade proporcional ao peso.</summary>
        public static int PickWeighted(IReadOnlyList<double> weights, IRandom rng) => rng.PickWeightedIndex(weights);

        /// <summary>
        /// Sorteia a casa. Se ela é Rachada, a bola salta uma vez para uma vizinha aleatória e fica lá,
        /// mesmo que a vizinha também seja Rachada (sem cascata).
        /// </summary>
        public static SpinDraw Draw(Wheel wheel, IReadOnlyList<double> weights, IRandom rng)
        {
            int drawn = PickWeighted(weights, rng);
            if (!wheel[drawn].Cracked)
                return new SpinDraw(drawn, drawn, 0);

            int bounce = rng.NextInt(2) == 0 ? -1 : 1;
            return new SpinDraw(drawn, wheel.Neighbor(drawn, bounce), bounce);
        }
    }
}
