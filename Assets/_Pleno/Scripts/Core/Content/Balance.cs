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
    }

    /// <summary>Recursos por mesa, antes dos modificadores de amuletos e chefes.</summary>
    public sealed class MesaBalance
    {
        public int Spins { get; set; } = 4;
        public int ChipsPerSpin { get; set; } = 5;
        public int Nudges { get; set; } = 2;
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
