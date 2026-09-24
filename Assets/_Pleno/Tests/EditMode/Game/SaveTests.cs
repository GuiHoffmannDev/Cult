using System.IO;
using System.Linq;
using NUnit.Framework;
using Pleno.Core.Content;
using Pleno.Core.Flow;
using Pleno.Core.Model;
using Pleno.Core.Rng;
using Pleno.Game.Save;
using Pleno.Tests.Core;

namespace Pleno.Tests.Game
{
    public sealed class SaveTests
    {
        static RunEngine Load(RunEngine original) =>
            new RunEngine(RunSerializer.Deserialize(RunSerializer.Serialize(original.State)));

        /// <summary>Joga os dois lados com as mesmas ações e compara tudo o que é observável.</summary>
        static void PlayInLockstep(RunEngine a, RunEngine b, int spins)
        {
            for (int i = 0; i < spins; i++)
            {
                foreach (RunEngine run in new[] { a, b })
                {
                    if (run.State.Phase == RunPhase.Shop)
                        run.LeaveShop();
                }
                if (a.State.Phase != RunPhase.Mesa)
                    break;

                foreach (RunEngine run in new[] { a, b })
                {
                    run.Place(PlacedBet.Red(2));
                    run.Place(PlacedBet.Neighbors(run.State.Wheel[5].Id, 2));
                    run.Place(PlacedBet.Straight(17, 1));
                }
                Assert.AreEqual(a.Spin().LandingIndex, b.Spin().LandingIndex, $"giro {i}");
                var ra = a.ConfirmSpin();
                var rb = b.ConfirmSpin();
                Assert.AreEqual(ra.Total, rb.Total, $"giro {i}");
                Assert.AreEqual(a.State.Money, b.State.Money, $"giro {i}");
            }
            Assert.AreEqual(RunSerializer.Serialize(a.State), RunSerializer.Serialize(b.State));
        }

        [Test]
        public void RoundTrip_IsLossless()
        {
            RunEngine run = RunTestUtil.New(StartingWheel.Golden);
            run.State.Relics.Add(new RelicInstance(RelicIds.Colecionador) { Edition = PocketEdition.Holographic });
            run.State.Relics[0].SetCounter("seen:17", 1);
            run.State.Consumables.Add(new ConsumableInstance(HouseCardIds.Pintor));
            run.State.Levels.Raise(BetType.Straight, 2);
            run.State.Wheel.CloneAdjacent(4);
            run.State.Wheel.RemoveAt(10, 18);
            run.State.Wheel[3].Cracked = true;
            run.Place(PlacedBet.Neighbors(run.State.Wheel[7].Id, 2));
            run.Place(PlacedBet.Corner(1, 0, 1, 2, 3));

            string json = RunSerializer.Serialize(run.State);
            RunState loaded = RunSerializer.Deserialize(json);

            Assert.AreEqual(json, RunSerializer.Serialize(loaded));
            Assert.AreEqual(run.State.Wheel.NextId, loaded.Wheel.NextId);
            CollectionAssert.AreEqual(run.State.Wheel.Pockets.Select(p => p.Id), loaded.Wheel.Pockets.Select(p => p.Id));
            Assert.AreEqual(PocketEdition.Holographic, loaded.Relics[0].Edition);
            Assert.AreEqual(3, loaded.Levels.Get(BetType.Straight));
            Assert.AreEqual("bet:quadra:0-1-2-3", loaded.Mesa.Bets[1].SourceId);
            StringAssert.Contains("\"StartingWheel\": \"Golden\"", json, "enums legíveis");
        }

        [Test]
        public void RngStreams_ContinueExactlyAfterLoading()
        {
            RunEngine run = RunTestUtil.New();
            foreach (string name in RngStreams.Names)
                for (int i = 0; i < 13; i++)
                    run.State.Rng.Get(name).NextUInt();

            RunState loaded = RunSerializer.Deserialize(RunSerializer.Serialize(run.State));
            foreach (string name in RngStreams.Names)
                for (int i = 0; i < 20; i++)
                    Assert.AreEqual(run.State.Rng.Get(name).NextUInt(), loaded.Rng.Get(name).NextUInt(), name);
        }

        [Test]
        public void SaveWhileBetting_ContinuesIdentically()
        {
            RunEngine original = RunTestUtil.New(seed: "QWER2345");
            RunEngine loaded = Load(original);
            PlayInLockstep(original, loaded, spins: 12);
        }

        [Test]
        public void SaveWhileTheBallIsDown_KeepsTheResult()
        {
            // Sair e voltar com a bola parada não pode trocar o resultado.
            RunEngine original = RunTestUtil.New(seed: "QWER2345");
            original.Place(PlacedBet.Red(5));
            original.Spin();
            RunEngine loaded = Load(original);

            Assert.AreEqual(MesaPhase.AwaitingNudge, loaded.State.Mesa.Phase);
            Assert.AreEqual(original.State.Mesa.BallIndex, loaded.State.Mesa.BallIndex);
            Assert.AreEqual(original.ConfirmSpin().Total, loaded.ConfirmSpin().Total);
        }

        [Test]
        public void SaveInTheShop_ContinuesIdentically()
        {
            RunEngine original = RunTestUtil.New(seed: "QWER2345");
            RunTestUtil.WinMesa(original);
            foreach (Pocket p in original.State.Wheel.Pockets) p.Weight = 1;
            original.State.Money = 40;

            RunEngine loaded = Load(original);
            foreach (RunEngine run in new[] { original, loaded })
            {
                run.Reroll();
                run.BuyPack();
            }
            CollectionAssert.AreEqual(original.State.Shop.OpenPack.Options, loaded.State.Shop.OpenPack.Options);
            CollectionAssert.AreEqual(original.State.Shop.Relics.Select(o => o.Id), loaded.State.Shop.Relics.Select(o => o.Id));

            RunEngine midPack = Load(original);
            Assert.AreEqual(original.State.Shop.OpenPack.PackId, midPack.State.Shop.OpenPack.PackId);

            foreach (RunEngine run in new[] { original, loaded })
                run.SkipPack();
            PlayInLockstep(original, loaded, spins: 8);
        }

        [Test]
        public void SaveDuringDuploZero_KeepsTheTemporaryPocketAndRemovesItLater()
        {
            RunEngine original = RunTestUtil.AtBoss(BossIds.DuploZero);
            RunEngine loaded = Load(original);
            Assert.AreEqual(38, loaded.State.Wheel.Count);
            RunTestUtil.WinMesa(loaded);
            Assert.AreEqual(37, loaded.State.Wheel.Count);
        }

        [Test]
        public void SaveToFile_WritesAndReads()
        {
            string path = Path.Combine(Path.GetTempPath(), "pleno-save-test.json");
            try
            {
                RunEngine run = RunTestUtil.New();
                RunSerializer.SaveToFile(run.State, path);
                RunSerializer.SaveToFile(run.State, path); // sobrescreve
                Assert.AreEqual(run.State.Seed, RunSerializer.LoadFromFile(path).Seed);
                Assert.IsFalse(File.Exists(path + ".tmp"));
            }
            finally
            {
                if (File.Exists(path)) File.Delete(path);
            }
        }

        [Test]
        public void UnknownVersion_IsRejected()
        {
            string json = RunSerializer.Serialize(RunTestUtil.New().State).Replace("\"Version\": 1", "\"Version\": 99");
            Assert.Throws<InvalidDataException>(() => RunSerializer.Deserialize(json));
        }
    }
}
