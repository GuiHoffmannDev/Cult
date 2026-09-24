using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Pleno.Core.Content;
using Pleno.Core.Model;

namespace Pleno.Core.Effects
{
    /// <summary>
    /// A regra de um Crupiê-chefe (GDD 8). Herde de <see cref="BossRuleBase"/> e registre com
    /// <see cref="BossRuleAttribute"/>. A regra aparece antes da mesa e vale só nela.
    /// </summary>
    public interface IBossRule
    {
        string Id { get; }

        /// <summary>Aplicado por último, depois da roda inicial e dos amuletos.</summary>
        MesaResources ModifyMesaResources(Balance balance, MesaResources resources);

        void OnMesaStart(RunState run, MesaState mesa);

        /// <summary>Desfaz o que a regra pôs na roda. Chamado ao vencer ou perder a mesa.</summary>
        void OnMesaEnd(RunState run, MesaState mesa);

        /// <summary>Falso: a aposta conta como perdida mesmo que a bola caia nela.</summary>
        bool BetScores(PlacedBet bet);

        /// <summary>A Névoa: os números da roda ficam ocultos durante o giro. Só apresentação.</summary>
        bool HidesWheelNumbers { get; }

        /// <summary>Mão Pesada: tentar empurrar é recusado (a tela reage com a batida na mesa).</summary>
        bool NudgesDisabled { get; }
    }

    public abstract class BossRuleBase : IBossRule
    {
        public string Id => BossRuleAttribute.IdOf(GetType());

        public virtual MesaResources ModifyMesaResources(Balance balance, MesaResources resources) => resources;
        public virtual void OnMesaStart(RunState run, MesaState mesa) { }
        public virtual void OnMesaEnd(RunState run, MesaState mesa) { }
        public virtual bool BetScores(PlacedBet bet) => true;
        public virtual bool HidesWheelNumbers => false;
        public virtual bool NudgesDisabled => false;
    }

    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public sealed class BossRuleAttribute : Attribute
    {
        public string Id { get; }

        public BossRuleAttribute(string id)
        {
            Id = id;
        }

        public static string IdOf(Type type)
        {
            var attribute = type.GetCustomAttribute<BossRuleAttribute>();
            if (attribute == null)
                throw new InvalidOperationException($"{type.Name} não tem [BossRule].");
            return attribute.Id;
        }
    }

    public static class BossRuleRegistry
    {
        static readonly Lazy<Dictionary<string, IBossRule>> Rules = new Lazy<Dictionary<string, IBossRule>>(Discover);

        public static IReadOnlyCollection<string> Ids => Rules.Value.Keys;

        public static IBossRule Get(string id)
        {
            if (!Rules.Value.TryGetValue(id, out IBossRule rule))
                throw new KeyNotFoundException($"Chefe sem regra registrada: '{id}'.");
            return rule;
        }

        static Dictionary<string, IBossRule> Discover()
        {
            var rules = new Dictionary<string, IBossRule>(StringComparer.Ordinal);
            foreach (Type type in typeof(IBossRule).Assembly.GetTypes()
                .Where(t => !t.IsAbstract && typeof(IBossRule).IsAssignableFrom(t)
                    && t.GetCustomAttribute<BossRuleAttribute>() != null))
            {
                string id = BossRuleAttribute.IdOf(type);
                if (rules.ContainsKey(id))
                    throw new InvalidOperationException($"Id de chefe duplicado: '{id}'.");
                rules[id] = (IBossRule)Activator.CreateInstance(type);
            }
            return rules;
        }
    }
}
