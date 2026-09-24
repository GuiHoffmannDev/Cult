using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using NUnit.Framework;
using Pleno.Core.Content;
using Pleno.Core.Effects;
using Pleno.Core.Events;
using Pleno.Core.Effects.Relics;
using Pleno.Core.Model;
using Pleno.Core.Rules;

namespace Pleno.Tests.Core
{
    public sealed class RelicTests
    {
        static readonly PlacedBet[] Straight19 = { PlacedBet.Straight(19, 1) }; // 69 Pontos × 5 Mult

        static IEnumerable<ScoreStep> RelicSteps(SpinResult r) => r.Steps.Where(s => s.Kind == ScoreStepKind.Relic);

        [Test]
        public void Registry_HasAllTenPhase1Relics()
        {
            CollectionAssert.IsSubsetOf(new[]
            {
                RelicIds.FichaGasta, RelicIds.RosaRubra, RelicIds.ArranhaCeu, RelicIds.Teorema, RelicIds.MiraLaser,
                RelicIds.ComboTriplo, RelicIds.Martingale, RelicIds.Colecionador, RelicIds.LuvaDeSeda,
                RelicIds.ImaDeNeodimio,
            }, RelicEffectRegistry.Ids.ToArray());
            Assert.AreEqual(RelicIds.Teorema, RelicEffectRegistry.Get(RelicIds.Teorema).Id);
            Assert.Throws<KeyNotFoundException>(() => RelicEffectRegistry.Get("nao-existe"));
        }

        [Test]
        public void FichaGasta_Adds4Mult()
        {
            SpinResult r = Spin.On(19, Straight19, Spin.Relics(RelicIds.FichaGasta));
            Assert.AreEqual(9, r.Mult);
            Assert.AreEqual("relic:ficha-gasta", RelicSteps(r).Single().SourceId);
        }

        [Test]
        public void RosaRubra_OnlyWhenARedBetWon()
        {
            var relics = Spin.Relics(RelicIds.RosaRubra);
            Assert.AreEqual(1 + 8, Spin.On(19, new[] { PlacedBet.Red(1) }, relics).Mult);
            Assert.AreEqual(1, Spin.On(20, new[] { PlacedBet.Black(1) }, relics).Mult);
            Assert.AreEqual(5, Spin.On(19, new[] { PlacedBet.Black(1), PlacedBet.Straight(19, 1) }, relics).Mult,
                "caiu num vermelho, mas a aposta em Vermelho não existia");
        }

        [Test]
        public void ArranhaCeu_CountsTheNumberTwice()
        {
            Assert.AreEqual(50 + 19 + 19, Spin.On(19, Straight19, Spin.Relics(RelicIds.ArranhaCeu)).Pontos);
            Assert.AreEqual(50 + 36 + 36, Spin.On(0, new[] { PlacedBet.Straight(0, 1) }, Spin.Relics(RelicIds.ArranhaCeu)).Pontos);
        }

        [TestCase(19, true)]
        [TestCase(2, true)]
        [TestCase(21, false)]
        [TestCase(1, false)]
        [TestCase(0, false)]
        public void Teorema_OnPrimeNumbers(int number, bool prime)
        {
            SpinResult r = Spin.On(number, new[] { PlacedBet.Straight(number, 1) }, Spin.Relics(RelicIds.Teorema));
            Assert.AreEqual(prime ? 15 : 5, r.Mult);
        }

        [Test]
        public void MiraLaser_TriplesMultWhenStraightWon()
        {
            var relics = Spin.Relics(RelicIds.MiraLaser);
            Assert.AreEqual(15, Spin.On(19, Straight19, relics).Mult);
            Assert.AreEqual(1, Spin.On(19, new[] { PlacedBet.Red(1) }, relics).Mult);
        }

        [Test]
        public void ComboTriplo_NeedsThreeWinningBets()
        {
            var relics = Spin.Relics(RelicIds.ComboTriplo);
            Assert.AreEqual(7 * 2, Spin.On(19, new[] { PlacedBet.Red(1), PlacedBet.Dozen(2, 1), PlacedBet.Straight(19, 1) }, relics).Mult);
            Assert.AreEqual(6, Spin.On(19, new[] { PlacedBet.Red(1), PlacedBet.Straight(19, 1) }, relics).Mult);
            Assert.AreEqual(2, Spin.On(19, new[] { PlacedBet.Red(1), PlacedBet.Dozen(2, 1), PlacedBet.Black(1) }, relics).Mult,
                "três apostas colocadas, só duas venceram");
        }

