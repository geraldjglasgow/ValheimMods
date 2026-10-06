using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BepInEx.Bootstrap;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace DevBridge.World
{
    /// <summary>
    /// ZDOs keep only the hash of each key. This maps hashes back to names by hashing every string literal in the game
    /// and in every loaded plugin; keys built at runtime can be added with keys=. Nothing is read until the first /zdo
    /// asks, and then in the background: a file's literals come from the cache on disk (ZdoNameCache) while the file is
    /// unchanged, so only a game update or a rebuilt mod is read with Cecil again.
    /// </summary>
    internal static class ZdoNames
    {
        private const int MaxLength = 80;

        private static readonly object Gate = new object();
        private static readonly Dictionary<int, string> Added = new Dictionary<int, string>();
        private static Task<Dictionary<int, string>> scanned;

        /// <summary>The scan, started in the background by the first call; /zdo waits for it before naming keys.</summary>
        internal static Task Prepare() => Scanned();

        internal static void Add(string commaSeparated)
        {
            if (string.IsNullOrEmpty(commaSeparated)) return;
            foreach (string key in commaSeparated.Split(',').Select(k => k.Trim()).Where(k => k.Length > 0))
                Added[key.GetStableHashCode()] = key;
        }

        internal static string Of(int hash)
        {
            if (Added.TryGetValue(hash, out string name) || Scanned().Result.TryGetValue(hash, out name)) return name;
            return "#" + hash;
        }

        private static Task<Dictionary<int, string>> Scanned()
        {
            lock (Gate) return scanned ?? (scanned = Task.Run(Scan));
        }

        // The game's names win over a plugin's with the same hash, and earlier plugins over later ones: the files are
        // added in that order and a hash keeps the first name it got.
        private static Dictionary<int, string> Scan()
        {
            var names = new Dictionary<int, string>();
            foreach (FileNames file in ZdoNameCache.For(Sources(), ScanFile))
            {
                foreach (string text in file.Texts)
                {
                    int hash = text.GetStableHashCode();
                    if (!names.ContainsKey(hash)) names[hash] = text;
                }
            }
            return names;
        }

        private static List<string> Sources() =>
            new[] { typeof(ZDO).Assembly.Location }
                .Concat(Chainloader.PluginInfos.Values.Select(p => p.Location))
                .Where(p => !string.IsNullOrEmpty(p))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

        /// <summary>A file's string literals short enough to be a key, each once, in the order they first appear; null when it cannot be read.</summary>
        private static List<string> ScanFile(string path)
        {
            try
            {
                var texts = new List<string>();
                var seen = new HashSet<string>(StringComparer.Ordinal);
                using (AssemblyDefinition assembly = AssemblyDefinition.ReadAssembly(path))
                {
                    foreach (MethodDefinition method in assembly.MainModule.GetTypes().SelectMany(t => t.Methods).Where(m => m.HasBody))
                        foreach (Instruction instruction in method.Body.Instructions)
                            if (instruction.OpCode.Code == Code.Ldstr && instruction.Operand is string text && text.Length <= MaxLength && seen.Add(text))
                                texts.Add(text);
                }
                return texts;
            }
            catch (Exception error)
            {
                UnityEngine.Debug.LogWarning($"DevBridge could not scan {path}: {error.Message}");
                return null;
            }
        }
    }
}
