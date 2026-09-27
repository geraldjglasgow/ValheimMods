using EarthWright.Actions;
using EarthWright.Core;
using EarthWright.Terrain;

namespace EarthWright.Protection
{
    /// <summary>
    /// The server switch "Terrain Tools Allowed" and admin-only entries. The sender decides with this player's own admin
    /// status. The owner of a compiler is usually another player's machine, which cannot see the server's admin list, so
    /// it lets an admin's edit through only when the server approved it: <see cref="AdminRouting"/> sends admins' edits
    /// through the server while such a rule is active. (The owner handler already drops privileged flags that did not
    /// come through the server, so a forged IgnoreLimits never reaches these guards.)
    /// </summary>
    public static class AccessGuards
    {
        public static string ToolsSender(GuardContext ctx) => ToolsReason(Side.LocalIsAdmin);

        public static string ToolsOwner(GuardContext ctx) => ToolsReason(ctx.ServerApproved);

        /// <summary>Why terrain tools are refused to a player who is (or is not) an admin, or null.</summary>
        public static string ToolsReason(bool admin)
        {
            switch (ProtectionSettings.ToolsAllowed.Value)
            {
                case ToolAccess.Nobody:
                    return ProtectionWords.ToolsOff;
                case ToolAccess.AdminsOnly:
                    return admin ? null : ProtectionWords.AdminsOnly;
                default:
                    return null;
            }
        }

        public static string EntrySender(GuardContext ctx)
        {
            return IsAdminEntry(ctx.Edit) && !Side.LocalIsAdmin ? ProtectionWords.AdminEntry : null;
        }

        public static string EntryOwner(GuardContext ctx)
        {
            return IsAdminEntry(ctx.Edit) && !ctx.ServerApproved ? ProtectionWords.AdminEntry : null;
        }

        private static bool IsAdminEntry(TerrainEdit edit)
        {
            return edit != null && ActionCatalog.ById(edit.Source)?.AdminOnly == true;
        }
    }
}
