using System.Collections.Generic;
using UnityEngine;

namespace EliteCreaturesReborn.Mutations
{
    /// <summary>
    /// Gives a pouch back: every item it holds drops at a position, exactly as it was taken - never re-made, never
    /// multiplied. Shared by every removal path the spec requires it for: a normal death (<see cref="Patches.DeathPatch"/>),
    /// `elite purge` (the one documented exception to "purge drops nothing"), and a game-driven despawn
    /// (<see cref="Patches.ThievingDespawnPatch"/>).
    /// </summary>
    public static class PouchDrop
    {
        // The same spread idiom Splitter.cs already uses for placing a splinter copy near its parent - one horizontal
        // jitter constant for "scatter near a point" in this codebase, rather than a second one invented here.
        private const float Spread = 0.5f;

        public static void DropAll(List<PouchStore.Entry> entries, Vector3 pos)
        {
            foreach (PouchStore.Entry entry in entries)
            {
                Vector3 dropPos = pos + Random.insideUnitSphere * Spread;
                dropPos.y = pos.y;
                ItemDrop.DropItem(entry.Item, entry.Item.m_stack, dropPos, Quaternion.identity);
            }
        }
    }
}
