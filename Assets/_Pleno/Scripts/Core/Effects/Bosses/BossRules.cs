using Pleno.Core.Content;
using Pleno.Core.Model;

namespace Pleno.Core.Effects.Bosses
{
    /// <summary>
    /// O Duplo Zero: adiciona uma casa 00 temporária, oposta ao primeiro 0 (como na roda americana).
    /// Ela faz as apostas externas perderem, como qualquer 00. Sai no fim da mesa.
    /// </summary>
    [BossRule(BossIds.DuploZero)]
    public sealed class DuploZero : BossRuleBase
    {
        public override void OnMesaStart(RunState run, MesaState mesa)
        {
            Wheel wheel = run.Wheel;
            int zero = 0;
            for (int i = 0; i < wheel.Count; i++)
            {
                if (wheel[i].Number == Pocket.Zero)
                {
                    zero = i;
                    break;
                }
            }

            // Inserir meia volta depois do 0 deixa o mesmo número de casas dos dois lados.
            int position = (zero + (wheel.Count + 1) / 2) % wheel.Count;
            Pocket added = wheel.Insert(position, new Pocket(Pocket.DoubleZero, PocketColor.Green));
            mesa.TemporaryPocketIds.Add(added.Id);
        }

        public override void OnMesaEnd(RunState run, MesaState mesa)
        {
            foreach (int id in mesa.TemporaryPocketIds)
            {
                int index = run.Wheel.IndexOfId(id);
                if (index >= 0)
                    run.Wheel.RemoveAt(index, minimumPockets: 0);
            }
            mesa.TemporaryPocketIds.Clear();
        }
    }

    /// <summary>A Névoa: os números da roda ficam ocultos durante o giro. A pista e o pano continuam visíveis.</summary>
    [BossRule(BossIds.Nevoa)]
    public sealed class Nevoa : BossRuleBase
    {
        public override bool HidesWheelNumbers => true;
    }

    /// <summary>O Contador: apostas de Pleno não pontuam (contam como perdidas).</summary>
    [BossRule(BossIds.Contador)]
    public sealed class Contador : BossRuleBase
    {
        public override bool BetScores(PlacedBet bet) => bet.Type != BetType.Straight;
    }

    /// <summary>Mão Pesada: empurrões desativados.</summary>
    [BossRule(BossIds.MaoPesada)]
    public sealed class MaoPesada : BossRuleBase
    {
        public override bool NudgesDisabled => true;

        public override MesaResources ModifyMesaResources(Balance balance, MesaResources resources)
        {
            resources.Nudges = 0;
            return resources;
        }
    }

    /// <summary>O Daltônico: apostas em cor (Vermelho, Preto) não pontuam (contam como perdidas).</summary>
    [BossRule(BossIds.Daltonico)]
    public sealed class Daltonico : BossRuleBase
    {
        public override bool BetScores(PlacedBet bet) =>
            bet.Outside != OutsideKind.Red && bet.Outside != OutsideKind.Black;
    }

    /// <summary>O Relojoeiro: só 3 giros nesta mesa.</summary>
    [BossRule(BossIds.Relojoeiro)]
    public sealed class Relojoeiro : BossRuleBase
    {
        public override MesaResources ModifyMesaResources(Balance balance, MesaResources resources)
        {
            resources.Spins = balance.Bosses.RelojoeiroSpins;
            return resources;
        }
    }
}
