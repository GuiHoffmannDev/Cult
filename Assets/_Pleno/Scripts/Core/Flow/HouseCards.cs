using System;
using System.Collections.Generic;
using System.Linq;
using Pleno.Core.Content;
using Pleno.Core.Model;
using Pleno.Core.Rng;

namespace Pleno.Core.Flow
{
    /// <summary>Casas escolhidas pelo jogador ao usar uma carta (índices físicos da roda).</summary>
    public sealed class CardTargets
    {
        public List<int> Pockets { get; set; } = new List<int>();

        /// <summary>Gêmeo: a casa copiada. -1 nas outras cartas.</summary>
        public int Source { get; set; } = -1;

        public static readonly CardTargets None = new CardTargets();

        public static CardTargets Of(params int[] pockets) => new CardTargets { Pockets = pockets.ToList() };

        public static CardTargets Copy(int source, int target) =>
            new CardTargets { Pockets = new List<int> { target }, Source = source };
    }

    /// <summary>
    /// Efeitos das 12 Cartas da Casa (GDD 6.2) e das Tábuas (6.1). A casa temporária do chefe
    /// (Duplo Zero) não pode ser alvo nem origem de carta: ela sai no fim da mesa.
    /// Sorteios das cartas (Oráculo, Fortuna) usam o stream <c>luck</c>.
    /// </summary>
    public static class HouseCards
    {
        /// <summary>Número de casas-alvo: mínimo e máximo. (0, 0) = carta sem alvo.</summary>
        public static (int min, int max) TargetRange(string id, Balance balance)
        {
            CardBalance c = balance.Cards;
            switch (id)
            {
                case HouseCardIds.Ourives: return (c.OurivesTargets, c.OurivesTargets);
                case HouseCardIds.Vidraceiro: return (1, c.VidraceiroTargets);
                case HouseCardIds.Ferreiro: return (c.FerreiroTargets, c.FerreiroTargets);
                case HouseCardIds.Ima: return (c.ImaTargets, c.ImaTargets);
                case HouseCardIds.Trevo: return (1, c.TrevoTargets);
                case HouseCardIds.Pintor: return (1, c.PintorTargets);
                case HouseCardIds.Gemeo: return (1, 1);
                case HouseCardIds.Coveiro: return (1, c.CoveiroTargets);
                case HouseCardIds.Artesao: return (1, 1);
                case HouseCardIds.Tesoureiro:
                case HouseCardIds.Oraculo:
                case HouseCardIds.Fortuna:
                    return (0, 0);
                default:
                    throw new ArgumentException($"Carta desconhecida: '{id}'.", nameof(id));
            }
        }

        public static bool NeedsSource(string id) => id == HouseCardIds.Gemeo;

        /// <summary>Motivo pelo qual a carta não pode ser usada com esses alvos, ou null se pode.</summary>
        public static string Check(string id, RunState run, CardTargets targets, Balance balance)
        {
            targets = targets ?? CardTargets.None;
            Wheel wheel = run.Wheel;
            var (min, max) = TargetRange(id, balance);
            List<int> pockets = targets.Pockets;

            if (pockets.Count < min || pockets.Count > max)
                return min == max ? $"Escolha {min} casa(s)." : $"Escolha de {min} a {max} casas.";
            if (pockets.Distinct().Count() != pockets.Count)
                return "Cada casa só pode ser escolhida uma vez.";
            if (pockets.Any(i => i < 0 || i >= wheel.Count))
                return "Casa fora da roda.";
            if (pockets.Any(i => IsTemporary(run, wheel[i])))
                return "A casa do chefe não pode ser alvo.";

            if (NeedsSource(id))
            {
                if (targets.Source < 0 || targets.Source >= wheel.Count)
                    return "Escolha a casa a copiar.";
                if (pockets.Contains(targets.Source))
                    return "A casa copiada precisa ser outra.";
                if (IsTemporary(run, wheel[targets.Source]))
                    return "A casa do chefe não pode ser copiada.";
            }

            switch (id)
            {
                case HouseCardIds.Pintor:
                    if (pockets.Any(i => wheel[i].Color == PocketColor.Green))
                        return "O Pintor não pinta casas verdes.";
                    break;
                case HouseCardIds.Coveiro:
                    if (PermanentCount(run) - pockets.Count < balance.Wheel.MinPockets)
                        return $"A roda não pode ter menos de {balance.Wheel.MinPockets} casas.";
                    break;
                case HouseCardIds.Artesao:
                    if (PermanentCount(run) >= balance.Wheel.MaxPockets)
                        return $"A roda não pode ter mais de {balance.Wheel.MaxPockets} casas.";
                    break;
            }
            return null;
        }

