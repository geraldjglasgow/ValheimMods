using System.Collections.Generic;
using System.IO;
using System.Linq;
using OpenKeep.Core;

namespace OpenKeep.Blueprints.Bench
{
    /// <summary>
    /// The bench window's left side: the player's own blueprint library (BepInEx/config/OpenKeep.Blueprints, the files
    /// the hammer's Blueprints tab shows) as a file explorer: one folder at a time, the way up first, then its folders,
    /// then its blueprints. New folder, Rename, Delete (asked first), drags onto a folder move, Share sends the picks
    /// (a folder: everything in it, its inner folders kept) into the folder the pool side shows when that is the
    /// player's own part, else to the top of it; Share all sends every blueprint to its own path. Lines dropped from the
    /// pool are taken as copies into the folder dropped on. Every change goes through <see cref="BlueprintFiles"/>, so
    /// the hammer's tab follows it.
    /// </summary>
    public sealed class LibraryPane : IBenchPane
    {
        private int seenLibrary = -1;
        private string seenFolder;

        public LibraryPane()
        {
            Actions = new List<BenchAction>
            {
                new BenchAction(BlueprintWords.NewFolderName, () => true, NewFolder),
                new BenchAction(BenchWords.Rename, () => Selection.Count == 1, () => Rename(Selection.Of(Entries)[0])),
                new BenchAction(BenchWords.Delete, () => Selection.Count > 0, Remove),
                new BenchAction(BenchWords.Share, () => Selection.Count > 0 && !BenchShare.Busy, ShareSelected),
                new BenchAction(BenchWords.ShareAll, () => !BenchShare.Busy, () => BenchShare.Share(BenchShare.AllOwn())),
            };
        }

        /// <summary>The folder shown ("" the top), kept while the game runs.</summary>
        public string Folder { get; private set; } = "";

        public string Header => Language.Localize(BenchWords.Yours);

        public string Location => BenchPaneView.Trail(Language.Localize(BlueprintWords.Tab), Folder);

        public List<BenchEntry> Entries { get; } = new List<BenchEntry>();

        public BenchSelection Selection { get; } = new BenchSelection();

        public int Version { get; private set; }

        public List<BenchAction> Actions { get; }

        public void Tick()
        {
            if (Folder.Length > 0 && !Directory.Exists(BlueprintLibrary.FullPath(Folder)))
                Folder = "";
            List<string> folders = BlueprintLibrary.Folders(Folder);
            List<string> names = BlueprintLibrary.Names(Folder);
            if (BlueprintLibrary.Version == seenLibrary && Folder == seenFolder)
                return;
            seenLibrary = BlueprintLibrary.Version;
            seenFolder = Folder;
            Entries.Clear();
            if (Folder.Length > 0)
                Entries.Add(BenchEntry.Up(0L, BlueprintLibrary.Parent(Folder)));
            Entries.AddRange(folders.Select(f => BenchEntry.Of(BenchKind.Folder, 0L, f)));
            Entries.AddRange(names.Select(n => BenchEntry.Of(BenchKind.Blueprint, 0L, n)));
            Selection.Keep(Entries);
            Version++;
        }

        public void Open(BenchEntry entry)
        {
            if (!entry.Holds)
                return;
            Folder = entry.Path;
            Selection.Clear();
        }

        private void NewFolder()
        {
            NamePrompt.Ask(Language.Localize(BlueprintWords.NewFolderTopic), "", BlueprintFiles.NameLimit, name =>
            {
                if (BlueprintFiles.CreateFolder(Folder, name, out string path, out string error))
                    Messages.TopLeft(BlueprintWords.Format(BlueprintWords.FolderMade, path));
                else
                    Messages.TopLeft(error);
                BlueprintLibrary.Rescan();
            });
        }

        public void Rename(BenchEntry entry)
        {
            if (entry.Kind != BenchKind.Folder && entry.Kind != BenchKind.Blueprint)
                return;
            NamePrompt.Ask(Language.Localize(BlueprintWords.RenameTopic), BlueprintLibrary.Leaf(entry.Path), BlueprintFiles.NameLimit,
                typed => Move(entry, typed, false));
        }