        [Test]
        public void RelicOrderMatters()
        {
            // Ficha Gasta (+4) antes da Mira Laser (×3): (5 + 4) × 3 = 27. Ao contrário: 5 × 3 + 4 = 19.
            Assert.AreEqual(27, Spin.On(19, Straight19, Spin.Relics(RelicIds.FichaGasta, RelicIds.MiraLaser)).Mult);
            Assert.AreEqual(19, Spin.On(19, Straight19, Spin.Relics(RelicIds.MiraLaser, RelicIds.FichaGasta)).Mult);
        }

        [Test]
        public void RelicSteps_RecordTheirSlot()
        {
            SpinResult r = Spin.On(19, Straight19, Spin.Relics(RelicIds.Teorema, RelicIds.FichaGasta, RelicIds.MiraLaser));
            CollectionAssert.AreEqual(new[] { 0, 1, 2 }, RelicSteps(r).Select(s => s.SourceIndex));
        }

        [Test]
        public void Martingale_AccumulatesOnLossesAndResetsAfterAWin()
        {
            Wheel wheel = WheelLayouts.European();
            RelicInstance[] relics = Spin.Relics(RelicIds.Martingale);
            RelicInstance martingale = relics[0];

            Spin.On(20, Straight19, relics, wheel);
            Spin.On(20, Straight19, relics, wheel);
            Assert.AreEqual(6, martingale.GetCounter(Martingale.StackKey));

            SpinResult win = Spin.On(19, Straight19, relics, wheel);
            Assert.AreEqual(5 + 6, win.Mult, "o giro vencedor recebe o acumulado");
            Assert.AreEqual(0, martingale.GetCounter(Martingale.StackKey), "e só depois zera");

            Assert.AreEqual(5, Spin.On(19, Straight19, relics, wheel).Mult);
        }

        [Test]
        public void Colecionador_CountsDistinctNumbersFromTheNextSpinOn()
        {
            Wheel wheel = WheelLayouts.European();
            RelicInstance[] relics = Spin.Relics(RelicIds.Colecionador);
            var bets = new[] { PlacedBet.Straight(19, 1), PlacedBet.Straight(7, 1), PlacedBet.Straight(20, 1) };

            Assert.AreEqual(5, Spin.On(19, bets, relics, wheel).Mult, "nenhum número visto ainda");
            Assert.AreEqual(5 + 1, Spin.On(19, bets, relics, wheel).Mult, "19 já tinha caído");
            Assert.AreEqual(5 + 1, Spin.On(7, bets, relics, wheel).Mult, "7 é novo, conta a partir do próximo");
            Spin.On(22, bets, relics, wheel); // giro perdido também conta o número
            Assert.AreEqual(5 + 3, Spin.On(20, bets, relics, wheel).Mult);
            Assert.AreEqual(4, relics[0].GetCounter(Colecionador.DistinctKey));
        }

        [Test]
        public void RelicCounters_SurviveJsonRoundTrip()
        {
            RelicInstance[] relics = Spin.Relics(RelicIds.Colecionador, RelicIds.Martingale);
            Spin.On(19, Straight19, relics);
            Spin.On(20, Straight19, relics);

            string json = JsonConvert.SerializeObject(relics);
            var restored = JsonConvert.DeserializeObject<RelicInstance[]>(json);

            Assert.AreEqual(RelicIds.Colecionador, restored[0].Id);
            CollectionAssert.AreEquivalent(relics[0].Counters, restored[0].Counters);
            Assert.AreEqual(3, restored[1].GetCounter(Martingale.StackKey));

            // A cópia restaurada pontua igual à original.
            Assert.AreEqual(Spin.On(19, Straight19, relics).Total, Spin.On(19, Straight19, restored).Total);
        }

        [Test]
        public void LuvaDeSeda_AddsOneNudgePerMesa()
        {
            MesaResources baseline = MesaRules.Resources(Balance.Default, new RelicInstance[0]);
            Assert.AreEqual(4, baseline.Spins);
            Assert.AreEqual(5, baseline.ChipsPerSpin);
            Assert.AreEqual(2, baseline.Nudges);

            MesaResources withGlove = MesaRules.Resources(Balance.Default, Spin.Relics(RelicIds.LuvaDeSeda));
            Assert.AreEqual(3, withGlove.Nudges);
            Assert.AreEqual(4, withGlove.Spins);
        }

        [Test]
        public void RelicScope_RejectsMoneyWhileScoringAndScoringAfterTheSpin()
        {
            Wheel wheel = WheelLayouts.European();
            var ctx = new SpinContext(wheel, 3, Straight19, Straight19, new BetLevels(), Balance.Default);
            var self = new RelicInstance("teste");

            var scoring = new RelicScope(self, 0, ctx, new ScoreAccumulator(), scoring: true);
            Assert.Throws<InvalidOperationException>(() => scoring.AddMoney(1));

            var after = new RelicScope(self, 0, ctx, new ScoreAccumulator(), scoring: false);
            Assert.Throws<InvalidOperationException>(() => after.AddMult(1));
        }
    }
}
