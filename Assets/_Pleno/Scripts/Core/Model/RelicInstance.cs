using System.Collections.Generic;

namespace Pleno.Core.Model
{
    /// <summary>
    /// Um amuleto possuído. O efeito é resolvido pelo <see cref="Id"/>; o estado que escala
    /// (Martingale, Colecionador) fica em <see cref="Counters"/>, que é serializável.
    /// </summary>
    public sealed class RelicInstance
    {
        public string Id { get; set; }
        public Dictionary<string, double> Counters { get; set; } = new Dictionary<string, double>();

        public RelicInstance()
        {
        }

        public RelicInstance(string id)
        {
            Id = id;
        }

        public double GetCounter(string key) => Counters.TryGetValue(key, out double value) ? value : 0;

        public void SetCounter(string key, double value) => Counters[key] = value;

        public bool HasCounter(string key) => Counters.ContainsKey(key);
    }
}
