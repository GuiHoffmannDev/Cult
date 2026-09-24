using System;
using System.Linq;
using Pleno.Core.Content;
using Pleno.Core.Effects;
using Pleno.Core.Events;
using Pleno.Core.Model;
using Pleno.Core.Rng;
using Pleno.Core.Rules;

namespace Pleno.Core.Flow
{
    /// <summary>
    /// As regras da run sobre o <see cref="RunState"/>: mesas, giros, empurrões, loja, pacotes e
    /// consumíveis. Cada ação tem um <c>Check…</c> que devolve o motivo da recusa (ou null); a ação
    /// lança <see cref="InvalidOperationException"/> com esse motivo se for chamada assim mesmo.
    /// </summary>
    public sealed class RunEngine
    {
        public RunState State { get; }
        public Balance Balance { get; }

        public RunEngine(RunState state, Balance balance = null)
        {
            State = state ?? throw new ArgumentNullException(nameof(state));
            Balance = balance ?? Balance.Default;
        }

        /// <summary>Run nova: roda inicial, chefes de cada Ante (sem repetir) e a primeira mesa aberta.</summary>
        public static RunEngine NewRun(string seed, StartingWheel startingWheel, Balance balance = null)
        {
            balance = balance ?? Balance.Default;
            var rng = new RngStreams(seed);
            var state = new RunState
            {
                Seed = rng.Seed,
                Rng = rng,
                StartingWheel = startingWheel,
                Money = balance.Run.StartingMoney,
                Wheel = StartingWheels.Build(startingWheel, balance, rng.Get(RngStreams.Wheel)),
                BossPlan = rng.Get(RngStreams.Boss)
                    .PickDistinct(BossIds.All.Count, balance.Run.Antes)
                    .Select(i => BossIds.All[i])
                    .ToList(),
            };
            var engine = new RunEngine(state, balance);
            engine.StartMesa();
            return engine;
        }

        MesaState Mesa => State.Mesa;
        ShopState Shop => State.Shop;
        Pcg32 Stream(string name) => State.Rng.Get(name);

        public IBossRule Boss => Mesa?.BossId == null ? null : BossRuleRegistry.Get(Mesa.BossId);

        public double TargetFor(int ante, int mesaIndex) => Balance.Run.Targets[ante - 1][mesaIndex];

        public bool IsLastMesa =>
            State.Ante == Balance.Run.Antes && State.MesaIndex == Balance.Run.MesasPerAnte - 1;

        bool InBetting => State.Phase == RunPhase.Mesa && Mesa.Phase == MesaPhase.Betting;
        bool InShop => State.Phase == RunPhase.Shop && Shop.OpenPack == null;

        /// <summary>"A qualquer momento fora do giro": apostando ou na loja (sem pacote aberto).</summary>
        public bool OutsideSpin => InBetting || InShop;

        // ── Mesa ───────────────────────────────────────────────────────

        /// <summary>Abre a mesa de <see cref="RunState.Ante"/>/<see cref="RunState.MesaIndex"/>. Interno: os testes o usam para pular direto a uma mesa.</summary>
        internal void StartMesa()
        {
            var kind = State.MesaIndex == Balance.Run.MesasPerAnte - 1 ? MesaKind.Boss
                : State.MesaIndex == 0 ? MesaKind.Small : MesaKind.Big;
            string bossId = kind == MesaKind.Boss ? State.BossPlan[State.Ante - 1] : null;
            IBossRule boss = bossId == null ? null : BossRuleRegistry.Get(bossId);

            MesaResources resources = MesaRules.Resources(Balance, State.StartingWheel, State.Relics, boss);
            var mesa = new MesaState
            {
                Kind = kind,
                Target = TargetFor(State.Ante, State.MesaIndex),
                Resources = resources,
                SpinsLeft = resources.Spins,
                NudgesLeft = resources.Nudges,
                BossId = bossId,
                Phase = MesaPhase.Betting,
            };
            State.Mesa = mesa;
            State.Shop = null;
            State.Phase = RunPhase.Mesa;
            boss?.OnMesaStart(State, mesa);
        }

        public string CheckPlace(PlacedBet bet)
        {
            if (!InBetting)
                return "Fora da fase de apostas.";
            if (bet.Chips > Mesa.ChipsFree)
                return $"Só restam {Mesa.ChipsFree} ficha(s) neste giro.";
            return null;
        }

