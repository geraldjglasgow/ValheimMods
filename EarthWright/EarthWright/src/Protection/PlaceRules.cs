using System.Collections.Generic;
using EarthWright.Terrain;

namespace EarthWright.Protection
{
    /// <summary>
    /// Places where the game itself forbids building. "Respect No-Build Zones": the footprint touches a location that is
    /// marked no-build (the game's own build check radius, sampled as the footprint's circles rather than one point).
    /// "Refuse In Dungeons": the stroke's centre is in an interior (the game puts interiors 5000 m up), or - on the
    /// sender - the player stands in one, so the ground above a dungeon cannot be changed from below. Owner: the
    /// same, from the edit alone and the locations loaded there.
    /// </summary>
    public static class PlaceRules
    {
        public static string Sender(GuardContext ctx)
        {
            if (ProtectionSettings.RefuseInDungeons.Value && (CenterInInterior(ctx.Edit) || LocalPlayerInInterior()))
                return ProtectionWords.Dungeon;
            return NoBuild(ctx.Edit);
        }

        public static string Owner(GuardContext ctx)
        {
            if (ProtectionSettings.RefuseInDungeons.Value && CenterInInterior(ctx.Edit))
                return ProtectionWords.Dungeon;
            return NoBuild(ctx.Edit);
        }

        private static string NoBuild(TerrainEdit edit)
        {
            if (!ProtectionSettings.RespectNoBuild.Value)
                return null;
            return TouchesNoBuild(Footprint.Of(edit)) ? ProtectionWords.NoBuild : null;
        }

        /// <summary>Any circle reaches into a no-build location (<c>Location.IsInsideNoBuildLocation</c> with a radius).</summary>
        public static bool TouchesNoBuild(List<Disc> discs)
        {
            if (discs.Count == 0)
                return false;
            foreach (Location location in Location.s_allLocations)
            {
                if (location == null || !location.m_noBuild)
                    continue;
                foreach (Disc disc in discs)
                {
                    if (location.IsInside(disc.Center, disc.Radius, buildCheck: true))
                        return true;
                }
            }
            return false;
        }

        private static bool CenterInInterior(TerrainEdit edit)
        {
            return edit != null && edit.Kind == EditKind.Stroke && edit.Stroke != null && Character.InInterior(edit.Stroke.Center);
        }

        private static bool LocalPlayerInInterior()
        {
            Player player = Player.m_localPlayer;
            return player != null && player.InInterior();
        }
    }
}
