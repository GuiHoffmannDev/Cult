using System;

namespace Pleno.Core.Rng
{
    /// <summary>Estado serializável de um <see cref="Pcg32"/>.</summary>
    [Serializable]
    public struct Pcg32State
    {
        public ulong State;
        public ulong Inc;

        public Pcg32State(ulong state, ulong inc)
        {
            State = state;
            Inc = inc;
        }
    }

    /// <summary>
    /// PCG32 (XSH RR 64/32), de O'Neill. Idêntico à implementação de referência <c>pcg32_random_r</c>,
    /// para que qualquer seed produza a mesma sequência em qualquer plataforma.
    /// </summary>
    public sealed class Pcg32 : IRandom
    {
        const ulong Multiplier = 6364136223846793005UL;

        ulong _state;
        ulong _inc;

        /// <summary>Equivalente a <c>pcg32_srandom_r(initState, initSeq)</c>.</summary>
        public Pcg32(ulong initState, ulong initSeq)
        {
            _state = 0;
            _inc = (initSeq << 1) | 1UL;
            NextUInt();
            _state += initState;
            NextUInt();
        }

        public Pcg32(Pcg32State state)
        {
            _state = state.State;
            _inc = state.Inc;
        }

        public Pcg32State State => new Pcg32State(_state, _inc);

        public Pcg32 Clone() => new Pcg32(State);

        public uint NextUInt()
        {
            ulong old = _state;
            _state = unchecked(old * Multiplier + _inc);
            uint xorShifted = (uint)(((old >> 18) ^ old) >> 27);
            int rot = (int)(old >> 59);
            return (xorShifted >> rot) | (xorShifted << ((-rot) & 31));
        }

        public int NextInt(int maxExclusive)
        {
            if (maxExclusive <= 0)
                throw new ArgumentOutOfRangeException(nameof(maxExclusive), maxExclusive, "Precisa ser positivo.");

            // Rejeição do resto, como pcg32_boundedrand_r: sem viés de módulo.
            uint bound = (uint)maxExclusive;
            uint threshold = (uint)((0x1_0000_0000UL - bound) % bound);
            while (true)
            {
                uint r = NextUInt();
                if (r >= threshold)
                    return (int)(r % bound);
            }
        }

        public double NextDouble()
        {
            ulong hi = NextUInt() >> 5; // 27 bits
            ulong lo = NextUInt() >> 6; // 26 bits
            return (hi * 67108864.0 + lo) * (1.0 / 9007199254740992.0);
        }
    }
}
