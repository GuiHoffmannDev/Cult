using System;
using System.Text;

namespace Pleno.Core.Rng
{
    /// <summary>
    /// A seed da run: 8 caracteres, exibida e compartilhável. O alfabeto evita pares que se confundem
    /// na leitura (I/1, O/0).
    /// </summary>
    public static class RunSeed
    {
        public const int Length = 8;
        public const string Alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";

        /// <summary>Maiúsculas e sem espaços nas pontas. Não valida.</summary>
        public static string Normalize(string seed) => (seed ?? string.Empty).Trim().ToUpperInvariant();

        public static bool IsValid(string seed)
        {
            if (seed == null || seed.Length != Length)
                return false;
            foreach (char c in seed)
                if (Alphabet.IndexOf(c) < 0)
                    return false;
            return true;
        }

        /// <summary>
        /// Converte 40 bits de entropia numa seed. Quem chama fornece a entropia (relógio, por
        /// exemplo); o Core não tem fonte de aleatoriedade própria.
        /// </summary>
        public static string FromEntropy(ulong entropy)
        {
            var sb = new StringBuilder(Length);
            for (int i = 0; i < Length; i++)
            {
                sb.Append(Alphabet[(int)(entropy & 31UL)]);
                entropy >>= 5;
            }
            return sb.ToString();
        }

        internal static string RequireValid(string seed)
        {
            string normalized = Normalize(seed);
            if (!IsValid(normalized))
                throw new ArgumentException($"Seed inválida: '{seed}'. Use {Length} caracteres de {Alphabet}.", nameof(seed));
            return normalized;
        }
    }
}
