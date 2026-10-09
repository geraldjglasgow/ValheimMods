using System;
using System.Collections.Generic;
using System.Linq;
using OpenKeep.Core;

namespace OpenKeep.Blueprints.Bench
{
    /// <summary>
    /// The bench window's right side: the world's shared pool as a file explorer. At the top, everyone who shared (the
    /// local player first, always listed so they can make folders before sharing); inside a player's part, the way up,
    /// their folders (empty ones too), their blueprints. Take a copy (or a drag onto the library side) copies the picks
    /// into the library folder shown (a folder with what is inside it, a player's whole part under their name). The
    /// sharer or an admin makes folders, renames, removes (asked first) and moves by dragging onto a folder; blueprints
    /// dragged in from the library are shared into the player's own part only. The server checks every request again.
    /// </summary>
    public sealed class PoolPane : IBenchPane
    {
        private int seenPool = -1;
        private long seenOwner = -1L;
        private string seenFolder;

        public PoolPane()
        {
            Actions = new List<BenchAction>
            {
                new BenchAction(BenchWords.Take, () => Selection.Count > 0 && !BenchTake.Busy,
                    () => TakeInto(Selection.Of(Entries), BenchWindow.Library.Folder)),
                new BenchAction(BlueprintWords.NewFolderName, () => MayChange(Owner), NewFolder),
                new BenchAction(BenchWords.Rename, () => Selection.Count == 1 && MayChange(Owner), () => Rename(Selection.Of(Entries)[0])),
                new BenchAction(BenchWords.Remove, () => Selection.Count > 0 && Selection.Of(Entries).All(e => MayChange(OwnerOf(e))), Remove),
            };
        }

        /// <summary>The player whose part is shown; 0 for the list of players.</summary>
        public long Owner { get; private set; }

        /// <summary>The folder shown in that part ("" its top).</summary>
        public string Folder { get; private set; } = "";

        /// <summary>Where shared blueprints go: the folder shown when it is in the player's own part, else the top of it.</summary>
        public string ShareFolder => Owner != 0L && Owner == Me ? Folder : "";

        public static long Me => Player.m_localPlayer != null ? Player.m_localPlayer.GetPlayerID() : 0L;

        private static bool Admin => ZNet.instance != null && ZNet.instance.LocalPlayerIsAdminOrHost();

        /// <summary>The local player may change this part (theirs, or they are an admin); the server checks again.</summary>
        public static bool MayChange(long owner) => owner != 0L && (owner == Me || Admin);

        public string Header => Language.Localize(BenchWords.Pool);

        public string Location => Owner == 0L ? Language.Localize(BenchWords.Players) : BenchPaneView.Trail(NameOf(Owner), Folder);

        public List<BenchEntry> Entries { get; } = new List<BenchEntry>();

        public BenchSelection Selection { get; } = new BenchSelection();

        public int Version { get; private set; }

        public List<BenchAction> Actions { get; }

        private long OwnerOf(BenchEntry entry) => entry.Kind == BenchKind.Owner ? entry.Owner : Owner;

        /// <summary>The listed players, the local player always among them (an empty part until they share).</summary>
        private static List<BenchOwner> Parts()
        {
            List<BenchOwner> parts = new List<BenchOwner>(BenchPool.Owners);
            long me = Me;
            if (me != 0L && parts.All(o => o.Id != me))
                parts.Add(new BenchOwner { Id = me, Name = BenchPaths.Safe(Player.m_localPlayer.GetPlayerName()) });
            return parts.OrderBy(o => o.Id == me ? 0 : 1).ThenBy(o => o.Name, StringComparer.OrdinalIgnoreCase).ToList();
        }

        private static string NameOf(long owner) => Parts().FirstOrDefault(o => o.Id == owner)?.Name ?? "?";

        public void Tick()
        {
            if (BenchPool.Version == seenPool && Owner == seenOwner && Folder == seenFolder)
                return;
            seenPool = BenchPool.Version;
            Entries.Clear();
            BenchOwner part = Owner == 0L ? null : Parts().FirstOrDefault(o => o.Id == Owner);
            if (part == null)
            {
                Owner = 0L;
                Folder = "";
                Entries.AddRange(BenchPool.Loaded ? Parts().Select(Line) : Enumerable.Empty<BenchEntry>());
            }
            else
                Fill(part);
            seenOwner = Owner;
            seenFolder = Folder;
            Selection.Keep(Entries);
            Version++;
        }

        private static BenchEntry Line(BenchOwner owner)
        {
            string you = owner.Id == Me ? " " + Language.Localize(BenchWords.You) : "";
            return new BenchEntry
            {
                Kind = BenchKind.Owner, Owner = owner.Id, Name = owner.Name,
                Label = $"<b>{owner.Name}</b>{you}  <color=#b4b4b4>{owner.Paths.Count}</color>",
            };
        }

        /// <summary>One folder of a player's part: the way up (to the list of players from its top), its folders, its blueprints.</summary>
        private void Fill(BenchOwner part)
        {
            if (!part.Paths.Any(p => BenchPaths.Within(p, Folder)) && !part.Folders.Any(f => BenchPaths.Within(f, Folder)))
                Folder = "";
            Entries.Add(Folder.Length > 0 ? BenchEntry.Up(Owner, BlueprintLibrary.Parent(Folder)) : BenchEntry.Up(0L, ""));
            Entries.AddRange(Children(part, Folder).Select(f => BenchEntry.Of(BenchKind.Folder, Owner, f)));
            Entries.AddRange(part.Paths.Where(p => Same(BlueprintLibrary.Parent(p), Folder))
                .Select(p => BenchEntry.Of(BenchKind.Blueprint, Owner, p)));
        }

