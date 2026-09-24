using System.Linq;
using NUnit.Framework;
using Pleno.Core.Content;
using Pleno.Core.Flow;
using Pleno.Core.Model;

namespace Pleno.Tests.Core
{
    public sealed class HouseCardTests
    {
        static readonly Balance B = Balance.Default;

        static RunEngine WithCard(string id)
        {
            RunEngine run = RunTestUtil.New();
            run.State.Consumables.Add(new ConsumableInstance(id));
            return run;
        }

        static Wheel W(RunEngine run) => run.State.Wheel;

        [TestCase(HouseCardIds.Ourives, PocketEnhancement.Gold)]
        [TestCase(HouseCardIds.Ferreiro, PocketEnhancement.Steel)]
        [TestCase(HouseCardIds.Ima, PocketEnhancement.Magnetic)]
        public void SingleTargetEnhancers(string id, PocketEnhancement expected)
        {
            RunEngine run = WithCard(id);
            Assert.AreEqual("Escolha 1 casa(s).", run.CheckUseConsumable(0, CardTargets.Of(1, 2)));
            run.UseConsumable(0, CardTargets.Of(7));
            Assert.AreEqual(expected, W(run)[7].Enhancement);
            Assert.IsEmpty(run.State.Consumables, "a carta é gasta");
        }

        [TestCase(HouseCardIds.Vidraceiro, PocketEnhancement.Glass)]
        [TestCase(HouseCardIds.Trevo, PocketEnhancement.Lucky)]
        public void UpToTwoEnhancers(string id, PocketEnhancement expected)
        {
            RunEngine run = WithCard(id);
            Assert.AreEqual("Escolha de 1 a 2 casas.", run.CheckUseConsumable(0, CardTargets.Of(1, 2, 3)));
            Assert.AreEqual("Cada casa só pode ser escolhida uma vez.", run.CheckUseConsumable(0, CardTargets.Of(4, 4)));
            run.UseConsumable(0, CardTargets.Of(4, 9));
            Assert.AreEqual(expected, W(run)[4].Enhancement);
            Assert.AreEqual(expected, W(run)[9].Enhancement);
            Assert.AreEqual(PocketEnhancement.None, W(run)[5].Enhancement);
        }

        [Test]
        public void Enhancing_RepairsACrackedPocket()
        {
            RunEngine run = WithCard(HouseCardIds.Ourives);
            W(run)[3].Cracked = true;
            run.UseConsumable(0, CardTargets.Of(3));
            Assert.IsFalse(W(run)[3].Cracked);
            Assert.AreEqual(PocketEnhancement.Gold, W(run)[3].Enhancement);
        }

        [Test]
        public void Pintor_FlipsUpToThreeAndRejectsGreen()
        {
            RunEngine run = WithCard(HouseCardIds.Pintor);
            int i19 = RunTestUtil.IndexOf(run, 19), i20 = RunTestUtil.IndexOf(run, 20), i0 = RunTestUtil.IndexOf(run, 0);
            Assert.AreEqual("O Pintor não pinta casas verdes.", run.CheckUseConsumable(0, CardTargets.Of(i19, i0)));
            Assert.AreEqual("Escolha de 1 a 3 casas.", run.CheckUseConsumable(0, CardTargets.Of(1, 2, 3, 4)));
            run.UseConsumable(0, CardTargets.Of(i19, i20));
            Assert.AreEqual(PocketColor.Black, W(run)[i19].Color);
            Assert.AreEqual(PocketColor.Red, W(run)[i20].Color);
        }

        [Test]
        public void Gemeo_CopiesNumberAndColorOnly()
        {
            RunEngine run = WithCard(HouseCardIds.Gemeo);
            int i17 = RunTestUtil.IndexOf(run, 17), i19 = RunTestUtil.IndexOf(run, 19);
            W(run)[i19].Enhancement = PocketEnhancement.Bonus;
            W(run)[i17].Enhancement = PocketEnhancement.Gold;

            Assert.AreEqual("Escolha a casa a copiar.", run.CheckUseConsumable(0, CardTargets.Of(i19)));
            Assert.AreEqual("A casa copiada precisa ser outra.", run.CheckUseConsumable(0, CardTargets.Copy(i19, i19)));
            run.UseConsumable(0, CardTargets.Copy(source: i17, target: i19));

            Assert.AreEqual(17, W(run)[i19].Number);
            Assert.AreEqual(PocketColor.Black, W(run)[i19].Color);
            Assert.AreEqual(PocketEnhancement.Bonus, W(run)[i19].Enhancement, "o aprimoramento do alvo fica");
            Assert.AreEqual(2, W(run).Pockets.Count(p => p.Number == 17));
        }

        [Test]
        public void Coveiro_RemovesUpToTwoRespectingTheMinimum()
        {
            RunEngine run = WithCard(HouseCardIds.Coveiro);
            int id5 = W(run)[5].Id, id30 = W(run)[30].Id;
            run.UseConsumable(0, CardTargets.Of(30, 5));
            Assert.AreEqual(35, W(run).Count);
            Assert.AreEqual(-1, W(run).IndexOfId(id5));
            Assert.AreEqual(-1, W(run).IndexOfId(id30));

            while (W(run).Count > 19)
                W(run).RemoveAt(0, 18);
            run.State.Consumables.Add(new ConsumableInstance(HouseCardIds.Coveiro));
            Assert.AreEqual("A roda não pode ter menos de 18 casas.", run.CheckUseConsumable(0, CardTargets.Of(1, 2)));
            Assert.IsNull(run.CheckUseConsumable(0, CardTargets.Of(1)));
        }

