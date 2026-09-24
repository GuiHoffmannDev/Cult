using System.Linq;
using NUnit.Framework;
using Pleno.Core.Content;
using Pleno.Core.Events;
using Pleno.Core.Model;
using Pleno.Core.Rules;

namespace Pleno.Tests.Core
{
    public sealed class ScoringPipelineTests
    {
        static PlacedBet[] GddExampleBets => new[] { PlacedBet.Red(2), PlacedBet.Dozen(2, 2), PlacedBet.Straight(19, 1) };

        static ScoreStep[] Scoring(SpinResult r) => r.Steps.TakeWhile(s => s.Kind != ScoreStepKind.Final).ToArray();

        [Test]
        public void Regression_GddExample_Is1683()
        {
            // GDD 3.4: 2 fichas no Vermelho, 2 na 2ª Dúzia, 1 no Pleno 19, cai o 19, com Teorema.
            SpinResult r = Spin.On(19, GddExampleBets, Spin.Relics(RelicIds.Teorema));

            Assert.AreEqual(1683, r.Total);
            Assert.AreEqual(99, r.Pontos);
            Assert.AreEqual(17, r.Mult);

            // A tabela do GDD, linha a linha: (Pontos, Mult) depois de cada passo.
            var expected = new (ScoreStepKind kind, string source, double pontos, double mult)[]
            {
                (ScoreStepKind.BetWon, "bet:vermelho", 10, 1),
                (ScoreStepKind.BetWon, "bet:duzia:2", 30, 2),
                (ScoreStepKind.BetWon, "bet:pleno:19", 80, 7),
                (ScoreStepKind.PocketValue, "pocket:3", 99, 7),
                (ScoreStepKind.Relic, "relic:teorema", 99, 17),
            };
            ScoreStep[] steps = Scoring(r);
            Assert.AreEqual(expected.Length, steps.Length, string.Join("\n", steps));
            for (int i = 0; i < expected.Length; i++)
            {
                Assert.AreEqual(expected[i].kind, steps[i].Kind, $"passo {i}");
                Assert.AreEqual(expected[i].source, steps[i].SourceId, $"passo {i}");
                Assert.AreEqual(expected[i].pontos, steps[i].PontosAfter, $"passo {i}");
                Assert.AreEqual(expected[i].mult, steps[i].MultAfter, $"passo {i}");
            }
            Assert.AreEqual(ScoreStepKind.Final, r.Steps.Last().Kind);
        }

        [Test]
        public void GddExample_WithoutTeorema_Is693()
        {
            Assert.AreEqual(99 * 7, Spin.On(19, GddExampleBets).Total);
        }

        [Test]
        public void ChipsMultiplyPointsButNotMult()
        {
            SpinResult one = Spin.On(19, new[] { PlacedBet.Straight(19, 1) });
            SpinResult five = Spin.On(19, new[] { PlacedBet.Straight(19, 5) });
            Assert.AreEqual(50 + 19, one.Pontos);
            Assert.AreEqual(250 + 19, five.Pontos);
            Assert.AreEqual(5, one.Mult);
            Assert.AreEqual(5, five.Mult);
        }

        [Test]
        public void BetLevelsRaisePointsAndMult()
        {
            var levels = new BetLevels();
            levels.Raise(BetType.Straight, 2); // nível 3: 50 + 2×30 = 110 Pontos, 5 + 2×3 = 11 Mult
            SpinResult r = Spin.On(19, new[] { PlacedBet.Straight(19, 1) }, levels: levels);
            Assert.AreEqual(110 + 19, r.Pontos);
            Assert.AreEqual(11, r.Mult);
        }

        [Test]
        public void SpinWithoutWin_ScoresZeroEvenWithMultRelics()
        {
            SpinResult r = Spin.On(20, new[] { PlacedBet.Red(5) }, Spin.Relics(RelicIds.FichaGasta, RelicIds.ArranhaCeu));
            Assert.IsFalse(r.IsWin);
            Assert.AreEqual(0, r.Total);
            CollectionAssert.AreEqual(new[] { ScoreStepKind.Final }, r.Steps.Select(s => s.Kind));
        }

