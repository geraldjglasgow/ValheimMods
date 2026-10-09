using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace OpenKeep.Blueprints.Bench
{
    /// <summary>
    /// Server side of the bench: lists the pool, stores a shared file once all its parts are in, sends a file back in
    /// parts, removes and moves. Every request is checked here whatever the asking machine checked: blueprints on, a
    /// known character, valid paths, the size and count limits, and for a change the sharer or an admin
    /// (<see cref="BenchRights"/>). Answers go to the asking machine; a change is announced to everybody.
    /// </summary>
    public static class BenchServer
    {
        private static readonly BenchInbox inbox = new BenchInbox();

        private static bool Serving(long sender)
        {
            if (ZNet.instance == null || !ZNet.instance.IsServer())
                return false;
            if (!BlueprintSettings.Enabled)
                BenchRpc.Tell(sender, BlueprintWords.Disabled);
            return BlueprintSettings.Enabled;
        }

        public static void OnList(long sender)
        {
            if (Serving(sender))
                ZRoutedRpc.instance.InvokeRoutedRPC(sender, BenchRpc.Listing, BenchStore.Listing);
        }

        /// <summary>One part of a shared file: path, part, parts, bytes. The last part stores it.</summary>
        public static void OnPut(long sender, ZPackage package)
        {
            if (!Serving(sender))
                return;
            string path = package.ReadString();
            int part = package.ReadInt();
            int parts = package.ReadInt();
            byte[] packed = inbox.Add(sender + "|" + path, part, parts, package.ReadByteArray());
            if (packed != null)
                ZRoutedRpc.instance.InvokeRoutedRPC(sender, BenchRpc.PutDone, path, Store(sender, path, packed) ?? "");
        }

        /// <summary>Stores a whole shared file; null when stored, else the reason's word.</summary>
        private static string Store(long sender, string path, byte[] packed)
        {
            long id = BenchRights.Who(sender, out string name);
            if (id == 0L)
                return BenchWords.Unknown;
            if (BenchPaths.Clean(path) == null)
                return BlueprintWords.BadName;
            byte[] json = BenchZip.Unpack(packed, BenchLimits.MaxFileBytes);
            if (json == null)
                return BenchWords.TooBig;
            if (!LooksLikeJson(json))
                return BenchWords.NotBlueprint;
            if (!BenchStore.Has(id, path) && (BenchStore.Find(id)?.Paths.Count ?? 0) >= BenchLimits.MaxPerPlayer)
                return BenchWords.Full;
            if (!Disk(() => BenchStore.Save(id, name, path, json)))
                return BenchWords.Failed;
            Plugin.Log.LogInfo($"OpenKeep: {name} shared the blueprint {path} at the bench ({json.Length} bytes)");
            Announce();
            return null;
        }

        /// <summary>A blueprint file is a JSON object: its first character that is not white space (or a byte order mark) is "{".</summary>
        private static bool LooksLikeJson(byte[] json)
        {
            foreach (byte b in json)
            {
                if (b == '{')
                    return true;
                if (b != ' ' && b != '\t' && b != '\r' && b != '\n' && b != 0xEF && b != 0xBB && b != 0xBF)
                    return false;
            }
            return false;
        }

        /// <summary>Sends one shared file in parts: owner, owner's name, path, part, parts, bytes; no parts when it is gone.</summary>
        public static void OnGet(long sender, long owner, string path)
        {
            if (!Serving(sender))
                return;
            byte[] json = null;
            if (BenchPaths.Clean(path) != null)
                Disk(() => json = BenchStore.Read(owner, path));
            string name = BenchStore.Find(owner)?.Name ?? "";
            List<byte[]> parts = json != null ? BenchZip.Split(BenchZip.Pack(json)) : new List<byte[]>();
            for (int i = 0; i < parts.Count; i++)
                SendPart(sender, owner, name, path, i, parts.Count, parts[i]);
            if (parts.Count == 0)
                SendPart(sender, owner, name, path, 0, 0, new byte[0]);
        }

        private static void SendPart(long target, long owner, string name, string path, int part, int parts, byte[] bytes)
        {
            ZPackage package = new ZPackage();
            package.Write(owner);
            package.Write(name);
            package.Write(path);
            package.Write(part);
            package.Write(parts);
            package.Write(bytes);
            ZRoutedRpc.instance.InvokeRoutedRPC(target, BenchRpc.File, package);
        }

        public static void OnRemove(long sender, long owner, string path, bool folder)
        {
            if (!Serving(sender) || !Allowed(sender, owner, path, folder))
                return;
            string shown = Shown(owner, path);
            int removed = -1;
            if (!Disk(() => removed = BenchStore.Delete(owner, path, folder)))
                BenchRpc.Tell(sender, BenchWords.Failed, shown);
            else
                BenchRpc.Tell(sender, removed >= 0 ? BenchWords.Removed : BenchWords.Gone, shown, removed.ToString(CultureInfo.InvariantCulture));
            if (removed >= 0)
                Announce();
        }

        /// <summary>An empty folder in a player's blueprints; the sender's own part is made when they have none yet.</summary>
        public static void OnMakeFolder(long sender, long owner, string path)
        {
            if (!Serving(sender) || !Allowed(sender, owner, path, false))
                return;
            long self = BenchRights.Who(sender, out string name);
            string refused = null;
            if (!Disk(() => refused = BenchStore.MakeFolder(owner, self == owner ? name : null, path)))
                BenchRpc.Tell(sender, BenchWords.Failed, path);
            else
                BenchRpc.Tell(sender, refused ?? BlueprintWords.FolderMade, path);
            if (refused == null)
                Announce();
        }

        public static void OnMove(long sender, long owner, string from, string to, bool folder)
        {
            if (!Serving(sender) || !Allowed(sender, owner, from, folder))
                return;
            string refused = null;
            bool moved = false;
            if (BenchPaths.Clean(to) == null)
                BenchRpc.Tell(sender, BlueprintWords.BadName, to);
            else if (!Disk(() => refused = BenchStore.Move(owner, from, to, folder)))
                BenchRpc.Tell(sender, BenchWords.Failed, Shown(owner, from));
            else
            {
                moved = refused == null;
                BenchRpc.Tell(sender, refused ?? BlueprintWords.Renamed, refused == BlueprintWords.Exists ? to : from, to);
            }
            if (moved)
                Announce();
        }

        /// <summary>A valid path (the top only for a whole folder) of an owner the sender may change; says why not otherwise.</summary>
        private static bool Allowed(long sender, long owner, string path, bool folder)
        {
            if (BenchPaths.Clean(path, folder) == null)
            {
                BenchRpc.Tell(sender, BlueprintWords.BadName, path);
                return false;
            }
            if (BenchRights.MayChange(sender, owner))
                return true;
            BenchRpc.Tell(sender, BenchWords.NotYours, Shown(owner, path));
            return false;
        }

        /// <summary>"Name/path" as players see it in the pool.</summary>
        private static string Shown(long owner, string path)
        {
            string name = BenchStore.Find(owner)?.Name ?? "?";
            return path.Length == 0 ? name : name + "/" + path;
        }

        /// <summary>Runs a file operation; false (logged) when the disk refused it.</summary>
        private static bool Disk(Action work)
        {
            try
            {
                work();
                return true;
            }
            catch (Exception e) when (e is IOException || e is UnauthorizedAccessException)
            {
                Plugin.Log.LogWarning("OpenKeep: blueprint bench file operation failed: " + e.Message);
                return false;
            }
        }

        private static void Announce() => ZRoutedRpc.instance.InvokeRoutedRPC(ZRoutedRpc.Everybody, BenchRpc.Changed);
    }
}
