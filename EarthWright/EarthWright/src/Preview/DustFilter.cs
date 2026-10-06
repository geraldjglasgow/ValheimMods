using System.Collections.Generic;
using System.Linq;
using EarthWright.Actions;
using EarthWright.Core;
using HarmonyLib;
using UnityEngine;

namespace EarthWright.Preview
{
    /// <summary>
    /// The "Remove Dust" setting. A terrain click spawns up to three effect lists (read from the game's prefabs): the
    /// entry's piece place effect (dust, pebbles and the hoe sound, played by <c>Player.PlacePiece</c>), the entry's
    /// TerrainOp placed effect (raise: dust and a rock sound, played by EarthWright's placement hook) and the tool's
    /// own build effect (empty on the vanilla tools, played by <c>Player.UpdatePlacement</c>). While the setting is on,
    /// each list is swapped for a copy without its particle effects for the duration of that call, so the sounds stay
    /// and nothing else changes. Only this player's terrain clicks with a terrain tool are affected.
    /// </summary>
    internal static class DustFilter
    {
        private static readonly Dictionary<EffectList, EffectList> filtered = new Dictionary<EffectList, EffectList>();

        /// <summary>True while this player's terrain piece is being placed with dust removal on.</summary>
        public static bool Placing;

        /// <summary>Dust removal applies to this player placing this piece.</summary>
        public static bool Applies(Player player, Piece piece)
        {
            if (player == null || player != Player.m_localPlayer || !HudSettings.RemoveDust.Value || !GeneralSettings.Active)
                return false;
            if (!LocalTool.IsToolName(LocalTool.RightItemName))
                return false;
            ToolAction action = ActionCatalog.For(piece);
            return action != null && !action.IsSpecial;
        }

        /// <summary>A copy of the list without the effects that show particles (kept per source list).</summary>
        public static EffectList SoundsOnly(EffectList list)
        {
            if (list == null || list.m_effectPrefabs == null)
                return list;
            if (!filtered.TryGetValue(list, out EffectList copy))
            {
                copy = Filter(list);
                filtered[list] = copy;
            }
            return copy;
        }

        /// <summary>A copy without particle effects, not remembered (for per-instance lists).</summary>
        public static EffectList Filter(EffectList list)
        {
            if (list == null || list.m_effectPrefabs == null)
                return list;
            return new EffectList { m_effectPrefabs = list.m_effectPrefabs.Where(e => e == null || !IsDust(e.m_prefab)).ToArray() };
        }

        /// <summary>An effect prefab that shows particles and makes no sound (the game plays every effect sound through ZSFX).</summary>
        private static bool IsDust(GameObject prefab)
        {
            if (prefab == null)
                return false;
            bool particles = prefab.GetComponentInChildren<ParticleSystem>(true) != null;
            bool sound = prefab.GetComponentInChildren<ZSFX>(true) != null;
            return particles && !sound;
        }
    }

    /// <summary>
    /// The entry's place effect while this player places a terrain piece; also flags the placement for the TerrainOp
    /// patch. The swap is kept in static fields (placing is not re-entrant and runs on the main thread) and undone by
    /// the finalizer even if the placement throws.
    /// </summary>
    [HarmonyPatch(typeof(Player), nameof(Player.PlacePiece))]
    public static class DustPlacePiecePatch
    {
        private static Piece swapped;
        private static EffectList original;

        [HarmonyPrefix]
        public static void Prefix(Player __instance, Piece piece)
        {
            if (piece == null || !HudSettings.RemoveDust.Value
                || !Safe.Call("EarthWright dust", (player, placed) => DustFilter.Applies(player, placed), __instance, piece, false))
                return;
            swapped = piece;
            original = piece.m_placeEffect;
            piece.m_placeEffect = DustFilter.SoundsOnly(original);
            DustFilter.Placing = true;
        }

        [HarmonyFinalizer]
        public static void Finalizer()
        {
            DustFilter.Placing = false;
            if (swapped != null)
                swapped.m_placeEffect = original;
            swapped = null;
            original = null;
        }
    }

    /// <summary>The TerrainOp's placed effect of that same placement (runs before EarthWright's placement hook plays it).</summary>
    [HarmonyPatch(typeof(TerrainOp), nameof(TerrainOp.Awake))]
    public static class DustTerrainOpPatch
    {
        [HarmonyPrefix]
        [HarmonyPriority(Priority.First)]
        public static void Prefix(TerrainOp __instance)
        {
            if (!DustFilter.Placing || __instance == null)
                return;
            EffectList placed = __instance.m_onPlacedEffect;
            __instance.m_onPlacedEffect = Safe.Call("EarthWright dust", list => DustFilter.Filter(list), placed, placed);
        }
    }

    /// <summary>
    /// The tool's own build effect, played by the game after a successful terrain placement: swapped in the local
    /// player's <c>Player.UpdatePlacement</c> prefix and put back by its finalizer (the shared patch in
    /// <c>Patches/UpdatePlacementPatch</c>).
    /// </summary>
    internal static class DustBuildEffect
    {
        private static ItemDrop.ItemData.SharedData swapped;
        private static EffectList original;

        public static void Begin(Player player)
        {
            if (!HudSettings.RemoveDust.Value || !player.InPlaceMode())
                return;
            ItemDrop.ItemData tool = player.GetRightItem();
            Piece piece = tool?.m_shared != null ? player.GetSelectedPiece() : null;
            if (piece == null || !Safe.Call("EarthWright dust", (user, selected) => DustFilter.Applies(user, selected), player, piece, false))
                return;
            swapped = tool.m_shared;
            original = swapped.m_buildEffect;
            swapped.m_buildEffect = DustFilter.SoundsOnly(original);
        }

        public static void End()
        {
            if (swapped != null)
                swapped.m_buildEffect = original;
            swapped = null;
            original = null;
        }
    }
}
