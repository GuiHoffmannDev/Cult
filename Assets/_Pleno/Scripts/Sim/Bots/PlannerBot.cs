using System;
using System.Collections.Generic;
using System.Linq;
using Pleno.Core.Content;
using Pleno.Core.Effects;
using Pleno.Core.Flow;
using Pleno.Core.Model;
using Pleno.Core.Rng;
using Pleno.Core.Rules;
using Pleno.Sim.Planning;

namespace Pleno.Sim
{
    /// <summary>
    /// Base de Cobertura, Sniper e Guloso: o mesmo cérebro, mudando só as apostas permitidas.
    /// Aposta pelo <see cref="BetPlanner"/> (E[min(pontos, falta)]), empurra pelo
    /// <see cref="NudgePlanner"/>, compra e usa pelo <see cref="Appraiser"/>. Toda previsão roda
    /// sobre cópias do estado, com a sorte simulada vinda do stream do bot.
    /// </summary>
    public abstract class PlannerBot : Bot
    {
        MesaState _preparedMesa;
        double _nudgeValue;

        /// <summary>Apostas que o bot considera, com 1 ficha cada.</summary>
        protected abstract IReadOnlyList<PlacedBet> Candidates(Wheel wheel);

        /// <summary>Teto de fichas numa mesma aposta.</summary>
        protected virtual int MaxChipsPerBet => int.MaxValue;

        /// <summary>Compra e usa Cartas da Casa (Tábuas todos usam).</summary>
        protected virtual bool UsesHouseCards => true;

        /// <summary>Sorte simulada nova a cada decisão: decisões diferentes não veem sempre a mesma sorte.</summary>
        Pcg32 Luck()
        {
            Rng.NextUInt();
            return Rng;
        }

        // ── Mesa ───────────────────────────────────────────────────────

        public override void PlaceBets(RunDriver d)
        {
            RunEngine run = d.Engine;
            MesaState mesa = run.State.Mesa;

            // Consumíveis guardados: revistos no começo de cada mesa (o chefe pode ter mudado a roda).
            if (!ReferenceEquals(_preparedMesa, mesa))
            {
                _preparedMesa = mesa;
                if (run.State.Consumables.Count > 0)
                    UseConsumables(d, MesaAppraiser(run), atShopEntry: false);
            }

            var ev = SpinEvaluator.For(run, Luck());
            double remaining = mesa.Target - mesa.Score;
            int reach = PlanningReach(mesa);
            int chips = mesa.ChipsFree;
            BetPlan plan = BetPlanner.Plan(ev, Candidates(ev.Wheel), chips, Math.Min(MaxChipsPerBet, chips), remaining, reach);
            _nudgeValue = BetPlanner.Expected(ev.Odds, plan.Scores, remaining, 1)
                - BetPlanner.Expected(ev.Odds, plan.Scores, remaining, 0);
            foreach (PlacedBet bet in plan.Bets)
                if (run.CheckPlace(bet) == null)
                    d.Place(bet);
        }

        /// <summary>Empurrões que o plano de apostas conta como disponíveis neste giro.</summary>
        static int PlanningReach(MesaState mesa)
        {
            if (mesa.NudgesLeft <= 0)
                return 0;
            return mesa.SpinsLeft <= 1 ? mesa.NudgesLeft : 1;
        }

        public override void Nudge(RunDriver d)
        {
            RunEngine run = d.Engine;
            MesaState mesa = run.State.Mesa;
            if (run.CheckNudge(1) != null)
                return;

            var ev = SpinEvaluator.For(run, Luck());
            int offset = NudgePlanner.Offset(ev, mesa.Bets, mesa.BallIndex, mesa.NudgesLeft, mesa.SpinsLeft,
                mesa.Target - mesa.Score, _nudgeValue);
            for (int i = 0; i < Math.Abs(offset) && run.CheckNudge(Math.Sign(offset)) == null; i++)
                d.Nudge(Math.Sign(offset));
        }

        // ── Avaliação ──────────────────────────────────────────────────

        /// <summary>
        /// Teto do giro de referência: a meta do Chefe do próximo Ante (ou do último). O que se compra
        /// fica para a run inteira, então medir só contra a próxima meta subestima quem multiplica.
        /// </summary>
        static double HorizonCap(RunEngine run)
        {
            int ante = Math.Min(run.State.Ante + 1, run.Balance.Run.Antes);
            return run.TargetFor(ante, run.Balance.Run.MesasPerAnte - 1);
        }

        /// <summary>Giro de referência durante a mesa (com a regra do chefe, se houver).</summary>
        Appraiser MesaAppraiser(RunEngine run)
        {
            MesaState mesa = run.State.Mesa;
            return Reference(run, mesa.Resources.ChipsPerSpin, HorizonCap(run), mesa.NudgesLeft > 0 ? 1 : 0, run.Boss);
        }

