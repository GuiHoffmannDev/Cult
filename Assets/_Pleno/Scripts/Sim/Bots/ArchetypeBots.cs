using System.Collections.Generic;
using System.Linq;
using Pleno.Core.Model;
using Pleno.Sim.Planning;

namespace Pleno.Sim
{
    /// <summary>Guloso: qualquer aposta do pano e da pista, fichas onde E[min(pontos, falta)] mais sobe.</summary>
    public sealed class GreedyBot : PlannerBot
    {
        public override string Id => Bot.IdOf(BotKind.Guloso);

        protected override IReadOnlyList<PlacedBet> Candidates(Wheel wheel) => BetCatalog.All(wheel);
    }

    /// <summary>Sniper: só Plenos e Vizinhos, fichas concentradas onde rendem mais.</summary>
    public sealed class SniperBot : PlannerBot
    {
        public override string Id => Bot.IdOf(BotKind.Sniper);

        protected override IReadOnlyList<PlacedBet> Candidates(Wheel wheel) =>
            BetCatalog.Straights().Concat(BetCatalog.Neighbors(wheel)).ToList();
    }

    /// <summary>
    /// Cobertura: só apostas externas, uma ficha em cada (espalha). Empurra e usa Tábuas, mas não
    /// compra Cartas da Casa: é a linha de base do jogador que só cobre a mesa.
    /// </summary>
    public sealed class CoverageBot : PlannerBot
    {
        public override string Id => Bot.IdOf(BotKind.Cobertura);

        protected override IReadOnlyList<PlacedBet> Candidates(Wheel wheel) => BetCatalog.Outside();

        protected override int MaxChipsPerBet => 1;

        protected override bool UsesHouseCards => false;
    }
}
