using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Pleno.Core.Content;
using Pleno.Core.Flow;
using Pleno.Core.Model;
using Pleno.Core.Rng;

namespace Pleno.Tests.Core
{
    public sealed class ShopTests
    {
        static readonly Balance B = Balance.Default;

        static RunState EmptyRun() => new RunState { Wheel = WheelLayouts.European() };

        static RunEngine InShop(StartingWheel wheel = StartingWheel.European)
        {
            RunEngine run = RunTestUtil.New(wheel);
            RunTestUtil.WinMesa(run);
            Assert.AreEqual(RunPhase.Shop, run.State.Phase);
            return run;
        }

        [Test]
        public void Shop_HasTwoRelicsTwoConsumablesAndOnePack()
        {
            ShopState shop = InShop().State.Shop;
            Assert.AreEqual(2, shop.Relics.Count);
            Assert.AreEqual(2, shop.Consumables.Count);
            Assert.IsNotNull(shop.Pack);
            CollectionAssert.Contains(PackIds.All, shop.Pack.Id);
            Assert.AreEqual(B.Shop.Packs[shop.Pack.Id].Price, shop.Pack.Price);
            foreach (ShopOffer offer in shop.Consumables)
                Assert.AreEqual(3, offer.Price);
        }

        [Test]
        public void Shop_NeverOffersOwnedRelicsOrTheSameRelicTwice()
        {
            var rng = new Pcg32(5, 7);
            RunState run = EmptyRun();
            run.Relics.Add(new RelicInstance(RelicIds.Teorema));
            run.Relics.Add(new RelicInstance(RelicIds.FichaGasta));

            for (int i = 0; i < 500; i++)
            {
                ShopState shop = ShopGenerator.Generate(run, B, rng);
                string[] ids = shop.Relics.Select(o => o.Id).ToArray();
                Assert.AreEqual(2, ids.Distinct().Count());
                CollectionAssert.DoesNotContain(ids, RelicIds.Teorema);
                CollectionAssert.DoesNotContain(ids, RelicIds.FichaGasta);
                CollectionAssert.IsSubsetOf(ids, RelicCatalog.Rarities.Keys);
            }
        }

        [Test]
        public void Shop_OffersFewerRelicsWhenFewAreLeft()
        {
            RunState run = EmptyRun();
            foreach (string id in RelicCatalog.Rarities.Keys.Where(id => id != RelicIds.Martingale))
                run.Relics.Add(new RelicInstance(id));
            ShopState shop = ShopGenerator.Generate(run, B, new Pcg32(1, 1));
            CollectionAssert.AreEqual(new[] { RelicIds.Martingale }, shop.Relics.Select(o => o.Id));
        }

        [Test]
        public void Shop_RarityOnlyAmongAvailableRarities_KeepingTheRatio()
        {
            // Não há Raro: Comum e Incomum ficam na proporção 70 : 25.
            var rng = new Pcg32(11, 3);
            RunState run = EmptyRun();
            int commons = 0, samples = 4000;
            for (int i = 0; i < samples; i++)
            {
                string first = ShopGenerator.Generate(run, B, rng).Relics[0].Id;
                if (RelicCatalog.Rarities[first] == Rarity.Common)
                    commons++;
            }
            Assert.That(commons / (double)samples, Is.EqualTo(70.0 / 95.0).Within(0.03));
        }

        [Test]
        public void Shop_RelicPriceFollowsRarity()
        {
            var rng = new Pcg32(2, 9);
            for (int i = 0; i < 100; i++)
                foreach (ShopOffer offer in ShopGenerator.Generate(EmptyRun(), B, rng).Relics)
                    Assert.AreEqual(RelicCatalog.Rarities[offer.Id] == Rarity.Common ? 4 : 6, offer.Price);
        }

