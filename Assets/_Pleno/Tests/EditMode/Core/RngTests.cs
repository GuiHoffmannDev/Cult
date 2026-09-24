using System.Linq;
using NUnit.Framework;
using Pleno.Core.Rng;

namespace Pleno.Tests.Core
{
    public sealed class RngTests
    {
        [Test]
        public void Pcg32_MatchesReferenceImplementation()
        {
            // Saída de pcg32-demo (pcg32_srandom_r(42, 54)), a implementação de referência de O'Neill.
            var rng = new Pcg32(42, 54);
            uint[] expected = { 0xa15c02b7, 0x7b47f409, 0xba1d3330, 0x83d2f293, 0xbfa4784b, 0xcbed606e };
            CollectionAssert.AreEqual(expected, expected.Select(_ => rng.NextUInt()).ToArray());
        }

        [Test]
        public void Pcg32_NextIntStaysInRange()
        {
            var rng = new Pcg32(7, 11);
            for (int i = 0; i < 10000; i++)
            {
                int v = rng.NextInt(37);
                Assert.That(v, Is.InRange(0, 36));
            }
            Assert.AreEqual(0, rng.NextInt(1));
        }

        [Test]
        public void Pcg32_NextDoubleIsInUnitInterval()
        {
            var rng = new Pcg32(3, 5);
            for (int i = 0; i < 10000; i++)
            {
                double v = rng.NextDouble();
                Assert.That(v, Is.GreaterThanOrEqualTo(0.0).And.LessThan(1.0));
            }
        }

        [Test]
        public void Pcg32_StateRoundTripContinuesSequence()
        {
            var rng = new Pcg32(99, 1);
            for (int i = 0; i < 50; i++) rng.NextUInt();
            var restored = new Pcg32(rng.State);
            for (int i = 0; i < 100; i++)
                Assert.AreEqual(rng.NextUInt(), restored.NextUInt());
        }

        [Test]
        public void RunSeed_ValidatesAndNormalizes()
        {
            Assert.IsTrue(RunSeed.IsValid("ABCD2345"));
            Assert.IsFalse(RunSeed.IsValid("ABCD234"), "curta");
            Assert.IsFalse(RunSeed.IsValid("ABCDO234"), "O é ambíguo");
            Assert.IsFalse(RunSeed.IsValid("ABCD1234"), "1 é ambíguo");
            Assert.AreEqual("ABCD2345", RunSeed.Normalize("  abcd2345 "));
            Assert.IsTrue(RunSeed.IsValid(RunSeed.FromEntropy(0xDEADBEEFUL)));
            Assert.AreNotEqual(RunSeed.FromEntropy(1), RunSeed.FromEntropy(2));
        }

        [Test]
        public void Streams_SameSeedGivesSameSequenceInEveryStream()
        {
            var a = new RngStreams("TESTSEED");
            var b = new RngStreams("testseed");
            foreach (string name in RngStreams.Names)
                for (int i = 0; i < 200; i++)
                    Assert.AreEqual(a.Get(name).NextUInt(), b.Get(name).NextUInt(), $"stream {name}, passo {i}");
        }

        [Test]
        public void Streams_AreIndependentOfEachOther()
        {
            var streams = new RngStreams("TESTSEED");
            var firstValues = RngStreams.Names.Select(n => streams.Get(n).NextUInt()).ToArray();
            Assert.AreEqual(RngStreams.Names.Count, firstValues.Distinct().Count());

            // Consumir a loja não muda o próximo valor da roda.
            var untouched = new RngStreams("TESTSEED");
            var consumed = new RngStreams("TESTSEED");
            for (int i = 0; i < 1000; i++) consumed.Get(RngStreams.Shop).NextUInt();
            Assert.AreEqual(untouched.Get(RngStreams.Wheel).NextUInt(), consumed.Get(RngStreams.Wheel).NextUInt());
        }

        [Test]
        public void Streams_DifferentSeedsDiffer()
        {
            var a = new RngStreams("TESTSEED").Get(RngStreams.Wheel);
            var b = new RngStreams("TESTSEEE").Get(RngStreams.Wheel);
            var seqA = Enumerable.Range(0, 8).Select(_ => a.NextUInt()).ToArray();
            var seqB = Enumerable.Range(0, 8).Select(_ => b.NextUInt()).ToArray();
            CollectionAssert.AreNotEqual(seqA, seqB);
        }

        [Test]
        public void Streams_SnapshotRestoreContinuesEveryStream()
        {
            var streams = new RngStreams("TESTSEED");
            foreach (string name in RngStreams.Names)
                for (int i = 0; i < 17; i++) streams.Get(name).NextUInt();

            var restored = RngStreams.Restore("TESTSEED", streams.Snapshot());
            foreach (string name in RngStreams.Names)
                for (int i = 0; i < 50; i++)
                    Assert.AreEqual(streams.Get(name).NextUInt(), restored.Get(name).NextUInt(), name);
        }

        [Test]
        public void Streams_InvalidSeedThrows()
        {
            Assert.Throws<System.ArgumentException>(() => new RngStreams("NOPE"));
        }
    }
}
