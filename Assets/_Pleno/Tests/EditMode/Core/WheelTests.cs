using System;
using System.Linq;
using NUnit.Framework;
using Pleno.Core.Content;
using Pleno.Core.Model;

namespace Pleno.Tests.Core
{
    public sealed class WheelTests
    {
        const int Min = 18;

        [Test]
        public void European_Has37PocketsInPhysicalOrder()
        {
            Wheel wheel = WheelLayouts.European();
            Assert.AreEqual(37, wheel.Count);
            CollectionAssert.AreEquivalent(Enumerable.Range(0, 37), wheel.Pockets.Select(p => p.Number));

            // GDD 2.2: na roda europeia o 17 fica entre o 25 e o 34.
            int i17 = Spin.IndexOfNumber(wheel, 17);
            Assert.AreEqual(25, wheel[wheel.Neighbor(i17, -1)].Number);
            Assert.AreEqual(34, wheel[wheel.Neighbor(i17, +1)].Number);
        }

        [Test]
        public void American_Has38PocketsWithDoubleZero()
        {
            Wheel wheel = WheelLayouts.American();
            Assert.AreEqual(38, wheel.Count);
            CollectionAssert.AreEquivalent(
                Enumerable.Range(0, 37).Append(Pocket.DoubleZero), wheel.Pockets.Select(p => p.Number));
            Pocket doubleZero = wheel.Pockets.Single(p => p.Number == Pocket.DoubleZero);
            Assert.AreEqual(PocketColor.Green, doubleZero.Color);
            Assert.AreEqual("00", doubleZero.Label);
            Assert.IsTrue(doubleZero.IsZero);
        }

        [TestCase(true)]
        [TestCase(false)]
        public void Layouts_HaveStandardColorsAlternatingAroundTheWheel(bool european)
        {
            Wheel wheel = european ? WheelLayouts.European() : WheelLayouts.American();
            Assert.AreEqual(18, wheel.Pockets.Count(p => p.Color == PocketColor.Red));
            Assert.AreEqual(18, wheel.Pockets.Count(p => p.Color == PocketColor.Black));
            for (int i = 0; i < wheel.Count; i++)
            {
                Pocket a = wheel[i], b = wheel[wheel.Neighbor(i, 1)];
                if (a.Color != PocketColor.Green && b.Color != PocketColor.Green)
                    Assert.AreNotEqual(a.Color, b.Color, $"{a.Label} e {b.Label} são vizinhos da mesma cor");
            }
        }

        [Test]
        public void Neighbor_WrapsAroundBothEnds()
        {
            Wheel wheel = WheelLayouts.European();
            Assert.AreEqual(36, wheel.Neighbor(0, -1));
            Assert.AreEqual(35, wheel.Neighbor(0, -2));
            Assert.AreEqual(0, wheel.Neighbor(36, 1));
            Assert.AreEqual(1, wheel.Neighbor(36, 2));
            Assert.AreEqual(0, wheel.Neighbor(0, 37 * 3));
            Assert.AreEqual(2, wheel.CircularDistance(35, 0));
            Assert.AreEqual(2, wheel.CircularDistance(0, 35));
            Assert.AreEqual(18, wheel.CircularDistance(0, 18));
        }

        [Test]
        public void CloneAdjacent_InsertsCopyRightAfterWithNewId()
        {
            Wheel wheel = WheelLayouts.European();
            int i17 = Spin.IndexOfNumber(wheel, 17);
            wheel[i17].Enhancement = PocketEnhancement.Gold;
            wheel[i17].Edition = PocketEdition.Holographic;
            int originalId = wheel[i17].Id;

            Pocket clone = wheel.CloneAdjacent(i17);

            Assert.AreEqual(38, wheel.Count);
            Assert.AreSame(clone, wheel[i17 + 1]);
            Assert.AreEqual(originalId, wheel[i17].Id);
            Assert.AreNotEqual(originalId, clone.Id);
            Assert.AreEqual(17, clone.Number);
            Assert.AreEqual(PocketColor.Black, clone.Color);
            Assert.AreEqual(PocketEnhancement.Gold, clone.Enhancement);
            Assert.AreEqual(PocketEdition.Holographic, clone.Edition);
            Assert.AreEqual(34, wheel[i17 + 2].Number, "o vizinho da direita foi empurrado uma casa");
            Assert.AreEqual(2, wheel.Pockets.Count(p => p.Number == 17), "números repetidos são permitidos");
        }

        [Test]
        public void CloneAdjacent_OnLastPocketAppendsBeforeTheWrap()
        {
            Wheel wheel = WheelLayouts.European();
            Pocket clone = wheel.CloneAdjacent(36);
            Assert.AreEqual(37, wheel.IndexOfId(clone.Id));
            Assert.AreEqual(0, wheel[wheel.Neighbor(37, 1)].Number);
        }

        [Test]
        public void RemoveAt_ClosesTheGapAndIdsAreNeverReused()
        {
            Wheel wheel = WheelLayouts.European();
            int i17 = Spin.IndexOfNumber(wheel, 17);
            Pocket removed = wheel.RemoveAt(i17, Min);

            Assert.AreEqual(36, wheel.Count);
            Assert.AreEqual(-1, wheel.IndexOfId(removed.Id));
            Assert.AreEqual(25, wheel[i17 - 1].Number);
            Assert.AreEqual(34, wheel[i17].Number, "25 e 34 viraram vizinhos");

            Pocket added = wheel.CloneAdjacent(0);
            Assert.That(added.Id, Is.GreaterThan(wheel.Pockets.Where(p => p != added).Max(p => p.Id)));
        }

        [Test]
        public void RemoveAt_RespectsMinimumOf18()
        {
            Wheel wheel = WheelLayouts.European();
            while (wheel.CanRemove(Min))
                wheel.RemoveAt(0, Min);

            Assert.AreEqual(18, wheel.Count);
            Assert.Throws<InvalidOperationException>(() => wheel.RemoveAt(0, Min));
            Assert.AreEqual(Balance.Default.Wheel.MinPockets, Min);
        }

        [Test]
        public void Clone_IsDeepAndKeepsIds()
        {
            Wheel wheel = WheelLayouts.European();
            Wheel copy = wheel.Clone();
            copy[5].Enhancement = PocketEnhancement.Glass;
            copy.CloneAdjacent(0);

            Assert.AreEqual(PocketEnhancement.None, wheel[5].Enhancement);
            Assert.AreEqual(37, wheel.Count);
            Assert.AreEqual(wheel[10].Id, copy[11].Id);
            Assert.AreEqual(wheel.NextId + 1, copy.NextId);
        }
    }
}
