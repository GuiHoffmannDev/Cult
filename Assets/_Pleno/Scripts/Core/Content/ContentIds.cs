using System.Collections.Generic;
using Pleno.Core.Model;

namespace Pleno.Core.Content
{
    public enum Rarity
    {
        Common,
        Uncommon,
        Rare,
        Legendary,
    }

    /// <summary>Raridade de cada amuleto com efeito implementado (GDD 7.1). Só estes entram na loja.</summary>
    public static class RelicCatalog
    {
        public static readonly IReadOnlyDictionary<string, Rarity> Rarities = new Dictionary<string, Rarity>
        {
            [RelicIds.FichaGasta] = Rarity.Common,
            [RelicIds.RosaRubra] = Rarity.Common,
            [RelicIds.ArranhaCeu] = Rarity.Common,
            [RelicIds.Teorema] = Rarity.Uncommon,
            [RelicIds.MiraLaser] = Rarity.Uncommon,
            [RelicIds.ComboTriplo] = Rarity.Uncommon,
            [RelicIds.Martingale] = Rarity.Common,
            [RelicIds.Colecionador] = Rarity.Uncommon,
            [RelicIds.LuvaDeSeda] = Rarity.Common,
            [RelicIds.ImaDeNeodimio] = Rarity.Uncommon,
        };
    }

    /// <summary>As 8 Tábuas: uma por tipo de aposta (GDD 6.1).</summary>
    public static class TabuaIds
    {
        public const string Chance = "tabua-chance";
        public const string DuziaColuna = "tabua-duzia-coluna";
        public const string Linha = "tabua-linha";
        public const string Vizinhos = "tabua-vizinhos";
        public const string Quadra = "tabua-quadra";
        public const string Rua = "tabua-rua";
        public const string Cavalo = "tabua-cavalo";
        public const string Pleno = "tabua-pleno";

        public static readonly IReadOnlyDictionary<string, BetType> BetTypes = new Dictionary<string, BetType>
        {
            [Chance] = BetType.SimpleChance,
            [DuziaColuna] = BetType.DozenColumn,
            [Linha] = BetType.Line,
            [Vizinhos] = BetType.Neighbors,
            [Quadra] = BetType.Corner,
            [Rua] = BetType.Street,
            [Cavalo] = BetType.Split,
            [Pleno] = BetType.Straight,
        };

        public static readonly IReadOnlyList<string> All = new[] { Chance, DuziaColuna, Linha, Vizinhos, Quadra, Rua, Cavalo, Pleno };
    }

    /// <summary>As 12 Cartas da Casa (GDD 6.2).</summary>
    public static class HouseCardIds
    {
        public const string Ourives = "ourives";
        public const string Vidraceiro = "vidraceiro";
        public const string Ferreiro = "ferreiro";
        public const string Ima = "ima";
        public const string Trevo = "trevo";
        public const string Pintor = "pintor";
        public const string Gemeo = "gemeo";
        public const string Coveiro = "coveiro";
        public const string Artesao = "artesao";
        public const string Tesoureiro = "tesoureiro";
        public const string Oraculo = "oraculo";
        public const string Fortuna = "fortuna";

        public static readonly IReadOnlyList<string> All = new[]
        {
            Ourives, Vidraceiro, Ferreiro, Ima, Trevo, Pintor, Gemeo, Coveiro, Artesao, Tesoureiro, Oraculo, Fortuna,
        };
    }

    /// <summary>Os 6 Crupiês-chefe da demo (GDD 8).</summary>
    public static class BossIds
    {
        public const string DuploZero = "duplo-zero";
        public const string Nevoa = "nevoa";
        public const string Contador = "contador";
        public const string MaoPesada = "mao-pesada";
        public const string Daltonico = "daltonico";
        public const string Relojoeiro = "relojoeiro";

        public static readonly IReadOnlyList<string> All = new[] { DuploZero, Nevoa, Contador, MaoPesada, Daltonico, Relojoeiro };
    }

    public static class PackIds
    {
        public const string Tabuas = "pacote-tabuas";
        public const string Casa = "pacote-casa";
        public const string Amuletos = "pacote-amuletos";

        public static readonly IReadOnlyList<string> All = new[] { Tabuas, Casa, Amuletos };
    }

    public enum ConsumableKind
    {
        Tabua,
        HouseCard,
    }

    public static class Consumables
    {
        public static ConsumableKind KindOf(string id) =>
            TabuaIds.BetTypes.ContainsKey(id) ? ConsumableKind.Tabua : ConsumableKind.HouseCard;

        public static int PriceOf(string id, Balance balance) =>
            KindOf(id) == ConsumableKind.Tabua ? balance.Shop.TabuaPrice : balance.Shop.HouseCardPrice;
    }
}
