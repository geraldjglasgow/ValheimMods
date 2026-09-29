using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DevBridge.Logs;
using DevBridge.Server;
using UnityEngine;
using Object = UnityEngine.Object;

namespace DevBridge.Stage
{
    /// <summary>An asset bundle loaded from a file, kept by its name.</summary>
    internal sealed class LoadedBundle
    {
        internal string Name;
        internal string Path;
        internal AssetBundle Bundle;
        internal long Bytes;
        internal DateTime FileTime;
        internal DateTime LoadedAt;
    }

    /// <summary>
    /// Asset bundles loaded from the workshop's build output while the game runs. Each is read into memory and loaded
    /// from there, so the file stays free for the next build to overwrite; unloading destroys everything loaded from it.
    /// </summary>
    internal static class Bundles
    {
        private static readonly Dictionary<string, LoadedBundle> Loaded = new Dictionary<string, LoadedBundle>(StringComparer.OrdinalIgnoreCase);

        internal static IEnumerable<LoadedBundle> All => Loaded.Values.Where(b => b.Bundle).OrderBy(b => b.Name).ToList();

        internal static LoadedBundle Get(string name)
        {
            if (Loaded.TryGetValue(name, out LoadedBundle bundle) && bundle.Bundle) return bundle;
            LoadedBundle byPath = All.FirstOrDefault(b => string.Equals(b.Path, Full(name), StringComparison.OrdinalIgnoreCase));
            return byPath ?? throw new BridgeException($"no bundle {name} is loaded (loaded: {Names()}); load one with /bundle?load=<file>");
        }

        /// <summary>
        /// The loaded bundle read from this file, or of this file's name (the same bundle built to another folder, which
        /// then reads from here): Unity loads no two bundles of one name at once. Null when neither is loaded.
        /// </summary>
        internal static LoadedBundle FromFile(string given)
        {
            string path = Resolve(given);
            LoadedBundle same = All.FirstOrDefault(b => string.Equals(b.Path, path, StringComparison.OrdinalIgnoreCase));
            if (same != null || !Loaded.TryGetValue(System.IO.Path.GetFileNameWithoutExtension(path), out LoadedBundle named) || !named.Bundle) return same;
            named.Path = path;
            return named;
        }

        internal static LoadedBundle Load(string given)
        {
            string path = Resolve(given);
            AssetBundle bundle = Read(path);
            var loaded = new LoadedBundle { Name = NameOf(bundle, path), Path = path, Bundle = bundle, LoadedAt = DateTime.Now };
            Stamp(loaded);
            Loaded[loaded.Name] = loaded;
            return loaded;
        }

        /// <summary>Unloads it with everything loaded from it (unloadAllLoadedObjects), then reads the file again.</summary>
        internal static void Refresh(LoadedBundle loaded)
        {
            Drop(loaded);
            loaded.Bundle = Read(loaded.Path);
            loaded.LoadedAt = DateTime.Now;
            Stamp(loaded);
            Loaded[loaded.Name] = loaded;
        }

        internal static void Drop(LoadedBundle loaded)
        {
            Loaded.Remove(loaded.Name);
            if (loaded.Bundle) loaded.Bundle.Unload(true);
            loaded.Bundle = null;
        }

        /// <summary>An asset by name from the named bundle, or from whichever loaded bundle has it.</summary>
        internal static T Asset<T>(string asset, string bundle, out LoadedBundle from) where T : Object
        {
            IEnumerable<LoadedBundle> pool = bundle != null ? new[] { Get(bundle) } : All;
            var hits = pool.Select(b => new { Bundle = b, Found = b.Bundle.LoadAsset<T>(asset) }).Where(h => h.Found).ToList();
            if (hits.Count == 0)
                throw new BridgeException($"no {typeof(T).Name} {asset} in {bundle ?? "any loaded bundle"} (loaded: {Names()}; list assets with /bundle?assets=<name>)");
            if (hits.Count > 1)
                throw new BridgeException($"{asset} is in {string.Join(", ", hits.Select(h => h.Bundle.Name))}: give bundle=");
            from = hits[0].Bundle;
            return hits[0].Found;
        }

        internal static string Names() => Loaded.Count == 0 ? "none" : string.Join(", ", All.Select(b => b.Name));

        private static AssetBundle Read(string path)
        {
            long mark = LogCapture.Lines.Next;
            AssetBundle bundle = AssetBundle.LoadFromMemory(File.ReadAllBytes(path));
            if (bundle) return bundle;
            string said = string.Join(" | ", LogCapture.Lines.Since(mark, 5, l => l.Text.IndexOf("bundle", StringComparison.OrdinalIgnoreCase) >= 0).Select(l => l.Text));
            string others = string.Join(", ", AssetBundle.GetAllLoadedAssetBundles().Select(b => b.name).OrderBy(n => n));
            throw new BridgeException(
                $"Unity would not load {path}: {(said.Length > 0 ? said : "see /log?level=error")}. A bundle only loads on the platform it was built " +
                "for (.windows here), and not while another bundle with the same name is loaded - a mod that embeds this bundle " +
                $"has it loaded already: build the preview under another name (build.ps1 -Bundle <name>_preview). Bundles loaded now: {others}");
        }

        private static string NameOf(AssetBundle bundle, string path) =>
            string.IsNullOrEmpty(bundle.name) ? System.IO.Path.GetFileNameWithoutExtension(path) : bundle.name;

        private static void Stamp(LoadedBundle loaded)
        {
            var file = new FileInfo(loaded.Path);
            loaded.Bytes = file.Length;
            loaded.FileTime = file.LastWriteTime;
        }

        private static string Full(string given) => System.IO.Path.GetFullPath(Fmt.WindowsPath(given.Trim().Trim('"')));

        /// <summary>The file itself, or the Windows build next to or under what was given (workshop output layouts).</summary>
        internal static string Resolve(string given)
        {
            string path = Full(given);
            string name = System.IO.Path.GetFileName(path.TrimEnd('\\', '/'));
            string[] candidates =
            {
                path, path + ".windows", System.IO.Path.Combine(path, name + ".windows"), System.IO.Path.Combine(path, "windows", name),
            };
            return candidates.FirstOrDefault(File.Exists) ?? throw new BridgeException(
                $"no bundle file at {path} (also tried .windows beside it, <folder>/{name}.windows and <folder>/windows/{name}); give an absolute path");
        }
    }
}
