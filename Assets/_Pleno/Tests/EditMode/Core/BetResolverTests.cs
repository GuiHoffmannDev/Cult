using System;
using System.Linq;
using NUnit.Framework;
using Pleno.Core.Content;
using Pleno.Core.Model;
using Pleno.Core.Rules;

namespace Pleno.Tests.Core
{
    public sealed class BetResolverTests
    {
        static readonly PlacedBet[] AllOutside =
        {
            PlacedBet.Red(1), PlacedBet.Black(1), PlacedBet.Even(1), PlacedBet.Odd(1),
            PlacedBet.Low(1), PlacedBet.High(1),
            PlacedBet.Dozen(1, 1), PlacedBet.Dozen(2, 1), PlacedBet.Dozen(3, 1),
            PlacedBet.Column(1, 1), PlacedBet.Column(2, 1), PlacedBet.Column(3, 1),
        };

        static bool WinsOn(Wheel wheel, int number, PlacedBet bet) =>
            BetResolver.Wins(bet, wheel, Spin.IndexOfNumber(wheel, number));

        [Test]
        public void ZeroRule_AllOutsideBetsLoseOnZero()
        {
            Wheel wheel = WheelLayouts.European();
            foreach (PlacedBet bet in AllOutside)
                Assert.IsFalse(WinsOn(wheel, 0, bet), bet.SourceId);
        }

        [Test]
        public void ZeroRule_AllOutsideBetsLoseOnDoubleZero()
        {
            Wheel wheel = WheelLayouts.American();
            foreach (PlacedBet bet in AllOutside)
                Assert.IsFalse(WinsOn(wheel, Pocket.DoubleZero, bet), bet.SourceId);
        }

        [Test]
        public void ZeroRule_InsideBetsIncludingZeroStillWin()
        {
            Wheel eu = WheelLayouts.European();
            Assert.IsTrue(WinsOn(eu, 0, PlacedBet.Straight(0, 1)));
            Assert.IsTrue(WinsOn(eu, 0, PlacedBet.Split(0, 1, 1)));
            Assert.IsTrue(WinsOn(eu, 0, PlacedBet.Street(1, 0, 2, 3)));
            Assert.IsTrue(WinsOn(eu, 0, PlacedBet.Corner(1, 0, 1, 2, 3)));

            Wheel us = WheelLayouts.American();
            Assert.IsTrue(WinsOn(us, Pocket.DoubleZero, PlacedBet.Straight(Pocket.DoubleZero, 1)));
            Assert.IsTrue(WinsOn(us, Pocket.DoubleZero, PlacedBet.Split(0, Pocket.DoubleZero, 1)));
            Assert.IsFalse(WinsOn(us, 0, PlacedBet.Straight(Pocket.DoubleZero, 1)), "0 e 00 são casas diferentes");
        }

        [TestCase(19, OutsideKind.Red, true)]
        [TestCase(19, OutsideKind.Black, false)]
        [TestCase(19, OutsideKind.Odd, true)]
        [TestCase(19, OutsideKind.Even, false)]
        [TestCase(19, OutsideKind.High, true)]
        [TestCase(18, OutsideKind.Low, true)]
        [TestCase(18, OutsideKind.High, false)]
        [TestCase(20, OutsideKind.Black, true)]
        [TestCase(20, OutsideKind.Even, true)]
        public void SimpleChances(int number, OutsideKind kind, bool wins)
        {
            var bet = new PlacedBet(BetType.SimpleChance, kind, 0, null, 0, 1);
            Assert.AreEqual(wins, WinsOn(WheelLayouts.European(), number, bet));
        }

