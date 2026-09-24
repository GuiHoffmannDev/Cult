using System.Collections.Generic;
using Pleno.Core.Model;

namespace Pleno.Core.Content
{
    /// <summary>
    /// O arquivo de balanceamento. Todo número de regra do jogo mora aqui, e só aqui. O simulador
    /// (Fase 3) cria variações desta classe para calibrar.
    /// </summary>
    public sealed class Balance
    {
        public static Balance Default => new Balance();

        public WheelBalance Wheel { get; } = new WheelBalance();
        public MesaBalance Mesa { get; } = new MesaBalance();
        public PocketBalance Pocket { get; } = new PocketBalance();
        public RelicBalance Relics { get; } = new RelicBalance();
        public RunBalance Run { get; } = new RunBalance();
        public ShopBalance Shop { get; } = new ShopBalance();
        public CardBalance Cards { get; } = new CardBalance();
        public BossBalance Bosses { get; } = new BossBalance();
        public StartingWheelBalance StartingWheels { get; } = new StartingWheelBalance();

        /// <summary>Tabela 3.3 (nível 1 + ganho por nível).</summary>
        public Dictionary<BetType, BetStats> Bets { get; } = new Dictionary<BetType, BetStats>
        {
            [BetType.SimpleChance] = new BetStats(points: 5, mult: 1, pointsPerLevel: 10, multPerLevel: 1),
            [BetType.DozenColumn] = new BetStats(points: 10, mult: 1, pointsPerLevel: 15, multPerLevel: 1),
            [BetType.Line] = new BetStats(points: 15, mult: 2, pointsPerLevel: 15, multPerLevel: 1),
            [BetType.Neighbors] = new BetStats(points: 20, mult: 2, pointsPerLevel: 20, multPerLevel: 2),
            [BetType.Corner] = new BetStats(points: 20, mult: 2, pointsPerLevel: 20, multPerLevel: 2),
            [BetType.Street] = new BetStats(points: 25, mult: 3, pointsPerLevel: 20, multPerLevel: 2),
            [BetType.Split] = new BetStats(points: 30, mult: 3, pointsPerLevel: 25, multPerLevel: 2),
            [BetType.Straight] = new BetStats(points: 50, mult: 5, pointsPerLevel: 30, multPerLevel: 3),
        };
    }

    public sealed class BetStats
    {
        public double Points { get; set; }
        public double Mult { get; set; }
        public double PointsPerLevel { get; set; }
        public double MultPerLevel { get; set; }

        public BetStats(double points, double mult, double pointsPerLevel, double multPerLevel)
        {
            Points = points;
            Mult = mult;
            PointsPerLevel = pointsPerLevel;
            MultPerLevel = multPerLevel;
        }

        public double PointsAt(int level) => Points + (level - 1) * PointsPerLevel;
        public double MultAt(int level) => Mult + (level - 1) * MultPerLevel;
    }

    public sealed class WheelBalance
    {
        public int MinPockets { get; set; } = 18;

        /// <summary>Limite do Artesão (o renderer da Fase 5 vai até 48). O Duplo Zero pode passar dele.</summary>
        public int MaxPockets { get; set; } = 48;
    }

    /// <summary>Metas e economia da run (GDD 4).</summary>
    public sealed class RunBalance
    {
        public int StartingMoney { get; set; } = 4;

        /// <summary>Meta por Ante (linha) e mesa (Pequena, Grande, Chefe).</summary>
        public double[][] Targets { get; set; } =
        {
            new double[] { 100, 150, 200 },
            new double[] { 300, 450, 600 },
            new double[] { 800, 1200, 1600 },
        };

        /// <summary>Recompensa por mesa vencida: Pequena, Grande, Chefe.</summary>
        public int[] MesaRewards { get; set; } = { 3, 4, 5 };

        public int UnusedSpinMoney { get; set; } = 1;

        /// <summary>Juros: +InterestPerStep a cada InterestStep guardados, até InterestCap.</summary>
        public int InterestStep { get; set; } = 5;
        public int InterestPerStep { get; set; } = 1;
        public int InterestCap { get; set; } = 5;

        public int RelicSlots { get; set; } = 5;
        public int ConsumableSlots { get; set; } = 2;

        /// <summary>Venda: preço / SellDivisor, arredondado para baixo, mínimo SellMinimum.</summary>
        public int SellDivisor { get; set; } = 2;
        public int SellMinimum { get; set; } = 1;

        public int Antes => Targets.Length;
        public int MesasPerAnte => MesaRewards.Length;
    }

    public sealed class ShopBalance
    {
        public int RelicOffers { get; set; } = 2;
        public int ConsumableOffers { get; set; } = 2;

        public int RerollBaseCost { get; set; } = 5;
        public int RerollCostStep { get; set; } = 1;

        /// <summary>Chance de raridade na loja. Raridades sem amuleto disponível saem do sorteio.</summary>
        public Dictionary<Rarity, double> RarityWeights { get; set; } = new Dictionary<Rarity, double>
        {
            [Rarity.Common] = 70,
            [Rarity.Uncommon] = 25,
            [Rarity.Rare] = 5,
        };

