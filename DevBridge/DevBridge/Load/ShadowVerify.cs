using System.Collections.Generic;

namespace DevBridge.Load
{
    /// <summary>
    /// /load?verify=1: whether every verifying fake holds what the server thinks it holds. For each object the server
    /// has the fake booked at (its ZDOPeer.m_zdos: the revisions of the last record sent to it), the fake's own copy
    /// (<see cref="FakeShadow"/>, built only from the packages it received, in either format) must stand at exactly
    /// those revisions, with the owner, position, flags, prefab, rotation and connection of the server's record of them
    /// and every list value in it, byte for byte (<see cref="ServerRecords"/>: the object itself while it is unchanged,
    /// else HighSeat's history). Keys the copy has beyond the record are counted apart: the game's own updates never
    /// remove a key either, so they are only a sign that the server removed one. An object asked for whole and not yet
    /// answered (two seconds) counts as awaiting; one whose record of those revisions is gone from both counts as
    /// unverifiable. The fake's own body is left out (it sends it, it is never sent it). Passed means no missing object,
    /// no revision or value mismatch and no unreadable package.
    /// </summary>
    internal static class ShadowVerify
    {
        private const int MaxSamples = 12;

        internal static Dictionary<string, object> Report()
        {
            FakeCrowd.RequireServer();
            var records = new ServerRecords();
            var total = new VerifyTally();
            var rows = new List<object>();
            foreach (FakePlayer fake in FakeCrowd.All)
            {
                if (fake.Socket.Shadow == null || fake.Holdings == null) continue;
                var tally = new VerifyTally();
                foreach (KeyValuePair<ZDOID, ZDOMan.ZDOPeer.PeerZDOInfo> booked in fake.Holdings.m_zdos)
                {
                    if (booked.Key != fake.BodyId) One(fake, records, booked.Key, booked.Value, tally, total);
                }
                total.Add(tally, fake.Socket.Shadow);
                rows.Add(tally.Row(fake));
            }
            return total.Reply(records, rows);
        }

        private static void One(FakePlayer fake, ServerRecords records, ZDOID id, ZDOMan.ZDOPeer.PeerZDOInfo info, VerifyTally tally,
            VerifyTally total)
        {
            tally.Checked++;
            FakeShadow shadow = fake.Socket.Shadow;
            bool held = shadow.Objects.TryGetValue(id, out ShadowObject copy);
            if (!held || copy.Data != info.m_dataRevision || copy.Owner != info.m_ownerRevision)
            {
                if (shadow.Awaiting(id)) tally.Awaiting++;
                else if (!held) Problem(tally, total, fake, id, "missing", "booked, never received");
                else Problem(tally, total, fake, id, "revision",
                    $"booked at {info.m_dataRevision}/{info.m_ownerRevision}, holds {copy.Data}/{copy.Owner}");
                return;
            }
            ShadowObject server = records.At(id, info.m_dataRevision, info.m_ownerRevision, out bool current);
            if (server == null)
            {
                tally.Unverifiable++;
                return;
            }
            if (current) tally.AgainstObject++;
            else tally.AgainstHistory++;
            string difference = Compare(copy, server, out bool extra);
            if (extra) tally.ExtraKeys++;
            if (difference == null) tally.Ok++;
            else Problem(tally, total, fake, id, "fields", difference);
        }

        private static void Problem(VerifyTally tally, VerifyTally total, FakePlayer fake, ZDOID id, string kind, string detail)
        {
            tally.Count(kind);
            if (total.Samples.Count >= MaxSamples) return;
            ZDO zdo = ZDOMan.instance.GetZDO(id);
            UnityEngine.GameObject found = zdo != null && ZNetScene.instance ? ZNetScene.instance.GetPrefab(zdo.GetPrefab()) : null;
            string prefab = found ? found.name : null;
            total.Samples.Add(new Dictionary<string, object>
            {
                ["fake"] = fake.Name, ["id"] = id.ToString(), ["prefab"] = prefab, ["problem"] = kind, ["detail"] = detail,
            });
        }

        // The first part of the server's record the copy does not hold, or null; extra: the copy has keys beyond it.
        private static string Compare(ShadowObject copy, ShadowObject server, out bool extra)
        {
            extra = false;
            if (copy.OwnerId != server.OwnerId) return $"owner {copy.OwnerId}, server {server.OwnerId}";
            if (!Same(copy.Position, server.Position)) return "position";
            if (copy.Flags != server.Flags) return $"flags {copy.Flags:X}, server {server.Flags:X}";
            if (copy.Prefab != server.Prefab) return "prefab";
            if (server.Rotation != null && !Same(copy.Rotation, server.Rotation)) return "rotation";
            if (server.Connection != null && !Same(copy.Connection, server.Connection)) return "connection";
            foreach (KeyValuePair<long, byte[]> field in server.Fields)
            {
                if (!copy.Fields.TryGetValue(field.Key, out byte[] value) || !Same(value, field.Value))
                    return $"{VerifyTally.TypeName((int)(field.Key >> 32))} {(int)field.Key}: {(value == null ? "missing" : "differs")}";
            }
            extra = copy.Fields.Count > server.Fields.Count;
            return null;
        }

        private static bool Same(byte[] a, byte[] b)
        {
            if (a == null || b == null || a.Length != b.Length) return a == b;
            for (int i = 0; i < a.Length; i++)
            {
                if (a[i] != b[i]) return false;
            }
            return true;
        }
    }
}
