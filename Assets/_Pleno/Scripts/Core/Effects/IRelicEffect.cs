using Pleno.Core.Content;
using Pleno.Core.Model;
using Pleno.Core.Rules;

namespace Pleno.Core.Effects
{
    /// <summary>
    /// Ganchos de um amuleto (GDD 10.5). O efeito não guarda estado: tudo o que escala fica em
    /// <see cref="RelicInstance.Counters"/>. Herde de <see cref="RelicEffectBase"/> e registre com
    /// <see cref="RelicEffectAttribute"/>.
    /// </summary>
    public interface IRelicEffect
    {
        string Id { get; }

        /// <summary>Giros, fichas e empurrões da mesa (Luva de Seda).</summary>
        MesaResources ModifyMesaResources(RelicInstance self, Balance balance, MesaResources resources);

        /// <summary>Regras gerais de peso, antes do cálculo por casa (Ímã de Neodímio).</summary>
        WeightRules ModifyWeightRules(RelicInstance self, Balance balance, WeightRules rules);

        /// <summary>Peso final de cada casa, por índice físico (Roda Viciada).</summary>
        void ModifyWeights(RelicInstance self, Wheel wheel, double[] weights);

        /// <summary>Passo 3 da pontuação. Só é chamado em giro com aposta vencedora.</summary>
        void OnScore(RelicScope scope);

        /// <summary>Passo 5 (pós-giro): dinheiro e atualização dos que escalam. Chamado em todo giro.</summary>
        void OnAfterSpin(RelicScope scope);
    }

    public abstract class RelicEffectBase : IRelicEffect
    {
        public string Id => RelicEffectAttribute.IdOf(GetType());

        public virtual MesaResources ModifyMesaResources(RelicInstance self, Balance balance, MesaResources resources) => resources;
        public virtual WeightRules ModifyWeightRules(RelicInstance self, Balance balance, WeightRules rules) => rules;
        public virtual void ModifyWeights(RelicInstance self, Wheel wheel, double[] weights) { }
        public virtual void OnScore(RelicScope scope) { }
        public virtual void OnAfterSpin(RelicScope scope) { }
    }
}
