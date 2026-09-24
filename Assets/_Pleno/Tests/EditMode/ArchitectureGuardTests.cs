using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;

namespace Pleno.Tests
{
    /// <summary>
    /// Guarda as regras inegociáveis do CLAUDE.md. Se um destes testes falhar, a correção é no
    /// código que violou a regra, nunca no teste.
    /// </summary>
    public sealed class ArchitectureGuardTests
    {
        static readonly string ScriptsRoot = Path.Combine(Application.dataPath, "_Pleno", "Scripts");

        [Test]
        public void CoreAssembly_HasNoUnityReference()
        {
            var core = Assembly.Load("Pleno.Core");

            var unityRefs = core.GetReferencedAssemblies()
                .Select(r => r.Name)
                .Where(n => n.StartsWith("UnityEngine", StringComparison.Ordinal)
                         || n.StartsWith("UnityEditor", StringComparison.Ordinal)
                         || n.StartsWith("Unity.", StringComparison.Ordinal))
                .ToArray();

            CollectionAssert.IsEmpty(unityRefs, "Pleno.Core não pode referenciar a Unity.");
        }

        // Todo o RNG passa pelo SeededRng do Core: nem UnityEngine.Random, nem System.Random.
        static readonly Regex ForbiddenRandom = new Regex(
            @"UnityEngine\.Random|System\.Random|new\s+Random\s*\(|(?<![\w.])Random\.[A-Z]\w*",
            RegexOptions.Compiled);

        [Test]
        public void Scripts_DoNotUseUnityOrSystemRandom()
        {
            var offenders = ScanScripts(ForbiddenRandom, allow: _ => false);
            CollectionAssert.IsEmpty(offenders, "RNG fora do SeededRng.");
        }

        // Cores moram no GraphicsProfile. Color.white/clear são neutros (sem tinta), não paleta.
        static readonly Regex ForbiddenColor = new Regex(
            @"new\s+(UnityEngine\.)?Color(32)?\s*\(|Color\.(red|green|blue|black|yellow|cyan|magenta|gray|grey)\b|ColorUtility\.TryParseHtmlString|""#[0-9A-Fa-f]{6}",
            RegexOptions.Compiled);

        [Test]
        public void Scripts_DoNotHardcodeColors()
        {
            var offenders = ScanScripts(ForbiddenColor,
                allow: path => Path.GetFileName(path) == "GraphicsProfile.cs");
            CollectionAssert.IsEmpty(offenders, "Cor escrita no código: use o GraphicsProfile.");
        }

        static string[] ScanScripts(Regex pattern, Func<string, bool> allow)
        {
            return Directory.GetFiles(ScriptsRoot, "*.cs", SearchOption.AllDirectories)
                .Where(path => !allow(path))
                .SelectMany(path => File.ReadAllLines(path)
                    .Select((line, i) => (path, line, i))
                    .Where(t => !t.line.TrimStart().StartsWith("//", StringComparison.Ordinal)
                             && pattern.IsMatch(t.line))
                    .Select(t => $"{Path.GetFileName(t.path)}:{t.i + 1}: {t.line.Trim()}"))
                .ToArray();
        }
    }
}
