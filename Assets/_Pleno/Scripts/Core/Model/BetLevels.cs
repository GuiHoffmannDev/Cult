using System;
using System.Collections.Generic;

namespace Pleno.Core.Model
{
    /// <summary>Nível de cada tipo de aposta. Começa em 1 e sobe com as Tábuas.</summary>
    public sealed class BetLevels
    {
        public Dictionary<BetType, int> Levels { get; set; } = new Dictionary<BetType, int>();

        public int Get(BetType type) => Levels.TryGetValue(type, out int level) ? level : 1;

        public void Raise(BetType type, int amount)
        {
            if (amount < 0)
                throw new ArgumentOutOfRangeException(nameof(amount));
            Levels[type] = Get(type) + amount;
        }
    }
}
