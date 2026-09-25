using System;
using System.Collections.Generic;
using System.Linq;
using Pleno.Core.Content;
using Pleno.Core.Flow;
using Pleno.Core.Model;
using Pleno.Core.Rng;
using Pleno.Sim.Planning;

namespace Pleno.Sim
{
    /// <summary>
    /// Aleatório: cada ficha numa aposta sorteada entre todas, empurra ao acaso, e na loja faz ações
    /// legais sorteadas (comprar, rerolar, usar consumível com alvos sorteados) até sortear sair.
    /// </summary>
    public sealed class RandomBot : Bot
    {
        public override string Id => IdOf(BotKind.Aleatorio);

        public override void PlaceBets(RunDriver d)
        {
            RunEngine run = d.Engine;
            List<PlacedBet> catalog = BetCatalog.All(run.State.Wheel);
            int chips = run.State.Mesa.ChipsFree;
            for (int i = 0; i < chips; i++)
            {
                PlacedBet bet = Rng.Pick(catalog);
                if (run.CheckPlace(bet) == null)
                    d.Place(bet);
            }
        }

        public override void Nudge(RunDriver d)
        {
            int direction = Rng.NextInt(2) == 0 ? -1 : 1;
            if (Rng.OneIn(2) && d.Engine.CheckNudge(direction) == null)
                d.Nudge(direction);
        }

        public override void Shop(RunDriver d)
        {
            RunEngine run = d.Engine;
            RunState state = run.State;
            for (int n = 0; n < BotTuning.RandomShopActions; n++)
            {
                var actions = new List<Action>();
                for (int i = 0; i < state.Shop.Relics.Count; i++)
                {
                    int offer = i;
                    if (run.CheckBuyRelic(offer) == null)
                        actions.Add(() => d.BuyRelic(offer));
                }
                for (int i = 0; i < state.Shop.Consumables.Count; i++)
                {
                    int offer = i;
                    if (run.CheckBuyConsumable(offer) == null)
                        actions.Add(() => d.BuyConsumable(offer));
                }
                if (run.CheckBuyPack() == null)
                    actions.Add(() => OpenPack(d));
                if (run.CheckReroll() == null)
                    actions.Add(d.Reroll);
                for (int i = 0; i < state.Consumables.Count; i++)
                {
                    int slot = i;
                    CardTargets targets = TargetsFor(state.Consumables[slot].Id, t => run.CheckUseConsumable(slot, t) == null, run);
                    if (targets != null)
                        actions.Add(() => d.UseConsumable(slot, targets));
                }

                // Sair é sempre uma das opções.
                int pick = Rng.NextInt(actions.Count + 1);
                if (pick == actions.Count)
                    return;
                actions[pick]();
            }
        }

        void OpenPack(RunDriver d)
        {
            RunEngine run = d.Engine;
            PackOpening pack = d.BuyPack();
            int pick = Rng.NextInt(pack.Options.Count + 1);
            if (pick < pack.Options.Count)
            {
                CardTargets targets = TargetsFor(pack.Options[pick], t => run.CheckChoosePackOption(pick, t) == null, run);
                if (targets != null)
                {
                    d.ChoosePackOption(pick, targets);
                    return;
                }
            }
            d.SkipPack();
        }

        /// <summary>Alvos sorteados que a regra aceita, ou null se não achou em algumas tentativas.</summary>
        CardTargets TargetsFor(string id, Func<CardTargets, bool> accepts, RunEngine run)
        {
            if (Consumables.KindOf(id) == ConsumableKind.Tabua || !HouseCardIds.All.Contains(id))
                return accepts(CardTargets.None) ? CardTargets.None : null;

            var (min, max) = HouseCards.TargetRange(id, run.Balance);
            int count = run.State.Wheel.Count;
            for (int attempt = 0; attempt < BotTuning.RandomTargetTries; attempt++)
            {
                int size = min + Rng.NextInt(max - min + 1);
                var targets = new CardTargets { Pockets = Rng.PickDistinct(count, size) };
                if (HouseCards.NeedsSource(id))
                    targets.Source = Rng.NextInt(count);
                if (accepts(targets))
                    return targets;
            }
            return null;
        }
    }
}
