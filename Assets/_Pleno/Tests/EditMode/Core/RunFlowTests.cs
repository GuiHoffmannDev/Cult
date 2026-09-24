using System.Linq;
using NUnit.Framework;
using Pleno.Core.Content;
using Pleno.Core.Events;
using Pleno.Core.Flow;
using Pleno.Core.Model;
using Pleno.Core.Rng;

namespace Pleno.Tests.Core
{
    public sealed class RunFlowTests
    {
        [Test]
        public void NewRun_StartsAtSmallMesaOfAnte1()
        {
            RunState s = RunTestUtil.New().State;
            Assert.AreEqual(RunPhase.Mesa, s.Phase);
            Assert.AreEqual(1, s.Ante);
            Assert.AreEqual(4, s.Money);
            Assert.AreEqual(100, s.Mesa.Target);
            Assert.AreEqual(4, s.Mesa.SpinsLeft);
            Assert.AreEqual(5, s.Mesa.Resources.ChipsPerSpin);
            Assert.AreEqual(2, s.Mesa.NudgesLeft);
            Assert.AreEqual(37, s.Wheel.Count);
        }

        [Test]
        public void FullRun_WalksThroughNineMesasWithTheRightTargets()
        {
            RunEngine run = RunTestUtil.New();
            run.State.BossPlan = new[] { BossIds.DuploZero, BossIds.Nevoa, BossIds.Relojoeiro }.ToList();
            double[] expected = { 100, 150, 200, 300, 450, 600, 800, 1200, 1600 };

            for (int m = 0; m < expected.Length; m++)
            {
                Assert.AreEqual(RunPhase.Mesa, run.State.Phase, $"mesa {m}");
                Assert.AreEqual(expected[m], run.State.Mesa.Target, $"meta da mesa {m}");
                Assert.AreEqual(m / 3 + 1, run.State.Ante);
                Assert.AreEqual(m % 3 == 2 ? MesaKind.Boss : m % 3 == 0 ? MesaKind.Small : MesaKind.Big, run.State.Mesa.Kind);
                RunTestUtil.WinMesa(run);
                if (m < expected.Length - 1)
                {
                    Assert.AreEqual(RunPhase.Shop, run.State.Phase);
                    run.LeaveShop();
                }
            }
            Assert.AreEqual(RunPhase.Won, run.State.Phase);
            Assert.AreEqual(9, run.State.Stats.MesasWon);
            Assert.IsNull(run.State.Shop, "vencer a última mesa não abre loja");
        }

        [Test]
        public void MesaIsWonTheMomentTheTargetIsReached()
        {
            RunEngine run = RunTestUtil.New();
            RunTestUtil.SpinOn(run, 20, PlacedBet.Black(1));      // 25 pontos
            Assert.AreEqual(RunPhase.Mesa, run.State.Phase);
            RunTestUtil.SpinOn(run, 19, PlacedBet.Straight(19, 1)); // + 345
            Assert.AreEqual(RunPhase.Shop, run.State.Phase);
            Assert.AreEqual(2, run.State.Mesa.Payout.UnusedSpins);
        }

        [Test]
        public void RunIsLostWhenSpinsRunOut()
        {
            RunEngine run = RunTestUtil.New();
            for (int i = 0; i < 4; i++)
            {
                Assert.AreEqual(RunPhase.Mesa, run.State.Phase);
                RunTestUtil.SpinOn(run, 20, PlacedBet.Red(1));
            }
            Assert.AreEqual(RunPhase.Lost, run.State.Phase);
            Assert.AreEqual(MesaPhase.Lost, run.State.Mesa.Phase);
            Assert.AreEqual("Fora da fase de apostas.", run.CheckSpin());
        }

        [Test]
        public void Chips_AreLimitedPerSpin_StackOnTheSameBet_AndDoNotCarryOver()
        {
            RunEngine run = RunTestUtil.New();
            run.Place(PlacedBet.Red(2));
            run.Place(PlacedBet.Straight(19, 1));
            run.Place(PlacedBet.Red(1));
            CollectionAssert.AreEqual(new[] { "bet:vermelho", "bet:pleno:19" }, run.State.Mesa.Bets.Select(b => b.SourceId),
                "a aposta mantém a posição da primeira ficha");
            Assert.AreEqual(3, run.State.Mesa.Bets[0].Chips);
            Assert.AreEqual(1, run.State.Mesa.ChipsFree);
            Assert.AreEqual("Só restam 1 ficha(s) neste giro.", run.CheckPlace(PlacedBet.Black(2)));

            run.Remove("bet:vermelho", 3);
            CollectionAssert.AreEqual(new[] { "bet:pleno:19" }, run.State.Mesa.Bets.Select(b => b.SourceId));

            RunTestUtil.Force(run, 20);
            run.Spin();
            run.ConfirmSpin();
            Assert.IsEmpty(run.State.Mesa.Bets);
            Assert.AreEqual(5, run.State.Mesa.ChipsFree, "fichas não usadas não acumulam");
        }

