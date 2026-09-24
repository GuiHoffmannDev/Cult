using System;
using Pleno.Core.Content;
using Pleno.Core.Model;

namespace Pleno.Core.Flow
{
    public static class Economy
    {
        /// <summary>+$1 a cada $5 guardados, com teto de +$5.</summary>
        public static int Interest(int money, RunBalance balance) =>
            Math.Min(Math.Max(money, 0) / balance.InterestStep * balance.InterestPerStep, balance.InterestCap);

        /// <summary>Metade do preço, arredondado para baixo, mínimo $1.</summary>
        public static int SellPrice(int price, RunBalance balance) =>
            Math.Max(price / balance.SellDivisor, balance.SellMinimum);

        /// <summary>$5 no primeiro reroll da loja, +$1 a cada reroll seguinte na mesma loja.</summary>
        public static int RerollCost(int rerollsDone, ShopBalance balance) =>
            balance.RerollBaseCost + rerollsDone * balance.RerollCostStep;

        public static int RelicPrice(string relicId, Balance balance) =>
            balance.Shop.RelicPrices[RelicCatalog.Rarities[relicId]];

        /// <summary>
        /// Pagamento ao vencer a mesa. Os juros incidem sobre o $ que o jogador tinha ao bater a meta,
        /// antes de somar recompensa e giros não usados.
        /// </summary>
        public static MesaPayout Payout(int moneyBeforePayout, int mesaIndex, int spinsLeft, Balance balance) =>
            new MesaPayout
            {
                Interest = Interest(moneyBeforePayout, balance.Run),
                Reward = balance.Run.MesaRewards[mesaIndex],
                UnusedSpins = spinsLeft * balance.Run.UnusedSpinMoney,
            };
    }
}
