using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Pleno.Core.Content;
using Pleno.Core.Flow;
using Pleno.Core.Model;
using Pleno.Game.Save;
using Pleno.Sim;
using Pleno.Sim.Planning;
using Pleno.Tests.Core;

namespace Pleno.Tests.Sim
{
    public sealed class BotTests
    {
        const string MasterSeed = "TESTSEED";

        static IEnumerable<BotKind> AllBots => Bot.All;

        static bool Finished(RunEngine run) => run.State.Phase == RunPhase.Won || run.State.Phase == RunPhase.Lost;

        /// <summary>Metas 100× maiores: os bots perdem cedo, e o caminho da derrota também é exercitado.</summary>
        static Balance Harsh()
        {
            Balance balance = Balance.Default;
            foreach (double[] ante in balance.Run.Targets)
                for (int i = 0; i < ante.Length; i++)
                    ante[i] *= 100;
            return balance;
        }

        // ── Legalidade ─────────────────────────────────────────────────

        [Test]
        public void Bot_NeverActsIllegally([ValueSource(nameof(AllBots))] BotKind kind)
        {
            foreach (string seed in SimBatch.Seeds(MasterSeed, 6))
            {
                RunEngine run = null;
                Assert.DoesNotThrow(() => SimRunner.Play(Bot.Create(kind), seed, Balance.Default, out run), seed);
                Assert.IsTrue(Finished(run), seed);
            }
        }

        [Test]
        public void Bot_NeverActsIllegally_WhenLosing([ValueSource(nameof(AllBots))] BotKind kind)
        {
            foreach (string seed in SimBatch.Seeds(MasterSeed, 4))
            {
                RunEngine run = null;
                Assert.DoesNotThrow(() => SimRunner.Play(Bot.Create(kind), seed, Harsh(), out run), seed);
                Assert.AreEqual(RunPhase.Lost, run.State.Phase, seed);
            }
        }

        [Test]
        public void Bot_NeverActsIllegally_AgainstEachBoss(
            [ValueSource(nameof(AllBots))] BotKind kind, [ValueSource(typeof(BossIds), nameof(BossIds.All))] string boss)
        {
            RunEngine run = RunEngine.NewRun("CHEFE234", StartingWheel.European);
            for (int i = 0; i < run.State.BossPlan.Count; i++)
                run.State.BossPlan[i] = boss;

            RunRecord record = null;
            Assert.DoesNotThrow(() => record = SimRunner.Play(Bot.Create(kind), run));
            Assert.IsTrue(Finished(run));
            Assert.IsTrue(record.Mesas.Where(m => m.BossId != null).All(m => m.BossId == boss));
        }

        // ── Determinismo ───────────────────────────────────────────────

        [Test]
        public void SameSeed_SameRun([ValueSource(nameof(AllBots))] BotKind kind)
        {
            foreach (string seed in SimBatch.Seeds(MasterSeed, 2))
            {
                RunRecord a = SimRunner.Play(Bot.Create(kind), seed, Balance.Default, out RunEngine runA);
                RunRecord b = SimRunner.Play(Bot.Create(kind), seed, Balance.Default, out RunEngine runB);

                Assert.AreEqual(RunSerializer.Serialize(runA.State), RunSerializer.Serialize(runB.State), seed);
                Assert.AreEqual(Digest(a), Digest(b), seed);
            }
        }

        [Test]
        public void Batch_IsTheSameInParallelAndSerial()
        {
            var serial = new SimConfig { Runs = 4, MasterSeed = MasterSeed, Threads = 1 };
            var parallel = new SimConfig { Runs = 4, MasterSeed = MasterSeed, Threads = 4 };

            Assert.AreEqual(SimReport.Runs(SimBatch.Run(serial)), SimReport.Runs(SimBatch.Run(parallel)));
        }

        [Test]
        public void Seeds_AreFixedValidAndDistinct()
        {
            List<string> seeds = SimBatch.Seeds(SimConfig.DefaultMasterSeed, 800);

            CollectionAssert.AreEqual(seeds, SimBatch.Seeds(SimConfig.DefaultMasterSeed, 800));
            Assert.IsTrue(seeds.All(Pleno.Core.Rng.RunSeed.IsValid));
            Assert.AreEqual(800, seeds.Distinct().Count());
        }

        static string Digest(RunRecord r) =>
            $"{r.Won}|{r.AntesCleared}|{r.NudgesUsed}|{r.HouseCardsUsed}|{r.TabuasUsed}|{r.Rerolls}|{r.FinalMoney}|"
            + string.Join(",", r.FinalRelics) + "|"
            + string.Join(";", r.Mesas.Select(m => $"{m.Ante}.{m.Index}:{string.Join("/", m.Spins)}"));

        // ── Os bots simulam em cópias ──────────────────────────────────