        [Test]
        public void ZeroRule_OnlyInsideBetsScore_AndZeroIsWorth36()
        {
            SpinResult r = Spin.On(0, new[] { PlacedBet.Red(3), PlacedBet.Straight(0, 1), PlacedBet.Dozen(1, 1) });
            CollectionAssert.AreEqual(new[] { "bet:pleno:0" }, r.WinningBets.Select(b => b.SourceId));
            Assert.AreEqual(50 + 36, r.Pontos);
            Assert.AreEqual(5, r.Mult);
        }

        [Test]
        public void DoubleZero_IsAlsoWorth36()
        {
            SpinResult r = Spin.On(Pocket.DoubleZero, new[] { PlacedBet.Straight(Pocket.DoubleZero, 1) },
                wheel: WheelLayouts.American());
            Assert.AreEqual(50 + 36, r.Pontos);
        }

        [Test]
        public void BetStepsRecordPlacementIndex()
        {
            var bets = new[] { PlacedBet.Black(1), PlacedBet.Red(1), PlacedBet.Straight(19, 1) };
            SpinResult r = Spin.On(19, bets);
            CollectionAssert.AreEqual(new[] { 1, 2 },
                r.Steps.Where(s => s.Kind == ScoreStepKind.BetWon).Select(s => s.SourceIndex));
        }

        // ── Aprimoramentos ─────────────────────────────────────────────

        static SpinResult Straight19With(PocketEnhancement enhancement, ScriptedRandom luck = null, PocketEdition edition = PocketEdition.None)
        {
            Wheel wheel = WheelLayouts.European();
            Pocket p = wheel[Spin.IndexOfNumber(wheel, 19)];
            p.Enhancement = enhancement;
            p.Edition = edition;
            return Spin.On(19, new[] { PlacedBet.Straight(19, 1) }, wheel: wheel, luck: luck);
        }

        [Test]
        public void Bonus_Adds30Points()
        {
            SpinResult r = Straight19With(PocketEnhancement.Bonus);
            Assert.AreEqual(50 + 19 + 30, r.Pontos);
            Assert.AreEqual(ScoreStepKind.Enhancement, Scoring(r).Last().Kind);
        }

        [Test]
        public void MultEnhancement_Adds4Mult()
        {
            Assert.AreEqual(5 + 4, Straight19With(PocketEnhancement.Mult).Mult);
        }

        [Test]
        public void Glass_DoublesMult_AndMayBreakIntoCrackedPocket()
        {
            Wheel wheel = WheelLayouts.European();
            int i19 = Spin.IndexOfNumber(wheel, 19);
            wheel[i19].Enhancement = PocketEnhancement.Glass;
            wheel[i19].Edition = PocketEdition.Polished;

            SpinResult kept = Spin.On(19, new[] { PlacedBet.Straight(19, 1) }, wheel: wheel, luck: new ScriptedRandom().Ints(1));
            Assert.AreEqual(10, kept.Mult);
            Assert.IsFalse(kept.GlassBroke);
            Assert.AreEqual(PocketEnhancement.Glass, wheel[i19].Enhancement);

            SpinResult broke = Spin.On(19, new[] { PlacedBet.Straight(19, 1) }, wheel: wheel, luck: new ScriptedRandom().Ints(0));
            Assert.AreEqual(10, broke.Mult, "o Vidro ainda pontua no giro em que quebra");
            Assert.IsTrue(broke.GlassBroke);
            Assert.AreEqual(ScoreStepKind.GlassBreak, broke.Steps.Last().Kind);
            Assert.IsTrue(wheel[i19].Cracked);
            Assert.AreEqual(PocketEnhancement.None, wheel[i19].Enhancement);
            Assert.AreEqual(PocketEdition.Polished, wheel[i19].Edition, "a edição sobrevive à quebra");
        }