        [Test]
        public void Shop_ConsumableSlotsAreHalfTabuasHalfCards_AndPacksAreUniform()
        {
            var rng = new Pcg32(8, 8);
            int tabuas = 0, slots = 0;
            var packs = new Dictionary<string, int>();
            for (int i = 0; i < 3000; i++)
            {
                ShopState shop = ShopGenerator.Generate(EmptyRun(), B, rng);
                foreach (ShopOffer offer in shop.Consumables)
                {
                    slots++;
                    if (Consumables.KindOf(offer.Id) == ConsumableKind.Tabua)
                        tabuas++;
                }
                packs[shop.Pack.Id] = packs.TryGetValue(shop.Pack.Id, out int n) ? n + 1 : 1;
            }
            Assert.That(tabuas / (double)slots, Is.EqualTo(0.5).Within(0.03));
            foreach (string pack in PackIds.All)
                Assert.That(packs[pack] / 3000.0, Is.EqualTo(1 / 3.0).Within(0.03), pack);
        }

        [Test]
        public void Shop_SameSeedSameShop()
        {
            ShopState a = InShop().State.Shop, b = InShop().State.Shop;
            CollectionAssert.AreEqual(a.Relics.Select(o => o.Id), b.Relics.Select(o => o.Id));
            CollectionAssert.AreEqual(a.Consumables.Select(o => o.Id), b.Consumables.Select(o => o.Id));
            Assert.AreEqual(a.Pack.Id, b.Pack.Id);
        }

        [Test]
        public void Shop_UsesItsOwnStream_SoShoppingDoesNotChangeTheNextSpin()
        {
            RunEngine browsing = InShop(), idle = InShop();
            browsing.State.Money = 100;
            browsing.Reroll();
            browsing.Reroll();
            browsing.LeaveShop();
            idle.LeaveShop();

            foreach (RunEngine run in new[] { browsing, idle })
                foreach (Pocket p in run.State.Wheel.Pockets) p.Weight = 1;
            browsing.Place(PlacedBet.Red(1));
            idle.Place(PlacedBet.Red(1));
            Assert.AreEqual(idle.Spin().LandingIndex, browsing.Spin().LandingIndex);
        }

        [Test]
        public void Reroll_ReplacesRelicsAndConsumablesButKeepsThePack()
        {
            RunEngine run = InShop();
            run.State.Money = 100;
            ShopOffer pack = run.State.Shop.Pack;
            var before = run.State.Shop.Relics.Select(o => o.Id).Concat(run.State.Shop.Consumables.Select(o => o.Id)).ToList();

            bool changed = false;
            for (int i = 0; i < 5 && !changed; i++)
            {
                run.Reroll();
                var after = run.State.Shop.Relics.Select(o => o.Id).Concat(run.State.Shop.Consumables.Select(o => o.Id)).ToList();
                changed = !after.SequenceEqual(before);
            }
            Assert.IsTrue(changed);
            Assert.AreSame(pack, run.State.Shop.Pack);
        }

        [Test]
        public void Buying_RespectsSlotLimits()
        {
            RunEngine run = InShop();
            run.State.Money = 100;
            for (int i = 0; i < 5; i++)
                run.State.Relics.Add(new RelicInstance(RelicIds.FichaGasta));
            Assert.AreEqual("Sem espaço de amuleto.", run.CheckBuyRelic(0));

            run.State.Consumables.Add(new ConsumableInstance(TabuaIds.Pleno));
            run.State.Consumables.Add(new ConsumableInstance(TabuaIds.Pleno));
            Assert.AreEqual("Sem espaço de consumível.", run.CheckBuyConsumable(0));
        }

        [Test]
        public void MoveRelic_ReordersResolution()
        {
            RunEngine run = RunTestUtil.New();
            run.State.Relics.Add(new RelicInstance(RelicIds.FichaGasta));
            run.State.Relics.Add(new RelicInstance(RelicIds.MiraLaser));
            run.MoveRelic(1, 0);
            CollectionAssert.AreEqual(new[] { RelicIds.MiraLaser, RelicIds.FichaGasta }, run.State.Relics.Select(r => r.Id));
        }

        // ── Pacotes ────────────────────────────────────────────────────