        [Test]
        public void Planning_DoesNotTouchTheRun()
        {
            RunEngine run = RunTestUtil.New();
            RunState state = run.State;
            foreach (string id in new[] { RelicIds.Martingale, RelicIds.Colecionador, RelicIds.Teorema, RelicIds.LuvaDeSeda })
                state.Relics.Add(new RelicInstance(id));
            for (int i = 0; i < state.Wheel.Count; i += 3)
                state.Wheel[i].Enhancement = i % 2 == 0 ? PocketEnhancement.Glass : PocketEnhancement.Lucky;
            string before = RunSerializer.Serialize(state);

            var luck = Pleno.Core.Rng.RngStreams.Derive(state.Seed, "test-eval");
            var ev = SpinEvaluator.For(run, luck);
            BetPlan plan = BetPlanner.Plan(ev, BetCatalog.All(ev.Wheel), 5, 5, 200, 1);
            ev.Scores(plan.Bets);
            var app = new Appraiser(run, plan.Bets, 600, 1, luck);
            foreach (string card in HouseCardIds.All)
                app.Card(card);
            foreach (string tabua in TabuaIds.All)
                app.TabuaGain(tabua);
            foreach (string relic in RelicCatalog.Rarities.Keys)
                app.RelicGain(relic);
            app.RelicWorth(0);
            app.BestOrder();

            Assert.AreEqual(before, RunSerializer.Serialize(state));
        }

        [Test]
        public void Evaluator_PredictsTheRealSpin()
        {
            RunEngine run = RunTestUtil.New();
            run.State.Relics.Add(new RelicInstance(RelicIds.Teorema));
            var bets = new List<PlacedBet> { PlacedBet.Red(2), PlacedBet.Dozen(2, 2), PlacedBet.Straight(19, 1) };
            var ev = SpinEvaluator.For(run, Pleno.Core.Rng.RngStreams.Derive(run.State.Seed, "test-eval"));
            double predicted = ev.Score(bets, RunTestUtil.IndexOf(run, 19));

            double real = RunTestUtil.SpinOn(run, 19, bets.ToArray()).Total;

            Assert.AreEqual(1683, real, "regressão do GDD 3.4");
            Assert.AreEqual(real, predicted);
        }

        // ── Estratégia ─────────────────────────────────────────────────

        [Test]
        public void NudgePlanner_PushesTheBallOntoTheWinningPocket()
        {
            RunEngine run = RunTestUtil.New();
            var bets = new List<PlacedBet> { PlacedBet.Straight(19, 5) };
            var ev = SpinEvaluator.For(run, Pleno.Core.Rng.RngStreams.Derive(run.State.Seed, "test-eval"));
            int nineteen = RunTestUtil.IndexOf(run, 19);

            Assert.AreEqual(1, NudgePlanner.Offset(ev, bets, run.State.Wheel.Neighbor(nineteen, -1), 2, 4, 100, 0));
            Assert.AreEqual(-2, NudgePlanner.Offset(ev, bets, run.State.Wheel.Neighbor(nineteen, 2), 2, 4, 100, 0));
            Assert.AreEqual(0, NudgePlanner.Offset(ev, bets, run.State.Wheel.Neighbor(nineteen, 3), 2, 4, 100, 0));
            Assert.AreEqual(0, NudgePlanner.Offset(ev, bets, nineteen, 2, 4, 100, 0));
        }

        [Test]
        public void Planner_PlacesEveryChip_WithinTheBotsVocabulary()
        {
            RunEngine run = RunTestUtil.New();
            var luck = Pleno.Core.Rng.RngStreams.Derive(run.State.Seed, "test-eval");
            var ev = SpinEvaluator.For(run, luck);

            BetPlan greedy = BetPlanner.Plan(ev, BetCatalog.All(ev.Wheel), 5, 5, 100, 1);
            BetPlan cover = BetPlanner.Plan(ev, BetCatalog.Outside(), 5, 1, 100, 1);
            BetPlan sniper = BetPlanner.Plan(ev, BetCatalog.Straights().Concat(BetCatalog.Neighbors(ev.Wheel)).ToList(), 5, 5, 100, 1);

            Assert.AreEqual(5, greedy.Bets.Sum(b => b.Chips));
            Assert.AreEqual(5, cover.Bets.Count);
            Assert.IsTrue(cover.Bets.All(b => b.Type.IsOutside() && b.Chips == 1));
            Assert.AreEqual(5, sniper.Bets.Sum(b => b.Chips));
            Assert.IsTrue(sniper.Bets.All(b => b.Type == BetType.Straight || b.Type == BetType.Neighbors));
        }

        [Test]
        public void GreedyAndSniper_UseNudgesAndHouseCards([Values(BotKind.Guloso, BotKind.Sniper)] BotKind kind)
        {
            List<RunRecord> runs = SimBatch.Seeds(MasterSeed, 6).Select(seed => SimRunner.Play(kind, seed)).ToList();

            Assert.Greater(runs.Sum(r => r.NudgesUsed), 0, "empurrões");
            Assert.Greater(runs.Sum(r => r.HouseCardsUsed), 0, "Cartas da Casa");
        }

        // ── Relatório ──────────────────────────────────────────────────

        [Test]
        public void Report_P90_UsesNearestRank()
        {
            Assert.AreEqual(9, SimReport.P90(Enumerable.Range(1, 10).Select(i => (double)i)));
            Assert.AreEqual(5, SimReport.P90(new double[] { 5 }));
            Assert.AreEqual(0, SimReport.P90(new double[0]));
        }

        [Test]
        public void Report_WritesEveryRelicForEveryBot()
        {
            var config = new SimConfig { Runs = 2, MasterSeed = MasterSeed, Bots = new List<BotKind> { BotKind.Cobertura } };
            string relics = SimReport.Relics(SimBatch.Run(config));

            string[] lines = relics.TrimEnd('\n').Split('\n');
            Assert.AreEqual(1 + RelicCatalog.Rarities.Count, lines.Length);
            StringAssert.StartsWith("bot,relic,rarity,offered_shops,bought,buy_rate", lines[0]);
        }
    }
}
