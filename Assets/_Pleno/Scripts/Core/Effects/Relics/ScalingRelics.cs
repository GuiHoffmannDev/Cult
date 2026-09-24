using Pleno.Core.Content;
using Pleno.Core.Rules;

namespace Pleno.Core.Effects.Relics
{
    // Amuletos que escalam (GDD 7.1). O estado fica nos Counters do RelicInstance.

    /// <summary>
    /// #17 Martingale: +3 Mult acumulado a cada giro sem vitória; zera ao vencer. O giro vencedor
    /// recebe o acumulado e só então zera (no pós-giro).
    /// </summary>
    [RelicEffect(RelicIds.Martingale)]
    public sealed class Martingale : RelicEffectBase
    {
        public const string StackKey = "stack";

        public override void OnScore(RelicScope r)
        {
            double stack = r.Self.GetCounter(StackKey);
            if (stack > 0)
                r.AddMult(stack);
        }

        public override void OnAfterSpin(RelicScope r)
        {
            double stack = r.Spin.HasWin ? 0 : r.Self.GetCounter(StackKey) + r.Values.MartingaleMultPerLoss;
            r.Self.SetCounter(StackKey, stack);
        }
    }

    /// <summary>
    /// #20 Colecionador: +1 Mult permanente por número diferente que já caiu desde que ele entrou.
    /// O número do giro atual passa a contar a partir do giro seguinte.
    /// </summary>
    [RelicEffect(RelicIds.Colecionador)]
    public sealed class Colecionador : RelicEffectBase
    {
        public const string DistinctKey = "distinct";

        public static string SeenKey(int number) => "seen:" + number;

        public override void OnScore(RelicScope r)
        {
            double distinct = r.Self.GetCounter(DistinctKey);
            if (distinct > 0)
                r.AddMult(distinct * r.Values.ColecionadorMultPerNumber);
        }

        public override void OnAfterSpin(RelicScope r)
        {
            string key = SeenKey(r.Spin.Pocket.Number);
            if (r.Self.HasCounter(key))
                return;
            r.Self.SetCounter(key, 1);
            r.Self.SetCounter(DistinctKey, r.Self.GetCounter(DistinctKey) + 1);
        }
    }
}