        /// <summary>The folders right inside one: those the server listed and those its blueprints lie in.</summary>
        private static IEnumerable<string> Children(BenchOwner part, string folder)
        {
            SortedSet<string> found = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (string path in part.Folders.Concat(part.Paths.Select(BlueprintLibrary.Parent)))
            {
                if (path.Length == 0 || Same(path, folder) || !BenchPaths.Within(path, folder))
                    continue;
                found.Add(BlueprintLibrary.Join(folder, BenchPaths.Below(path, folder).Split('/')[0]));
            }
            return found;
        }

        private static bool Same(string a, string b) => string.Equals(a, b, StringComparison.OrdinalIgnoreCase);

        public void Open(BenchEntry entry)
        {
            if (!entry.Holds)
                return;
            Owner = entry.Kind == BenchKind.Owner ? entry.Owner : entry.Kind == BenchKind.Up ? entry.Owner : Owner;
            Folder = entry.Kind == BenchKind.Owner ? "" : entry.Path;
            Selection.Clear();
        }

        private void NewFolder()
        {
            long owner = Owner;
            string at = Folder;
            NamePrompt.Ask(Language.Localize(BlueprintWords.NewFolderTopic), "", BlueprintFiles.NameLimit, name =>
            {
                string path = BlueprintFiles.Target(name, at, out string error);
                if (error != null)
                    Messages.TopLeft(error);
                else
                    ZRoutedRpc.instance?.InvokeRoutedRPC(BenchRpc.MakeFolder, owner, path);
            });
        }

        public void Rename(BenchEntry entry)
        {
            if ((entry.Kind != BenchKind.Folder && entry.Kind != BenchKind.Blueprint) || !MayChange(Owner))
                return;
            long owner = Owner;
            NamePrompt.Ask(Language.Localize(BlueprintWords.RenameTopic), BlueprintLibrary.Leaf(entry.Path), BlueprintFiles.NameLimit, typed =>
            {
                string to = BlueprintFiles.Target(typed, BlueprintLibrary.Parent(entry.Path), out string error);
                if (error != null)
                    Messages.TopLeft(error);
                else if (to != entry.Path)
                    ZRoutedRpc.instance?.InvokeRoutedRPC(BenchRpc.Move, owner, entry.Path, to, entry.Kind == BenchKind.Folder);
            });
        }

        public void Remove()
        {
            List<BenchEntry> picked = Selection.Of(Entries).Where(e => MayChange(OwnerOf(e))).ToList();
            if (picked.Count == 0)
                return;
            BenchConfirm.Ask(BenchWords.Remove, BlueprintWords.Format(BenchWords.RemoveAsk, BenchConfirm.Names(picked)), () =>
            {
                foreach (BenchEntry entry in picked)
                    ZRoutedRpc.instance?.InvokeRoutedRPC(BenchRpc.Remove, OwnerOf(entry), entry.Path, entry.Kind != BenchKind.Blueprint);
                Selection.Clear();
            });
        }

        public bool Accepts(IBenchPane from, BenchEntry onto)
        {
            if (from is LibraryPane)
                return Owner == 0L ? onto == null || (onto.Kind == BenchKind.Owner && onto.Owner == Me) : Owner == Me && (onto == null || onto.Holds);
            return from == this && onto != null && onto.Holds && onto.Kind != BenchKind.Owner && onto.Owner != 0L && MayChange(Owner);
        }

        public void Drop(IBenchPane from, List<BenchEntry> dragged, BenchEntry onto)
        {
            if (from is LibraryPane)
                BenchShare.Share(LibraryPane.SharePairs(dragged, Owner == Me && onto != null && onto.Owner != 0L ? onto.Path : ShareFolder));
            else
                MoveInto(dragged, onto.Path);
        }

        /// <summary>Asks the server to move each line into a folder of the same part; lines already there are left.</summary>
        private void MoveInto(List<BenchEntry> dragged, string into)
        {
            foreach (BenchEntry entry in dragged.Where(e => e.Kind == BenchKind.Folder || e.Kind == BenchKind.Blueprint))
            {
                if (!Same(BlueprintLibrary.Parent(entry.Path), into) && !BenchPaths.Within(into, entry.Path))
                    ZRoutedRpc.instance?.InvokeRoutedRPC(BenchRpc.Move, Owner, entry.Path,
                        BlueprintLibrary.Join(into, BlueprintLibrary.Leaf(entry.Path)), entry.Kind == BenchKind.Folder);
            }
            Selection.Clear();
        }

        /// <summary>Copies lines into a library folder: a blueprint by name, a folder with its contents, a player's part under their name.</summary>
        public void TakeInto(List<BenchEntry> picked, string into)
        {
            foreach (BenchEntry entry in picked)
            {
                BenchOwner part = BenchPool.Find(OwnerOf(entry));
                if (part != null)
                    BenchTake.Take(part.Id, TakePairs(part, entry, into), into);
            }
        }

        private static IEnumerable<(string, string)> TakePairs(BenchOwner part, BenchEntry entry, string into)
        {
            if (entry.Kind == BenchKind.Blueprint)
                return new[] { (entry.Path, BlueprintLibrary.Join(into, BlueprintLibrary.Leaf(entry.Path))) };
            if (entry.Kind == BenchKind.Owner)
                return part.Paths.Select(p => (p, BlueprintLibrary.Join(into, BlueprintLibrary.Join(BenchPaths.Safe(part.Name), p))));
            string parent = BlueprintLibrary.Parent(entry.Path);
            return part.Paths.Where(p => p != entry.Path && BenchPaths.Within(p, entry.Path))
                .Select(p => (p, BlueprintLibrary.Join(into, BenchPaths.Below(p, parent))));
        }
    }
}