        /// <summary>
        /// Põe <c>bet.Chips</c> fichas na aposta. Fichas numa aposta que já existe se empilham nela, e
        /// ela mantém a posição da primeira ficha (a ordem de resolução).
        /// </summary>
        public void Place(PlacedBet bet)
        {
            Require(CheckPlace(bet));
            int i = Mesa.Bets.FindIndex(b => b.SourceId == bet.SourceId);
            if (i >= 0)
                Mesa.Bets[i] = Mesa.Bets[i].WithChips(Mesa.Bets[i].Chips + bet.Chips);
            else
                Mesa.Bets.Add(bet);
        }

        /// <summary>Tira fichas de uma aposta; ela sai do pano quando fica sem fichas.</summary>
        public void Remove(string sourceId, int chips = 1)
        {
            Require(InBetting ? null : "Fora da fase de apostas.");
            int i = Mesa.Bets.FindIndex(b => b.SourceId == sourceId);
            if (i < 0)
                return;
            int left = Mesa.Bets[i].Chips - chips;
            if (left > 0)
                Mesa.Bets[i] = Mesa.Bets[i].WithChips(left);
            else
                Mesa.Bets.RemoveAt(i);
        }

        public void ClearBets()
        {
            Require(InBetting ? null : "Fora da fase de apostas.");
            Mesa.Bets.Clear();
        }

        public string CheckSpin()
        {
            if (!InBetting)
                return "Fora da fase de apostas.";
            if (Mesa.ChipsPlaced < Balance.Mesa.MinChipsToSpin)
                return "Coloque pelo menos uma ficha.";
            return null;
        }

        /// <summary>Sorteia a casa (stream <c>wheel</c>). A bola para ali e espera empurrão ou confirmação.</summary>
        public SpinDraw Spin()
        {
            Require(CheckSpin());
            double[] weights = WheelRng.Weights(State.Wheel, Balance, State.Relics);
            SpinDraw draw = WheelRng.Draw(State.Wheel, weights, Stream(RngStreams.Wheel));
            Mesa.DrawnIndex = draw.DrawnIndex;
            Mesa.BallIndex = draw.LandingIndex;
            Mesa.Phase = MesaPhase.AwaitingNudge;
            return draw;
        }

        public string CheckNudge(int direction)
        {
            if (State.Phase != RunPhase.Mesa || Mesa.Phase != MesaPhase.AwaitingNudge)
                return "A bola não está parada.";
            if (direction != -1 && direction != 1)
                return "O empurrão move a bola 1 casa para um dos lados.";
            if (Boss != null && Boss.NudgesDisabled)
                return "O Crupiê não deixa empurrar.";
            if (Mesa.NudgesLeft <= 0)
                return "Sem empurrões nesta mesa.";
            return null;
        }

        /// <summary>Move a bola 1 casa na ordem física da roda. Não há salto de Casa Rachada no empurrão.</summary>
        public int Nudge(int direction)
        {
            Require(CheckNudge(direction));
            Mesa.BallIndex = State.Wheel.Neighbor(Mesa.BallIndex, direction);
            Mesa.NudgesLeft--;
            State.Stats.NudgesUsed++;
            return Mesa.BallIndex;
        }

        /// <summary>Pontua o giro onde a bola está, e decide se a mesa foi vencida ou perdida.</summary>
        public SpinResult ConfirmSpin()
        {
            Require(State.Phase == RunPhase.Mesa && Mesa.Phase == MesaPhase.AwaitingNudge ? null : "Nenhum giro para confirmar.");
            IBossRule boss = Boss;

            SpinResult result = ScoringPipeline.Resolve(new SpinInput
            {
                Wheel = State.Wheel,
                LandingIndex = Mesa.BallIndex,
                Bets = Mesa.Bets.ToList(),
                Relics = State.Relics,
                Levels = State.Levels,
                Balance = Balance,
                Luck = Stream(RngStreams.Luck),
                BetFilter = boss == null ? null : (Func<PlacedBet, bool>)boss.BetScores,
            });

            State.Money += result.MoneyDelta;
            Mesa.Score += result.Total;
            Mesa.SpinsLeft--;
            Mesa.BestSpin = Math.Max(Mesa.BestSpin, result.Total);
            State.Stats.SpinsPlayed++;
            State.Stats.BestSpin = Math.Max(State.Stats.BestSpin, result.Total);
            Mesa.Bets.Clear();
            Mesa.DrawnIndex = Mesa.BallIndex = -1;

            if (Mesa.Score >= Mesa.Target)
                WinMesa();
            else if (Mesa.SpinsLeft <= 0)
                LoseMesa();
            else
                Mesa.Phase = MesaPhase.Betting;
            return result;
        }

