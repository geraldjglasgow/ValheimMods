using System;
using HarmonyLib;
using UnityEngine;
using UnityEngine.Rendering;

namespace HighSeatPlus.Meshes
{
    /// <summary>
    /// Starts combined building meshes with the world: <see cref="CombinedMeshes"/> is added to ZNetScene's object,
    /// so it lives and dies with the loaded world. Not on a machine without graphics (a dedicated server draws
    /// nothing). ZNetScene.Awake must never throw (the world would never finish loading), so this catches everything.
    /// </summary>
    [HarmonyPatch(typeof(ZNetScene), nameof(ZNetScene.Awake))]
    internal static class CombinedMeshesStart
    {
        private static void Postfix(ZNetScene __instance)
        {
            try
            {
                if (SystemInfo.graphicsDeviceType != GraphicsDeviceType.Null)
                {
                    CombinedMeshes.Attach(__instance.gameObject);
                }
            }
            catch (Exception error)
            {
                HighSeatPlus.Log.LogWarning("Combined building meshes could not start: " + error.Message);
            }
        }
    }

    /// <summary>A piece the game lists (one with a ZDO) is known from its Awake on, and waits to settle.</summary>
    [HarmonyPatch(typeof(WearNTear), nameof(WearNTear.Awake))]
    internal static class PieceAwake
    {
        private static void Postfix(WearNTear __instance)
        {
            if (!CombinedMeshes.Active)
            {
                return;
            }
            try
            {
                PieceRegistry.Add(__instance);
            }
            catch (Exception error)
            {
                CombinedMeshes.Fail(error);
            }
        }
    }

    /// <summary>A removed, destroyed or unloaded piece leaves its group, which opens so its look is gone at once.</summary>
    [HarmonyPatch(typeof(WearNTear), nameof(WearNTear.OnDestroy))]
    internal static class PieceDestroyed
    {
        private static void Postfix(WearNTear __instance)
        {
            if (!CombinedMeshes.Active)
            {
                return;
            }
            try
            {
                PieceRegistry.Remove(__instance);
            }
            catch (Exception error)
            {
                CombinedMeshes.Fail(error);
            }
        }
    }

    /// <summary>
    /// The hammer's hover highlight (and its support colours) tints the piece through MaterialMan every frame the piece
    /// is looked at, and resets 0.2 s after the last: the piece's group opens so its own renderers show the tint, and
    /// shows its build again once nothing in it has been touched for the settle time. Called every frame while hovering:
    /// one dictionary lookup, and the group is already open after the first.
    /// </summary>
    [HarmonyPatch(typeof(WearNTear), nameof(WearNTear.Highlight))]
    internal static class PieceHighlight
    {
        private static void Postfix(WearNTear __instance)
        {
            if (!CombinedMeshes.Active)
            {
                return;
            }
            try
            {
                if (PieceRegistry.TryGet(__instance, out PieceEntry entry))
                {
                    PieceRegistry.Touch(entry);
                }
            }
            catch (Exception error)
            {
                CombinedMeshes.Fail(error);
            }
        }
    }

    /// <summary>
    /// Damage and repair reach every peer as RPC_HealthChanged, which switches the new, worn and broken looks. A combined
    /// piece whose look switched opens its group; one whose look stayed the same (most hits, and the rain wear every
    /// minute) keeps it shown. A piece not yet combined starts settling again.
    /// </summary>
    [HarmonyPatch(typeof(WearNTear), nameof(WearNTear.RPC_HealthChanged))]
    internal static class PieceHealthChanged
    {
        private static void Postfix(WearNTear __instance)
        {
            if (!CombinedMeshes.Active)
            {
                return;
            }
            try
            {
                if (PieceRegistry.TryGet(__instance, out PieceEntry entry) && (!entry.InBuild || PieceRules.StateBits(__instance) != entry.StateBits))
                {
                    PieceRegistry.Touch(entry);
                }
            }
            catch (Exception error)
            {
                CombinedMeshes.Fail(error);
            }
        }
    }

    /// <summary>
    /// The burn and lava glow (_TakingAshlandsDamage through MaterialMan, set once a second while a piece burns or
    /// stands in lava): a glowing piece opens its group. A value of 0 (the common case, every piece every second) costs
    /// one comparison.
    /// </summary>
    [HarmonyPatch(typeof(WearNTear), nameof(WearNTear.SetAshlandsMaterialValue))]
    internal static class PieceGlow
    {
        private static void Postfix(WearNTear __instance, float v)
        {
            if (v == 0f || !CombinedMeshes.Active)
            {
                return;
            }
            try
            {
                if (PieceRegistry.TryGet(__instance, out PieceEntry entry))
                {
                    PieceRegistry.Touch(entry);
                }
            }
            catch (Exception error)
            {
                CombinedMeshes.Fail(error);
            }
        }
    }

    /// <summary>
    /// A destroyed piece breaks into fragments copied from its renderers, but only from renderers Unity drew last frame
    /// (Renderer.isVisible), and a turned-off renderer is never drawn: when the piece's build was drawn, every shown
    /// renderer of the piece is fragmented instead, which is what the game would have done for a piece in view.
    /// </summary>
    [HarmonyPatch(typeof(Destructible), nameof(Destructible.CreateFragments))]
    internal static class PieceFragments
    {
        private static void Prefix(GameObject rootObject, ref bool visibleOnly)
        {
            if (!visibleOnly || !CombinedMeshes.Active || rootObject == null)
            {
                return;
            }
            try
            {
                if (rootObject.TryGetComponent(out WearNTear piece) && PieceRegistry.TryGet(piece, out PieceEntry entry) &&
                    entry.Forced && entry.Group != null && entry.Group.Build != null && entry.Group.Build.Visible())
                {
                    visibleOnly = false;
                }
            }
            catch (Exception error)
            {
                CombinedMeshes.Fail(error);
            }
        }
    }
}