        [TestCase(1, 1, 1)]
        [TestCase(12, 1, 3)]
        [TestCase(13, 2, 1)]
        [TestCase(19, 2, 1)]
        [TestCase(24, 2, 3)]
        [TestCase(35, 3, 2)]
        [TestCase(36, 3, 3)]
        public void DozensAndColumns(int number, int dozen, int column)
        {
            Wheel wheel = WheelLayouts.European();
            for (int k = 1; k <= 3; k++)
            {
                Assert.AreEqual(k == dozen, WinsOn(wheel, number, PlacedBet.Dozen(k, 1)), $"dúzia {k}");
                Assert.AreEqual(k == column, WinsOn(wheel, number, PlacedBet.Column(k, 1)), $"coluna {k}");
            }
        }

        [Test]
        public void ColorComesFromThePocketNotTheNumber()
        {
            Wheel wheel = WheelLayouts.European();
            Assert.IsTrue(WinsOn(wheel, 17, PlacedBet.Black(1)), "o 17 é preto de fábrica");
            wheel[Spin.IndexOfNumber(wheel, 17)].Color = PocketColor.Red; // O Pintor
            Assert.IsTrue(WinsOn(wheel, 17, PlacedBet.Red(1)));
            Assert.IsFalse(WinsOn(wheel, 17, PlacedBet.Black(1)));
        }

        [Test]
        public void InsideBets_CoverTheirNumbers()
        {
            Wheel wheel = WheelLayouts.European();
            Assert.IsTrue(WinsOn(wheel, 17, PlacedBet.Split(17, 20, 1)));
            Assert.IsTrue(WinsOn(wheel, 20, PlacedBet.Split(17, 20, 1)));
            Assert.IsFalse(WinsOn(wheel, 18, PlacedBet.Split(17, 20, 1)));
            Assert.IsTrue(WinsOn(wheel, 15, PlacedBet.Street(1, 13, 14, 15)));
            Assert.IsTrue(WinsOn(wheel, 21, PlacedBet.Corner(1, 17, 18, 20, 21)));
            Assert.IsTrue(WinsOn(wheel, 36, PlacedBet.Line(31, 1)));
            Assert.IsFalse(WinsOn(wheel, 30, PlacedBet.Line(31, 1)));
        }

        [Test]
        public void Neighbors_CoversFivePhysicalPockets()
        {
            Wheel wheel = WheelLayouts.European();
            int i17 = Spin.IndexOfNumber(wheel, 17);
            var bet = PlacedBet.Neighbors(wheel[i17].Id, 1);

            // Ordem europeia: … 21, 2, 25, [17], 34, 6, 27 …
            foreach (int n in new[] { 2, 25, 17, 34, 6 })
                Assert.IsTrue(WinsOn(wheel, n, bet), $"deveria cobrir {n}");
            foreach (int n in new[] { 21, 27, 0 })
                Assert.IsFalse(WinsOn(wheel, n, bet), $"não deveria cobrir {n}");
        }

        [Test]
        public void Neighbors_WrapsAcrossIndexZero()
        {
            Wheel wheel = WheelLayouts.European();
            var onZero = PlacedBet.Neighbors(wheel[0].Id, 1);   // âncora no índice 0 (o 0)
            var onLast = PlacedBet.Neighbors(wheel[36].Id, 1);  // âncora no índice 36 (o 26)

            CollectionAssert.AreEquivalent(new[] { 35, 36, 0, 1, 2 }, BetResolver.NeighborsCoverage(onZero, wheel));
            CollectionAssert.AreEquivalent(new[] { 34, 35, 36, 0, 1 }, BetResolver.NeighborsCoverage(onLast, wheel));

            foreach (int i in new[] { 35, 36, 0, 1, 2 })
                Assert.IsTrue(BetResolver.Wins(onZero, wheel, i), $"índice {i}");
            Assert.IsFalse(BetResolver.Wins(onZero, wheel, 3));
            Assert.IsFalse(BetResolver.Wins(onZero, wheel, 34));
            Assert.IsTrue(BetResolver.Wins(onLast, wheel, 1));
            Assert.IsFalse(BetResolver.Wins(onLast, wheel, 2));
        }

