namespace Pleno.Sim
{
    /// <summary>
    /// Parâmetros de estratégia dos bots. Não são regras do jogo (essas moram no <c>Balance</c>):
    /// mudar um número daqui muda como o bot joga, não o jogo.
    /// </summary>
    public static class BotTuning
    {
        /// <summary>Martingale avaliado como se já tivesse 1 giro perdido acumulado.</summary>
        public const double MartingaleLossesAssumed = 1;

        /// <summary>Colecionador avaliado como se já tivesse visto 8 números.</summary>
        public const double ColecionadorNumbersAssumed = 8;

        /// <summary>Casas testadas como alvo de carta (as que mais marcam, ou as que menos marcam).</summary>
        public const int CardTargetPool = 8;

        /// <summary>Casas testadas como origem do Gêmeo.</summary>
        public const int GemeoSources = 4;

        /// <summary>Troca um amuleto por outro só se o novo valer este tanto a mais que o pior.</summary>
        public const double RelicSwapMargin = 1.25;

        /// <summary>Reroll só com este $ sobrando depois de pagar, e no máximo estas vezes por loja.</summary>
        public const int RerollReserve = 10;
        public const int MaxRerollsPerShop = 2;

        /// <summary>Tesoureiro só vale a compra se o $ depois de pagar render pelo menos isto.</summary>
        public const int TesoureiroMinMoney = 4;

        /// <summary>Limite de ações do Aleatório numa loja.</summary>
        public const int RandomShopActions = 12;

        /// <summary>Tentativas de sortear alvos válidos para uma carta (Aleatório).</summary>
        public const int RandomTargetTries = 8;
    }
}
