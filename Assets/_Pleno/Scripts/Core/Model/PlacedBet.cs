using System;
using System.Collections.Generic;
using System.Linq;
using Pleno.Core.Rules;

namespace Pleno.Core.Model
{
    /// <summary>
    /// Uma aposta no pano com suas fichas. Fichas empilhadas multiplicam os Pontos da aposta, não o Mult.
    /// Crie pelas fábricas (<see cref="Red"/>, <see cref="Straight"/>…), que validam a geometria do pano.
    /// </summary>
    public sealed class PlacedBet
    {
        static readonly int[] NoNumbers = new int[0];

        public BetType Type { get; }
        public OutsideKind Outside { get; }

        /// <summary>1 a 3 para Dúzia e Coluna; 0 nas demais.</summary>
        public int Index { get; }

        /// <summary>Números cobertos pelas apostas internas de pano, em ordem crescente (00 = -1).</summary>
        public IReadOnlyList<int> Numbers { get; }

        /// <summary>Vizinhos: Id da casa central. A aposta cobre as 5 posições físicas em volta dela.</summary>
        public int AnchorPocketId { get; }

        public int Chips { get; }

        /// <summary>Identifica a aposta no pano e nos ScoreSteps, ex.: "bet:pleno:19".</summary>
        public string SourceId { get; }

        public PlacedBet(BetType type, OutsideKind outside, int index, IEnumerable<int> numbers, int anchorPocketId, int chips)
        {
            if (chips < 1)
                throw new ArgumentOutOfRangeException(nameof(chips), chips, "Uma aposta tem pelo menos 1 ficha.");
            Type = type;
            Outside = outside;
            Index = index;
            Numbers = numbers == null ? NoNumbers : numbers.OrderBy(n => n).ToArray();
            AnchorPocketId = anchorPocketId;
            Chips = chips;
            SourceId = BuildSourceId();
            Validate();
        }

        public PlacedBet WithChips(int chips) => new PlacedBet(Type, Outside, Index, Numbers, AnchorPocketId, chips);

        public static PlacedBet Red(int chips) => Outer(OutsideKind.Red, chips);
        public static PlacedBet Black(int chips) => Outer(OutsideKind.Black, chips);
        public static PlacedBet Even(int chips) => Outer(OutsideKind.Even, chips);
        public static PlacedBet Odd(int chips) => Outer(OutsideKind.Odd, chips);
        public static PlacedBet Low(int chips) => Outer(OutsideKind.Low, chips);
        public static PlacedBet High(int chips) => Outer(OutsideKind.High, chips);

        public static PlacedBet Dozen(int index, int chips) =>
            new PlacedBet(BetType.DozenColumn, OutsideKind.Dozen, index, null, 0, chips);

        public static PlacedBet Column(int index, int chips) =>
            new PlacedBet(BetType.DozenColumn, OutsideKind.Column, index, null, 0, chips);

        public static PlacedBet Straight(int number, int chips) => Inside(BetType.Straight, chips, number);
        public static PlacedBet Split(int a, int b, int chips) => Inside(BetType.Split, chips, a, b);
        public static PlacedBet Street(int chips, params int[] numbers) => Inside(BetType.Street, chips, numbers);
        public static PlacedBet Corner(int chips, params int[] numbers) => Inside(BetType.Corner, chips, numbers);

        /// <summary>Linha: as duas ruas que começam em <paramref name="firstNumber"/> (1, 4, …, 31).</summary>
        public static PlacedBet Line(int firstNumber, int chips) =>
            Inside(BetType.Line, chips, Enumerable.Range(firstNumber, 6).ToArray());

        public static PlacedBet Neighbors(int anchorPocketId, int chips) =>
            new PlacedBet(BetType.Neighbors, OutsideKind.None, 0, null, anchorPocketId, chips);

        static PlacedBet Outer(OutsideKind kind, int chips) =>
            new PlacedBet(BetType.SimpleChance, kind, 0, null, 0, chips);

        static PlacedBet Inside(BetType type, int chips, params int[] numbers) =>
            new PlacedBet(type, OutsideKind.None, 0, numbers, 0, chips);

        void Validate()
        {
            switch (Type)
            {
                case BetType.SimpleChance:
                    if (Outside < OutsideKind.Red || Outside > OutsideKind.High)
                        throw Invalid("chance simples sem alvo");
                    break;
                case BetType.DozenColumn:
                    if ((Outside != OutsideKind.Dozen && Outside != OutsideKind.Column) || Index < 1 || Index > 3)
                        throw Invalid("dúzia/coluna precisa de índice 1 a 3");
                    break;
                case BetType.Neighbors:
                    break;
                default:
                    if (!TableLayout.IsValidInside(Type, Numbers))
                        throw Invalid($"combinação fora do pano: {string.Join(",", Numbers.Select(Pocket.FormatNumber))}");
                    break;
            }
        }

        ArgumentException Invalid(string reason) => new ArgumentException($"Aposta {Type} inválida: {reason}.");

        string BuildSourceId()
        {
            switch (Type)
            {
                case BetType.SimpleChance: return "bet:" + OutsideSlug(Outside);
                case BetType.DozenColumn: return $"bet:{OutsideSlug(Outside)}:{Index}";
                case BetType.Neighbors: return $"bet:vizinhos:#{AnchorPocketId}";
                default: return $"bet:{TypeSlug(Type)}:{string.Join("-", Numbers.Select(Pocket.FormatNumber))}";
            }
        }

        static string OutsideSlug(OutsideKind kind)
        {
            switch (kind)
            {
                case OutsideKind.Red: return "vermelho";
                case OutsideKind.Black: return "preto";
                case OutsideKind.Even: return "par";
                case OutsideKind.Odd: return "impar";
                case OutsideKind.Low: return "baixo";
                case OutsideKind.High: return "alto";
                case OutsideKind.Dozen: return "duzia";
                case OutsideKind.Column: return "coluna";
                default: return "nenhuma";
            }
        }

        static string TypeSlug(BetType type)
        {
            switch (type)
            {
                case BetType.Line: return "linha";
                case BetType.Corner: return "quadra";
                case BetType.Street: return "rua";
                case BetType.Split: return "cavalo";
                case BetType.Straight: return "pleno";
                default: return type.ToString().ToLowerInvariant();
            }
        }

        public override string ToString() => $"{SourceId} x{Chips}";
    }
}
