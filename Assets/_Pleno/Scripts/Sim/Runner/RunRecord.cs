using System.Collections.Generic;

namespace Pleno.Sim
{
    /// <summary>Uma mesa jogada por um bot.</summary>
    public sealed class MesaRecord
    {
        public int Ante { get; set; }

        /// <summary>0 Pequena, 1 Grande, 2 Chefe.</summary>
        public int Index { get; set; }

        public string BossId { get; set; }
        public double Target { get; set; }
        public double Score { get; set; }
        public bool Won { get; set; }

        /// <summary>Total de cada giro, em ordem.</summary>
        public List<double> Spins { get; } = new List<double>();
    }

    /// <summary>O que o simulador guarda de uma run.</summary>
    public sealed class RunRecord
    {
        public string Bot { get; }
        public string Seed { get; }

        public bool Won { get; set; }

        /// <summary>Antes vencidos (Mesa do Chefe batida).</summary>
        public int AntesCleared { get; set; }

        public List<MesaRecord> Mesas { get; } = new List<MesaRecord>();

        /// <summary>$ ao entrar em cada loja (depois do pagamento da mesa).</summary>
        public List<int> ShopMoney { get; } = new List<int>();

        /// <summary>Lojas em que o amuleto apareceu à venda (conta uma vez por loja, mesmo com reroll).</summary>
        public Dictionary<string, int> RelicsOffered { get; } = new Dictionary<string, int>();

        public Dictionary<string, int> RelicsBought { get; } = new Dictionary<string, int>();

        /// <summary>Amuletos escolhidos no Pacote de Amuletos.</summary>
        public Dictionary<string, int> RelicsPicked { get; } = new Dictionary<string, int>();

        public HashSet<string> RelicsEverHeld { get; } = new HashSet<string>();

        /// <summary>Amuletos no fim da run, da esquerda para a direita.</summary>
        public List<string> FinalRelics { get; } = new List<string>();

        /// <summary>Cartas da Casa e Tábuas usadas (do espaço ou direto do pacote), por id.</summary>
        public Dictionary<string, int> ConsumablesUsed { get; } = new Dictionary<string, int>();

        public int HouseCardsUsed { get; set; }
        public int TabuasUsed { get; set; }
        public int NudgesUsed { get; set; }
        public int Rerolls { get; set; }
        public double BestSpin { get; set; }
        public int FinalMoney { get; set; }

        /// <summary>Mensagem do erro, se a run quebrou (ação ilegal do bot ou exceção do Core).</summary>
        public string Error { get; set; }

        public RunRecord(string bot, string seed)
        {
            Bot = bot;
            Seed = seed;
        }

        internal static void Count(Dictionary<string, int> counts, string id)
        {
            counts.TryGetValue(id, out int n);
            counts[id] = n + 1;
        }
    }
}