        [TestCase(PackIds.Tabuas, 3)]
        [TestCase(PackIds.Casa, 3)]
        [TestCase(PackIds.Amuletos, 2)]
        public void Pack_OptionsAreDistinct(string packId, int options)
        {
            var rng = new Pcg32(4, 4);
            for (int i = 0; i < 200; i++)
            {
                PackOpening pack = ShopGenerator.OpenPack(packId, EmptyRun(), B, rng);
                Assert.AreEqual(options, pack.Options.Count);
                Assert.AreEqual(options, pack.Options.Distinct().Count());
            }
        }

        [Test]
        public void RelicPack_ExcludesOwnedAndOnSale()
        {
            RunState run = EmptyRun();
            run.Relics.Add(new RelicInstance(RelicIds.Teorema));
            run.Shop = new ShopState();
            run.Shop.Relics.Add(new ShopOffer(RelicIds.MiraLaser, 6));
            run.Shop.Relics.Add(new ShopOffer(RelicIds.RosaRubra, 4) { Sold = true });

            var rng = new Pcg32(6, 1);
            var seen = new HashSet<string>();
            for (int i = 0; i < 300; i++)
                seen.UnionWith(ShopGenerator.OpenPack(PackIds.Amuletos, run, B, rng).Options);
            CollectionAssert.DoesNotContain(seen, RelicIds.Teorema);
            CollectionAssert.DoesNotContain(seen, RelicIds.MiraLaser);
            CollectionAssert.Contains(seen, RelicIds.RosaRubra, "já vendido não está mais à venda");
        }

        static RunEngine WithPack(string packId)
        {
            RunEngine run = InShop();
            run.State.Money = 100;
            run.State.Shop.Pack = new ShopOffer(packId, B.Shop.Packs[packId].Price);
            run.BuyPack();
            return run;
        }

        [Test]
        public void TabuaPack_ChosenTabuaIsUsedImmediately()
        {
            RunEngine run = WithPack(PackIds.Tabuas);
            Assert.AreEqual(96, run.State.Money);
            string chosen = run.State.Shop.OpenPack.Options[1];
            run.ChoosePackOption(1);
            Assert.AreEqual(2, run.State.Levels.Get(TabuaIds.BetTypes[chosen]));
            Assert.IsEmpty(run.State.Consumables, "não ocupa espaço de consumível");
            Assert.IsNull(run.State.Shop.OpenPack);
        }

        [Test]
        public void HousePack_ChosenCardIsUsedImmediatelyWithTargets()
        {
            RunEngine run = WithPack(PackIds.Casa);
            run.State.Shop.OpenPack.Options[0] = HouseCardIds.Ourives;
            Assert.AreEqual("Escolha 1 casa(s).", run.CheckChoosePackOption(0));
            run.ChoosePackOption(0, CardTargets.Of(5));
            Assert.AreEqual(PocketEnhancement.Gold, run.State.Wheel[5].Enhancement);
        }

        [Test]
        public void RelicPack_NeedsAFreeSlot_ButCanBeSkipped()
        {
            RunEngine run = WithPack(PackIds.Amuletos);
            for (int i = 0; i < 5; i++)
                run.State.Relics.Add(new RelicInstance(RelicIds.FichaGasta));
            Assert.AreEqual("Sem espaço de amuleto.", run.CheckChoosePackOption(0));
            Assert.AreEqual("Escolha um item do pacote ou pule.", run.CheckLeaveShop());

            run.SkipPack();
            Assert.IsNull(run.CheckLeaveShop());
        }

        [Test]
        public void RelicPack_ChosenRelicGoesToTheNextSlot()
        {
            RunEngine run = WithPack(PackIds.Amuletos);
            string chosen = run.State.Shop.OpenPack.Options[0];
            run.ChoosePackOption(0);
            Assert.AreEqual(chosen, run.State.Relics.Last().Id);
        }

        [Test]
        public void OpenPack_BlocksOtherShopActions()
        {
            RunEngine run = WithPack(PackIds.Tabuas);
            Assert.AreEqual("Não está na loja.", run.CheckReroll());
            Assert.AreEqual("Não está na loja.", run.CheckBuyRelic(0));
        }
    }
}
