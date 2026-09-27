using System.Collections.Generic;
using EarthWright.Actions;
using EarthWright.Core;
using EarthWright.Gear;
using EarthWright.Terrain;

namespace EarthWright.Protection
{
    /// <summary>
    /// "Lock Terrain Editing": no terrain changes at all - EarthWright's edits, the local player's own game terrain ops
    /// (the hoe with EarthWright off, pickaxe digging) and object removal by the terrain tools (<see cref="ObjectRules"/>)
    /// - except for admins ("Admins Bypass Lock") and exempt tools.
    /// <list type="bullet">
    /// <item>Sender: this player's admin status; exempt when the held item or the entry's tool is on the list.</item>
    /// <item>Owner: an admin passes only when the server approved the edit; exemption is judged from the entry's tool
    /// (its item "Hoe" or "Cultivator"). A source that names no entry ("reset",
    /// "undo", "command:terrain") is trusted to the sender's check whenever any tool is exempt.</item>
    /// </list>
    /// </summary>
    public static class TerrainLock
    {
        public static bool Locked => ProtectionSettings.LockTerrain.Value;

        private static bool AdminsBypass => ProtectionSettings.AdminsBypassLock.Value;

        private static HashSet<string> Exempt => ProtectionSettings.ExemptToolNames.Names;

        public static string Sender(GuardContext ctx)
        {
            if (!Locked || (AdminsBypass && Side.LocalIsAdmin))
                return null;
            if (IsExempt(LocalTool.RightItemName) || FamilyExempt(FamilyOf(ctx.Edit)))
                return null;
            return ProtectionWords.Locked;
        }

        public static string Owner(GuardContext ctx)
        {
            if (!Locked || (AdminsBypass && ctx.ServerApproved))
                return null;
            ToolFamily? family = FamilyOf(ctx.Edit);
            bool exempt = family == null ? Exempt.Count > 0 : FamilyExempt(family);
            return exempt ? null : ProtectionWords.Locked;
        }

        /// <summary>The local player's work with this item (prefab name, may be null) is refused by the lock.</summary>
        public static bool BlocksLocal(string itemName)
        {
            if (!Locked || (AdminsBypass && Side.LocalIsAdmin))
                return false;
            return !IsExempt(itemName);
        }

        private static bool IsExempt(string name) => !string.IsNullOrEmpty(name) && Exempt.Contains(name);

        private static bool FamilyExempt(ToolFamily? family)
        {
            return family != null && (IsExempt(family.Value.ToString()) || IsExempt(ItemOf(family.Value)));
        }

        /// <summary>The item prefab name of an EarthWright tool family.</summary>
        private static string ItemOf(ToolFamily family)
        {
            switch (family)
            {
                case ToolFamily.Hoe: return ToolNames.Hoe;
                case ToolFamily.Cultivator: return ToolNames.Cultivator;
                default: return null;
            }
        }

        /// <summary>The tool family of the edit's entry; null when the source names no entry or a modded one.</summary>
        private static ToolFamily? FamilyOf(TerrainEdit edit)
        {
            ToolAction action = edit != null ? ActionCatalog.ById(edit.Source) : null;
            if (action == null || action.Family == ToolFamily.Modded)
                return null;
            return action.Family;
        }
    }
}
