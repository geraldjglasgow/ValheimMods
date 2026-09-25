using System;
using System.Collections.Generic;
using Charter;
using EliteCrafting.Core;
using YamlDotNet.RepresentationModel;

namespace EliteCrafting.Rules
{
    /// <summary>
    /// One YAML family end to end: this machine's files, the build (merge, parse, validate), and the server binding
    /// through a Charter article carrying the author's file texts (never the parsed model). The author (server, host,
    /// single player, or a player the server does not bind) adopts its own files and publishes them; a bound player
    /// adopts what the server pushed. A family that fails validation keeps the previous rules; at startup, with no
    /// previous rules, the author falls back to the built-in defaults alone and publishes an empty file list, so every
    /// peer plays the same rules and nothing is written to disk.
    /// </summary>
    internal sealed class RuleFamily<T> where T : class
    {
        private readonly FamilySpec _spec;
        private readonly Func<YamlMappingNode, RuleIssues, T?> _parse;
        private readonly RuleFiles _files;
        private List<SourceText> _local = new List<SourceText>();
        private Charter.Charter _charter = null!;
        private Article<List<string>> _article = null!;

        public RuleFamily(FamilySpec spec, Func<YamlMappingNode, RuleIssues, T?> parse)
        {
            _spec = spec;
            _parse = parse;
            _files = new RuleFiles(spec);
        }

        public T? Active { get; private set; }

        /// <summary>The texts <see cref="Active"/> was built from.</summary>
        public RuleSources InForce { get; private set; } = RuleSources.None;

        /// <summary>Raised after <see cref="Active"/> was replaced.</summary>
        public event Action? Adopted;

        public void Setup(Charter.Charter charter)
        {
            _charter = charter;
            _files.EnsureMainFile();
            _local = _files.Read();
            _article = new Article<List<string>>(charter, _spec.SyncKey, Pack(_local));
            _article.Changed += () => Guarded("server push", OnPushed);
            charter.BindingChanged += () => Guarded("binding change", OnBindingChanged);
            Recompute();
        }

        /// <summary>Hot reload tick: re-read the files when anything in the family changed on disk.</summary>
        public void PollDisk()
        {
            if (_files.Changed())
            {
                ReloadLocal();
            }
        }

        /// <summary>Re-reads this machine's files; the author adopts them when they build, a bound player only keeps them.</summary>
        public FamilyReload ReloadLocal()
        {
            List<SourceText> files = _files.Read();
            if (files.Count == 0 && _local.Count > 0)
            {
                Log.Warn($"every {_spec.Prefix}*.yml file is gone; keeping the loaded {_spec.Prefix} rules");
                return FamilyReload.NoFiles;
            }
            _local = files;
            if (!_charter.IsAuthor)
            {
                return FamilyReload.Bound;
            }
            return AdoptLocal() ? FamilyReload.Applied : FamilyReload.Rejected;
        }

        private void Recompute()
        {
            if (_charter.IsAuthor)
            {
                AdoptLocal();
            }
            else
            {
                AdoptPushed();
            }
        }

        private void OnPushed()
        {
            if (!_charter.IsAuthor)
            {
                AdoptPushed();
            }
        }

        private void OnBindingChanged()
        {
            if (_charter.IsAuthor)
            {
                _local = _files.Read();
            }
            Recompute();
        }

        // True when this machine's files built and were adopted.
        private bool AdoptLocal()
        {
            T? model = Build(_local, "this machine's files", out bool defaults);
            if (model != null)
            {
                Adopt(model, new RuleSources(_local, defaults, fromServer: false));
                Publish(_local);
                return true;
            }
            // No previous rules of our own: at startup, or back from a server whose rules must not outlive the session.
            if (Active == null || InForce.FromServer)
            {
                Log.Error($"{_spec.Prefix}: using the built-in defaults alone until the files are fixed");
                List<SourceText> none = new List<SourceText>();
                Adopt(Build(none, "built-in defaults", out _) ?? throw new InvalidOperationException(
                    $"the built-in {_spec.MainFile} does not validate"), new RuleSources(none, true, fromServer: false));
                Publish(none);
            }
            return false;
        }

        private void AdoptPushed()
        {
            List<SourceText> files = Unpack(_article.Value);
            T? model = Build(files, "the server's files", out bool defaults);
            if (model != null)
            {
                Adopt(model, new RuleSources(files, defaults, fromServer: true));
                return;
            }
            Log.Error($"{_spec.Prefix}: the server's files do not build here; keeping the previous rules. Received:");
            foreach (SourceText file in files)
            {
                Log.Error($"--- {file.Name}\n{file.Text}");
            }
        }

        private T? Build(List<SourceText> files, string what, out bool usedDefaults)
        {
            RuleIssues issues = new RuleIssues();
            YamlMappingNode merged = FamilyBuilder.Merge(_spec, files, issues, out usedDefaults);
            T? model = issues.HasErrors ? null : SafeParse(merged, issues);
            Report(issues, what);
            return issues.HasErrors ? null : model;
        }

        private T? SafeParse(YamlMappingNode merged, RuleIssues issues)
        {
            try
            {
                return _parse(merged, issues);
            }
            catch (Exception e)
            {
                issues.Error("", null, $"internal error while reading the rules: {e}");
                return null;
            }
        }

        private void Report(RuleIssues issues, string what)
        {
            foreach (string note in issues.Notes)
            {
                Log.Info($"{_spec.Prefix}: {note}");
            }
            foreach (string warning in issues.Warnings)
            {
                Log.Warn($"{_spec.Prefix}: {warning}");
            }
            if (issues.HasErrors)
            {
                Log.Error($"{_spec.Prefix} ({what}) has {issues.Errors.Count} error(s); the family was not applied:");
                issues.Errors.ForEach(e => Log.Error("  " + e));
            }
        }

        private void Adopt(T model, RuleSources sources)
        {
            Active = model;
            InForce = sources;
            Adopted?.Invoke();
        }

        private void Publish(List<SourceText> files)
        {
            if (_charter.IsAuthor)
            {
                _article.Assign(Pack(files));
            }
        }

        private static List<string> Pack(List<SourceText> files)
        {
            List<string> packed = new List<string>(files.Count * 2);
            foreach (SourceText file in files)
            {
                packed.Add(file.Name);
                packed.Add(file.Text);
            }
            return packed;
        }

        private static List<SourceText> Unpack(List<string>? packed)
        {
            List<SourceText> files = new List<SourceText>();
            for (int i = 0; packed != null && i + 1 < packed.Count; i += 2)
            {
                files.Add(new SourceText(packed[i], packed[i + 1]));
            }
            return files;
        }

        private void Guarded(string what, Action action)
        {
            try
            {
                action();
            }
            catch (Exception e)
            {
                Log.Error($"{_spec.Prefix} ({what}) threw: {e}");
            }
        }
    }
}
