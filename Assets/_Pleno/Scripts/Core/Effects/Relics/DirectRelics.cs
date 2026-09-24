using System.Linq;
using Pleno.Core.Content;
using Pleno.Core.Model;
using Pleno.Core.Rules;

namespace Pleno.Core.Effects.Relics
{
    // Amuletos de Pontos e Mult diretos (GDD 7.1, primeira tabela).

    /// <summary>#1 Ficha Gasta: +4 Mult.</summary>
    [RelicEffect(RelicIds.FichaGasta)]
    public sealed class FichaGasta : RelicEffectBase
    {
        public override void OnScore(RelicScope r) => r.AddMult(r.Values.FichaGastaMult);
    }

    /// <summary>#2 Rosa Rubra: aposta em Vermelho venceu, +8 Mult.</summary>
    [RelicEffect(RelicIds.RosaRubra)]
    public sealed class RosaRubra : RelicEffectBase
    {
        public override void OnScore(RelicScope r)
        {
            if (r.Spin.WinningBets.Any(b => b.Outside == OutsideKind.Red))
                r.AddMult(r.Values.RosaRubraMult);
        }
    }

    /// <summary>#8 Arranha-céu: o valor do número conta em dobro nos Pontos.</summary>
    [RelicEffect(RelicIds.ArranhaCeu)]
    public sealed class ArranhaCeu : RelicEffectBase
    {
        public override void OnScore(RelicScope r) =>
            r.AddPontos(SpinContext.NumberValue(r.Spin.Pocket, r.Balance) * r.Values.ArranhaCeuExtraCopies);
    }

    /// <summary>#10 Teorema: número primo, +10 Mult.</summary>
    [RelicEffect(RelicIds.Teorema)]
    public sealed class Teorema : RelicEffectBase
    {
        public override void OnScore(RelicScope r)
        {
            if (NumberFacts.IsPrime(r.Spin.Pocket.Number))
                r.AddMult(r.Values.TeoremaMult);
        }
    }

    /// <summary>#12 Mira Laser: Pleno venceu, ×3 Mult.</summary>
    [RelicEffect(RelicIds.MiraLaser)]
    public sealed class MiraLaser : RelicEffectBase
    {
        public override void OnScore(RelicScope r)
        {
            if (r.Spin.WinningBets.Any(b => b.Type == BetType.Straight))
                r.MultiplyMult(r.Values.MiraLaserMultX);
        }
    }

    /// <summary>#14 Combo Triplo: 3 ou mais apostas venceram no giro, ×2 Mult.</summary>
    [RelicEffect(RelicIds.ComboTriplo)]
    public sealed class ComboTriplo : RelicEffectBase
    {
        public override void OnScore(RelicScope r)
        {
            if (r.Spin.WinningBets.Count >= r.Values.ComboTriploMinWins)
                r.MultiplyMult(r.Values.ComboTriploMultX);
        }
    }
}
