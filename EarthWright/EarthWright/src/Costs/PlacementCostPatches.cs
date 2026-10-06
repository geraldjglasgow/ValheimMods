using EarthWright.Core;
using HarmonyLib;

namespace EarthWright.Costs
{
    /// <summary>Marks TryPlacePiece (special handlers run inside it) and opens the charging window after a placed brush entry.</summary>
    [HarmonyPatch(typeof(Player), nameof(Player.TryPlacePiece))]
    public static class TryPlacePieceCostScope
    {
        [HarmonyPrefix]
        [HarmonyPriority(Priority.First)]
        public static void Prefix() => PlacementCharges.TryDepth++;

        [HarmonyPostfix]
        public static void Postfix(Player __instance, Piece piece, bool __result)
        {
            if (__result && PlacementCharges.InUpdate)
                Safe.Run("EarthWright costs", (player, placed) => PlacementCharges.AfterPlace(player, placed, true), __instance, piece);
        }

        [HarmonyFinalizer]
        public static void Finalizer()
        {
            if (PlacementCharges.TryDepth > 0)
                PlacementCharges.TryDepth--;
        }
    }

    /// <summary>The game's stamina check before a terrain click: EarthWright's stamina, or none for free build and special entries.</summary>
    [HarmonyPatch(typeof(Player), nameof(Player.HaveStamina))]
    public static class HaveStaminaCostPatch
    {
        [HarmonyPrefix]
        public static bool Prefix(Player __instance, ref float amount, ref bool __result)
        {
            if (!PlacementCharges.InUpdate || PlacementCharges.TryDepth > 0 || __instance != Player.m_localPlayer)
                return true;
            float? needed = Safe.Call<Player, float, float?>("EarthWright stamina check", (player, requested) => PlacementCharges.StaminaCheck(player, requested), __instance, amount, null);
            if (needed == null)
                return true;
            if (needed.Value <= 0f)
            {
                __result = true;
                return false;
            }
            amount = needed.Value;
            return true;
        }
    }

    /// <summary>The requirement check of a terrain entry: EarthWright's stations and materials (the volume is checked by the sender guard).</summary>
    [HarmonyPatch(typeof(Player), nameof(Player.HaveRequirements), new[] { typeof(Piece), typeof(Player.RequirementMode) })]
    public static class HaveRequirementsCostPatch
    {
        [HarmonyPrefix]
        public static bool Prefix(Player __instance, Piece piece, Player.RequirementMode mode, ref bool __result)
        {
            if (mode != Player.RequirementMode.CanBuild || piece == null || __instance != Player.m_localPlayer)
                return true;
            bool? verdict = Safe.Call<Player, Piece, bool?>("EarthWright requirements", (player, checkedPiece) => PlacementCharges.CanStart(player, checkedPiece), __instance, piece, null);
            if (verdict == null)
                return true;
            __result = verdict.Value;
            return false;
        }
    }

    /// <summary>The materials the game takes for a placed terrain click: EarthWright's bill instead of the piece's own list.</summary>
    [HarmonyPatch(typeof(Player), nameof(Player.ConsumeResources))]
    public static class ConsumeResourcesCostPatch
    {
        [HarmonyPrefix]
        [HarmonyPriority(Priority.High)]
        public static void Prefix(Player __instance, ref Piece.Requirement[] requirements)
        {
            CostContext ctx = PlacementCharges.Charging;
            if (ctx == null || ctx.Player != __instance || ctx.Piece == null || requirements != ctx.Piece.m_resources)
                return;
            Piece.Requirement[] replaced = Safe.Call("EarthWright materials", charged => PlacementCharges.Materials(charged), ctx, null);
            if (replaced != null)
                requirements = replaced;
        }
    }

    /// <summary>The stamina the game takes for a placed terrain click.</summary>
    [HarmonyPatch(typeof(Player), nameof(Player.GetBuildStamina))]
    public static class BuildStaminaCostPatch
    {
        [HarmonyPostfix]
        public static void Postfix(Player __instance, ref float __result)
        {
            CostContext ctx = PlacementCharges.Charging;
            if (ctx == null || ctx.Player != __instance)
                return;
            __result = Safe.Call("EarthWright stamina", (charged, vanilla) => StaminaCost.For(charged, vanilla), ctx, __result, __result);
        }
    }

    /// <summary>The tool wear the game applies for a placed terrain click.</summary>
    [HarmonyPatch(typeof(Player), nameof(Player.GetPlaceDurability))]
    public static class PlaceDurabilityCostPatch
    {
        [HarmonyPostfix]
        public static void Postfix(Player __instance, ref float __result)
        {
            CostContext ctx = PlacementCharges.Charging;
            if (ctx == null || ctx.Player != __instance)
                return;
            __result = Safe.Call("EarthWright tool wear", (charged, vanilla) => WearCost.For(charged, vanilla), ctx, __result, __result);
        }
    }
}