        /// <summary>Aplica a carta. Chame <see cref="Check"/> antes; aqui um alvo inválido lança exceção.</summary>
        public static void Apply(string id, RunState run, CardTargets targets, Balance balance, IRandom luck)
        {
            string problem = Check(id, run, targets, balance);
            if (problem != null)
                throw new InvalidOperationException($"{id}: {problem}");

            targets = targets ?? CardTargets.None;
            Wheel wheel = run.Wheel;
            List<int> pockets = targets.Pockets;

            switch (id)
            {
                case HouseCardIds.Ourives: Enhance(wheel, pockets, PocketEnhancement.Gold); break;
                case HouseCardIds.Vidraceiro: Enhance(wheel, pockets, PocketEnhancement.Glass); break;
                case HouseCardIds.Ferreiro: Enhance(wheel, pockets, PocketEnhancement.Steel); break;
                case HouseCardIds.Ima: Enhance(wheel, pockets, PocketEnhancement.Magnetic); break;
                case HouseCardIds.Trevo: Enhance(wheel, pockets, PocketEnhancement.Lucky); break;

                case HouseCardIds.Pintor:
                    foreach (int i in pockets)
                        wheel[i].Color = wheel[i].Color == PocketColor.Red ? PocketColor.Black : PocketColor.Red;
                    break;

                case HouseCardIds.Gemeo:
                    wheel[pockets[0]].Number = wheel[targets.Source].Number;
                    wheel[pockets[0]].Color = wheel[targets.Source].Color;
                    break;

                case HouseCardIds.Coveiro:
                    // Do maior índice para o menor, para os índices restantes continuarem válidos.
                    foreach (int i in pockets.OrderByDescending(i => i))
                        wheel.RemoveAt(i, minimumPockets: 0);
                    break;

                case HouseCardIds.Artesao:
                    wheel.CloneAdjacent(pockets[0]);
                    break;

                case HouseCardIds.Tesoureiro:
                    run.Money += Math.Min(Math.Max(run.Money, 0), balance.Cards.TesoureiroMaxGain);
                    break;

                case HouseCardIds.Oraculo:
                    for (int n = 0; n < balance.Cards.OraculoTabuas && run.Consumables.Count < balance.Run.ConsumableSlots; n++)
                        run.Consumables.Add(new ConsumableInstance(luck.Pick(TabuaIds.All)));
                    break;

                case HouseCardIds.Fortuna:
                    if (!luck.OneIn(balance.Cards.FortunaOneIn))
                        break;
                    var plain = run.Relics.Where(r => r.Edition == PocketEdition.None).ToList();
                    if (plain.Count == 0)
                        break;
                    RelicInstance chosen = luck.Pick(plain);
                    chosen.Edition = luck.Pick(new[] { PocketEdition.Polished, PocketEdition.Holographic, PocketEdition.Polychrome });
                    break;
            }
        }

        /// <summary>Tábua: sobe o nível do tipo de aposta.</summary>
        public static void ApplyTabua(string id, RunState run, Balance balance)
        {
            if (!TabuaIds.BetTypes.TryGetValue(id, out BetType type))
                throw new ArgumentException($"Tábua desconhecida: '{id}'.", nameof(id));
            run.Levels.Raise(type, balance.Cards.TabuaLevels);
        }

        /// <summary>Aprimorar uma Casa Rachada a conserta.</summary>
        static void Enhance(Wheel wheel, List<int> pockets, PocketEnhancement enhancement)
        {
            foreach (int i in pockets)
            {
                wheel[i].Enhancement = enhancement;
                wheel[i].Cracked = false;
            }
        }

        static bool IsTemporary(RunState run, Pocket pocket) =>
            run.Mesa != null && run.Mesa.TemporaryPocketIds.Contains(pocket.Id);

        static int PermanentCount(RunState run) =>
            run.Wheel.Count - (run.Mesa?.TemporaryPocketIds.Count(id => run.Wheel.IndexOfId(id) >= 0) ?? 0);
    }
}
