using System.Collections.Generic;
using System.Linq;
using EarthWright.Core;

namespace EarthWright.Menu
{
    /// <summary>
    /// Which entries this player's menus list. The game's terrain entries follow their toggles while EarthWright is
    /// switched on on the server (a player's own "ew off" does not bring back an entry the server removed) and all come
    /// back when the server switches EarthWright off. EarthWright's entries and the custom ones need EarthWright active
    /// for this player, their toggle, admin rights for admin-only entries, and for Clear objects the clearing switch.
    /// </summary>
    public static class EntryVisibility
    {
        public static bool Visible(string id)
        {
            if (GameEntries.Is(id))
                return !GeneralSettings.Enabled.Value || MenuSettings.Toggle(id);
            if (!GeneralSettings.Active)
                return false;
            EntryDef def = EntryDefs.Get(id);
            if (def != null)
                return OwnVisible(def);
            CustomEntry custom = CustomEntries.ByPrefab(id);
            return custom != null && (!custom.Admin || Side.LocalIsAdmin);
        }

        /// <summary>Every entry the Menu module manages: the game's terrain entries, EarthWright's, the custom ones.</summary>
        public static IEnumerable<string> ManagedIds()
        {
            return GameEntries.All.Select(e => e.Id)
                .Concat(EntryDefs.All.Select(d => d.Id))
                .Concat(CustomEntries.All.Select(e => e.PrefabName));
        }

        /// <summary>The visible entries as one text, to tell whether the menus need rebuilding.</summary>
        public static string State() => string.Join(",", ManagedIds().Where(Visible));

        /// <summary>
        /// Sets <c>Piece.m_enabled</c> on every managed prefab. The game leaves a disabled piece out of every build menu
        /// and never adds it to the player's known pieces.
        /// </summary>
        public static void ApplyFlags()
        {
            foreach (string id in ManagedIds())
            {
                Piece piece = EntryRegistry.PieceOf(id);
                if (piece != null)
                    piece.m_enabled = Visible(id);
            }
        }

        private static bool OwnVisible(EntryDef def)
        {
            if (!MenuSettings.Toggle(def.Id))
                return false;
            if (def.Action.AdminOnly && !Side.LocalIsAdmin)
                return false;
            return def.Id != "ew_clear" || Safe.Call("EarthWright clearing switch", () => MenuHooks.ClearingEnabled(), false);
        }
    }
}
