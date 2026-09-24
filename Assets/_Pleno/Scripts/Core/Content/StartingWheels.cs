using System;
using Pleno.Core.Model;
using Pleno.Core.Rng;

namespace Pleno.Core.Content
{
    /// <summary>As 3 rodas iniciais (GDD 5.4), que equivalem aos baralhos.</summary>
    public enum StartingWheel
    {
        European,
        American,
        Golden,
    }

    public static class StartingWheels
    {
        /// <summary>Monta a roda inicial. A Dourada sorteia suas casas Douradas no stream <c>wheel</c>.</summary>
        public static Wheel Build(StartingWheel kind, Balance balance, IRandom wheelRng)
        {
            switch (kind)
            {
                case StartingWheel.European:
                    return WheelLayouts.European();
                case StartingWheel.American:
                    return WheelLayouts.American();
                case StartingWheel.Golden:
                    Wheel wheel = WheelLayouts.European();
                    foreach (int index in wheelRng.PickDistinct(wheel.Count, balance.StartingWheels.GoldenGoldPockets))
                        wheel[index].Enhancement = PocketEnhancement.Gold;
                    return wheel;
                default:
                    throw new ArgumentOutOfRangeException(nameof(kind), kind, null);
            }
        }

        /// <summary>Recursos da mesa depois da regra da roda inicial.</summary>
        public static MesaResources Apply(StartingWheel kind, Balance balance, MesaResources resources)
        {
            if (kind == StartingWheel.American)
                resources.Nudges += balance.StartingWheels.AmericanExtraNudges;
            if (kind == StartingWheel.Golden)
                resources.ChipsPerSpin -= balance.StartingWheels.GoldenChipPenalty;
            return resources;
        }
    }
}
