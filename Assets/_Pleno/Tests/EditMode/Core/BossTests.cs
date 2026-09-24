using System.Linq;
using NUnit.Framework;
using Pleno.Core.Content;
using Pleno.Core.Effects;
using Pleno.Core.Effects.Relics;
using Pleno.Core.Events;
using Pleno.Core.Flow;
using Pleno.Core.Model;

namespace Pleno.Tests.Core
{
    public sealed class BossTests
    {
        [Test]
        public void Registry_HasTheSixDemoBosses()
        {
            CollectionAssert.AreEquivalent(BossIds.All, BossRuleRegistry.Ids);
        }

        [Test]
        public void BossPlan_HasOneDistinctBossPerAnte_AndDependsOnlyOnTheSeed()
        {
            RunEngine a = RunTestUtil.New(seed: "ABCD2345");
            RunEngine b = RunTestUtil.New(seed: "ABCD2345");
            Assert.AreEqual(3, a.State.BossPlan.Count);
            Assert.AreEqual(3, a.State.BossPlan.Distinct().Count());
            CollectionAssert.AreEqual(a.State.BossPlan, b.State.BossPlan);

            var plans = new[] { "AAAAAAAA", "BBBBBBBB", "CCCCCCCC", "DDDDDDDD", "EEEEEEEE" }
                .Select(s => string.Join(",", RunTestUtil.New(seed: s).State.BossPlan)).Distinct().Count();
            Assert.That(plans, Is.GreaterThan(1), "seeds diferentes sorteiam chefes diferentes");
        }

        [Test]
        public void OnlyTheBossMesaHasABoss()
        {
            RunEngine run = RunTestUtil.New();
            Assert.IsNull(run.State.Mesa.BossId);
            Assert.AreEqual(MesaKind.Small, run.State.Mesa.Kind);
            Assert.AreEqual(MesaKind.Boss, RunTestUtil.AtBoss(BossIds.Nevoa).State.Mesa.Kind);
        }

        // ── O Duplo Zero ───────────────────────────────────────────────

        [Test]
        public void DuploZero_AddsATemporaryDoubleZeroOppositeTheZero()
        {
            RunEngine run = RunTestUtil.AtBoss(BossIds.DuploZero);
            Wheel wheel = run.State.Wheel;

            Assert.AreEqual(38, wheel.Count);
            int zero = RunTestUtil.IndexOf(run, 0);
            int doubleZero = RunTestUtil.IndexOf(run, Pocket.DoubleZero);
            Assert.AreEqual(19, wheel.CircularDistance(zero, doubleZero), "meia volta de distância");
            Assert.AreEqual(PocketColor.Green, wheel[doubleZero].Color);
            CollectionAssert.AreEqual(new[] { wheel[doubleZero].Id }, run.State.Mesa.TemporaryPocketIds);
        }

        [Test]
        public void DuploZero_MakesOutsideBetsLose()
        {
            RunEngine run = RunTestUtil.AtBoss(BossIds.DuploZero);
            SpinResult r = RunTestUtil.SpinOn(run, Pocket.DoubleZero, PlacedBet.Red(2), PlacedBet.Dozen(1, 2), PlacedBet.Straight(Pocket.DoubleZero, 1));
            CollectionAssert.AreEqual(new[] { "bet:pleno:00" }, r.WinningBets.Select(b => b.SourceId));
        }

        [Test]
        public void DuploZero_PocketLeavesWhenTheMesaEnds()
        {
            RunEngine won = RunTestUtil.AtBoss(BossIds.DuploZero);
            int[] idsBefore = WheelIdsWithoutTemporary(won);
            RunTestUtil.WinMesa(won);
            Assert.AreEqual(37, won.State.Wheel.Count);
            CollectionAssert.AreEqual(idsBefore, won.State.Wheel.Pockets.Select(p => p.Id));

            RunEngine lost = RunTestUtil.AtBoss(BossIds.DuploZero);
            while (lost.State.Phase == RunPhase.Mesa)
                RunTestUtil.SpinOn(lost, 20, PlacedBet.Red(1));
            Assert.AreEqual(RunPhase.Lost, lost.State.Phase);
            Assert.AreEqual(37, lost.State.Wheel.Count);
        }

        static int[] WheelIdsWithoutTemporary(RunEngine run) =>
            run.State.Wheel.Pockets.Where(p => !run.State.Mesa.TemporaryPocketIds.Contains(p.Id)).Select(p => p.Id).ToArray();

