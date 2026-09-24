using System;
using System.Collections.Generic;
using System.Linq;
using Pleno.Core.Content;
using Pleno.Core.Model;
using Pleno.Core.Rng;

namespace Pleno.Core.Flow
{
    /// <summary>
    /// Sorteia a loja (stream <c>shop</c>) e o conteúdo dos pacotes (stream <c>pack</c>).
    /// Um amuleto nunca é oferecido se o jogador já o tem ou se já está à venda.
    /// </summary>
    public static class ShopGenerator
    {
        public static ShopState Generate(RunState run, Balance balance, IRandom shopRng)
        {
            var shop = new ShopState();
            FillOffers(shop, run, balance, shopRng);

            var packWeights = PackIds.All.Select(id => balance.Shop.Packs[id].Weight).ToArray();
            string packId = PackIds.All[shopRng.PickWeightedIndex(packWeights)];
            shop.Pack = new ShopOffer(packId, balance.Shop.Packs[packId].Price);
            return shop;
        }

        /// <summary>Reroll: troca amuletos e consumíveis; o pacote fica.</summary>
        public static void Reroll(ShopState shop, RunState run, Balance balance, IRandom shopRng)
        {
            shop.Relics.Clear();
            shop.Consumables.Clear();
            FillOffers(shop, run, balance, shopRng);
        }

        static void FillOffers(ShopState shop, RunState run, Balance balance, IRandom rng)
        {
            var excluded = new HashSet<string>(run.Relics.Select(r => r.Id));
            foreach (string id in PickRelics(balance.Shop.RelicOffers, excluded, balance, rng, allowLegendary: false))
            {
                shop.Relics.Add(new ShopOffer(id, Economy.RelicPrice(id, balance)));
                excluded.Add(id);
            }

            for (int i = 0; i < balance.Shop.ConsumableOffers; i++)
            {
                string id = PickConsumable(balance, rng);
                shop.Consumables.Add(new ShopOffer(id, Consumables.PriceOf(id, balance)));
            }
        }

        /// <summary>Espaço de consumível: sorteia o tipo pelo peso, depois um item uniforme do tipo.</summary>
        static string PickConsumable(Balance balance, IRandom rng)
        {
            int kind = rng.PickWeightedIndex(new[] { balance.Shop.TabuaWeight, balance.Shop.HouseCardWeight });
            return kind == 0 ? rng.Pick(TabuaIds.All) : rng.Pick(HouseCardIds.All);
        }

        /// <summary>
        /// Até <paramref name="count"/> amuletos diferentes fora de <paramref name="excluded"/>. A raridade é
        /// sorteada só entre raridades que ainda têm amuleto disponível, mantendo a proporção entre elas.
        /// </summary>
        public static List<string> PickRelics(int count, ISet<string> excluded, Balance balance, IRandom rng, bool allowLegendary)
        {
            var picked = new List<string>();
            var blocked = new HashSet<string>(excluded);
            for (int n = 0; n < count; n++)
            {
                var byRarity = RelicCatalog.Rarities
                    .Where(pair => !blocked.Contains(pair.Key))
                    .GroupBy(pair => pair.Value)
                    .ToDictionary(g => g.Key, g => g.Select(pair => pair.Key).OrderBy(id => id, StringComparer.Ordinal).ToList());

                if (allowLegendary && byRarity.TryGetValue(Rarity.Legendary, out List<string> legendaries)
                    && rng.NextDouble() < balance.Shop.LegendaryChanceInRelicPack)
                {
                    string legendary = rng.Pick(legendaries);
                    picked.Add(legendary);
                    blocked.Add(legendary);
                    continue;
                }

                var rarities = byRarity.Keys
                    .Where(r => r != Rarity.Legendary && balance.Shop.RarityWeights.ContainsKey(r))
                    .OrderBy(r => r)
                    .ToList();
                if (rarities.Count == 0)
                    break;

                Rarity rarity = rarities[rng.PickWeightedIndex(rarities.Select(r => balance.Shop.RarityWeights[r]).ToArray())];
                string id = rng.Pick(byRarity[rarity]);
                picked.Add(id);
                blocked.Add(id);
            }
            return picked;
        }

        /// <summary>Opções de um pacote, todas diferentes entre si.</summary>
        public static PackOpening OpenPack(string packId, RunState run, Balance balance, IRandom packRng)
        {
            int options = balance.Shop.Packs[packId].Options;
            var opening = new PackOpening { PackId = packId };
            switch (packId)
            {
                case PackIds.Tabuas:
                    opening.Options.AddRange(packRng.PickDistinct(TabuaIds.All.Count, options).Select(i => TabuaIds.All[i]));
                    break;
                case PackIds.Casa:
                    opening.Options.AddRange(packRng.PickDistinct(HouseCardIds.All.Count, options).Select(i => HouseCardIds.All[i]));
                    break;
                case PackIds.Amuletos:
                    var excluded = new HashSet<string>(run.Relics.Select(r => r.Id));
                    if (run.Shop != null)
                        excluded.UnionWith(run.Shop.Relics.Where(o => !o.Sold).Select(o => o.Id));
                    opening.Options.AddRange(PickRelics(options, excluded, balance, packRng, allowLegendary: true));
                    break;
                default:
                    throw new ArgumentException($"Pacote desconhecido: '{packId}'.", nameof(packId));
            }
            return opening;
        }
    }
}
