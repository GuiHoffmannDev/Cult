using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using Pleno.Core.Model;
using Pleno.Core.Rng;

namespace Pleno.Game.Save
{
    /// <summary>Envelope do arquivo de save, com versão para migrações futuras.</summary>
    public sealed class RunSaveFile
    {
        public const int CurrentVersion = 1;

        public int Version { get; set; } = CurrentVersion;
        public RunState Run { get; set; }
    }

    /// <summary>
    /// Save da run em JSON (GDD 10.8). Grava o <see cref="RunState"/> inteiro, incluindo o estado de
    /// cada stream de RNG: sair e voltar não troca nenhum resultado.
    /// </summary>
    public static class RunSerializer
    {
        public static readonly JsonSerializerSettings Settings = new JsonSerializerSettings
        {
            Formatting = Formatting.Indented,
            MissingMemberHandling = MissingMemberHandling.Ignore,
            ObjectCreationHandling = ObjectCreationHandling.Replace,
            Converters = new List<JsonConverter>
            {
                new StringEnumConverter(),
                new RngStreamsConverter(),
                new WheelConverter(),
            },
        };

        public static string Serialize(RunState run) =>
            JsonConvert.SerializeObject(new RunSaveFile { Run = run }, Settings);

        public static RunState Deserialize(string json)
        {
            var file = JsonConvert.DeserializeObject<RunSaveFile>(json, Settings);
            if (file == null || file.Run == null)
                throw new InvalidDataException("Save vazio ou corrompido.");
            if (file.Version != RunSaveFile.CurrentVersion)
                throw new InvalidDataException($"Versão de save não suportada: {file.Version}.");
            return file.Run;
        }

        /// <summary>Grava num arquivo temporário e troca, para um crash no meio não corromper o save.</summary>
        public static void SaveToFile(RunState run, string path)
        {
            string temp = path + ".tmp";
            File.WriteAllText(temp, Serialize(run));
            if (File.Exists(path))
                File.Delete(path);
            File.Move(temp, path);
        }

        public static RunState LoadFromFile(string path) => Deserialize(File.ReadAllText(path));
    }

    /// <summary>Seed + estado de cada stream. Os ulongs vão em hexadecimal para não perder precisão.</summary>
    sealed class RngStreamsConverter : JsonConverter<RngStreams>
    {
        public override void WriteJson(JsonWriter writer, RngStreams value, JsonSerializer serializer)
        {
            var streams = new JObject();
            foreach (var pair in value.Snapshot())
            {
                streams[pair.Key] = new JObject
                {
                    ["state"] = pair.Value.State.ToString("x16"),
                    ["inc"] = pair.Value.Inc.ToString("x16"),
                };
            }
            new JObject { ["seed"] = value.Seed, ["streams"] = streams }.WriteTo(writer);
        }

        public override RngStreams ReadJson(JsonReader reader, Type objectType, RngStreams existingValue,
            bool hasExistingValue, JsonSerializer serializer)
        {
            JObject obj = JObject.Load(reader);
            var snapshot = new Dictionary<string, Pcg32State>();
            foreach (var pair in (JObject)obj["streams"])
            {
                snapshot[pair.Key] = new Pcg32State(
                    ulong.Parse((string)pair.Value["state"], NumberStyles.HexNumber, CultureInfo.InvariantCulture),
                    ulong.Parse((string)pair.Value["inc"], NumberStyles.HexNumber, CultureInfo.InvariantCulture));
            }
            return RngStreams.Restore((string)obj["seed"], snapshot);
        }
    }

    /// <summary>A roda com os Ids das casas e o próximo Id (as apostas de Vizinhos dependem deles).</summary>
    sealed class WheelConverter : JsonConverter<Wheel>
    {
        public override void WriteJson(JsonWriter writer, Wheel value, JsonSerializer serializer)
        {
            writer.WriteStartObject();
            writer.WritePropertyName("nextId");
            writer.WriteValue(value.NextId);
            writer.WritePropertyName("pockets");
            serializer.Serialize(writer, value.Pockets);
            writer.WriteEndObject();
        }

        public override Wheel ReadJson(JsonReader reader, Type objectType, Wheel existingValue,
            bool hasExistingValue, JsonSerializer serializer)
        {
            JObject obj = JObject.Load(reader);
            var pockets = obj["pockets"].ToObject<List<Pocket>>(serializer);
            return Wheel.Restore(pockets, (int)obj["nextId"]);
        }
    }
}