        /// <summary>Giro de referência na loja, com os recursos da próxima mesa e sem regra de chefe.</summary>
        Appraiser ShopAppraiser(RunEngine run)
        {
            RunState state = run.State;
            MesaResources next = MesaRules.Resources(run.Balance, state.StartingWheel, state.Relics, null);
            return Reference(run, next.ChipsPerSpin, HorizonCap(run), next.Nudges > 0 ? 1 : 0, null);
        }

        Appraiser Reference(RunEngine run, int chips, double cap, int reach, IBossRule boss)
        {
            RunState state = run.State;
            Pcg32 luck = Luck();
            var ev = new SpinEvaluator(state.Wheel, state.Relics, state.Levels, run.Balance, boss, luck);
            BetPlan plan = BetPlanner.Plan(ev, Candidates(ev.Wheel), chips, Math.Min(MaxChipsPerBet, chips), cap, reach);
            return new Appraiser(run, plan.Bets, cap, reach, luck);
        }

        // ── Loja ───────────────────────────────────────────────────────

        public override void Shop(RunDriver d)
        {
            RunEngine run = d.Engine;
            Appraiser app = ShopAppraiser(run);
            UseConsumables(d, app, atShopEntry: true);

            for (int round = 0; ; round++)
            {
                app = BuyRelics(d, app);
                if (round == 0)
                    app = BuyPack(d, app);
                app = BuyConsumables(d, app);
                if (round >= BotTuning.MaxRerollsPerShop || !ShouldReroll(d, app))
                    break;
                d.Reroll();
            }
            Reorder(d, ShopAppraiser(run));
        }

        Appraiser BuyRelics(RunDriver d, Appraiser app)
        {
            RunEngine run = d.Engine;
            RunState state = run.State;
            var skipped = new HashSet<int>();
            while (true)
            {
                int best = -1;
                double bestGain = 0;
                for (int i = 0; i < state.Shop.Relics.Count; i++)
                {
                    ShopOffer offer = state.Shop.Relics[i];
                    if (offer.Sold || skipped.Contains(i))
                        continue;
                    double gain = app.RelicGain(offer.Id);
                    if (gain > bestGain + 1e-9)
                    {
                        best = i;
                        bestGain = gain;
                    }
                }
                if (best < 0)
                    return app;
                skipped.Add(best);

                if (state.Relics.Count >= run.Balance.Run.RelicSlots && !MakeRoom(d, app, bestGain, state.Shop.Relics[best].Price))
                    continue;
                if (run.CheckBuyRelic(best) != null)
                    continue;
                d.BuyRelic(best);
                app = ShopAppraiser(run);
            }
        }

        /// <summary>Vende o amuleto que menos rende se o novo valer bem mais e o dinheiro fechar a conta.</summary>
        static bool MakeRoom(RunDriver d, Appraiser app, double newGain, int price)
        {
            RunEngine run = d.Engine;
            RunState state = run.State;
            int worst = -1;
            double worstWorth = double.PositiveInfinity;
            for (int slot = 0; slot < state.Relics.Count; slot++)
            {
                double worth = app.RelicWorth(slot);
                if (worth < worstWorth)
                {
                    worst = slot;
                    worstWorth = worth;
                }
            }
            if (worst < 0 || newGain <= Math.Max(0, worstWorth) * BotTuning.RelicSwapMargin)
                return false;
            int sellValue = Economy.SellPrice(Economy.RelicPrice(state.Relics[worst].Id, run.Balance), run.Balance.Run);
            if (state.Money + sellValue < price || run.CheckSellRelic(worst) != null)
                return false;
            d.SellRelic(worst);
            return true;
        }

        Appraiser BuyPack(RunDriver d, Appraiser app)
        {
            RunEngine run = d.Engine;
            RunState state = run.State;
            ShopOffer pack = state.Shop.Pack;
            if (pack == null || pack.Sold || run.CheckBuyPack() != null)
                return app;

            bool wanted;
            switch (pack.Id)
            {
                case PackIds.Amuletos:
                    wanted = state.Relics.Count < run.Balance.Run.RelicSlots;
                    break;
                case PackIds.Tabuas:
                    wanted = TabuaIds.All.Any(id => app.TabuaGain(id) > 1e-9);
                    break;
                default:
                    wanted = UsesHouseCards;
                    break;
            }
            if (!wanted)
                return app;

            PackOpening opening = d.BuyPack();
            int choice = -1;
            double choiceGain = 0;
            CardTargets choiceTargets = null;
            for (int i = 0; i < opening.Options.Count; i++)
            {
                string id = opening.Options[i];
                double gain;
                CardTargets targets = null;
                switch (opening.PackId)
                {
                    case PackIds.Amuletos:
                        gain = app.RelicGain(id);
                        break;
                    case PackIds.Tabuas:
                        gain = app.TabuaGain(id);
                        break;
                    default:
                        CardChoice card = app.Card(id);
                        if (!card.Useful)
                            continue;
                        // Cartas sem ganho de pontos (dinheiro, sorte) só entram se nenhuma outra render pontos.
                        gain = card.Gain > 0 ? card.Gain : 1e-6;
                        targets = card.Targets;
                        break;
                }
                if (gain > choiceGain && run.CheckChoosePackOption(i, targets) == null)
                {
                    choice = i;
                    choiceGain = gain;
                    choiceTargets = targets;
                }
            }

            if (choice >= 0)
                d.ChoosePackOption(choice, choiceTargets);
            else
                d.SkipPack();
            return ShopAppraiser(run);
        }

