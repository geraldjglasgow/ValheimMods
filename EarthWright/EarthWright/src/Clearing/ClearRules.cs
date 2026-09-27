using EarthWright.Protection;
using UnityEngine;

namespace EarthWright.Clearing
{
    /// <summary>
    /// The protection that applies to clearing objects, which is no terrain edit and so never meets the terrain guards.
    /// The places follow the Protection module's own switches ("Respect Wards", "Respect No-Build Zones", "Refuse In
    /// Dungeons"), tested per object as the game tests them (<c>PrivateArea.CheckAccess</c> without flashing, the no-build
    /// locations); the player rules (terrain tools switch, lock, admin zones, combat lock, and their admin exceptions)
    /// come from <see cref="ObjectRules"/>, once at the centre (the whole clearing is refused with its reason) and then
    /// per object (an object in a refused spot is left alone).
    /// </summary>
    public static class ClearRules
    {
        /// <summary>Why nothing may be cleared around this centre, or null.</summary>
        public static string Refusal(Player player, Vector3 center, string tool)
        {
            if (ProtectionSettings.RefuseInDungeons.Value && (player.InInterior() || Character.InInterior(center)))
                return ClearingWords.Interior;
            return ObjectRules.Refusal(player, center, tool);
        }

        /// <summary>The object at this position must be left alone.</summary>
        public static bool Protected(Player player, Vector3 position, string tool)
        {
            if (ProtectionSettings.RespectWards.Value && !PrivateArea.CheckAccess(position, 0f, false, false))
                return true;
            if (ProtectionSettings.RespectNoBuild.Value && Location.IsInsideNoBuildLocation(position))
                return true;
            return ObjectRules.Refusal(player, position, tool) != null;
        }
    }
}