        [Test]
        public void Glass_CanBreakOnASpinWithoutWin()
        {
            Wheel wheel = WheelLayouts.European();
            int i19 = Spin.IndexOfNumber(wheel, 19);
            wheel[i19].Enhancement = PocketEnhancement.Glass;
            SpinResult r = Spin.On(19, new[] { PlacedBet.Black(1) }, wheel: wheel, luck: new ScriptedRandom().Ints(0));
            Assert.AreEqual(0, r.Total);
            Assert.IsTrue(r.GlassBroke);
            Assert.IsTrue(wheel[i19].Cracked);
        }

        [Test]
        public void Steel_OnLandingPocket_Multiplies1_5()
        {
            Assert.AreEqual(7.5, Straight19With(PocketEnhancement.Steel).Mult);
        }

        [Test]
        public void Steel_OnLandingAndBothNeighbors_Stacks()
        {
            Wheel wheel = WheelLayouts.European();
            int i19 = Spin.IndexOfNumber(wheel, 19);
            wheel[i19].Enhancement = PocketEnhancement.Steel;
            wheel[wheel.Neighbor(i19, -1)].Enhancement = PocketEnhancement.Steel;
            wheel[wheel.Neighbor(i19, 1)].Enhancement = PocketEnhancement.Steel;
            wheel[wheel.Neighbor(i19, 2)].Enhancement = PocketEnhancement.Steel; // longe demais

            SpinResult r = Spin.On(19, new[] { PlacedBet.Straight(19, 1) }, wheel: wheel);
            Assert.AreEqual(5 * 1.5 * 1.5 * 1.5, r.Mult, 1e-9);

            var steelSteps = Scoring(r).Where(s => s.Kind == ScoreStepKind.Enhancement).Select(s => s.SourceIndex).ToArray();
            CollectionAssert.AreEqual(new[] { i19, i19 - 1, i19 + 1 }, steelSteps, "casa sorteada, depois esquerda, depois direita");
        }

        [Test]
        public void Steel_OnlyOnNeighbor_StillApplies()
        {
            Wheel wheel = WheelLayouts.European();
            int i19 = Spin.IndexOfNumber(wheel, 19);
            wheel[wheel.Neighbor(i19, 1)].Enhancement = PocketEnhancement.Steel;
            Assert.AreEqual(7.5, Spin.On(19, new[] { PlacedBet.Straight(19, 1) }, wheel: wheel).Mult);
        }

        [Test]
        public void Gold_Pays3AfterTheFinalStep_EvenWithoutWin()
        {
            SpinResult win = Straight19With(PocketEnhancement.Gold);
            Assert.AreEqual(3, win.MoneyDelta);
            Assert.AreEqual(ScoreStepKind.Money, win.Steps.Last().Kind);
            Assert.AreEqual(ScoreStepKind.Final, win.Steps[win.Steps.Count - 2].Kind);

            Wheel wheel = WheelLayouts.European();
            wheel[Spin.IndexOfNumber(wheel, 19)].Enhancement = PocketEnhancement.Gold;
            SpinResult loss = Spin.On(19, new[] { PlacedBet.Black(1) }, wheel: wheel);
            Assert.AreEqual(0, loss.Total);
            Assert.AreEqual(3, loss.MoneyDelta);
        }

        [Test]
        public void Lucky_RollsMultAndMoneyIndependently()
        {
            // Roteiro: 1º sorteio (1 em 5) para Mult, 2º (1 em 15) para $. 0 = acerta.
            SpinResult both = Straight19With(PocketEnhancement.Lucky, new ScriptedRandom().Ints(0, 0));
            Assert.AreEqual(5 + 20, both.Mult);
            Assert.AreEqual(20, both.MoneyDelta);

            SpinResult multOnly = Straight19With(PocketEnhancement.Lucky, new ScriptedRandom().Ints(0, 3));
            Assert.AreEqual(25, multOnly.Mult);
            Assert.AreEqual(0, multOnly.MoneyDelta);

            SpinResult moneyOnly = Straight19With(PocketEnhancement.Lucky, new ScriptedRandom().Ints(4, 0));
            Assert.AreEqual(5, moneyOnly.Mult);
            Assert.AreEqual(20, moneyOnly.MoneyDelta);
        }

