using System.Collections.Generic;
using System.Linq;
using Pleno.Core.Model;
using Pleno.Core.Rules;

namespace Pleno.Sim.Planning
{
    /// <summary>Todas as apostas possíveis no pano e na pista, com 1 ficha cada.</summary>
    public static class BetCatalog
    {
        const int Dz = Pocket.DoubleZero;

        public static List<PlacedBet> Outside() => new List<PlacedBet>
        {
            PlacedBet.Red(1), PlacedBet.Black(1), PlacedBet.Even(1), PlacedBet.Odd(1), PlacedBet.Low(1), PlacedBet.High(1),
            PlacedBet.Dozen(1, 1), PlacedBet.Dozen(2, 1), PlacedBet.Dozen(3, 1),
            PlacedBet.Column(1, 1), PlacedBet.Column(2, 1), PlacedBet.Column(3, 1),
        };

        /// <summary>Plenos de 0 a 36 e o 00.</summary>
        public static List<PlacedBet> Straights()
        {
            var bets = Enumerable.Range(0, 37).Select(n => PlacedBet.Straight(n, 1)).ToList();
            bets.Add(PlacedBet.Straight(Dz, 1));
            return bets;
        }

        /// <summary>Vizinhos ancorado em cada casa da roda.</summary>
        public static List<PlacedBet> Neighbors(Wheel wheel) =>
            wheel.Pockets.Select(p => PlacedBet.Neighbors(p.Id, 1)).ToList();

        /// <summary>Cavalo, Rua, Quadra e Linha, com as combinações do zero do pano tradicional.</summary>
        public static List<PlacedBet> Combinations()
        {
            var bets = new List<PlacedBet>();
            for (int n = 1; n <= 36; n++)
            {
                if (TableLayout.Col(n) < 2)
                    bets.Add(PlacedBet.Split(n, n + 1, 1));
                if (n + 3 <= 36)
                    bets.Add(PlacedBet.Split(n, n + 3, 1));
            }
            foreach (int[] s in new[] { new[] { 0, 1 }, new[] { 0, 2 }, new[] { 0, 3 }, new[] { Dz, 2 }, new[] { Dz, 3 }, new[] { Dz, 0 } })
                bets.Add(PlacedBet.Split(s[0], s[1], 1));

            for (int first = 1; first <= 34; first += 3)
                bets.Add(PlacedBet.Street(1, first, first + 1, first + 2));
            foreach (int[] s in new[] { new[] { 0, 1, 2 }, new[] { 0, 2, 3 }, new[] { Dz, 2, 3 }, new[] { Dz, 0, 2 } })
                bets.Add(PlacedBet.Street(1, s));

            for (int n = 1; n + 4 <= 36; n++)
                if (TableLayout.Col(n) < 2)
                    bets.Add(PlacedBet.Corner(1, n, n + 1, n + 3, n + 4));
            bets.Add(PlacedBet.Corner(1, 0, 1, 2, 3));

            for (int first = 1; first <= 31; first += 3)
                bets.Add(PlacedBet.Line(first, 1));
            return bets;
        }

        public static List<PlacedBet> All(Wheel wheel) =>
            Outside().Concat(Straights()).Concat(Combinations()).Concat(Neighbors(wheel)).ToList();
    }
}
