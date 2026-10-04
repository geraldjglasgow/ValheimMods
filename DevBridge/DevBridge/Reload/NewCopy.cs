using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using BepInEx;
using BepInEx.Bootstrap;
using DevBridge.Server;
using Mono.Cecil;

namespace DevBridge.Reload
{
    /// <summary>A newly built plugin DLL, loaded under a new assembly name beside the copy it replaces.</summary>
    internal sealed class NewCopy
    {
        internal Assembly Assembly;
        internal string Name;
        internal string CachePath;
        internal DateTime FileTime;
        internal long Bytes;
        internal readonly List<PluginInfo> Plugins = new List<PluginInfo>();
        internal readonly Dictionary<string, Type> Types = new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Reads a built DLL, renames its assembly with Cecil (Mono refuses a second assembly of the same name, and cannot
    /// unload the first), writes it to BepInEx/cache/DevBridgeReload and loads it from there, so Assembly.Location is a
    /// real file. Everything here happens before the old copy is touched: a DLL that does not load changes nothing.
    /// </summary>
    internal static class CopyLoader
    {
        private static int counter;
        private static bool swept;

        internal static NewCopy Load(string file)
        {
            var copy = new NewCopy { FileTime = File.GetLastWriteTime(file) };
            byte[] bytes = ReadComplete(file);
            copy.Bytes = bytes.Length;
            var typeNames = new Dictionary<PluginInfo, string>();
            using (var stream = new MemoryStream(bytes))
            using (AssemblyDefinition definition = AssemblyDefinition.ReadAssembly(stream, TypeLoader.ReaderParameters))
            {
                foreach (TypeDefinition type in definition.MainModule.Types)
                    Collect(type, copy, typeNames);
                if (copy.Plugins.Count == 0) throw new BridgeException($"{file} holds no BepInEx plugin");
                copy.CachePath = Rename(definition, copy);
            }
            copy.Assembly = Assembly.LoadFrom(copy.CachePath);
            Guarded(copy, () => ResolveTypes(copy, typeNames));
            return copy;
        }

        private static void ResolveTypes(NewCopy copy, Dictionary<PluginInfo, string> typeNames)
        {
            CheckTypes(copy.Assembly);
            foreach (KeyValuePair<PluginInfo, string> pair in typeNames)
                copy.Types[pair.Key.Metadata.GUID] = copy.Assembly.GetType(pair.Value, true);
        }

        /// <summary>
        /// Runs a check on a loaded copy; a copy it refuses stays loaded (Mono cannot unload it) and is marked superseded,
        /// so /eval and the sweeps never take its types for the running ones.
        /// </summary>
        internal static void Guarded(NewCopy copy, Action check)
        {
            try
            {
                check();
            }
            catch (Exception)
            {
                ReloadHistory.Discard(copy.Assembly, copy.Bytes);
                throw;
            }
        }

        private static void Collect(TypeDefinition type, NewCopy copy, Dictionary<PluginInfo, string> typeNames)
        {
            PluginInfo info = Chainloader.ToPluginInfo(type);
            if (info == null) return;
            copy.Plugins.Add(info);
            typeNames[info] = type.FullName;
        }

        /// <summary>The whole file, opened so that a build still writing it makes this fail instead of reading half.</summary>
        internal static byte[] ReadComplete(string file)
        {
            try
            {
                using (var stream = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.Read))
                using (var memory = new MemoryStream())
                {
                    stream.CopyTo(memory);
                    return memory.ToArray();
                }
            }
            catch (IOException error)
            {
                throw new BridgeException($"cannot read {file} yet (a build still writing it?): {error.Message}");
            }
        }

        /// <summary>Gives the assembly a new name and module id, writes it to the cache and returns the path.</summary>
        private static string Rename(AssemblyDefinition definition, NewCopy copy)
        {
            string folder = Path.Combine(Paths.CachePath, "DevBridgeReload");
            Directory.CreateDirectory(folder);
            if (!swept) SweepOldFiles(folder);
            string path;
            do
            {
                copy.Name = $"{definition.Name.Name}-reload-{++counter}";
                path = Path.Combine(folder, copy.Name + ".dll");
            }
            while (System.IO.File.Exists(path) && !TryDelete(path));
            definition.Name.Name = copy.Name;
            definition.MainModule.Name = copy.Name + ".dll";
            definition.MainModule.Mvid = Guid.NewGuid();
            definition.Write(path);
            return path;
        }

        /// <summary>Copies written by earlier game sessions; the ones this session loaded stay locked and are kept.</summary>
        private static void SweepOldFiles(string folder)
        {
            swept = true;
            foreach (string old in Directory.GetFiles(folder, "*.dll")) TryDelete(old);
        }

        private static bool TryDelete(string path)
        {
            try
            {
                System.IO.File.Delete(path);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>Every type must load (a reference the game does not have shows up here) before the old copy goes.</summary>
        private static void CheckTypes(Assembly assembly)
        {
            try
            {
                assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException error)
            {
                IEnumerable<string> causes = error.LoaderExceptions.Where(e => e != null).Select(e => e.Message).Distinct().Take(5);
                throw new BridgeException($"the new DLL loaded but some of its types did not, nothing was torn down: {string.Join("; ", causes)}");
            }
        }
    }
}
