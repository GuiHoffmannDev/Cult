using NUnit.Framework;
using Pleno.Core.Content;
using Pleno.Core.Flow;
using Pleno.Core.Model;

namespace Pleno.Tests.Core
{
    public sealed class EconomyTests
    {
        static readonly Balance B = Balance.Default;

        [TestCase(-3, 0)]
        [TestCase(0, 0)]
        [TestCase(4, 0)]
        [TestCase(5, 1)]
        [TestCase(9, 1)]
        [TestCase(10, 2)]
        [TestCase(24, 4)]
        [TestCase(25, 5)]
        [TestCase(26, 5)]
        [TestCase(200, 5)]
        public void Interest_OneDollarPerFiveCappedAtFive(int money, int interest)
        {
            Assert.AreEqual(interest, Economy.Interest(money, B.Run));
        }

        [TestCase(1, 1)]
        [TestCase(2, 1)]
        [TestCase(3, 1)]
        [TestCase(4, 2)]
        [TestCase(5, 2)]
        [TestCase(6, 3)]
        [TestCase(8, 4)]
        public void SellPrice_IsHalfRoundedDownWithMinimumOne(int price, int sell)
        {
            Assert.AreEqual(sell, Economy.SellPrice(price, B.Run));
        }

        [Test]
        public void RerollCost_StartsAtFiveAndGrowsByOne()
        {
            Assert.AreEqual(5, Economy.RerollCost(0, B.Shop));
            Assert.AreEqual(6, Economy.RerollCost(1, B.Shop));
            Assert.AreEqual(8, Economy.RerollCost(3, B.Shop));
        }

        [Test]
        public void RelicPrices_AreTheFloorOfEachRarityRange()
        {
            Assert.AreEqual(4, Economy.RelicPrice(RelicIds.FichaGasta, B));
            Assert.AreEqual(6, Economy.RelicPrice(RelicIds.Teorema, B));
        }

        [TestCase(0, 3)]
        [TestCase(1, 4)]
        [TestCase(2, 5)]
        public void Payout_RewardPerMesa(int mesaIndex, int reward)
        {
            Assert.AreEqual(reward, Economy.Payout(0, mesaIndex, 0, B).Reward);
        }

        [Test]
        public void Payout_InterestIsOnMoneyBeforeTheReward()
        {
            // $9 ao bater a meta: juros de $1 (não $2, que seria sobre 9 + 4 + 2).
            MesaPayout p = Economy.Payout(moneyBeforePayout: 9, mesaIndex: 1, spinsLeft: 2, balance: B);
            Assert.AreEqual(4, p.Reward);
            Assert.AreEqual(2, p.UnusedSpins);
            Assert.AreEqual(1, p.Interest);
            Assert.AreEqual(7, p.Total);
        }

        [Test]
        public void WinningAMesa_PaysRewardUnusedSpinsAndInterest()
        {
            RunEngine run = RunTestUtil.New();
            Assert.AreEqual(4, run.State.Money, "$ inicial");
            run.State.Money = 10;

            // Um giro de 1.345 bate a meta de 100 com 3 giros sobrando.
            RunTestUtil.SpinOn(run, 19, PlacedBet.Straight(19, 5));

            Assert.AreEqual(RunPhase.Shop, run.State.Phase);
            Assert.AreEqual(3, run.State.Mesa.Payout.Reward);
            Assert.AreEqual(3, run.State.Mesa.Payout.UnusedSpins);
            Assert.AreEqual(2, run.State.Mesa.Payout.Interest);
            Assert.AreEqual(10 + 8, run.State.Money);
        }

        [Test]
        public void GoldPocketMoney_CountsForInterestOnTheWinningSpin()
        {
            RunEngine run = RunTestUtil.New();
            run.State.Money = 3;
            run.State.Wheel[RunTestUtil.IndexOf(run, 19)].Enhancement = PocketEnhancement.Gold;
            RunTestUtil.SpinOn(run, 19, PlacedBet.Straight(19, 5));
            // $3 + $3 da Dourada = $6 ao bater a meta → juros $1; + recompensa 3 + 3 giros.
            Assert.AreEqual(6 + 1 + 3 + 3, run.State.Money);
        }

        [Test]
        public void Shop_BuySellAndRerollMoveMoney()
        {
            RunEngine run = RunTestUtil.New();
            RunTestUtil.WinMesa(run);
            run.State.Money = 30;

            ShopOffer relic = run.State.Shop.Relics[0];
            run.BuyRelic(0);
            Assert.AreEqual(30 - relic.Price, run.State.Money);
            Assert.IsTrue(relic.Sold);
            Assert.AreEqual("Item indisponível.", run.CheckBuyRelic(0));

            int money = run.State.Money;
            Assert.AreEqual(relic.Price / 2, run.SellRelic(0));
            Assert.AreEqual(money + relic.Price / 2, run.State.Money);

            money = run.State.Money;
            run.Reroll();
            run.Reroll();
            Assert.AreEqual(money - 5 - 6, run.State.Money);
            Assert.AreEqual(7, run.RerollCost);

            run.State.Money = 3;
            Assert.AreEqual("Dinheiro insuficiente.", run.CheckReroll());
        }

        [Test]
        public void RerollCost_ResetsInTheNextShop()
        {
            RunEngine run = RunTestUtil.New();
            RunTestUtil.WinMesa(run);
            run.State.Money = 50;
            run.Reroll();
            run.Reroll();
            run.LeaveShop();
            RunTestUtil.WinMesa(run);
            Assert.AreEqual(5, run.RerollCost);
        }

        [Test]
        public void ConsumableSellsForOne()
        {
            RunEngine run = RunTestUtil.New();
            run.State.Consumables.Add(new ConsumableInstance(TabuaIds.Pleno));
            int money = run.State.Money;
            Assert.AreEqual(1, run.SellConsumable(0));
            Assert.AreEqual(money + 1, run.State.Money);
        }
    }
}