        void WinMesa()
        {
            Mesa.Payout = Economy.Payout(State.Money, State.MesaIndex, Mesa.SpinsLeft, Balance);
            State.Money += Mesa.Payout.Total;
            Boss?.OnMesaEnd(State, Mesa);
            Mesa.Phase = MesaPhase.Won;
            State.Stats.MesasWon++;

            if (IsLastMesa)
            {
                State.Phase = RunPhase.Won;
                return;
            }
            State.Shop = ShopGenerator.Generate(State, Balance, Stream(RngStreams.Shop));
            State.Phase = RunPhase.Shop;
        }

        void LoseMesa()
        {
            Boss?.OnMesaEnd(State, Mesa);
            Mesa.Phase = MesaPhase.Lost;
            State.Phase = RunPhase.Lost;
        }

        // ── Loja ───────────────────────────────────────────────────────

        public string CheckLeaveShop()
        {
            if (State.Phase != RunPhase.Shop)
                return "Não está na loja.";
            if (Shop.OpenPack != null)
                return "Escolha um item do pacote ou pule.";
            return null;
        }

        /// <summary>Sai da loja para a próxima mesa (depois do Chefe, o próximo Ante).</summary>
        public void LeaveShop()
        {
            Require(CheckLeaveShop());
            State.MesaIndex++;
            if (State.MesaIndex >= Balance.Run.MesasPerAnte)
            {
                State.Ante++;
                State.MesaIndex = 0;
            }
            StartMesa();
        }

        public string CheckBuyRelic(int offer)
        {
            if (!InShop)
                return "Não está na loja.";
            if (offer < 0 || offer >= Shop.Relics.Count || Shop.Relics[offer].Sold)
                return "Item indisponível.";
            if (State.Money < Shop.Relics[offer].Price)
                return "Dinheiro insuficiente.";
            if (State.Relics.Count >= Balance.Run.RelicSlots)
                return "Sem espaço de amuleto.";
            return null;
        }

        public void BuyRelic(int offer)
        {
            Require(CheckBuyRelic(offer));
            ShopOffer item = Shop.Relics[offer];
            State.Money -= item.Price;
            State.Relics.Add(new RelicInstance(item.Id));
            item.Sold = true;
        }

        public string CheckBuyConsumable(int offer)
        {
            if (!InShop)
                return "Não está na loja.";
            if (offer < 0 || offer >= Shop.Consumables.Count || Shop.Consumables[offer].Sold)
                return "Item indisponível.";
            if (State.Money < Shop.Consumables[offer].Price)
                return "Dinheiro insuficiente.";
            if (State.Consumables.Count >= Balance.Run.ConsumableSlots)
                return "Sem espaço de consumível.";
            return null;
        }

        public void BuyConsumable(int offer)
        {
            Require(CheckBuyConsumable(offer));
            ShopOffer item = Shop.Consumables[offer];
            State.Money -= item.Price;
            State.Consumables.Add(new ConsumableInstance(item.Id));
            item.Sold = true;
        }

        public string CheckBuyPack()
        {
            if (!InShop)
                return "Não está na loja.";
            if (Shop.Pack == null || Shop.Pack.Sold)
                return "Item indisponível.";
            if (State.Money < Shop.Pack.Price)
                return "Dinheiro insuficiente.";
            return null;
        }

        /// <summary>Compra e abre o pacote (conteúdo no stream <c>pack</c>).</summary>
        public PackOpening BuyPack()
        {
            Require(CheckBuyPack());
            State.Money -= Shop.Pack.Price;
            Shop.Pack.Sold = true;
            Shop.OpenPack = ShopGenerator.OpenPack(Shop.Pack.Id, State, Balance, Stream(RngStreams.Pack));
            return Shop.OpenPack;
        }

        public string CheckChoosePackOption(int option, CardTargets targets = null)
        {
            if (State.Phase != RunPhase.Shop || Shop.OpenPack == null)
                return "Nenhum pacote aberto.";
            if (option < 0 || option >= Shop.OpenPack.Options.Count)
                return "Opção inválida.";
            string id = Shop.OpenPack.Options[option];
            switch (Shop.OpenPack.PackId)
            {
                case PackIds.Amuletos:
                    return State.Relics.Count >= Balance.Run.RelicSlots ? "Sem espaço de amuleto." : null;
                case PackIds.Casa:
                    return HouseCards.Check(id, State, targets, Balance);
                default:
                    return null;
            }
        }

