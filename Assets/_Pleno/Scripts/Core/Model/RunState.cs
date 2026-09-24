using System.Collections.Generic;
using Pleno.Core.Content;
using Pleno.Core.Rng;

namespace Pleno.Core.Model
{
    public enum RunPhase
    {
        /// <summary>Numa mesa (apostando ou esperando o empurrão).</summary>
        Mesa,

        /// <summary>Na loja, entre mesas.</summary>
        Shop,

        /// <summary>Venceu o Chefe do último Ante.</summary>
        Won,

        /// <summary>Os giros acabaram sem bater a meta.</summary>
        Lost,
    }

    /// <summary>
    /// Tudo o que persiste da run. É o que o save grava, incluindo o estado de cada stream de RNG:
    /// sair e voltar não troca nenhum resultado. As regras que mexem neste estado estão em <c>RunEngine</c>.
    /// </summary>
    public sealed class RunState
    {
        public string Seed { get; set; }
        public RngStreams Rng { get; set; }
        public StartingWheel StartingWheel { get; set; }
        public RunPhase Phase { get; set; }

        public int Money { get; set; }

        /// <summary>Ante atual, começando em 1.</summary>
        public int Ante { get; set; } = 1;

        /// <summary>Mesa dentro do Ante: 0 Pequena, 1 Grande, 2 Chefe.</summary>
        public int MesaIndex { get; set; }

        public Wheel Wheel { get; set; }

        /// <summary>Amuletos da esquerda para a direita: a ordem é a de resolução.</summary>
        public List<RelicInstance> Relics { get; set; } = new List<RelicInstance>();

        public List<ConsumableInstance> Consumables { get; set; } = new List<ConsumableInstance>();
        public BetLevels Levels { get; set; } = new BetLevels();

        /// <summary>Chefe de cada Ante, sorteado no início da run sem repetir.</summary>
        public List<string> BossPlan { get; set; } = new List<string>();

        public MesaState Mesa { get; set; }
        public ShopState Shop { get; set; }

        public RunStats Stats { get; set; } = new RunStats();
    }

    public sealed class ConsumableInstance
    {
        public string Id { get; set; }

        public ConsumableInstance()
        {
        }

        public ConsumableInstance(string id)
        {
            Id = id;
        }
    }

    public sealed class RunStats
    {
        public int SpinsPlayed { get; set; }
        public int MesasWon { get; set; }
        public double BestSpin { get; set; }
        public int NudgesUsed { get; set; }
    }
}
