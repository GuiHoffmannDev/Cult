using System;
using Pleno.Core.Content;
using Pleno.Core.Model;
using Pleno.Core.Rules;

namespace Pleno.Core.Effects.Relics
{
    // Amuletos de manipulação da roda (GDD 7.1).

    /// <summary>#31 Luva de Seda: +1 Empurrão por mesa.</summary>
    [RelicEffect(RelicIds.LuvaDeSeda)]
    public sealed class LuvaDeSeda : RelicEffectBase
    {
        public override MesaResources ModifyMesaResources(RelicInstance self, Balance balance, MesaResources resources)
        {
            resources.Nudges += balance.Relics.LuvaDeSedaNudges;
            return resources;
        }
    }

    /// <summary>
    /// #33 Ímã de Neodímio: casas Magnéticas têm peso ×4 em vez de ×2. Substitui o multiplicador, não
    /// empilha: dois Ímãs continuam dando ×4.
    /// </summary>
    [RelicEffect(RelicIds.ImaDeNeodimio)]
    public sealed class ImaDeNeodimio : RelicEffectBase
    {
        public override WeightRules ModifyWeightRules(RelicInstance self, Balance balance, WeightRules rules)
        {
            rules.MagneticWeightX = Math.Max(rules.MagneticWeightX, balance.Relics.NeodimioMagneticWeightX);
            return rules;
        }
    }
}
