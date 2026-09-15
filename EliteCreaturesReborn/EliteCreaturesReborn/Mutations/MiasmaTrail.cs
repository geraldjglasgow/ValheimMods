using System.Collections.Generic;
using EliteCreaturesReborn.Traits;
using EliteCreaturesReborn.Util;
using UnityEngine;

namespace EliteCreaturesReborn.Mutations
{
    /// <summary>
    /// The wire form of a Miasmic creature's recent cloud drops. The owner keeps the authoritative list and writes it
    /// into the creature's ZDO as one small packed blob; the game replicates it, and every client reads it back to draw
    /// the same trail. Each drop carries a monotonic id (so a client spawns each cloud exactly once) and a shared-clock
    /// timestamp (so every machine retires it at the same instant). This is only the codec; the field component owns
    /// the list.
    /// </summary>
    public static class MiasmaTrail
    {
        public struct Drop
        {
            public long Id;
            public Vector3 Pos;
            public long TimeMs;
        }

        /// <summary>Reads the trail from a creature's ZDO. Works on any machine - the ZDO is shared. Empty when unset.</summary>
        public static List<Drop> Read(ZDO zdo)
        {
            List<Drop> drops = new List<Drop>();
            byte[]? bytes = zdo != null ? zdo.GetByteArray(TraitKeys.MiasmaTrail) : null;
            if (bytes == null || bytes.Length == 0)
            {
                return drops;
            }
            Decode(new ZPackage(bytes), drops);
            return drops;
        }

        private static void Decode(ZPackage pkg, List<Drop> drops)
        {
            int count = pkg.ReadInt();
            for (int i = 0; i < count; i++)
            {
                Drop drop = default;
                drop.Id = pkg.ReadLong();
                drop.Pos = new Vector3(pkg.ReadSingle(), pkg.ReadSingle(), pkg.ReadSingle());
                drop.TimeMs = pkg.ReadLong();
                drops.Add(drop);
            }
        }

        /// <summary>Owner-only write of the whole trail as one blob; a non-owner write would be dropped by the game.</summary>
        public static void Write(ZDO zdo, List<Drop> drops)
        {
            ZPackage pkg = new ZPackage();
            pkg.Write(drops.Count);
            foreach (Drop drop in drops)
            {
                pkg.Write(drop.Id);
                pkg.Write(drop.Pos.x); pkg.Write(drop.Pos.y); pkg.Write(drop.Pos.z);
                pkg.Write(drop.TimeMs);
            }
            zdo.Set(TraitKeys.MiasmaTrail, pkg.GetArray());
        }

        /// <summary>Drops still within their lifetime survive a trim; expired ones fall off so the blob stays small.</summary>
        public static void TrimExpired(List<Drop> drops, float life)
        {
            drops.RemoveAll(drop => NetTime.SecondsSince(drop.TimeMs) >= life);
        }

        /// <summary>
        /// Next drop id, from a monotonic ZDO counter - never a max-of-list, which would reuse an id after the trail
        /// trims its highest drop and make a client skip (leaving a cloud that still hurts) the reissued cloud.
        /// </summary>
        public static long NextId(ZDO zdo)
        {
            long next = zdo.GetLong(TraitKeys.MiasmaSeq, 1L);
            zdo.Set(TraitKeys.MiasmaSeq, next + 1L);
            return next;
        }
    }
}
