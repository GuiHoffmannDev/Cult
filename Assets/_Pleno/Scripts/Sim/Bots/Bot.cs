using System;
using System.Collections.Generic;
using Pleno.Core.Rng;

namespace Pleno.Sim
{
    public enum BotKind
    {
        Aleatorio,
        Cobertura,
        Sniper,
        Guloso,
    }

    /// <summary>
    /// Um jogador automático. Joga só pelo <see cref="RunDriver"/> (que passa pelo <c>RunEngine</c>)
    /// e só lê o <c>RunState</c>. As escolhas aleatórias do bot usam um stream derivado da seed com
    /// nome próprio, nunca um dos sete streams da run: o bot não muda o que a run sorteia.
    /// </summary>
    public abstract class Bot
    {
        public abstract string Id { get; }

        /// <summary>Escolhas do bot (Aleatório) e sorte simulada nas avaliações (os outros).</summary>
        protected Pcg32 Rng { get; private set; }

        public virtual void BeginRun(RunDriver driver) => Rng = RngStreams.Derive(driver.Seed, "sim-bot:" + Id);

        /// <summary>Põe as fichas do giro. Tem de deixar pelo menos uma no pano.</summary>
        public abstract void PlaceBets(RunDriver driver);

        /// <summary>A bola parou: empurra (ou não).</summary>
        public abstract void Nudge(RunDriver driver);

        /// <summary>Compra, vende, usa e reordena. Tem de sair com o pacote fechado; o runner sai da loja.</summary>
        public abstract void Shop(RunDriver driver);

        public static Bot Create(BotKind kind)
        {
            switch (kind)
            {
                case BotKind.Aleatorio: return new RandomBot();
                case BotKind.Cobertura: return new CoverageBot();
                case BotKind.Sniper: return new SniperBot();
                case BotKind.Guloso: return new GreedyBot();
                default: throw new ArgumentOutOfRangeException(nameof(kind), kind, null);
            }
        }

        public static IReadOnlyList<BotKind> All { get; } =
            new[] { BotKind.Aleatorio, BotKind.Cobertura, BotKind.Sniper, BotKind.Guloso };

        public static string IdOf(BotKind kind)
        {
            switch (kind)
            {
                case BotKind.Aleatorio: return "aleatorio";
                case BotKind.Cobertura: return "cobertura";
                case BotKind.Sniper: return "sniper";
                case BotKind.Guloso: return "guloso";
                default: throw new ArgumentOutOfRangeException(nameof(kind), kind, null);
            }
        }
    }
}
