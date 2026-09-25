using System;
using System.Collections.Generic;
using Pleno.Core.Model;

namespace Pleno.Sim.Planning
{
    /// <summary>
    /// Decide o empurrão depois que a bola para. Os empurrões são da mesa inteira, então gastar um
    /// agora custa o que ele renderia num giro futuro (<c>nudgeValue</c>), a menos que sobrem
    /// empurrões para todos os giros restantes, que este seja o último giro ou que o empurrão vença a mesa.
    /// </summary>
    public static class NudgePlanner
    {
        /// <summary>
        /// Deslocamento com sinal (em casas) para onde vale empurrar a bola; 0 = deixar onde está.
        /// </summary>
        /// <param name="spinsLeft">Giros restantes contando o atual.</param>
        /// <param name="nudgeValue">Ganho esperado de guardar um empurrão para um giro futuro.</param>
        public static int Offset(SpinEvaluator ev, IReadOnlyList<PlacedBet> bets, int ball, int nudgesLeft,
            int spinsLeft, double remaining, double nudgeValue)
        {
            if (nudgesLeft <= 0)
                return 0;

            Wheel wheel = ev.Wheel;
            double stay = Math.Min(ev.Score(bets, ball), remaining);
            int spinsAfter = spinsLeft - 1;
            int bestOffset = 0;
            double bestNet = 0;

            // Mais perto primeiro: num empate, gasta menos empurrões.
            for (int step = 1; step <= nudgesLeft && step <= wheel.Count / 2; step++)
            {
                foreach (int sign in new[] { 1, -1 })
                {
                    double moved = Math.Min(ev.Score(bets, wheel.Neighbor(ball, sign * step)), remaining);
                    double gain = moved - stay;
                    if (gain <= 0)
                        continue;
                    bool free = moved >= remaining || spinsAfter == 0 || nudgesLeft - step >= spinsAfter;
                    double net = free ? gain : gain - step * nudgeValue;
                    if (moved >= remaining)
                        net += remaining; // vencer a mesa agora vale mais que qualquer ganho parcial
                    if (net > bestNet + 1e-9)
                    {
                        bestNet = net;
                        bestOffset = sign * step;
                    }
                }
            }
            return bestOffset;
        }
    }
}
