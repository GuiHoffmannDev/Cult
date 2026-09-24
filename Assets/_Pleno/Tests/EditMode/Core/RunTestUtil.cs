using System.Linq;
using Pleno.Core.Content;
using Pleno.Core.Events;
using Pleno.Core.Flow;
using Pleno.Core.Model;

namespace Pleno.Tests.Core
{
    static class RunTestUtil
    {
        public static RunEngine New(StartingWheel wheel = StartingWheel.European, string seed = "TESTSEED") =>
            RunEngine.NewRun(seed, wheel);

        /// <summary>Zera o peso de todas as casas menos as de <paramref name="number"/>: o próximo sorteio cai nela.</summary>
        public static void Force(RunEngine run, int number)
        {
            foreach (Pocket pocket in run.State.Wheel.Pockets)
                pocket.Weight = pocket.Number == number ? 1 : 0;
        }

        public static SpinResult SpinOn(RunEngine run, int number, params PlacedBet[] bets)
        {
            foreach (PlacedBet bet in bets)
                run.Place(bet);
            Force(run, number);
            run.Spin();
            return run.ConfirmSpin();
        }

        /// <summary>Todas as fichas no Pleno 19 e a bola no 19, até vencer (ou perder) a mesa.</summary>
        public static void WinMesa(RunEngine run)
        {
            while (run.State.Phase == RunPhase.Mesa)
                SpinOn(run, 19, PlacedBet.Straight(19, run.State.Mesa.ChipsFree));
        }

        /// <summary>Põe a run direto na Mesa do Chefe do Ante 1 com o chefe dado.</summary>
        public static RunEngine AtBoss(string bossId, StartingWheel wheel = StartingWheel.European)
        {
            RunEngine run = New(wheel);
            run.State.BossPlan[0] = bossId;
            run.State.MesaIndex = 2;
            run.StartMesa();
            return run;
        }

        public static int IndexOf(RunEngine run, int number) =>
            Enumerable.Range(0, run.State.Wheel.Count).First(i => run.State.Wheel[i].Number == number);
    }
}
