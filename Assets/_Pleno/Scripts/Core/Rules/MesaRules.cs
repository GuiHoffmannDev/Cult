using System.Collections.Generic;
using Pleno.Core.Content;
using Pleno.Core.Effects;
using Pleno.Core.Model;

namespace Pleno.Core.Rules
{
    public static class MesaRules
    {
        /// <summary>Recursos da mesa: valores base do balanceamento, depois os amuletos da esquerda para a direita.</summary>
        public static MesaResources Resources(Balance balance, IReadOnlyList<RelicInstance> relics)
        {
            var resources = new MesaResources(balance.Mesa.Spins, balance.Mesa.ChipsPerSpin, balance.Mesa.Nudges);
            foreach (RelicInstance relic in relics)
                resources = RelicEffectRegistry.Get(relic.Id).ModifyMesaResources(relic, balance, resources);
            return resources;
        }
    }
}
