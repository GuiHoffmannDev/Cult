using System;
using System.Collections.Generic;
using Pleno.Core.Content;
using Pleno.Core.Effects;
using Pleno.Core.Events;
using Pleno.Core.Model;
using Pleno.Core.Rng;

namespace Pleno.Core.Rules
{
    public sealed class SpinInput
    {
        public Wheel Wheel;
        public int LandingIndex;

        /// <summary>Apostas na ordem em que foram colocadas.</summary>
        public IReadOnlyList<PlacedBet> Bets;

        /// <summary>Amuletos da esquerda para a direita.</summary>
        public IReadOnlyList<RelicInstance> Relics;

        public BetLevels Levels;
        public Balance Balance;

        /// <summary>Stream <c>luck</c>: Sortuda e quebra de Vidro.</summary>
        public IRandom Luck;
    }

    /// <summary>
    /// Pontua um giro na ordem do GDD 3.2 e devolve os passos para a tela tocar.
    /// 1. apostas vencedoras, na ordem colocada; 2. casa sorteada: número, aprimoramento, edição;
    /// 3. amuletos, da esquerda para a direita; 4. floor(Pontos × Mult); 5. pós-giro: dinheiro,
    /// quebra de Vidro, amuletos que escalam.
    /// Giro sem aposta vencedora marca 0 e pula os passos 1 a 3, mas o pós-giro acontece.
    /// Os efeitos do passo 5 já são aplicados na roda e nos amuletos ao retornar.
    /// </summary>
    public static class ScoringPipeline
    {
        public static SpinResult Resolve(SpinInput input)
        {
            Validate(input);
            Wheel wheel = input.Wheel;
            int landing = input.LandingIndex;
            Pocket pocket = wheel[landing];
            PocketBalance values = input.Balance.Pocket;
            string pocketSource = PocketSource(landing);

            var winners = new List<PlacedBet>();
            var winnerOrder = new List<int>();
            for (int i = 0; i < input.Bets.Count; i++)
            {
                if (BetResolver.Wins(input.Bets[i], wheel, landing))
                {
                    winners.Add(input.Bets[i]);
                    winnerOrder.Add(i);
                }
            }

            var ctx = new SpinContext(wheel, landing, input.Bets, winners, input.Levels, input.Balance);
            var acc = new ScoreAccumulator();

            // Os sorteios da Sortuda saem sempre, ganhando ou não, para o consumo do stream não
            // depender das apostas. Ordem fixa: Mult, depois $.
            bool luckyMult = false, luckyMoney = false;
            if (pocket.Enhancement == PocketEnhancement.Lucky)
            {
                luckyMult = input.Luck.OneIn(values.LuckyMultOneIn);
                luckyMoney = input.Luck.OneIn(values.LuckyMoneyOneIn);
            }

            if (ctx.HasWin)
            {
                // 1. Apostas vencedoras.
                for (int w = 0; w < winners.Count; w++)
                {
                    PlacedBet bet = winners[w];
                    BetStats stats = input.Balance.Bets[bet.Type];
                    int level = input.Levels.Get(bet.Type);
                    acc.Apply(ScoreStepKind.BetWon, bet.SourceId, winnerOrder[w],
                        pontosAdd: stats.PointsAt(level) * bet.Chips, multAdd: stats.MultAt(level));
                }

                // 2. Casa sorteada.
                acc.Apply(ScoreStepKind.PocketValue, pocketSource, landing,
                    pontosAdd: SpinContext.NumberValue(pocket, input.Balance));
                ScoreEnhancement(pocket, landing, values, luckyMult, acc);
                ScoreSteelNeighbors(wheel, landing, values, acc);
                ScoreEdition(pocket, landing, values, acc);

                // 3. Amuletos.
                for (int slot = 0; slot < input.Relics.Count; slot++)
                {
                    RelicInstance relic = input.Relics[slot];
                    RelicEffectRegistry.Get(relic.Id).OnScore(new RelicScope(relic, slot, ctx, acc, scoring: true));
                }
            }

            // 4. Resultado.
            double pontos = acc.Pontos, mult = acc.Mult;
            double total = ctx.HasWin ? Math.Floor(pontos * mult) : 0;
            acc.Apply(ScoreStepKind.Final, "final", -1);

            // 5. Pós-giro.
            if (pocket.Enhancement == PocketEnhancement.Gold)
                acc.Apply(ScoreStepKind.Money, pocketSource, landing, money: values.GoldMoney);
            if (luckyMoney)
                acc.Apply(ScoreStepKind.Money, pocketSource, landing, money: values.LuckyMoney);

            bool glassBroke = false;
            if (pocket.Enhancement == PocketEnhancement.Glass && input.Luck.OneIn(values.GlassBreakOneIn))
            {
                // Vira Casa Rachada: perde o Vidro, mantém a edição.
                pocket.Enhancement = PocketEnhancement.None;
                pocket.Cracked = true;
                glassBroke = true;
                acc.Apply(ScoreStepKind.GlassBreak, pocketSource, landing);
            }

            for (int slot = 0; slot < input.Relics.Count; slot++)
            {
                RelicInstance relic = input.Relics[slot];
                RelicEffectRegistry.Get(relic.Id).OnAfterSpin(new RelicScope(relic, slot, ctx, acc, scoring: false));
            }

            return new SpinResult
            {
                PocketIndex = landing,
                Steps = acc.Steps,
                WinningBets = winners,
                Pontos = pontos,
                Mult = mult,
                Total = total,
                MoneyDelta = acc.Money,
                GlassBroke = glassBroke,
            };
        }

