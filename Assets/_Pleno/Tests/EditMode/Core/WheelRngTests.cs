using System.Linq;
using NUnit.Framework;
using Pleno.Core.Content;
using Pleno.Core.Model;
using Pleno.Core.Rng;
using Pleno.Core.Rules;

namespace Pleno.Tests.Core
{
    public sealed class WheelRngTests
    {
        const int Samples = 100_000;

        // Qui-quadrado crítico para 36 graus de liberdade (37 casas) a p = 0,001.
        const double ChiSquareCritical36 = 67.985;

        static RelicInstance[] NoRelics => new RelicInstance[0];

        static int[] Histogram(Wheel wheel, double[] weights, IRandom rng)
        {
            var counts = new int[wheel.Count];
            for (int i = 0; i < Samples; i++)
                counts[WheelRng.PickWeighted(weights, rng)]++;
            return counts;
        }

        static void AssertMatchesDistribution(int[] counts, double[] weights)
        {
            double total = weights.Sum();
            double chiSquare = 0;
            for (int i = 0; i < counts.Length; i++)
            {
                double expected = Samples * weights[i] / total;
                chiSquare += (counts[i] - expected) * (counts[i] - expected) / expected;

                double sigma = System.Math.Sqrt(expected * (1 - weights[i] / total));
                Assert.That(counts[i], Is.InRange(expected - 5 * sigma, expected + 5 * sigma), $"casa {i}");
            }
            Assert.That(chiSquare, Is.LessThan(ChiSquareCritical36), "qui-quadrado");
        }

        [Test]
        public void UniformWheel_MatchesExpectedDistributionOver100kSamples()
        {
            Wheel wheel = WheelLayouts.European();
            double[] weights = WheelRng.Weights(wheel, Balance.Default, NoRelics);
            CollectionAssert.AreEqual(Enumerable.Repeat(1.0, 37), weights);

            int[] counts = Histogram(wheel, weights, new RngStreams("TESTSEED").Get(RngStreams.Wheel));
            AssertMatchesDistribution(counts, weights);
        }

        [Test]
        public void WeightedWheel_MatchesExpectedDistributionOver100kSamples()
        {
            Wheel wheel = WheelLayouts.European();
            wheel[3].Enhancement = PocketEnhancement.Magnetic;  // peso 2
            wheel[20].Enhancement = PocketEnhancement.Magnetic; // peso 2
            wheel[30].Weight = 3.0;                             // peso 3
            wheel[31].Weight = 0.5;

            double[] weights = WheelRng.Weights(wheel, Balance.Default, NoRelics);
            Assert.AreEqual(2.0, weights[3]);
            Assert.AreEqual(3.0, weights[30]);
            Assert.AreEqual(0.5, weights[31]);

            int[] counts = Histogram(wheel, weights, new RngStreams("TESTSEED").Get(RngStreams.Wheel));
            AssertMatchesDistribution(counts, weights);

            // Sanidade visível: a Magnética sai cerca do dobro de uma casa comum.
            double ratio = (double)counts[3] / counts[4];
            Assert.That(ratio, Is.InRange(1.8, 2.2));
        }

        [Test]
        public void ZeroWeightPocket_IsNeverDrawn()
        {
            Wheel wheel = WheelLayouts.European();
            wheel[7].Weight = 0;
            double[] weights = WheelRng.Weights(wheel, Balance.Default, NoRelics);
            int[] counts = Histogram(wheel, weights, new Pcg32(1, 2));
            Assert.AreEqual(0, counts[7]);
        }

        [Test]
        public void ImaDeNeodimio_MakesMagneticWeightFourAndDoesNotStack()
        {
            Wheel wheel = WheelLayouts.European();
            wheel[3].Enhancement = PocketEnhancement.Magnetic;

            Assert.AreEqual(4.0, WheelRng.Weights(wheel, Balance.Default, Spin.Relics(RelicIds.ImaDeNeodimio))[3]);
            Assert.AreEqual(4.0, WheelRng.Weights(wheel, Balance.Default,
                Spin.Relics(RelicIds.ImaDeNeodimio, RelicIds.ImaDeNeodimio))[3]);
            Assert.AreEqual(1.0, WheelRng.Weights(wheel, Balance.Default, Spin.Relics(RelicIds.ImaDeNeodimio))[4],
                "casas comuns não mudam");
        }

        [Test]
        public void SameSeed_DrawsSameSequence()
        {
            Wheel wheel = WheelLayouts.European();
            double[] weights = WheelRng.Weights(wheel, Balance.Default, NoRelics);
            Pcg32 a = new RngStreams("ABCD2345").Get(RngStreams.Wheel);
            Pcg32 b = new RngStreams("ABCD2345").Get(RngStreams.Wheel);
            for (int i = 0; i < 1000; i++)
                Assert.AreEqual(WheelRng.Draw(wheel, weights, a).LandingIndex, WheelRng.Draw(wheel, weights, b).LandingIndex);
        }

        [Test]
        public void CrackedPocket_BouncesToARandomNeighbor()
        {
            Wheel wheel = WheelLayouts.European();
            var weights = new double[wheel.Count];
            weights[10] = 1;
            wheel[10].Cracked = true;

            var left = WheelRng.Draw(wheel, weights, new ScriptedRandom().Doubles(0.5).Ints(0));
            var right = WheelRng.Draw(wheel, weights, new ScriptedRandom().Doubles(0.5).Ints(1));
            Assert.AreEqual(10, left.DrawnIndex);
            Assert.AreEqual(9, left.LandingIndex);
            Assert.AreEqual(-1, left.CrackBounce);
            Assert.AreEqual(11, right.LandingIndex);

            var rng = new RngStreams("TESTSEED").Get(RngStreams.Wheel);
            int[] landings = Enumerable.Range(0, 2000).Select(_ => WheelRng.Draw(wheel, weights, rng).LandingIndex).ToArray();
            CollectionAssert.IsSubsetOf(landings.Distinct(), new[] { 9, 11 });
            Assert.That(landings.Count(i => i == 9), Is.InRange(900, 1100));
        }

        [Test]
        public void CrackedPocket_BouncesOnlyOnceEvenIntoAnotherCrackedPocket()
        {
            Wheel wheel = WheelLayouts.European();
            var weights = new double[wheel.Count];
            weights[0] = 1;
            wheel[0].Cracked = true;
            wheel[36].Cracked = true;

            var draw = WheelRng.Draw(wheel, weights, new ScriptedRandom().Doubles(0.1).Ints(0));
            Assert.AreEqual(36, draw.LandingIndex, "salta para a esquerda, dando a volta, e fica lá");
        }
    }
}
