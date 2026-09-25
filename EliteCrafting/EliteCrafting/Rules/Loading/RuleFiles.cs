using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using BepInEx;
using EliteCrafting.Core;

namespace EliteCrafting.Rules
{
    /// <summary>
    /// A family's files on this machine, in the BepInEx config folder: the main file first, then every other
    /// <c>&lt;prefix&gt;*.yml</c> in case-insensitive name order. Writes the main file from the built-in defaults on
    /// first run (no file of the family exists) and never writes anything else. Tracks names, sizes and write times
    /// so the reload can tell when anything changed.
    /// </summary>
    internal sealed class RuleFiles
    {
        private readonly FamilySpec _spec;
        private string _signature = "";

        public RuleFiles(FamilySpec spec)
        {
            _spec = spec;
        }

        public static string Folder => Paths.ConfigPath;

        /// <summary>Writes the main file from the defaults when the family has no file at all.</summary>
        public void EnsureMainFile()
        {
            if (List().Count > 0)
            {
                return;
            }
            string path = Path.Combine(Folder, _spec.MainFile);
            try
            {
                File.WriteAllText(path, FamilyBuilder.DefaultText(_spec), new UTF8Encoding(false));
                Log.Info($"wrote the default {_spec.MainFile}");
            }
            catch (Exception e)
            {
                Log.Error($"could not write {path}: {e.Message}");
            }
        }

        /// <summary>Reads every file of the family, in order. A file that cannot be read is skipped with an error.</summary>
        public List<SourceText> Read()
        {
            List<SourceText> texts = new List<SourceText>();
            List<string> paths = List();
            foreach (string path in paths)
            {
                try
                {
                    texts.Add(new SourceText(Path.GetFileName(path), File.ReadAllText(path)));
                }
                catch (Exception e)
                {
                    Log.Error($"could not read {path}: {e.Message}");
                }
            }
            _signature = Signature(paths);
            return texts;
        }

        /// <summary>True when a file was added, removed or rewritten since the last <see cref="Read"/>.</summary>
        public bool Changed() => Signature(List()) != _signature;

        private List<string> List()
        {
            List<string> paths = new List<string>();
            try
            {
                foreach (string path in Directory.GetFiles(Folder, _spec.Prefix + "*.yml"))
                {
                    if (path.EndsWith(".yml", StringComparison.OrdinalIgnoreCase))
                    {
                        paths.Add(path);
                    }
                }
            }
            catch (Exception e)
            {
                Log.Error($"could not list {Folder}: {e.Message}");
            }
            paths.Sort(CompareFiles);
            return paths;
        }

        private int CompareFiles(string a, string b)
        {
            bool mainA = string.Equals(Path.GetFileName(a), _spec.MainFile, StringComparison.OrdinalIgnoreCase);
            bool mainB = string.Equals(Path.GetFileName(b), _spec.MainFile, StringComparison.OrdinalIgnoreCase);
            if (mainA != mainB)
            {
                return mainA ? -1 : 1;
            }
            return StringComparer.OrdinalIgnoreCase.Compare(Path.GetFileName(a), Path.GetFileName(b));
        }

        private static string Signature(List<string> paths)
        {
            StringBuilder sb = new StringBuilder();
            foreach (string path in paths)
            {
                try
                {
                    FileInfo info = new FileInfo(path);
                    sb.Append(info.Name).Append('|').Append(info.Length).Append('|').Append(info.LastWriteTimeUtc.Ticks).Append(';');
                }
                catch (Exception)
                {
                    sb.Append(path).Append("|?;");
                }
            }
            return sb.ToString();
        }
    }
}
