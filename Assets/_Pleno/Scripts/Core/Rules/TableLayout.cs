using System.Collections.Generic;
using System.Linq;
using Pleno.Core.Model;

namespace Pleno.Core.Rules
{
    /// <summary>
    /// Geometria do pano 3×12 de roleta: quais combinações de números formam Cavalo, Rua, Quadra e
    /// Linha. As apostas que incluem o zero seguem o pano tradicional europeu (0-1, 0-2, 0-3, 0-1-2,
    /// 0-2-3, 0-1-2-3) e americano (00-2, 00-3, 0-00, 00-2-3, 0-00-2).
    /// </summary>
    public static class TableLayout
    {
        const int Dz = Pocket.DoubleZero;

        static readonly int[][] ZeroSplits =
        {
            new[] { 0, 1 }, new[] { 0, 2 }, new[] { 0, 3 },
            new[] { Dz, 2 }, new[] { Dz, 3 }, new[] { Dz, 0 },
        };

        static readonly int[][] ZeroStreets =
        {
            new[] { 0, 1, 2 }, new[] { 0, 2, 3 },
            new[] { Dz, 2, 3 }, new[] { Dz, 0, 2 },
        };

        static readonly int[] ZeroCorner = { 0, 1, 2, 3 };

        /// <summary>Linha do pano (0 a 11) de um número de 1 a 36.</summary>
        public static int Row(int number) => (number - 1) / 3;

        /// <summary>Coluna do pano (0 a 2) de um número de 1 a 36.</summary>
        public static int Col(int number) => (number - 1) % 3;

        public static bool IsTableNumber(int number) => number >= 1 && number <= 36;

        public static bool IsValidInside(BetType type, IReadOnlyList<int> sortedNumbers)
        {
            int[] n = sortedNumbers.ToArray();
            if (n.Distinct().Count() != n.Length)
                return false;

            switch (type)
            {
                case BetType.Straight:
                    return n.Length == 1 && (n[0] == Dz || (n[0] >= 0 && n[0] <= 36));
                case BetType.Split:
                    return n.Length == 2 && (IsTableSplit(n[0], n[1]) || ContainsSet(ZeroSplits, n));
                case BetType.Street:
                    return n.Length == 3 && (IsTableStreet(n) || ContainsSet(ZeroStreets, n));
                case BetType.Corner:
                    return n.Length == 4 && (IsTableCorner(n) || n.SequenceEqual(ZeroCorner));
                case BetType.Line:
                    return n.Length == 6 && n.All(IsTableNumber) && Col(n[0]) == 0 && n[5] - n[0] == 5
                        && n.Zip(n.Skip(1), (a, b) => b - a).All(d => d == 1);
                default:
                    return false;
            }
        }

        static bool IsTableSplit(int a, int b)
        {
            if (!IsTableNumber(a) || !IsTableNumber(b))
                return false;
            bool sideBySide = Row(a) == Row(b) && b - a == 1;
            bool stacked = Col(a) == Col(b) && b - a == 3;
            return sideBySide || stacked;
        }

        static bool IsTableStreet(int[] n) =>
            n.All(IsTableNumber) && Col(n[0]) == 0 && n[1] == n[0] + 1 && n[2] == n[0] + 2;

        static bool IsTableCorner(int[] n) =>
            n.All(IsTableNumber) && Col(n[0]) < 2
            && n[1] == n[0] + 1 && n[2] == n[0] + 3 && n[3] == n[0] + 4;

        static bool ContainsSet(int[][] sets, int[] sortedNumbers) =>
            sets.Any(s => s.OrderBy(x => x).SequenceEqual(sortedNumbers));
    }
}
