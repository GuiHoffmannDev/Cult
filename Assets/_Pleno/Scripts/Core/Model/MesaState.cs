using System.Collections.Generic;
using System.Linq;

namespace Pleno.Core.Model
{
    public enum MesaKind
    {
        Small,
        Big,
        Boss,
    }

    public enum MesaPhase
    {
        Betting,

        /// <summary>A bola parou (já sorteada); o jogador pode empurrar antes de confirmar.</summary>
        AwaitingNudge,

        Won,
        Lost,
    }

    public sealed class MesaState
    {
        public MesaKind Kind { get; set; }
        public double Target { get; set; }

        /// <summary>Total acumulado da mesa.</summary>
        public double Score { get; set; }

        /// <summary>Recursos já com roda inicial, amuletos e chefe aplicados.</summary>
        public MesaResources Resources { get; set; }

        public int SpinsLeft { get; set; }
        public int NudgesLeft { get; set; }
        public MesaPhase Phase { get; set; }

        /// <summary>Id do chefe (só na Mesa do Chefe).</summary>
        public string BossId { get; set; }

        /// <summary>Apostas do giro atual, na ordem em que foram colocadas.</summary>
        public List<PlacedBet> Bets { get; set; } = new List<PlacedBet>();

        /// <summary>Casa sorteada e casa atual da bola, enquanto em <see cref="MesaPhase.AwaitingNudge"/>.</summary>
        public int DrawnIndex { get; set; } = -1;

        public int BallIndex { get; set; } = -1;

        /// <summary>Casas que o chefe pôs na roda e que saem no fim da mesa (Ids).</summary>
        public List<int> TemporaryPocketIds { get; set; } = new List<int>();

        public double BestSpin { get; set; }

        /// <summary>Preenchido quando a mesa é vencida.</summary>
        public MesaPayout Payout { get; set; }

        public int ChipsPlaced => Bets.Sum(b => b.Chips);
        public int ChipsFree => Resources.ChipsPerSpin - ChipsPlaced;
    }

    /// <summary>O que a mesa vencida paga (GDD 4.2).</summary>
    public sealed class MesaPayout
    {
        public int Reward { get; set; }
        public int UnusedSpins { get; set; }
        public int Interest { get; set; }
        public int Total => Reward + UnusedSpins + Interest;
    }
}