        [Test]
        public void Artesao_AddsAFullCopyNextToTheChosenPocket_UpTo48()
        {
            RunEngine run = WithCard(HouseCardIds.Artesao);
            W(run)[10].Enhancement = PocketEnhancement.Gold;
            W(run)[10].Edition = PocketEdition.Polished;
            run.UseConsumable(0, CardTargets.Of(10));

            Assert.AreEqual(38, W(run).Count);
            Assert.AreEqual(W(run)[10].Number, W(run)[11].Number);
            Assert.AreEqual(PocketEnhancement.Gold, W(run)[11].Enhancement);
            Assert.AreEqual(PocketEdition.Polished, W(run)[11].Edition);

            while (W(run).Count < 48)
                W(run).CloneAdjacent(0);
            run.State.Consumables.Add(new ConsumableInstance(HouseCardIds.Artesao));
            Assert.AreEqual("A roda não pode ter mais de 48 casas.", run.CheckUseConsumable(0, CardTargets.Of(0)));
        }

        [TestCase(0, 0)]
        [TestCase(7, 14)]
        [TestCase(20, 40)]
        [TestCase(35, 55)]
        public void Tesoureiro_DoublesMoneyCappedAtPlus20(int money, int after)
        {
            RunEngine run = WithCard(HouseCardIds.Tesoureiro);
            run.State.Money = money;
            run.UseConsumable(0);
            Assert.AreEqual(after, run.State.Money);
        }

        [Test]
        public void Oraculo_CreatesTwoTabuasUsingItsOwnFreedSlot()
        {
            RunEngine run = WithCard(HouseCardIds.Oraculo);
            run.UseConsumable(0);
            Assert.AreEqual(2, run.State.Consumables.Count);
            Assert.IsTrue(run.State.Consumables.All(c => Consumables.KindOf(c.Id) == ConsumableKind.Tabua));

            RunEngine crowded = WithCard(HouseCardIds.Oraculo);
            crowded.State.Consumables.Add(new ConsumableInstance(HouseCardIds.Ourives));
            crowded.UseConsumable(0);
            Assert.AreEqual(2, crowded.State.Consumables.Count, "só coube uma Tábua");
        }

        [Test]
        public void Fortuna_OneInFourGivesARandomEditionToARelicWithoutEdition()
        {
            RunState run = RunTestUtil.New().State;
            run.Relics.Add(new RelicInstance(RelicIds.FichaGasta) { Edition = PocketEdition.Holographic });
            run.Relics.Add(new RelicInstance(RelicIds.Teorema));

            // Roteiro: sorteio 1 em 4 falha (1).
            HouseCards.Apply(HouseCardIds.Fortuna, run, null, B, new ScriptedRandom().Ints(1));
            Assert.AreEqual(PocketEdition.None, run.Relics[1].Edition);

            // Acerta (0); só o Teorema é elegível (índice 0 da lista); edição de índice 2 = Policromática.
            HouseCards.Apply(HouseCardIds.Fortuna, run, null, B, new ScriptedRandom().Ints(0, 0, 2));
            Assert.AreEqual(PocketEdition.Polychrome, run.Relics[1].Edition);
            Assert.AreEqual(PocketEdition.Holographic, run.Relics[0].Edition, "não troca edição existente");
        }

        [Test]
        public void RelicEdition_ScoresRightAfterTheRelic()
        {
            RunEngine run = RunTestUtil.New();
            run.State.Relics.Add(new RelicInstance(RelicIds.FichaGasta) { Edition = PocketEdition.Polychrome });
            // (5 + 4) × 1,5 = 13,5 → (50 + 19) × 13,5 = 931,5 → 931
            Assert.AreEqual(931, RunTestUtil.SpinOn(run, 19, PlacedBet.Straight(19, 1)).Total);
        }

        [Test]
        public void Tabua_RaisesOneLevel()
        {
            RunEngine run = WithCard(TabuaIds.Pleno);
            run.UseConsumable(0);
            Assert.AreEqual(2, run.State.Levels.Get(BetType.Straight));
            // Pleno nível 2: 80 Pontos, 8 Mult → (80 + 19) × 8
            Assert.AreEqual(792, RunTestUtil.SpinOn(run, 19, PlacedBet.Straight(19, 1)).Total);
        }

        [Test]
        public void AllEightTabuasMapToDistinctBetTypes()
        {
            Assert.AreEqual(8, TabuaIds.All.Select(id => TabuaIds.BetTypes[id]).Distinct().Count());
        }

        [Test]
        public void BossTemporaryPocket_IsNotAValidTarget()
        {
            RunEngine run = RunTestUtil.AtBoss(BossIds.DuploZero);
            int dz = RunTestUtil.IndexOf(run, Pocket.DoubleZero);
            run.State.Consumables.Add(new ConsumableInstance(HouseCardIds.Artesao));
            run.State.Consumables.Add(new ConsumableInstance(HouseCardIds.Gemeo));
            Assert.AreEqual("A casa do chefe não pode ser alvo.", run.CheckUseConsumable(0, CardTargets.Of(dz)));
            Assert.AreEqual("A casa do chefe não pode ser copiada.", run.CheckUseConsumable(1, CardTargets.Copy(dz, 3)));
        }

        [Test]
        public void Consumables_CannotBeUsedWhileTheBallIsDown()
        {
            RunEngine run = WithCard(TabuaIds.Pleno);
            run.Place(PlacedBet.Red(1));
            run.Spin();
            Assert.AreEqual("Consumíveis só fora do giro.", run.CheckUseConsumable(0));
        }
    }
}