        /// <summary>Preço por raridade (piso da faixa do GDD 7) até o simulador calibrar.</summary>
        public Dictionary<Rarity, int> RelicPrices { get; set; } = new Dictionary<Rarity, int>
        {
            [Rarity.Common] = 4,
            [Rarity.Uncommon] = 6,
            [Rarity.Rare] = 8,
        };

        /// <summary>Peso de cada tipo num espaço de consumível.</summary>
        public double TabuaWeight { get; set; } = 1;
        public double HouseCardWeight { get; set; } = 1;

        public int TabuaPrice { get; set; } = 3;
        public int HouseCardPrice { get; set; } = 3;

        public Dictionary<string, PackStats> Packs { get; set; } = new Dictionary<string, PackStats>
        {
            [PackIds.Tabuas] = new PackStats(price: 4, options: 3, weight: 1),
            [PackIds.Casa] = new PackStats(price: 4, options: 3, weight: 1),
            [PackIds.Amuletos] = new PackStats(price: 6, options: 2, weight: 1),
        };

        /// <summary>Chance de Lendário por opção do Pacote de Amuletos (só se houver Lendário no conjunto).</summary>
        public double LegendaryChanceInRelicPack { get; set; } = 0.005;
    }

    public sealed class PackStats
    {
        public int Price { get; set; }
        public int Options { get; set; }
        public double Weight { get; set; }

        public PackStats(int price, int options, double weight)
        {
            Price = price;
            Options = options;
            Weight = weight;
        }
    }

    /// <summary>Cartas da Casa (GDD 6.2) e Tábuas (6.1).</summary>
    public sealed class CardBalance
    {
        public int TabuaLevels { get; set; } = 1;

        public int OurivesTargets { get; set; } = 1;
        public int VidraceiroTargets { get; set; } = 2;
        public int FerreiroTargets { get; set; } = 1;
        public int ImaTargets { get; set; } = 1;
        public int TrevoTargets { get; set; } = 2;
        public int PintorTargets { get; set; } = 3;
        public int CoveiroTargets { get; set; } = 2;
        public int TesoureiroMaxGain { get; set; } = 20;
        public int OraculoTabuas { get; set; } = 2;
        public int FortunaOneIn { get; set; } = 4;
    }

    public sealed class BossBalance
    {
        public int RelojoeiroSpins { get; set; } = 3;
    }

    /// <summary>Rodas iniciais (GDD 5.4).</summary>
    public sealed class StartingWheelBalance
    {
        public int AmericanExtraNudges { get; set; } = 1;
        public int GoldenGoldPockets { get; set; } = 3;
        public int GoldenChipPenalty { get; set; } = 1;
    }

    /// <summary>Recursos por mesa, antes dos modificadores de amuletos e chefes.</summary>
    public sealed class MesaBalance
    {
        public int Spins { get; set; } = 4;
        public int ChipsPerSpin { get; set; } = 5;
        public int Nudges { get; set; } = 2;

        /// <summary>Fichas mínimas no pano para girar.</summary>
        public int MinChipsToSpin { get; set; } = 1;
    }

    /// <summary>Valor do número (3.2), aprimoramentos (5.2) e edições (5.3).</summary>
    public sealed class PocketBalance
    {
        /// <summary>O 0 e o 00 somam 36 nos Pontos.</summary>
        public double ZeroValue { get; set; } = 36;

        public int GoldMoney { get; set; } = 3;
        public double GlassMultX { get; set; } = 2;
        public int GlassBreakOneIn { get; set; } = 4;
        public double SteelMultX { get; set; } = 1.5;
        public double BonusPoints { get; set; } = 30;
        public double MultEnhancementMult { get; set; } = 4;
        public double MagneticWeightX { get; set; } = 2;
        public int LuckyMultOneIn { get; set; } = 5;
        public double LuckyMult { get; set; } = 20;
        public int LuckyMoneyOneIn { get; set; } = 15;
        public int LuckyMoney { get; set; } = 20;

        public double PolishedPoints { get; set; } = 50;
        public double HolographicMult { get; set; } = 10;
        public double PolychromeMultX { get; set; } = 1.5;
    }

    /// <summary>Números dos amuletos (GDD 7.1).</summary>
    public sealed class RelicBalance
    {
        public double FichaGastaMult { get; set; } = 4;
        public double RosaRubraMult { get; set; } = 8;
        public double ArranhaCeuExtraCopies { get; set; } = 1;
        public double TeoremaMult { get; set; } = 10;
        public double MiraLaserMultX { get; set; } = 3;
        public int ComboTriploMinWins { get; set; } = 3;
        public double ComboTriploMultX { get; set; } = 2;
        public double MartingaleMultPerLoss { get; set; } = 3;
        public double ColecionadorMultPerNumber { get; set; } = 1;
        public int LuvaDeSedaNudges { get; set; } = 1;
        public double NeodimioMagneticWeightX { get; set; } = 4;
    }
}
