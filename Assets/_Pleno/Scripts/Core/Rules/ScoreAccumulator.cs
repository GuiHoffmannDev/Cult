using System.Collections.Generic;
using Pleno.Core.Events;

namespace Pleno.Core.Rules
{
    /// <summary>Pontos e Mult do giro, registrando um <see cref="ScoreStep"/> a cada mudança.</summary>
    public sealed class ScoreAccumulator
    {
        readonly List<ScoreStep> _steps = new List<ScoreStep>();

        /// <summary>O Mult começa em 0: o exemplo do GDD 3.4 (1.683) exige 1 + 1 + 5 + 10 = 17.</summary>
        public double Pontos { get; private set; }

        public double Mult { get; private set; }
        public int Money { get; private set; }
        public IReadOnlyList<ScoreStep> Steps => _steps;

        /// <summary>Aplica, nesta ordem: +Pontos, +Mult, ×Mult, +$.</summary>
        public void Apply(ScoreStepKind kind, string sourceId, int sourceIndex,
            double pontosAdd = 0, double multAdd = 0, double multX = 1, int money = 0)
        {
            Pontos += pontosAdd;
            Mult = (Mult + multAdd) * multX;
            Money += money;
            _steps.Add(new ScoreStep(kind, sourceId, sourceIndex, pontosAdd, multAdd, multX, money, Pontos, Mult));
        }
    }
}
