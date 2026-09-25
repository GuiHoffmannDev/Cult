using System;
using Pleno.Core.Content;
using Pleno.Core.Flow;
using Pleno.Core.Model;

namespace Pleno.Sim
{
    /// <summary>Joga uma run inteira com um bot, na roda Europeia.</summary>
    public static class SimRunner
    {
        /// <summary>Uma run tem 9 mesas e 9 lojas; isto só pega um bot em laço infinito.</summary>
        public const int MaxSteps = 1000;

        public static RunRecord Play(BotKind kind, string seed, Balance balance = null) =>
            Play(Bot.Create(kind), seed, balance, out _);

        /// <param name="engine">A run ao terminar (para os testes compararem o estado final).</param>
        public static RunRecord Play(Bot bot, string seed, Balance balance, out RunEngine engine)
        {
            engine = RunEngine.NewRun(seed, StartingWheel.European, balance);
            return Play(bot, engine);
        }

        /// <summary>Joga uma run já criada (os testes a preparam antes, por exemplo escolhendo os chefes).</summary>
        public static RunRecord Play(Bot bot, RunEngine engine)
        {
            string seed = engine.State.Seed;
            var driver = new RunDriver(engine, seed, new RunRecord(bot.Id, seed));
            bot.BeginRun(driver);

            for (int step = 0; engine.State.Phase == RunPhase.Mesa || engine.State.Phase == RunPhase.Shop; step++)
            {
                if (step >= MaxSteps)
                    throw new InvalidOperationException($"[{bot.Id} {seed}] a run não terminou em {MaxSteps} passos.");

                if (engine.State.Phase == RunPhase.Shop)
                {
                    driver.EnterShop();
                    bot.Shop(driver);
                    driver.LeaveShop();
                    continue;
                }

                bot.PlaceBets(driver);
                driver.Spin();
                bot.Nudge(driver);
                driver.ConfirmSpin();
            }

            driver.Finish();
            return driver.Record;
        }
    }
}
