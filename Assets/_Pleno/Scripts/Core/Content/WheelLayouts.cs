using System.Collections.Generic;
using System.Linq;
using Pleno.Core.Model;

namespace Pleno.Core.Content
{
    /// <summary>Ordem física real das rodas de roleta, no sentido horário a partir do 0.</summary>
    public static class WheelLayouts
    {
        public static readonly IReadOnlyList<int> EuropeanOrder = new[]
        {
            0, 32, 15, 19, 4, 21, 2, 25, 17, 34, 6, 27, 13, 36, 11, 30, 8, 23, 10,
            5, 24, 16, 33, 1, 20, 14, 31, 9, 22, 18, 29, 7, 28, 12, 35, 3, 26,
        };

        public static readonly IReadOnlyList<int> AmericanOrder = new[]
        {
            0, 28, 9, 26, 30, 11, 7, 20, 32, 17, 5, 22, 34, 15, 3, 24, 36, 13, 1,
            Pocket.DoubleZero, 27, 10, 25, 29, 12, 8, 19, 31, 18, 6, 21, 33, 16, 4, 23, 35, 14, 2,
        };

        static readonly HashSet<int> RedNumbers = new HashSet<int>
        {
            1, 3, 5, 7, 9, 12, 14, 16, 18, 19, 21, 23, 25, 27, 30, 32, 34, 36,
        };

        /// <summary>Cor de fábrica de um número (antes de qualquer Pintor).</summary>
        public static PocketColor StandardColor(int number)
        {
            if (number == Pocket.Zero || number == Pocket.DoubleZero)
                return PocketColor.Green;
            return RedNumbers.Contains(number) ? PocketColor.Red : PocketColor.Black;
        }

        public static Wheel European() => Build(EuropeanOrder);

        public static Wheel American() => Build(AmericanOrder);

        public static Wheel Build(IEnumerable<int> order) =>
            new Wheel(order.Select(n => new Pocket(n, StandardColor(n))));
    }
}
