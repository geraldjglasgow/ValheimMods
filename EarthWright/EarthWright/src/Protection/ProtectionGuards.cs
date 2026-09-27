using System;
using EarthWright.Core;
using EarthWright.Terrain;

namespace EarthWright.Protection
{
    /// <summary>
    /// Registers the protection checks with the edit pipeline, in the order their reasons are worth showing: who may edit
    /// at all, then the lock, combat, places the game forbids, wards and admin zones.
    /// <list type="bullet">
    /// <item>Sender guards run on the player's machine before anything is sent (and from the preview a few times a
    /// second), so each is cheap.</item>
    /// <item>Owner guards run on the machine that owns each terrain compiler, just before the edit is applied; a refusal
    /// goes back to the sender. Combat is sender-only: only the player's machine knows who hunts the player.</item>
    /// </list>
    /// </summary>
    public static class ProtectionGuards
    {
        private static readonly (string Name, Func<GuardContext, string> Check)[] SenderChecks =
        {
            ("tools allowed", AccessGuards.ToolsSender),
            ("admin entry", AccessGuards.EntrySender),
            ("terrain lock", TerrainLock.Sender),
            ("combat lock", CombatLock.Sender),
            ("no-build and dungeons", PlaceRules.Sender),
            ("wards", WardGuard.Sender),
            ("admin zones", ZoneGuard.Sender),
        };

        private static readonly (string Name, Func<GuardContext, string> Check)[] OwnerChecks =
        {
            ("tools allowed", AccessGuards.ToolsOwner),
            ("admin entry", AccessGuards.EntryOwner),
            ("terrain lock", TerrainLock.Owner),
            ("no-build and dungeons", PlaceRules.Owner),
            ("wards", WardGuard.Owner),
            ("admin zones", ZoneGuard.Owner),
        };

        public static void Register()
        {
            foreach (var guard in SenderChecks)
                EditGuards.AddSender("Protection: " + guard.Name, guard.Check);
            foreach (var guard in OwnerChecks)
                EditGuards.AddOwner("Protection: " + guard.Name, guard.Check);
        }

        /// <summary>The first protection refusal for an edit this player is about to make, or null (for the preview).</summary>
        public static string SenderReason(TerrainEdit edit)
        {
            GuardContext context = new GuardContext { Edit = edit };
            foreach (var guard in SenderChecks)
            {
                string reason = Safe.Call("Protection: " + guard.Name, () => guard.Check(context), null);
                if (!string.IsNullOrEmpty(reason))
                    return reason;
            }
            return null;
        }
    }
}
