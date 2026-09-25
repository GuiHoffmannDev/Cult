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
        public void Presets_VaryFromTheDefault_AndNeverTouchIt()
        {
            Balance scaled = BalancePresets.Get("metas-x1.1").Build();
            Balance summary = BalancePresets.Get("apostas-do-resumo").Build();

            CollectionAssert.AreEqual(Balance.Default.Run.Targets[0], scaled.Run.Targets[0]);
            Assert.AreEqual(Balance.Default.Run.Targets[2][2] * 1.1, scaled.Run.Targets[2][2], 1e-9);
            Assert.AreEqual(20, summary.Bets[BetType.Neighbors].Points);
            Assert.AreEqual(30, summary.Bets[BetType.Straight].PointsPerLevel);

            Assert.AreEqual(14400, Balance.Default.Run.Targets[2][2]);
            Assert.AreEqual(40, Balance.Default.Bets[BetType.Neighbors].Points);
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