        public static string PocketSource(int index) => "pocket:" + index;

        static void ScoreEnhancement(Pocket pocket, int index, PocketBalance values, bool luckyMult, ScoreAccumulator acc)
        {
            string source = PocketSource(index);
            switch (pocket.Enhancement)
            {
                case PocketEnhancement.Bonus:
                    acc.Apply(ScoreStepKind.Enhancement, source, index, pontosAdd: values.BonusPoints);
                    break;
                case PocketEnhancement.Mult:
                    acc.Apply(ScoreStepKind.Enhancement, source, index, multAdd: values.MultEnhancementMult);
                    break;
                case PocketEnhancement.Glass:
                    acc.Apply(ScoreStepKind.Enhancement, source, index, multX: values.GlassMultX);
                    break;
                case PocketEnhancement.Steel:
                    acc.Apply(ScoreStepKind.Enhancement, source, index, multX: values.SteelMultX);
                    break;
                case PocketEnhancement.Lucky:
                    if (luckyMult)
                        acc.Apply(ScoreStepKind.Enhancement, source, index, multAdd: values.LuckyMult);
                    break;
            }
        }

        /// <summary>Aço também age quando a bola cai numa vizinha. Cada casa de Aço conta: esquerda, depois direita.</summary>
        static void ScoreSteelNeighbors(Wheel wheel, int landing, PocketBalance values, ScoreAccumulator acc)
        {
            foreach (int offset in new[] { -1, 1 })
            {
                int neighbor = wheel.Neighbor(landing, offset);
                if (neighbor != landing && wheel[neighbor].Enhancement == PocketEnhancement.Steel)
                    acc.Apply(ScoreStepKind.Enhancement, PocketSource(neighbor), neighbor, multX: values.SteelMultX);
            }
        }

        static void ScoreEdition(Pocket pocket, int index, PocketBalance values, ScoreAccumulator acc)
        {
            string source = PocketSource(index);
            switch (pocket.Edition)
            {
                case PocketEdition.Polished:
                    acc.Apply(ScoreStepKind.Edition, source, index, pontosAdd: values.PolishedPoints);
                    break;
                case PocketEdition.Holographic:
                    acc.Apply(ScoreStepKind.Edition, source, index, multAdd: values.HolographicMult);
                    break;
                case PocketEdition.Polychrome:
                    acc.Apply(ScoreStepKind.Edition, source, index, multX: values.PolychromeMultX);
                    break;
            }
        }

        static void Validate(SpinInput input)
        {
            if (input.Wheel == null || input.Bets == null || input.Relics == null || input.Levels == null
                || input.Balance == null || input.Luck == null)
                throw new ArgumentException("SpinInput incompleto.", nameof(input));
            if (input.LandingIndex < 0 || input.LandingIndex >= input.Wheel.Count)
                throw new ArgumentOutOfRangeException(nameof(input), input.LandingIndex, "Casa fora da roda.");
        }
    }
}
