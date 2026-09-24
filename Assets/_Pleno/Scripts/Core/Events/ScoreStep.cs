namespace Pleno.Core.Events
{
    public enum ScoreStepKind
    {
        BetWon,
        PocketValue,
        Enhancement,
        Edition,
        Relic,
        Final,
        Money,
        GlassBreak,
    }

    /// <summary>
    /// Um passo da pontuação, já calculado. A tela toca a lista na ordem, destacando a origem
    /// (<see cref="SourceId"/> e <see cref="SourceIndex"/>) e atualizando os contadores com os snapshots.
    /// </summary>
    public readonly struct ScoreStep
    {
        public readonly ScoreStepKind Kind;

        /// <summary>"bet:pleno:19", "pocket:3", "relic:teorema"…</summary>
        public readonly string SourceId;

        /// <summary>Posição da origem: ordem da aposta, índice físico da casa ou espaço do amuleto.</summary>
        public readonly int SourceIndex;

        public readonly double PontosAdd;
        public readonly double MultAdd;

        /// <summary>Multiplicador do Mult; 1.0 = sem efeito.</summary>
        public readonly double MultX;

        public readonly int MoneyDelta;

        /// <summary>Snapshot depois do passo, para a UI.</summary>
        public readonly double PontosAfter;

        public readonly double MultAfter;

        public ScoreStep(ScoreStepKind kind, string sourceId, int sourceIndex, double pontosAdd, double multAdd,
            double multX, int moneyDelta, double pontosAfter, double multAfter)
        {
            Kind = kind;
            SourceId = sourceId;
            SourceIndex = sourceIndex;
            PontosAdd = pontosAdd;
            MultAdd = multAdd;
            MultX = multX;
            MoneyDelta = moneyDelta;
            PontosAfter = pontosAfter;
            MultAfter = multAfter;
        }

        public override string ToString() =>
            $"{Kind} {SourceId} +{PontosAdd}P +{MultAdd}M x{MultX} ${MoneyDelta} => {PontosAfter} x {MultAfter}";
    }
}
