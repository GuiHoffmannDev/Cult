using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Pleno.Core.Effects
{
    /// <summary>Encontra os efeitos de amuleto do Core pelo <see cref="RelicEffectAttribute"/>.</summary>
    public static class RelicEffectRegistry
    {
        static readonly Lazy<Dictionary<string, IRelicEffect>> Effects =
            new Lazy<Dictionary<string, IRelicEffect>>(Discover);

        public static IReadOnlyCollection<string> Ids => Effects.Value.Keys;

        public static bool TryGet(string id, out IRelicEffect effect) => Effects.Value.TryGetValue(id, out effect);

        public static IRelicEffect Get(string id)
        {
            if (!TryGet(id, out IRelicEffect effect))
                throw new KeyNotFoundException($"Amuleto sem efeito registrado: '{id}'.");
            return effect;
        }

        static Dictionary<string, IRelicEffect> Discover()
        {
            var effects = new Dictionary<string, IRelicEffect>(StringComparer.Ordinal);
            IEnumerable<Type> types = typeof(IRelicEffect).Assembly.GetTypes()
                .Where(t => !t.IsAbstract && typeof(IRelicEffect).IsAssignableFrom(t)
                    && t.GetCustomAttribute<RelicEffectAttribute>() != null);

            foreach (Type type in types)
            {
                string id = RelicEffectAttribute.IdOf(type);
                if (effects.ContainsKey(id))
                    throw new InvalidOperationException($"Id de amuleto duplicado: '{id}'.");
                effects[id] = (IRelicEffect)Activator.CreateInstance(type);
            }
            return effects;
        }
    }
}