        [Test]
        public void Lucky_StillRollsMoneyOnASpinWithoutWin()
        {
            Wheel wheel = WheelLayouts.European();
            wheel[Spin.IndexOfNumber(wheel, 19)].Enhancement = PocketEnhancement.Lucky;
            var luck = new ScriptedRandom().Ints(0, 0);
            SpinResult r = Spin.On(19, new[] { PlacedBet.Black(1) }, wheel: wheel, luck: luck);
            Assert.AreEqual(0, r.Total);
            Assert.AreEqual(20, r.MoneyDelta);
            Assert.AreEqual(0, luck.Remaining, "os dois sorteios saem mesmo sem vitória");
        }

        [Test]
        public void Magnetic_HasNoScoringEffect()
        {
            SpinResult r = Straight19With(PocketEnhancement.Magnetic);
            Assert.AreEqual(69, r.Pontos);
            Assert.AreEqual(5, r.Mult);
        }

        // ── Edições ────────────────────────────────────────────────────

        [TestCase(PocketEdition.Polished, 50 + 19 + 50, 5)]
        [TestCase(PocketEdition.Holographic, 50 + 19, 15)]
        [TestCase(PocketEdition.Polychrome, 50 + 19, 7.5)]
        public void Editions(PocketEdition edition, double pontos, double mult)
        {
            SpinResult r = Straight19With(PocketEnhancement.None, edition: edition);
            Assert.AreEqual(pontos, r.Pontos);
            Assert.AreEqual(mult, r.Mult);
            Assert.AreEqual(ScoreStepKind.Edition, Scoring(r).Last().Kind);
        }

        [Test]
        public void StepOrder_FollowsGdd32()
        {
            Wheel wheel = WheelLayouts.European();
            int i19 = Spin.IndexOfNumber(wheel, 19);
            wheel[i19].Enhancement = PocketEnhancement.Glass;
            wheel[i19].Edition = PocketEdition.Holographic;
            wheel[wheel.Neighbor(i19, 1)].Enhancement = PocketEnhancement.Steel;

            SpinResult r = Spin.On(19, GddExampleBets, Spin.Relics(RelicIds.FichaGasta), wheel: wheel,
                luck: new ScriptedRandom().Ints(0));

            CollectionAssert.AreEqual(new[]
            {
                ScoreStepKind.BetWon, ScoreStepKind.BetWon, ScoreStepKind.BetWon,
                ScoreStepKind.PocketValue, ScoreStepKind.Enhancement, ScoreStepKind.Enhancement,
                ScoreStepKind.Edition, ScoreStepKind.Relic, ScoreStepKind.Final, ScoreStepKind.GlassBreak,
            }, r.Steps.Select(s => s.Kind));

            // (99 Pontos) × (7 → ×2 = 14 → ×1,5 = 21 → +10 = 31 → +4 = 35)
            Assert.AreEqual(99 * 35, r.Total);
        }

        [Test]
        public void Total_IsFlooredProduct()
        {
            // Pontos 69, Mult 5 × 1,5 × 1,5 = 11,25 → 776,25 → 776
            Wheel wheel = WheelLayouts.European();
            int i19 = Spin.IndexOfNumber(wheel, 19);
            wheel[i19].Enhancement = PocketEnhancement.Steel;
            wheel[i19].Edition = PocketEdition.Polychrome;
            SpinResult r = Spin.On(19, new[] { PlacedBet.Straight(19, 1) }, wheel: wheel);
            Assert.AreEqual(776, r.Total);
        }
    }
}
