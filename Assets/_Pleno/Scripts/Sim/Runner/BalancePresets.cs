using System;
using System.Collections.Generic;
using System.Linq;
using Pleno.Core.Content;
using Pleno.Core.Model;

namespace Pleno.Sim
{
    /// <summary>Uma variação do balanceamento para experimento, aplicada em memória sobre o padrão.</summary>
    public sealed class BalancePreset
    {
        public string Id { get; }
        public string Description { get; }
        readonly Action<Balance> _apply;

        public BalancePreset(string id, string description, Action<Balance> apply)
        {
            Id = id;
            Description = description;
            _apply = apply;
        }

        public Balance Build()
        {
            Balance balance = Balance.Default;
            _apply(balance);
            return balance;
        }
    }

    /// <summary>
    /// Experimentos de calibragem. Não são o balanceamento do jogo (esse é o <c>Balance</c> do Core, que
    /// estes presets nunca alteram): cada um parte de <see cref="Balance.Default"/> e muda alguns
    /// números em memória, só para o simulador medir o efeito.
    /// </summary>
    public static class BalancePresets
    {
        public const string DefaultId = "padrao";

        public static readonly IReadOnlyList<BalancePreset> All = new[]
        {
            new BalancePreset(DefaultId, "Balance do jogo, sem mudança.", _ => { }),
            new BalancePreset("sem-empurrao", "0 empurrões por mesa.", b => b.Mesa.Nudges = 0),
            new BalancePreset("metas-1-4-8", "Metas do Ante 2 ×4 e do Ante 3 ×8.", b => ScaleTargets(b, 1, 4, 8)),
            new BalancePreset("metas-1-4-9", "Metas do Ante 2 ×4 e do Ante 3 ×9.", b => ScaleTargets(b, 1, 4, 9)),
            new BalancePreset("metas-1-4-10", "Metas do Ante 2 ×4 e do Ante 3 ×10.", b => ScaleTargets(b, 1, 4, 10)),
            new BalancePreset("metas-1-4-8-um-empurrao", "Metas ×(1, 4, 8) e 1 empurrão por mesa.", b =>
            {
                ScaleTargets(b, 1, 4, 8);
                b.Mesa.Nudges = 1;
            }),
            new BalancePreset("metas-1-4-8-sem-giro-extra", "Metas ×(1, 4, 8) e sem $ por giro não usado.", b =>
            {
                ScaleTargets(b, 1, 4, 8);
                b.Run.UnusedSpinMoney = 0;
            }),
            new BalancePreset("metas-1-4-9-pleno-x2", "Metas ×(1, 4, 9) e Pontos de Pleno e Vizinhos ×2 (base e por nível).", b =>
            {
                ScaleTargets(b, 1, 4, 9);
                ScalePoints(b, 2, BetType.Straight, BetType.Neighbors);
            }),
        };

        public static BalancePreset Get(string id) =>
            All.FirstOrDefault(p => p.Id == id)
            ?? throw new ArgumentException($"Preset desconhecido: '{id}'. Use {string.Join(", ", All.Select(p => p.Id))}.");

        /// <summary>Faz o lote usar o preset (e gravar numa pasta com o nome dele).</summary>
        public static void Use(SimConfig config, BalancePreset preset)
        {
            config.MakeBalance = preset.Build;
            config.BalanceLabel = preset.Id;
        }

        /// <summary>Multiplica as metas de cada Ante (Pequena, Grande e Chefe juntas).</summary>
        static void ScaleTargets(Balance balance, params double[] perAnte)
        {
            for (int ante = 0; ante < balance.Run.Targets.Length; ante++)
                for (int mesa = 0; mesa < balance.Run.Targets[ante].Length; mesa++)
                    balance.Run.Targets[ante][mesa] *= perAnte[ante];
        }

        static void ScalePoints(Balance balance, double factor, params BetType[] types)
        {
            foreach (BetType type in types)
            {
                balance.Bets[type].Points *= factor;
                balance.Bets[type].PointsPerLevel *= factor;
            }
        }
    }
}
