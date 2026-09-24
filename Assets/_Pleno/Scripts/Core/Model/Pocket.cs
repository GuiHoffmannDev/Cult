using System;

namespace Pleno.Core.Model
{
    /// <summary>
    /// Uma casa da roda. As apostas de pano usam o <see cref="Number"/> e a <see cref="Color"/>, nunca
    /// a posição: pintar o 17 de preto faz ele contar para "Preto".
    /// </summary>
    [Serializable]
    public sealed class Pocket
    {
        public const int Zero = 0;
        public const int DoubleZero = -1;

        /// <summary>Identidade estável dentro da roda. Sobrevive a clonagens e remoções de outras casas.</summary>
        public int Id { get; internal set; }

        public int Number { get; set; }
        public PocketColor Color { get; set; }
        public PocketEnhancement Enhancement { get; set; }
        public PocketEdition Edition { get; set; }

        /// <summary>Peso base do sorteio (1.0). Modificadores como Magnética entram no <c>WheelRng</c>.</summary>
        public double Weight { get; set; } = 1.0;

        /// <summary>Casa Rachada: Vidro que quebrou. A bola sorteada aqui salta para uma vizinha.</summary>
        public bool Cracked { get; set; }

        public Pocket()
        {
        }

        public Pocket(int number, PocketColor color)
        {
            Number = number;
            Color = color;
        }

        /// <summary>0 ou 00: faz as apostas externas perderem.</summary>
        public bool IsZero => Number == Zero || Number == DoubleZero;

        public string Label => FormatNumber(Number);

        public static string FormatNumber(int number) => number == DoubleZero ? "00" : number.ToString();

        /// <summary>Cópia com todas as propriedades; o Id é atribuído pela roda ao inserir.</summary>
        public Pocket CopyContents() => new Pocket(Number, Color)
        {
            Enhancement = Enhancement,
            Edition = Edition,
            Weight = Weight,
            Cracked = Cracked,
        };

        public override string ToString() => $"#{Id} {Label} {Color} {Enhancement} {Edition}";
    }
}
