using System.Collections.Generic;
using Pleno.Core.Content;
using Pleno.Core.Effects;
using Pleno.Core.Model;

namespace Pleno.Core.Rules
{
    public static class MesaRules
    {
        /// <summary>Recursos da mesa: valores base do balanceamento, depois os amuletos da esquerda para a direita.</summary>
        public static MesaResources Resources(Balance balance, IReadOnlyList<RelicInstance> relics) =>
            Resources(balance, StartingWheel.European, relics, null);

        /// <summary>Base → roda inicial → amuletos → chefe. O chefe vem por último e pode anular os demais.</summary>
        public static MesaResources Resources(Balance balance, StartingWheel startingWheel,
            IReadOnlyList<RelicInstance> relics, IBossRule boss)
        {
            var resources = new MesaResources(balance.Mesa.Spins, balance.Mesa.ChipsPerSpin, balance.Mesa.Nudges);
            resources = StartingWheels.Apply(startingWheel, balance, resources);
            foreach (RelicInstance relic in relics)
                resources = RelicEffectRegistry.Get(relic.Id).ModifyMesaResources(relic, balance, resources);
            if (boss != null)
                resources = boss.ModifyMesaResources(balance, resources);
            return resources;
        }
    }
}