        // ── A Névoa ────────────────────────────────────────────────────

        [Test]
        public void Nevoa_OnlyHidesNumbers()
        {
            RunEngine run = RunTestUtil.AtBoss(BossIds.Nevoa);
            Assert.IsTrue(run.Boss.HidesWheelNumbers);
            Assert.IsFalse(run.Boss.NudgesDisabled);
            Assert.AreEqual(4, run.State.Mesa.SpinsLeft);
            Assert.AreEqual(2, run.State.Mesa.NudgesLeft);
            Assert.AreEqual(345, RunTestUtil.SpinOn(run, 19, PlacedBet.Straight(19, 1)).Total, "(50 + 19) × 5, sem alteração");
        }

        // ── O Contador ─────────────────────────────────────────────────

        [Test]
        public void Contador_StraightBetsCountAsLost()
        {
            RunEngine run = RunTestUtil.AtBoss(BossIds.Contador);
            run.State.Relics.Add(new RelicInstance(RelicIds.MiraLaser));
            SpinResult r = RunTestUtil.SpinOn(run, 19, PlacedBet.Straight(19, 4), PlacedBet.Red(1));

            CollectionAssert.AreEqual(new[] { "bet:vermelho" }, r.WinningBets.Select(b => b.SourceId));
            Assert.AreEqual((5 + 19) * 1, r.Total, "só o Vermelho pontua; a Mira Laser não ativa");
        }

        [Test]
        public void Contador_SpinWithOnlyStraightWinningScoresZero_AndMartingaleStacks()
        {
            RunEngine run = RunTestUtil.AtBoss(BossIds.Contador);
            run.State.Relics.Add(new RelicInstance(RelicIds.Martingale));
            SpinResult r = RunTestUtil.SpinOn(run, 19, PlacedBet.Straight(19, 5));
            Assert.IsFalse(r.IsWin);
            Assert.AreEqual(0, r.Total);
            Assert.AreEqual(3, run.State.Relics[0].GetCounter(Martingale.StackKey));
        }

        [Test]
        public void Contador_DoesNotAffectOtherMesas()
        {
            RunEngine run = RunTestUtil.New();
            run.State.BossPlan[0] = BossIds.Contador;
            Assert.AreEqual(1345, RunTestUtil.SpinOn(run, 19, PlacedBet.Straight(19, 5)).Total);
        }

        // ── Mão Pesada ─────────────────────────────────────────────────

        [Test]
        public void MaoPesada_DisablesNudgesEvenWithExtraNudges()
        {
            RunEngine run = RunTestUtil.New(StartingWheel.American);
            run.State.Relics.Add(new RelicInstance(RelicIds.LuvaDeSeda));
            run.State.BossPlan[0] = BossIds.MaoPesada;
            run.State.MesaIndex = 2;
            run.StartMesa();

            Assert.AreEqual(0, run.State.Mesa.NudgesLeft);
            run.Place(PlacedBet.Red(1));
            run.Spin();
            Assert.AreEqual("O Crupiê não deixa empurrar.", run.CheckNudge(1));
            Assert.Throws<System.InvalidOperationException>(() => run.Nudge(1));
        }

        // ── O Daltônico ────────────────────────────────────────────────

        [Test]
        public void Daltonico_ColorBetsCountAsLost_OtherOutsideBetsStillScore()
        {
            RunEngine run = RunTestUtil.AtBoss(BossIds.Daltonico);
            run.State.Relics.Add(new RelicInstance(RelicIds.RosaRubra));
            SpinResult r = RunTestUtil.SpinOn(run, 19, PlacedBet.Red(2), PlacedBet.Odd(2), PlacedBet.Black(1));

            CollectionAssert.AreEqual(new[] { "bet:impar" }, r.WinningBets.Select(b => b.SourceId));
            Assert.AreEqual((10 + 19) * 1, r.Total, "a Rosa Rubra não ativa");
        }

        // ── O Relojoeiro ───────────────────────────────────────────────

        [Test]
        public void Relojoeiro_OnlyThreeSpins()
        {
            RunEngine run = RunTestUtil.AtBoss(BossIds.Relojoeiro);
            Assert.AreEqual(3, run.State.Mesa.SpinsLeft);

            for (int i = 0; i < 3; i++)
                RunTestUtil.SpinOn(run, 20, PlacedBet.Red(1));
            Assert.AreEqual(RunPhase.Lost, run.State.Phase);
            Assert.AreEqual(3, run.State.Stats.SpinsPlayed);
        }
    }
}
