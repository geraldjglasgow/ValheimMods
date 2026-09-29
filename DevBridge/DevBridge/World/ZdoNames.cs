using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using BepInEx.Bootstrap;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace DevBridge.World
{
    /// <summary>
    /// ZDOs keep only the hash of each key. This maps hashes back to names by hashing every string literal in the game
    /// and in every loaded plugin, scanned once in the background; keys built at runtime can be added with keys=.
    /// </summary>
    internal static class ZdoNames
    {
        private const int MaxLength = 80;

        private static readonly Lazy<Dictionary<int, string>> Scanned =
            new Lazy<Dictionary<int, string>>(Scan, LazyThreadSafetyMode.ExecutionAndPublication);

        private static readonly Dictionary<int, string> Added = new Dictionary<int, string>();

        /// <summary>Starts the scan early so the first /zdo does not wait for it.</summary>
        internal static void Prepare() => ThreadPool.QueueUserWorkItem(_ => { _ = Scanned.Value; });

        internal static void Add(string commaSeparated)
        {
            if (string.IsNullOrEmpty(commaSeparated)) return;
            foreach (string key in commaSeparated.Split(',').Select(k => k.Trim()).Where(k => k.Length > 0))
                Added[key.GetStableHashCode()] = key;
        }

        internal static string Of(int hash)
        {
            if (Added.TryGetValue(hash, out string name) || Scanned.Value.TryGetValue(hash, out name)) return name;
            return "#" + hash;
        }

        private static Dictionary<int, string> Scan()
        {
            var names = new Dictionary<int, string>();
            foreach (string path in Sources())
            {
                try { ScanFile(path, names); }
                catch (Exception error) { UnityEngine.Debug.LogWarning($"DevBridge could not scan {path}: {error.Message}"); }
            }
            return names;
        }

        private static IEnumerable<string> Sources() =>
            new[] { typeof(ZDO).Assembly.Location }
                .Concat(Chainloader.PluginInfos.Values.Select(p => p.Location))
                .Where(p => !string.IsNullOrEmpty(p))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

        private static void ScanFile(string path, Dictionary<int, string> names)
        {
            using (AssemblyDefinition assembly = AssemblyDefinition.ReadAssembly(path))
            {
                foreach (MethodDefinition method in assembly.MainModule.GetTypes().SelectMany(t => t.Methods).Where(m => m.HasBody))
                    foreach (Instruction instruction in method.Body.Instructions)
                        if (instruction.OpCode.Code == Code.Ldstr && instruction.Operand is string text && text.Length <= MaxLength)
                            AddName(names, text);
            }
        }

        private static void AddName(Dictionary<int, string> names, string text)
        {
            int hash = text.GetStableHashCode();
            if (!names.ContainsKey(hash)) names[hash] = text;
        }
    }
}
