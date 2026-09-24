using System;
using Pleno.Core.Content;
using Pleno.Core.Events;
using Pleno.Core.Model;

namespace Pleno.Core.Rules
{
    /// <summary>
    /// O que um amuleto enxerga e pode fazer durante um gancho. Cada efeito vira um
    /// <see cref="ScoreStep"/> com a origem do amuleto.
    /// </summary>
    public sealed class RelicScope
    {
        readonly ScoreAccumulator _acc;
        readonly bool _scoring;

        public RelicInstance Self { get; }

        /// <summary>Espaço do amuleto, da esquerda (0) para a direita.</summary>
        public int Slot { get; }

        public SpinContext Spin { get; }
        public Balance Balance => Spin.Balance;
        public RelicBalance Values => Spin.Balance.Relics;

        string SourceId => "relic:" + Self.Id;

        internal RelicScope(RelicInstance self, int slot, SpinContext spin, ScoreAccumulator acc, bool scoring)
        {
            Self = self;
            Slot = slot;
            Spin = spin;
            _acc = acc;
            _scoring = scoring;
        }

        public void AddPontos(double amount) => Score(pontos: amount);
        public void AddMult(double amount) => Score(mult: amount);
        public void MultiplyMult(double factor) => Score(multX: factor);

        /// <summary>Dinheiro é efeito de pós-giro (passo 5) e só pode sair do OnAfterSpin.</summary>
        public void AddMoney(int amount)
        {
            if (_scoring)
                throw new InvalidOperationException($"{Self.Id}: dinheiro sai no pós-giro (OnAfterSpin), não no OnScore.");
            _acc.Apply(ScoreStepKind.Money, SourceId, Slot, money: amount);
        }

        void Score(double pontos = 0, double mult = 0, double multX = 1)
        {
            if (!_scoring)
                throw new InvalidOperationException($"{Self.Id}: Pontos e Mult só mudam no OnScore (passo 3).");
            _acc.Apply(ScoreStepKind.Relic, SourceId, Slot, pontos, mult, multX);
        }
    }
}
