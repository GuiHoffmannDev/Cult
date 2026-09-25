using System.Linq;
using NUnit.Framework;
using Pleno.Core.Content;
using Pleno.Core.Model;
using Pleno.Sim;

namespace Pleno.Tests.Sim
{
    public sealed class BalancePresetTests
    {
        [Test]
        public void DefaultPreset_IsTheGameBalance()
        {
            Balance preset = BalancePresets.Get(BalancePresets.DefaultId).Build();
            Balance game = Balance.Default;

            CollectionAssert.AreEqual(game.Run.Targets, preset.Run.Targets);
            Assert.AreEqual(game.Mesa.Nudges, preset.Mesa.Nudges);
            Assert.AreEqual(game.Run.UnusedSpinMoney, preset.Run.UnusedSpinMoney);
        }

        [Test]
        public void Presets_ScaleFromTheDefault_AndNeverTouchIt()
        {
            Balance scaled = BalancePresets.Get("metas-1-4-9-pleno-x2").Build();

            CollectionAssert.AreEqual(new double[] { 100, 150, 200 }, scaled.Run.Targets[0]);
            CollectionAssert.AreEqual(new double[] { 1200, 1800, 2400 }, scaled.Run.Targets[1]);
            CollectionAssert.AreEqual(new double[] { 7200, 10800, 14400 }, scaled.Run.Targets[2]);
            Assert.AreEqual(100, scaled.Bets[BetType.Straight].Points);
            Assert.AreEqual(40, scaled.Bets[BetType.Neighbors].PointsPerLevel);

            Assert.AreEqual(1600, Balance.Default.Run.Targets[2][2]);
            Assert.AreEqual(50, Balance.Default.Bets[BetType.Straight].Points);
        }

        [Test]
        public void Presets_HaveDistinctIds()
        {
            Assert.AreEqual(BalancePresets.All.Count, BalancePresets.All.Select(p => p.Id).Distinct().Count());
        }

        [Test]
        public void Use_LabelsTheBatch()
        {
            var config = new SimConfig();
            BalancePresets.Use(config, BalancePresets.Get("sem-empurrao"));

            Assert.AreEqual("sem-empurrao", config.BalanceLabel);
            Assert.AreEqual(0, config.MakeBalance().Mesa.Nudges);
        }
    }
}
