using System.Collections.Generic;
using System.Linq;
using Pleno.Core.Model;

namespace Pleno.Sim.Planning
{
    /// <summary>
    /// Cópias profundas das partes da run que o pipeline e as cartas alteram. Os bots simulam sobre
    /// elas e nunca sobre a run real.
    /// </summary>
    public static class SimClone
    {
        public static RelicInstance Relic(RelicInstance relic) => new RelicInstance(relic.Id)
        {
            Edition = relic.Edition,
            Counters = new Dictionary<string, double>(relic.Counters),
        };

        public static List<RelicInstance> Relics(IEnumerable<RelicInstance> relics) => relics.Select(Relic).ToList();

        public static BetLevels Levels(BetLevels levels) =>
            new BetLevels { Levels = new Dictionary<BetType, int>(levels.Levels) };

        /// <summary>
        /// Run de rascunho para testar uma Carta da Casa: roda, amuletos, níveis, dinheiro e consumíveis
        /// copiados. A mesa é compartilhada só para leitura (casas temporárias do chefe); os streams
        /// de RNG não vão junto, e a carta recebe o stream de avaliação do bot.
        /// </summary>
        public static RunState ForCards(RunState run) => new RunState
        {
            Seed = run.Seed,
            StartingWheel = run.StartingWheel,
            Phase = run.Phase,
            Money = run.Money,
            Ante = run.Ante,
            MesaIndex = run.MesaIndex,
            Wheel = run.Wheel.Clone(),
            Relics = Relics(run.Relics),
            Consumables = run.Consumables.Select(c => new ConsumableInstance(c.Id)).ToList(),
            Levels = Levels(run.Levels),
            BossPlan = run.BossPlan.ToList(),
            Mesa = run.Mesa,
        };
    }
}
