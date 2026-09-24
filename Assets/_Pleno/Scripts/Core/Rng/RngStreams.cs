using System;
using System.Collections.Generic;

namespace Pleno.Core.Rng
{
    /// <summary>
    /// Um PCG32 independente por sistema, todos derivados da seed da run. Comprar na loja não mexe
    /// no stream da roda, e por isso a run é reproduzível.
    /// </summary>
    public sealed class RngStreams
    {
        public const string Wheel = "wheel";
        public const string Shop = "shop";
        public const string Pack = "pack";
        public const string Relic = "relic";
        public const string Boss = "boss";
        public const string Luck = "luck";
        public const string Vault = "vault";

        public static readonly IReadOnlyList<string> Names = new[] { Wheel, Shop, Pack, Relic, Boss, Luck, Vault };

        readonly Dictionary<string, Pcg32> _streams = new Dictionary<string, Pcg32>(StringComparer.Ordinal);

        public string Seed { get; }

        public RngStreams(string seed)
        {
            Seed = RunSeed.RequireValid(seed);
            foreach (string name in Names)
                _streams[name] = Derive(Seed, name);
        }

        public Pcg32 Get(string name)
        {
            if (!_streams.TryGetValue(name, out Pcg32 stream))
                throw new KeyNotFoundException($"Stream de RNG desconhecido: '{name}'.");
            return stream;
        }

        /// <summary>
        /// Deriva um stream por nome. Serve também para streams fora da regra (ruído de animação e
        /// som), que nunca podem consumir um dos sete streams de <see cref="Names"/>.
        /// </summary>
        public static Pcg32 Derive(string seed, string streamName)
        {
            ulong seedHash = StableHash.Fnv1a64(RunSeed.RequireValid(seed));
            ulong nameHash = StableHash.Fnv1a64(streamName);
            ulong initState = StableHash.SplitMix64(seedHash ^ nameHash);
            ulong initSeq = StableHash.SplitMix64(initState ^ nameHash);
            return new Pcg32(initState, initSeq);
        }

        public Dictionary<string, Pcg32State> Snapshot()
        {
            var snapshot = new Dictionary<string, Pcg32State>(StringComparer.Ordinal);
            foreach (var pair in _streams)
                snapshot[pair.Key] = pair.Value.State;
            return snapshot;
        }

        public static RngStreams Restore(string seed, IReadOnlyDictionary<string, Pcg32State> snapshot)
        {
            var streams = new RngStreams(seed);
            foreach (string name in Names)
            {
                if (!snapshot.TryGetValue(name, out Pcg32State state))
                    throw new ArgumentException($"Snapshot sem o stream '{name}'.", nameof(snapshot));
                streams._streams[name] = new Pcg32(state);
            }
            return streams;
        }
    }
}
