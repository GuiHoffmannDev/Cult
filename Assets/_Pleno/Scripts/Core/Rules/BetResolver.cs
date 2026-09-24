using System.Collections.Generic;
using System.Linq;
using Pleno.Core.Model;

namespace Pleno.Core.Rules
{
    /// <summary>Decide quais apostas vencem numa casa sorteada.</summary>
    public static class BetResolver
    {
        /// <summary>Apostas vencedoras, na ordem em que foram colocadas.</summary>
        public static List<PlacedBet> WinningBets(IEnumerable<PlacedBet> bets, Wheel wheel, int landingIndex) =>
            bets.Where(b => Wins(b, wheel, landingIndex)).ToList();

        public static bool Wins(PlacedBet bet, Wheel wheel, int landingIndex)
        {
            Pocket pocket = wheel[landingIndex];

            // Regra do zero: caindo 0 ou 00, todas as externas perdem.
            if (bet.Type.IsOutside() && pocket.IsZero)
                return false;

            switch (bet.Type)
            {
                case BetType.SimpleChance:
                    return WinsSimpleChance(bet.Outside, pocket);
                case BetType.DozenColumn:
                    return bet.Outside == OutsideKind.Dozen
                        ? (pocket.Number - 1) / 12 == bet.Index - 1
                        : TableLayout.Col(pocket.Number) == bet.Index - 1;
                case BetType.Neighbors:
                    return WinsNeighbors(bet, wheel, landingIndex);
                default:
                    return bet.Numbers.Contains(pocket.Number);
            }
        }

        static bool WinsSimpleChance(OutsideKind kind, Pocket pocket)
        {
            int n = pocket.Number;
            switch (kind)
            {
                case OutsideKind.Red: return pocket.Color == PocketColor.Red;
                case OutsideKind.Black: return pocket.Color == PocketColor.Black;
                case OutsideKind.Even: return n % 2 == 0;
                case OutsideKind.Odd: return n % 2 == 1;
                case OutsideKind.Low: return n >= 1 && n <= 18;
                case OutsideKind.High: return n >= 19 && n <= 36;
                default: return false;
            }
        }

        /// <summary>
        /// Vizinhos cobre 5 posições físicas: a casa âncora e 2 de cada lado. Um número repetido em
        /// outro ponto da roda não conta. Se a âncora foi apagada, a aposta não pode vencer.
        /// </summary>
        static bool WinsNeighbors(PlacedBet bet, Wheel wheel, int landingIndex)
        {
            int anchor = wheel.IndexOfId(bet.AnchorPocketId);
            return anchor >= 0 && wheel.CircularDistance(anchor, landingIndex) <= NeighborsReach;
        }

        /// <summary>Casas de cada lado da âncora cobertas por Vizinhos. É geometria da aposta, não balanceamento.</summary>
        public const int NeighborsReach = 2;

        /// <summary>Índices físicos cobertos por uma aposta de Vizinhos (vazio se a âncora sumiu).</summary>
        public static IEnumerable<int> NeighborsCoverage(PlacedBet bet, Wheel wheel)
        {
            int anchor = wheel.IndexOfId(bet.AnchorPocketId);
            if (anchor < 0)
                yield break;
            for (int offset = -NeighborsReach; offset <= NeighborsReach; offset++)
                yield return wheel.Neighbor(anchor, offset);
        }
    }
}
