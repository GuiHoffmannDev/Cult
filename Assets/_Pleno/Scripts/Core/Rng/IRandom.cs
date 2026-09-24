using System;
using System.Collections.Generic;

namespace Pleno.Core.Rng
{
    /// <summary>Fonte de aleatoriedade das regras. Em produção é sempre um stream do <see cref="RngStreams"/>.</summary>
    public interface IRandom
    {
        uint NextUInt();

        /// <summary>Inteiro uniforme em [0, maxExclusive), sem viés.</summary>
        int NextInt(int maxExclusive);

        /// <summary>Double uniforme em [0, 1), com 53 bits de precisão.</summary>
        double NextDouble();
    }

    public static class RandomExtensions
    {
        /// <summary>"1 em N": verdadeiro com probabilidade 1/N.</summary>
        public static bool OneIn(this IRandom rng, int n) => rng.NextInt(n) == 0;

        /// <summary>Um elemento uniforme da lista.</summary>
        public static T Pick<T>(this IRandom rng, IReadOnlyList<T> items)
        {
            if (items.Count == 0)
                throw new ArgumentException("Lista vazia.", nameof(items));
            return items[rng.NextInt(items.Count)];
        }

        /// <summary>
        /// <paramref name="count"/> índices distintos de [0, n), na ordem do Fisher-Yates parcial.
        /// Devolve menos se n &lt; count.
        /// </summary>
        public static List<int> PickDistinct(this IRandom rng, int n, int count)
        {
            var pool = new int[n];
            for (int i = 0; i < n; i++) pool[i] = i;

            var picked = new List<int>();
            for (int i = 0; i < Math.Min(count, n); i++)
            {
                int j = i + rng.NextInt(n - i);
                int tmp = pool[i];
                pool[i] = pool[j];
                pool[j] = tmp;
                picked.Add(pool[i]);
            }
            return picked;
        }

        /// <summary>Índice sorteado com probabilidade proporcional ao peso (pesos ≥ 0, pelo menos um positivo).</summary>
        public static int PickWeightedIndex(this IRandom rng, IReadOnlyList<double> weights)
        {
            double total = 0;
            int lastPositive = -1;
            for (int i = 0; i < weights.Count; i++)
            {
                if (weights[i] < 0 || double.IsNaN(weights[i]))
                    throw new ArgumentException($"Peso inválido no índice {i}: {weights[i]}.", nameof(weights));
                if (weights[i] > 0)
                    lastPositive = i;
                total += weights[i];
            }
            if (lastPositive < 0)
                throw new ArgumentException("Nenhum peso positivo.", nameof(weights));

            double target = rng.NextDouble() * total;
            double cumulative = 0;
            for (int i = 0; i < weights.Count; i++)
            {
                cumulative += weights[i];
                if (target < cumulative)
                    return i;
            }
            return lastPositive; // arredondamento de ponto flutuante no fim do intervalo
        }
    }
}
