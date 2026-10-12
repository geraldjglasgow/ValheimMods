using HarmonyLib;
using UnityEngine;

namespace GrindstoneSkills
{
    /// <summary>
    /// Sturdy Build: a piece the local player places with a tool whose pieces train Crafting (the hammer) gets up to 25%
    /// more max health at Crafting 100, linear from 0, from the builder's level stored on the piece.
    /// <list type="bullet">
    /// <item>Player.PlacePiece instantiates the piece on the builder's client, which owns its new ZDO, and then calls
    /// Piece.SetCreator with the builder's ID. A postfix there stores the builder's level in the ZDO
    /// (<see cref="Keys.BuilderLevel"/>) and raises the new piece's max health; its WearNTear.Awake ran before the level
    /// was stored. The ZDO goes out to the other peers with the level already in it.</item>
    /// <item>Every machine that loads the piece later (the other clients, the dedicated server, the builder after a
    /// restart) raises it in a WearNTear.Awake postfix from the stored level, so all agree on the max, and the owner's
    /// damage and repairs (which fill to the max) use it. The world level bonus Awake adds is a factor too, so the order
    /// does not matter; the health share and the worn look Awake worked out are worked out again.</item>
    /// </list>
    /// Ships are left to Sailing's <see cref="ShipwrightHealth"/>. The current health in the ZDO is an absolute number and
    /// stays untouched: a new piece has none stored and reads as full. Pieces placed without the level (before this
    /// module, while it was off, by a player with no Crafting, or with another tool) keep the game's health; a stored
    /// level applies whatever the setting says now.
    /// </summary>
    public static class SturdyBuild
    {
        /// <summary>Extra max health at Crafting 100.</summary>
        private const float HealthAt100 = 0.25f;

        private static readonly int LevelHash = Keys.BuilderLevel.GetStableHashCode();

        /// <summary>The extra max health a builder of this level gives, 0 to 0.25; 0 for a level that is not a number.</summary>
        public static float Share(float level) => level > 0f ? HealthAt100 * Mathf.Clamp01(level / 100f) : 0f;

        [HarmonyPatch(typeof(Piece), nameof(Piece.SetCreator))]
        private static class Placed
        {
            [HarmonyPostfix]
            private static void Postfix(Piece __instance, long uid)
            {
                if (CraftingPerks.Enabled)
                    HookGuard.Run("Sturdy Build placed", static placed => Stamp(placed.piece, placed.uid), (piece: __instance, uid));
            }
        }

        [HarmonyPatch(typeof(WearNTear), nameof(WearNTear.Awake))]
        private static class Loaded
        {
            [HarmonyPostfix]
            private static void Postfix(WearNTear __instance) =>
                HookGuard.Run("Sturdy Build loaded", static wear => Load(wear), __instance);
        }

        private static void Stamp(Piece piece, long uid)
        {
            Player player = Player.m_localPlayer;
            if (player == null || player.GetPlayerID() != uid || !BuildsWithCrafting(player))
                return;
            ZNetView nview = piece.m_nview;
            WearNTear wear = piece.GetComponent<WearNTear>();
            if (wear == null || nview == null || !nview.IsValid() || !nview.IsOwner() || piece.GetComponent<Ship>() != null)
                return;
            float level = LocalSkill.Level(Skills.SkillType.Crafting);
            ZDO zdo = nview.GetZDO();
            if (Share(level) <= 0f || zdo.GetFloat(LevelHash, -1f) >= 0f)
                return;
            zdo.Set(LevelHash, level);
            Raise(wear, zdo, level);
        }

        /// <summary>Whether the player's build tool places pieces that train Crafting (the hammer's piece table).</summary>
        private static bool BuildsWithCrafting(Player player)
        {
            PieceTable table = player.GetBuildTool();
            return table != null && table.m_skill == Skills.SkillType.Crafting;
        }

        private static void Load(WearNTear wear)
        {
            ZDO zdo = wear.m_nview != null ? wear.m_nview.GetZDO() : null;
            if (zdo == null)
                return;
            float level = zdo.GetFloat(LevelHash);
            if (Share(level) <= 0f)
                return;
            Raise(wear, zdo, level);
            wear.UpdateVisual(false);
        }

        private static void Raise(WearNTear wear, ZDO zdo, float level)
        {
            if (wear.m_health <= 0f)
                return;
            wear.m_health *= 1f + Share(level);
            wear.m_healthPercentage = Mathf.Clamp01(zdo.GetFloat(ZDOVars.s_health, wear.m_health) / wear.m_health);
        }
    }
}
