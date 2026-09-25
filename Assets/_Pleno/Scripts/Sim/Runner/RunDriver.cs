using System;
using System.Collections.Generic;
using System.Linq;
using Pleno.Core.Content;
using Pleno.Core.Events;
using Pleno.Core.Flow;
using Pleno.Core.Model;

namespace Pleno.Sim
{
    /// <summary>Um bot tentou uma ação que o <see cref="RunEngine"/> recusa. É bug do bot.</summary>
    public sealed class IllegalBotActionException : Exception
    {
        public IllegalBotActionException(string message) : base(message)
        {
        }
    }

    /// <summary>
    /// O único caminho de um bot até a run. Cada ação chama antes o <c>Check…()</c> do
    /// <see cref="RunEngine"/>; se ele recusa, lança <see cref="IllegalBotActionException"/> (o bot
    /// deveria ter checado). No caminho, registra o que o simulador mede.
    /// </summary>
    public sealed class RunDriver
    {
        HashSet<string> _offeredThisShop;
        MesaRecord _mesa;

        public RunEngine Engine { get; }
        public RunRecord Record { get; }
        public string Seed { get; }

        public RunState State => Engine.State;

        public RunDriver(RunEngine engine, string seed, RunRecord record)
        {
            Engine = engine;
            Seed = seed;
            Record = record;
        }

        // ── Mesa ───────────────────────────────────────────────────────

        public void Place(PlacedBet bet) => Do(Engine.CheckPlace(bet), () => Engine.Place(bet));

        public void Nudge(int direction)
        {
            Do(Engine.CheckNudge(direction), () => Engine.Nudge(direction));
            Record.NudgesUsed++;
        }

        internal void Spin()
        {
            if (_mesa == null)
            {
                MesaState mesa = State.Mesa;
                _mesa = new MesaRecord
                {
                    Ante = State.Ante,
                    Index = State.MesaIndex,
                    BossId = mesa.BossId,
                    Target = mesa.Target,
                };
                Record.Mesas.Add(_mesa);
            }
            Do(Engine.CheckSpin(), () => Engine.Spin());
        }

        internal void ConfirmSpin()
        {
            SpinResult result = null;
            Do(State.Mesa.Phase == MesaPhase.AwaitingNudge ? null : "Nenhum giro para confirmar.",
                () => result = Engine.ConfirmSpin());
            _mesa.Spins.Add(result.Total);
            _mesa.Score = State.Mesa.Score;
            Record.BestSpin = Math.Max(Record.BestSpin, result.Total);
            if (State.Mesa.Phase == MesaPhase.Won || State.Mesa.Phase == MesaPhase.Lost)
            {
                _mesa.Won = State.Mesa.Phase == MesaPhase.Won;
                if (_mesa.Won && _mesa.Index == Engine.Balance.Run.MesasPerAnte - 1)
                    Record.AntesCleared++;
                _mesa = null;
            }
        }

        // ── Loja ───────────────────────────────────────────────────────

        internal void EnterShop()
        {
            _offeredThisShop = new HashSet<string>();
            Record.ShopMoney.Add(State.Money);
            NoteOffers();
        }

        internal void LeaveShop() => Do(Engine.CheckLeaveShop(), Engine.LeaveShop);

        public void BuyRelic(int offer)
        {
            Do(Engine.CheckBuyRelic(offer), () => Engine.BuyRelic(offer));
            string id = State.Shop.Relics[offer].Id; // a oferta fica na loja, marcada como vendida
            RunRecord.Count(Record.RelicsBought, id);
            Record.RelicsEverHeld.Add(id);
        }

        public void BuyConsumable(int offer) => Do(Engine.CheckBuyConsumable(offer), () => Engine.BuyConsumable(offer));

        public PackOpening BuyPack()
        {
            PackOpening opening = null;
            Do(Engine.CheckBuyPack(), () => opening = Engine.BuyPack());
            return opening;
        }

        public void ChoosePackOption(int option, CardTargets targets = null)
        {
            PackOpening pack = State.Phase == RunPhase.Shop ? State.Shop.OpenPack : null;
            Do(Engine.CheckChoosePackOption(option, targets), () => Engine.ChoosePackOption(option, targets));
            string id = pack.Options[option];
            if (pack.PackId == PackIds.Amuletos)
            {
                RunRecord.Count(Record.RelicsPicked, id);
                Record.RelicsEverHeld.Add(id);
            }
            else
            {
                NoteConsumableUsed(id);
            }
        }

        public void SkipPack() =>
            Do(State.Phase == RunPhase.Shop && State.Shop.OpenPack != null ? null : "Nenhum pacote aberto.", Engine.SkipPack);

        public void Reroll()
        {
            Do(Engine.CheckReroll(), Engine.Reroll);
            Record.Rerolls++;
            NoteOffers();
        }

        // ── Inventário ─────────────────────────────────────────────────

        public void SellRelic(int slot) => Do(Engine.CheckSellRelic(slot), () => Engine.SellRelic(slot));

        public void SellConsumable(int slot) => Do(Engine.CheckSellConsumable(slot), () => Engine.SellConsumable(slot));

        public void MoveRelic(int from, int to)
        {
            int n = State.Relics.Count;
            string problem = !Engine.OutsideSpin ? "Não dá para reordenar agora."
                : from < 0 || from >= n || to < 0 || to >= n ? "Espaço vazio." : null;
            Do(problem, () => Engine.MoveRelic(from, to));
        }

        public void UseConsumable(int slot, CardTargets targets = null)
        {
            string id = slot >= 0 && slot < State.Consumables.Count ? State.Consumables[slot].Id : null;
            Do(Engine.CheckUseConsumable(slot, targets), () => Engine.UseConsumable(slot, targets));
            NoteConsumableUsed(id);
        }

        // ── Fim ────────────────────────────────────────────────────────

        internal void Finish()
        {
            Record.Won = State.Phase == RunPhase.Won;
            Record.FinalMoney = State.Money;
            Record.FinalRelics.AddRange(State.Relics.Select(r => r.Id));
        }

        void NoteOffers()
        {
            foreach (ShopOffer offer in State.Shop.Relics)
                if (_offeredThisShop.Add(offer.Id))
                    RunRecord.Count(Record.RelicsOffered, offer.Id);
        }

        void NoteConsumableUsed(string id)
        {
            RunRecord.Count(Record.ConsumablesUsed, id);
            if (Consumables.KindOf(id) == ConsumableKind.Tabua)
                Record.TabuasUsed++;
            else
                Record.HouseCardsUsed++;
        }

        void Do(string problem, Action action)
        {
            if (problem != null)
                throw new IllegalBotActionException(
                    $"[{Record.Bot} {Seed}] Ante {State.Ante} mesa {State.MesaIndex} ({State.Phase}): {problem}");
            action();
        }
    }
}
