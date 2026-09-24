using System.Collections.Generic;
using Pleno.Core.Content;
using Pleno.Core.Model;

namespace Pleno.Core.Rules
{
    /// <summary>O que os efeitos podem ler sobre o giro em andamento.</summary>
    public sealed class SpinContext
    {
        public Wheel Wheel { get; }
        public int LandingIndex { get; }
        public Pocket Pocket => Wheel[LandingIndex];
        public IReadOnlyList<PlacedBet> Bets { get; }
        public IReadOnlyList<PlacedBet> WinningBets { get; }
        public bool HasWin => WinningBets.Count > 0;
        public BetLevels Levels { get; }
        public Balance Balance { get; }

        public SpinContext(Wheel wheel, int landingIndex, IReadOnlyList<PlacedBet> bets,
            IReadOnlyList<PlacedBet> winningBets, BetLevels levels, Balance balance)
        {
            Wheel = wheel;
            LandingIndex = landingIndex;
            Bets = bets;
            WinningBets = winningBets;
            Levels = levels;
            Balance = balance;
        }

        /// <summary>Valor do número nos Pontos: o próprio número, e 36 para 0 e 00.</summary>
        public static double NumberValue(Pocket pocket, Balance balance) =>
            pocket.IsZero ? balance.Pocket.ZeroValue : pocket.Number;
    }

    /// <summary>Regras gerais de peso do sorteio, que amuletos podem alterar.</summary>
    public struct WeightRules
    {
        public double MagneticWeightX;
    }
}