        Appraiser BuyConsumables(RunDriver d, Appraiser app)
        {
            RunEngine run = d.Engine;
            RunState state = run.State;
            for (int i = 0; i < state.Shop.Consumables.Count; i++)
            {
                ShopOffer offer = state.Shop.Consumables[i];
                if (offer.Sold || !Wants(run, app, offer))
                    continue;
                if (state.Consumables.Count >= run.Balance.Run.ConsumableSlots)
                    SellDeadWeight(d, app);
                if (run.CheckBuyConsumable(i) != null)
                    continue;
                d.BuyConsumable(i);
                UseConsumables(d, app, atShopEntry: false);
                app = ShopAppraiser(run);
            }
            return app;
        }

        bool Wants(RunEngine run, Appraiser app, ShopOffer offer)
        {
            if (Consumables.KindOf(offer.Id) == ConsumableKind.Tabua)
                return app.TabuaGain(offer.Id) > 1e-9;
            if (!UsesHouseCards)
                return false;
            switch (offer.Id)
            {
                case HouseCardIds.Tesoureiro:
                    return run.State.Money - offer.Price >= BotTuning.TesoureiroMinMoney;
                case HouseCardIds.Oraculo:
                    return TabuaIds.All.Any(id => app.TabuaGain(id) > 1e-9);
                case HouseCardIds.Ourives:
                    return false;
                default:
                    CardChoice card = app.Card(offer.Id);
                    return card.Useful && (card.Gain > 1e-9 || offer.Id == HouseCardIds.Fortuna);
            }
        }

        /// <summary>Vende uma Carta da Casa guardada que hoje não rende nada, para abrir espaço.</summary>
        void SellDeadWeight(RunDriver d, Appraiser app)
        {
            RunState state = d.State;
            for (int slot = state.Consumables.Count - 1; slot >= 0; slot--)
            {
                string id = state.Consumables[slot].Id;
                if (Consumables.KindOf(id) == ConsumableKind.HouseCard && !app.Card(id).Useful
                    && d.Engine.CheckSellConsumable(slot) == null)
                {
                    d.SellConsumable(slot);
                    return;
                }
            }
        }

        /// <summary>
        /// Usa o que vale usar agora: Tábuas sempre (nível só ajuda); Cartas da Casa com ganho no giro
        /// de referência; Tesoureiro ao entrar na loja, quando o $ acabou de subir.
        /// </summary>
        void UseConsumables(RunDriver d, Appraiser app, bool atShopEntry)
        {
            RunEngine run = d.Engine;
            RunState state = run.State;
            for (int guard = 0; guard < 8; guard++)
            {
                bool used = false;
                for (int slot = state.Consumables.Count - 1; slot >= 0 && !used; slot--)
                {
                    string id = state.Consumables[slot].Id;
                    CardTargets targets = null;
                    if (Consumables.KindOf(id) == ConsumableKind.HouseCard)
                    {
                        if (id == HouseCardIds.Tesoureiro && !atShopEntry && state.Phase == RunPhase.Shop)
                            continue;
                        CardChoice card = app.Card(id);
                        bool worth = card.Useful && (card.Gain > 1e-9 || card.Targets.Pockets.Count == 0 || id == HouseCardIds.Ourives);
                        if (!worth)
                            continue;
                        targets = card.Targets;
                    }
                    if (run.CheckUseConsumable(slot, targets) != null)
                        continue;
                    d.UseConsumable(slot, targets);
                    used = true;
                }
                if (!used)
                    return;
                // A roda ou os níveis mudaram: o giro de referência também.
                app = state.Phase == RunPhase.Shop ? ShopAppraiser(run) : MesaAppraiser(run);
            }
        }

        bool ShouldReroll(RunDriver d, Appraiser app)
        {
            RunEngine run = d.Engine;
            RunState state = run.State;
            return state.Relics.Count < run.Balance.Run.RelicSlots
                && run.CheckReroll() == null
                && state.Money - run.RerollCost >= BotTuning.RerollReserve;
        }

        /// <summary>Põe os amuletos na ordem que mais rende (os que multiplicam o Mult tendem a ir por último).</summary>
        static void Reorder(RunDriver d, Appraiser app)
        {
            List<RelicInstance> best = app.BestOrder();
            List<RelicInstance> relics = d.State.Relics;
            for (int target = 0; target < best.Count; target++)
            {
                int from = relics.IndexOf(best[target]);
                if (from != target)
                    d.MoveRelic(from, target);
            }
        }
    }
}
