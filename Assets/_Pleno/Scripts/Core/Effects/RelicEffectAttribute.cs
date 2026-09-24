using System;
using System.Reflection;

namespace Pleno.Core.Effects
{
    /// <summary>Registra um efeito de amuleto pelo id de conteúdo: <c>[RelicEffect("teorema")]</c>.</summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public sealed class RelicEffectAttribute : Attribute
    {
        public string Id { get; }

        public RelicEffectAttribute(string id)
        {
            Id = id;
        }

        public static string IdOf(Type type)
        {
            var attribute = type.GetCustomAttribute<RelicEffectAttribute>();
            if (attribute == null)
                throw new InvalidOperationException($"{type.Name} não tem [RelicEffect].");
            return attribute.Id;
        }
    }
}