        /// <summary>Renames or moves one line to what was typed (a path from the top with "/"); says why not at the top left.</summary>
        private static bool Move(BenchEntry entry, string typed, bool quiet)
        {
            bool folder = entry.Kind == BenchKind.Folder;
            if (BlueprintFiles.Rename(entry.Path, folder, typed, out string to, out string error))
            {
                if (!folder)
                    BlueprintMenu.Moved(entry.Path, to);
                if (!quiet)
                    Messages.TopLeft(BlueprintWords.Format(BlueprintWords.Renamed, entry.Path, to));
                return true;
            }
            if (error != null)
                Messages.TopLeft(BlueprintWords.Format(BlueprintWords.MoveRefused, entry.Label, error));
            return false;
        }

        public void Remove()
        {
            List<BenchEntry> picked = Selection.Of(Entries);
            if (picked.Count == 0)
                return;
            BenchConfirm.Ask(BenchWords.Delete, BlueprintWords.Format(BenchWords.DeleteAsk, BenchConfirm.Names(picked)), () => Delete(picked));
        }

        private void Delete(List<BenchEntry> picked)
        {
            int deleted = 0;
            foreach (BenchEntry entry in picked)
            {
                if (BlueprintFiles.Delete(entry.Path, entry.Kind == BenchKind.Folder, out string error))
                    deleted++;
                else if (error != null)
                    Messages.TopLeft(error);
            }
            Selection.Clear();
            if (deleted > 0)
                Messages.TopLeft(BlueprintWords.Format(BenchWords.Deleted, BenchConfirm.Names(picked)));
        }

        public bool Accepts(IBenchPane from, BenchEntry onto) => onto == null || onto.Holds;

        public void Drop(IBenchPane from, List<BenchEntry> dragged, BenchEntry onto)
        {
            string into = onto?.Path ?? Folder;
            if (from is PoolPane pool)
                pool.TakeInto(dragged, into);
            else
                MoveInto(dragged, into);
        }

        /// <summary>Moves the lines into a folder one by one; a refused one says why and the rest still move.</summary>
        private void MoveInto(List<BenchEntry> dragged, string into)
        {
            int moved = 0;
            foreach (BenchEntry entry in dragged.Where(e => e.Kind == BenchKind.Folder || e.Kind == BenchKind.Blueprint))
            {
                if (BlueprintLibrary.Parent(entry.Path) == into || entry.Path == into)
                    continue;
                if (Move(entry, "/" + BlueprintLibrary.Join(into, BlueprintLibrary.Leaf(entry.Path)), true))
                    moved++;
            }
            Selection.Clear();
            if (moved > 0)
                Messages.TopLeft(BlueprintWords.Format(BlueprintWords.Moved, moved, BlueprintEntries.Shown(into)));
        }

        private void ShareSelected()
        {
            List<(string, string)> files = SharePairs(Selection.Of(Entries), BenchWindow.Pool.ShareFolder);
            if (files.Count == 0)
                Messages.TopLeft(Language.Localize(BenchWords.NothingToShare));
            BenchShare.Share(files);
        }

        /// <summary>
        /// Library blueprints and where each goes in the player's part: a blueprint into <paramref name="into"/> under its
        /// name, a folder's blueprints into <paramref name="into"/> with the folder and its inner folders kept.
        /// </summary>
        public static List<(string, string)> SharePairs(List<BenchEntry> picked, string into)
        {
            List<(string, string)> files = new List<(string, string)>();
            List<string> all = null;
            foreach (BenchEntry entry in picked)
            {
                if (entry.Kind == BenchKind.Blueprint)
                    files.Add((entry.Path, BlueprintLibrary.Join(into, BlueprintLibrary.Leaf(entry.Path))));
                if (entry.Kind != BenchKind.Folder)
                    continue;
                all = all ?? BlueprintLibrary.Everything(false);
                string parent = BlueprintLibrary.Parent(entry.Path);
                foreach (string path in all.Where(p => p != entry.Path && BenchPaths.Within(p, entry.Path)))
                    files.Add((path, BlueprintLibrary.Join(into, BenchPaths.Below(path, parent))));
            }
            return files;
        }
    }
}
