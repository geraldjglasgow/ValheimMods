using HarmonyLib;
using UnityEngine;

namespace PackPanel.Backpacks
{
    /// <summary>
    /// Every client hangs a player's worn backpack on that player's back: the player's ZDO names the pack
    /// (<see cref="Backpack.WornKey"/>, written by the player's own client), and when it changes the model is swapped or
    /// taken down. It hangs on the Spine2 bone, the upper back, the way the game hangs a sheathed shield on the spine: kept
    /// at its own world size, turned upright in the player's frame (every pack's model is built with Spine2's rest
    /// position as its origin, AssetWorkshop's <c>packpanel_*</c> assets). Runs after the game's own equipment update, every
    /// frame for every player in view; unless something changed it only looks up one child by name. The main menu's
    /// character has no ZDO and wears none.
    /// </summary>
    [HarmonyPatch(typeof(VisEquipment), nameof(VisEquipment.UpdateEquipmentVisuals))]
    public static class BackpackMount
    {
        private const string Bone = "Spine2";
        private const string Name = "PackPanel_backpack";

        /// <summary>Spine2 at rest leans back about 5.7 degrees from the player's upright frame (the game's Player prefab).</summary>
        private static readonly Quaternion Upright = Quaternion.Inverse(new Quaternion(-0.0494f, 0f, 0f, 0.9988f));

        [HarmonyPostfix]
        public static void Postfix(VisEquipment __instance)
        {
            if (!__instance.m_isPlayer || __instance.m_isArmorStand || __instance.m_nview == null)
                return;
            ZDO zdo = __instance.m_nview.GetZDO();
            BackpackKind wanted = zdo != null ? BackpackCatalog.ByHash(zdo.GetInt(Backpack.WornKey)) : null;
            if (wanted != null && wanted.Worn == null)
                wanted = null;
            if (!__instance.m_boneMap.TryGetValue(Bone, out Transform spine) || spine == null)
                return;
            Transform hung = spine.Find(Name);
            if (Wears(hung) == wanted?.Id)
                return;
            if (hung != null)
                TakeDown(hung);
            if (wanted != null)
                Hang(spine, wanted);
            __instance.UpdateLodgroup();   // collects the renderers under the character's visual again
        }

        /// <summary>The prefab name of the pack on a mount (its model is named after it), or null.</summary>
        private static string Wears(Transform mount) => mount != null && mount.childCount > 0 ? mount.GetChild(0).name : null;

        /// <summary>
        /// A mount made in the world and parented keeping its world size (as the game attaches its own items), placed on the
        /// bone and turned upright; the model goes under it as the prefab has it.
        /// </summary>
        private static void Hang(Transform spine, BackpackKind kind)
        {
            GameObject mount = new GameObject(Name);
            mount.transform.SetParent(spine, true);
            mount.transform.localPosition = Vector3.zero;
            mount.transform.localRotation = Upright;
            Object.Instantiate(kind.Worn, mount.transform, false).name = kind.Id;
        }

        /// <summary>Off the character at once, so the LOD group update right after leaves it out; destroyed at the frame's end.</summary>
        private static void TakeDown(Transform mount)
        {
            mount.SetParent(null, false);
            Object.Destroy(mount.gameObject);
        }
    }
}
