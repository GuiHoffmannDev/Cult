namespace Pleno.Core.Model
{
    /// <summary>
    /// Os 8 tipos de aposta da tabela 3.3. É o tipo que tem nível (Tábuas): Vermelho e Par compartilham
    /// o nível de "Chance simples"; Dúzia e Coluna compartilham o de "Dúzia / Coluna".
    /// </summary>
    public enum BetType
    {
        SimpleChance,
        DozenColumn,
        Line,
        Neighbors,
        Corner,
        Street,
        Split,
        Straight,
    }

    /// <summary>Qual aposta externa. <see cref="None"/> para as internas e Vizinhos.</summary>
    public enum OutsideKind
    {
        None,
        Red,
        Black,
        Even,
        Odd,
        Low,
        High,
        Dozen,
        Column,
    }

    public static class BetTypeExtensions
    {
        /// <summary>Externas perdem quando cai 0 ou 00 (regra do zero).</summary>
        public static bool IsOutside(this BetType type) =>
            type == BetType.SimpleChance || type == BetType.DozenColumn;
    }
}