        [Test]
        public void Neighbors_IsPositionalSoDuplicatesElsewhereDoNotCount()
        {
            Wheel wheel = WheelLayouts.European();
            int i17 = Spin.IndexOfNumber(wheel, 17);
            var neighbors = PlacedBet.Neighbors(wheel[i17].Id, 1);
            var straight = PlacedBet.Straight(17, 1);

            // Um segundo "17" do outro lado da roda.
            int farIndex = wheel.Neighbor(i17, 18);
            wheel.Insert(farIndex, new Pocket(17, PocketColor.Black));
            int farSeventeen = farIndex;

            Assert.IsTrue(BetResolver.Wins(straight, wheel, farSeventeen), "o Pleno usa o número");
            Assert.IsFalse(BetResolver.Wins(neighbors, wheel, farSeventeen), "Vizinhos usa a posição");
        }

        [Test]
        public void Neighbors_WhoseAnchorWasRemovedCannotWin()
        {
            Wheel wheel = WheelLayouts.European();
            var bet = PlacedBet.Neighbors(wheel[10].Id, 1);
            wheel.RemoveAt(10, 18);
            Assert.IsFalse(Enumerable.Range(0, wheel.Count).Any(i => BetResolver.Wins(bet, wheel, i)));
        }

        [Test]
        public void WinningBets_KeepPlacementOrder()
        {
            Wheel wheel = WheelLayouts.European();
            var bets = new[] { PlacedBet.Straight(19, 1), PlacedBet.Black(1), PlacedBet.Red(2), PlacedBet.Dozen(2, 1) };
            var winners = BetResolver.WinningBets(bets, wheel, Spin.IndexOfNumber(wheel, 19));
            CollectionAssert.AreEqual(new[] { bets[0], bets[2], bets[3] }, winners);
        }

        [Test]
        public void TableLayout_AcceptsOnlyRealCombinations()
        {
            Assert.DoesNotThrow(() => PlacedBet.Split(1, 2, 1));
            Assert.DoesNotThrow(() => PlacedBet.Split(1, 4, 1));
            Assert.DoesNotThrow(() => PlacedBet.Split(0, 3, 1));
            Assert.DoesNotThrow(() => PlacedBet.Corner(1, 32, 33, 35, 36));
            Assert.DoesNotThrow(() => PlacedBet.Street(1, 34, 35, 36));
            Assert.DoesNotThrow(() => PlacedBet.Line(1, 1));

            Assert.Throws<ArgumentException>(() => PlacedBet.Split(3, 4, 1), "3 e 4 estão em linhas diferentes");
            Assert.Throws<ArgumentException>(() => PlacedBet.Split(1, 5, 1));
            Assert.Throws<ArgumentException>(() => PlacedBet.Street(1, 2, 3, 4));
            Assert.Throws<ArgumentException>(() => PlacedBet.Corner(1, 3, 4, 6, 7));
            Assert.Throws<ArgumentException>(() => PlacedBet.Line(2, 1));
            Assert.Throws<ArgumentException>(() => PlacedBet.Straight(37, 1));
            Assert.Throws<ArgumentException>(() => PlacedBet.Dozen(4, 1));
            Assert.Throws<ArgumentOutOfRangeException>(() => PlacedBet.Red(0));
        }

        [Test]
        public void SourceIds_AreReadable()
        {
            Assert.AreEqual("bet:vermelho", PlacedBet.Red(2).SourceId);
            Assert.AreEqual("bet:duzia:2", PlacedBet.Dozen(2, 2).SourceId);
            Assert.AreEqual("bet:pleno:19", PlacedBet.Straight(19, 1).SourceId);
            Assert.AreEqual("bet:pleno:00", PlacedBet.Straight(Pocket.DoubleZero, 1).SourceId);
            Assert.AreEqual("bet:cavalo:17-20", PlacedBet.Split(20, 17, 1).SourceId);
            Assert.AreEqual("bet:vizinhos:#7", PlacedBet.Neighbors(7, 1).SourceId);
            Assert.AreEqual(12, AllOutside.Select(b => b.SourceId).Distinct().Count());
        }
    }
}