        [Test]
        public void Spin_NeedsAtLeastOneChip()
        {
            RunEngine run = RunTestUtil.New();
            Assert.AreEqual("Coloque pelo menos uma ficha.", run.CheckSpin());
        }

        [Test]
        public void Nudge_MovesTheBallAlongTheWheelBeforeScoring()
        {
            RunEngine run = RunTestUtil.New();
            run.Place(PlacedBet.Straight(17, 5));
            RunTestUtil.Force(run, 25); // o 17 fica entre o 25 e o 34
            run.Spin();
            Assert.AreEqual(25, run.State.Wheel[run.State.Mesa.BallIndex].Number);

            run.Nudge(+1);
            Assert.AreEqual(17, run.State.Wheel[run.State.Mesa.BallIndex].Number);
            Assert.AreEqual(1, run.State.Mesa.NudgesLeft);

            SpinResult r = run.ConfirmSpin();
            Assert.IsTrue(r.IsWin);
            Assert.AreEqual(run.State.Wheel.Pockets.ToList().FindIndex(p => p.Number == 17), r.PocketIndex);
        }

        [Test]
        public void Nudge_BudgetIsPerMesa()
        {
            RunEngine run = RunTestUtil.New();
            run.Place(PlacedBet.Red(1));
            RunTestUtil.Force(run, 0);
            run.Spin();
            run.Nudge(-1);
            run.Nudge(+1);
            Assert.AreEqual("Sem empurrões nesta mesa.", run.CheckNudge(1));
            Assert.AreEqual("O empurrão move a bola 1 casa para um dos lados.", run.CheckNudge(2));
            run.ConfirmSpin();

            run.Place(PlacedBet.Red(1));
            run.Spin();
            Assert.AreEqual("Sem empurrões nesta mesa.", run.CheckNudge(1), "não recarrega entre giros");
        }

        [Test]
        public void LuvaDeSeda_GivesAThirdNudgeFromTheNextMesa()
        {
            RunEngine run = RunTestUtil.New();
            RunTestUtil.WinMesa(run);
            run.State.Relics.Add(new RelicInstance(RelicIds.LuvaDeSeda));
            run.LeaveShop();
            Assert.AreEqual(3, run.State.Mesa.NudgesLeft);
        }

        // ── Rodas iniciais ─────────────────────────────────────────────

        [Test]
        public void AmericanWheel_Has38PocketsAndOneExtraNudge()
        {
            RunState s = RunTestUtil.New(StartingWheel.American).State;
            Assert.AreEqual(38, s.Wheel.Count);
            Assert.AreEqual(3, s.Mesa.NudgesLeft);
            Assert.AreEqual(5, s.Mesa.Resources.ChipsPerSpin);
        }

        [Test]
        public void GoldenWheel_HasThreeGoldPocketsAndOneLessChip()
        {
            RunState s = RunTestUtil.New(StartingWheel.Golden).State;
            Assert.AreEqual(37, s.Wheel.Count);
            Assert.AreEqual(3, s.Wheel.Pockets.Count(p => p.Enhancement == PocketEnhancement.Gold));
            Assert.AreEqual(4, s.Mesa.Resources.ChipsPerSpin);
            Assert.AreEqual(2, s.Mesa.NudgesLeft);
        }

        [Test]
        public void GoldenWheel_GoldPocketsDependOnlyOnTheSeed()
        {
            int[] Golds(string seed) => RunTestUtil.New(StartingWheel.Golden, seed).State.Wheel.Pockets
                .Where(p => p.Enhancement == PocketEnhancement.Gold).Select(p => p.Number).ToArray();
            CollectionAssert.AreEqual(Golds("ABCD2345"), Golds("ABCD2345"));
            Assert.That(new[] { "AAAAAAAA", "BBBBBBBB", "CCCCCCCC" }.Select(s => string.Join(",", Golds(s))).Distinct().Count(),
                Is.GreaterThan(1));
        }

        [Test]
        public void SameSeedAndSameActions_GiveTheSameRun()
        {
            RunEngine a = RunTestUtil.New(seed: "ZZZZ2222"), b = RunTestUtil.New(seed: "ZZZZ2222");
            for (int i = 0; i < 4 && a.State.Phase == RunPhase.Mesa; i++)
            {
                foreach (RunEngine run in new[] { a, b })
                {
                    run.Place(PlacedBet.Red(3));
                    run.Place(PlacedBet.Dozen(2, 2));
                }
                Assert.AreEqual(a.Spin().LandingIndex, b.Spin().LandingIndex);
                Assert.AreEqual(a.ConfirmSpin().Total, b.ConfirmSpin().Total);
            }
            Assert.AreEqual(a.State.Mesa.Score, b.State.Mesa.Score);
            Assert.AreEqual(a.State.Money, b.State.Money);
        }

        [Test]
        public void SeedFromEntropy_StartsAValidRun()
        {
            string seed = RunSeed.FromEntropy(123456789UL);
            Assert.AreEqual(seed, RunEngine.NewRun(seed, StartingWheel.European).State.Seed);
        }
    }
}
