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
    }
}