        /// <summary>
        /// Escolhe uma opção do pacote. Tábua e Carta da Casa são usadas na hora; o amuleto vai para o
        /// próximo espaço livre.
        /// </summary>
        public void ChoosePackOption(int option, CardTargets targets = null)
        {
            Require(CheckChoosePackOption(option, targets));
            PackOpening pack = Shop.OpenPack;
            string id = pack.Options[option];
            Shop.OpenPack = null;

            switch (pack.PackId)
            {
                case PackIds.Amuletos:
                    State.Relics.Add(new RelicInstance(id));
                    break;
                case PackIds.Tabuas:
                    HouseCards.ApplyTabua(id, State, Balance);
                    break;
                case PackIds.Casa:
                    HouseCards.Apply(id, State, targets, Balance, Stream(RngStreams.Luck));
                    break;
            }
        }

        /// <summary>Fecha o pacote sem escolher nada.</summary>
        public void SkipPack()
        {
            Require(State.Phase == RunPhase.Shop && Shop.OpenPack != null ? null : "Nenhum pacote aberto.");
            Shop.OpenPack = null;
        }

        public int RerollCost => Shop == null ? 0 : Economy.RerollCost(Shop.Rerolls, Balance.Shop);

        public string CheckReroll()
        {
            if (!InShop)
                return "Não está na loja.";
            if (State.Money < RerollCost)
                return "Dinheiro insuficiente.";
            return null;
        }

        public void Reroll()
        {
            Require(CheckReroll());
            State.Money -= RerollCost;
            Shop.Rerolls++;
            ShopGenerator.Reroll(Shop, State, Balance, Stream(RngStreams.Shop));
        }

        // ── Inventário (fora do giro) ─────────────────────────────────

        public string CheckSellRelic(int slot)
        {
            if (!OutsideSpin)
                return "Não dá para vender agora.";
            return slot < 0 || slot >= State.Relics.Count ? "Espaço vazio." : null;
        }

        public int SellRelic(int slot)
        {
            Require(CheckSellRelic(slot));
            int value = Economy.SellPrice(Economy.RelicPrice(State.Relics[slot].Id, Balance), Balance.Run);
            State.Relics.RemoveAt(slot);
            State.Money += value;
            return value;
        }

        public string CheckSellConsumable(int slot)
        {
            if (!OutsideSpin)
                return "Não dá para vender agora.";
            return slot < 0 || slot >= State.Consumables.Count ? "Espaço vazio." : null;
        }

        public int SellConsumable(int slot)
        {
            Require(CheckSellConsumable(slot));
            int value = Economy.SellPrice(Consumables.PriceOf(State.Consumables[slot].Id, Balance), Balance.Run);
            State.Consumables.RemoveAt(slot);
            State.Money += value;
            return value;
        }

        /// <summary>Reordena os amuletos (a ordem é a de resolução).</summary>
        public void MoveRelic(int from, int to)
        {
            Require(OutsideSpin ? null : "Não dá para reordenar agora.");
            if (from < 0 || from >= State.Relics.Count || to < 0 || to >= State.Relics.Count)
                throw new ArgumentOutOfRangeException(nameof(from));
            RelicInstance relic = State.Relics[from];
            State.Relics.RemoveAt(from);
            State.Relics.Insert(to, relic);
        }

        public string CheckUseConsumable(int slot, CardTargets targets = null)
        {
            if (!OutsideSpin)
                return "Consumíveis só fora do giro.";
            if (slot < 0 || slot >= State.Consumables.Count)
                return "Espaço vazio.";
            string id = State.Consumables[slot].Id;
            return Consumables.KindOf(id) == ConsumableKind.Tabua ? null : HouseCards.Check(id, State, targets, Balance);
        }

        /// <summary>Usa o consumível; o espaço se libera antes do efeito (o Oráculo pode ocupá-lo).</summary>
        public void UseConsumable(int slot, CardTargets targets = null)
        {
            Require(CheckUseConsumable(slot, targets));
            string id = State.Consumables[slot].Id;
            State.Consumables.RemoveAt(slot);
            if (Consumables.KindOf(id) == ConsumableKind.Tabua)
                HouseCards.ApplyTabua(id, State, Balance);
            else
                HouseCards.Apply(id, State, targets, Balance, Stream(RngStreams.Luck));
        }

        static void Require(string problem)
        {
            if (problem != null)
                throw new InvalidOperationException(problem);
        }
    }
}
