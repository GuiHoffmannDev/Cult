using System.Collections.Generic;
using Pleno.Core.Model;

namespace Pleno.Core.Events
{
    /// <summary>O giro inteiro, calculado antes de qualquer animação.</summary>
    public sealed class SpinResult
    {
        /// <summary>Índice físico onde a bola terminou.</summary>
        public int PocketIndex { get; internal set; }

        public IReadOnlyList<ScoreStep> Steps { get; internal set; }
        public IReadOnlyList<PlacedBet> WinningBets { get; internal set; }
        public bool IsWin => WinningBets.Count > 0;

        public double Pontos { get; internal set; }
        public double Mult { get; internal set; }

        /// <summary>floor(Pontos × Mult); 0 num giro sem vitória.</summary>
        public double Total { get; internal set; }

        public int MoneyDelta { get; internal set; }

        /// <summary>O Vidro da casa sorteada quebrou e ela virou Casa Rachada.</summary>
        public bool GlassBroke { get; internal set; }
    }
}
