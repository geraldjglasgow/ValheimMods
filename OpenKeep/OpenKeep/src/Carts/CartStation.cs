using HarmonyLib;
using UnityEngine;

namespace OpenKeep.Carts
{
    /// <summary>
    /// The workbench a cart carries: a <c>CraftingStation</c> component added to every loaded cart (in
    /// <c>Vagon.Awake</c> and when the setting changes), copied from the workbench prefab's station with the
    /// same name so every workbench recipe, piece and repair accepts it, without roof or fire requirements.
    /// The prefab itself is never changed, so turning the setting off removes the component from every loaded
    /// cart at once and nothing needs a relog. The station registers itself through its own <c>Start</c>.
    /// </summary>
    public static class CartStation
    {
        public const string WorkbenchPrefab = "piece_workbench";
        private const float UseDistance = 4f;

        public static bool IsCart(CraftingStation station) => station != null && station.GetComponent<Vagon>() != null;

        /// <summary>Adds, removes or updates the station of every loaded cart to match the settings.</summary>
        public static void ApplyAll()
        {
            foreach (Vagon cart in new System.Collections.Generic.List<Vagon>(Vagon.m_instances))
                Apply(cart);
        }

        public static void Apply(Vagon cart)
        {
            if (cart == null || cart.m_nview == null || cart.m_nview.GetZDO() == null)
                return;
            CraftingStation existing = cart.GetComponent<CraftingStation>();
            bool wanted = CartsSettings.CartWorkbench.Value;
            if (wanted && existing == null)
                Add(cart);
            else if (!wanted && existing != null)
                Object.Destroy(existing);
            else if (existing != null)
                existing.m_rangeBuild = CartsSettings.CartStationRange.Value;
        }

        private static void Add(Vagon cart)
        {
            CraftingStation template = Template();
            if (template == null)
            {
                Plugin.Log.LogWarning("OpenKeep: the workbench prefab has no crafting station; carts stay plain carts");
                return;
            }
            Copy(template, cart.gameObject.AddComponent<CraftingStation>(), cart.transform);
        }

        private static CraftingStation Template()
        {
            GameObject workbench = ZNetScene.instance != null ? ZNetScene.instance.GetPrefab(WorkbenchPrefab) : null;
            return workbench != null ? workbench.GetComponentInChildren<CraftingStation>(true) : null;
        }

        private static void Copy(CraftingStation from, CraftingStation to, Transform cart)
        {
            to.m_name = from.m_name;
            to.m_icon = from.m_icon;
            to.m_discoverRange = from.m_discoverRange;
            to.m_rangeBuild = CartsSettings.CartStationRange.Value;
            to.m_extraRangePerLevel = from.m_extraRangePerLevel;
            to.m_craftRequireRoof = false;
            to.m_craftRequireFire = false;
            to.m_roofCheckPoint = cart;
            to.m_connectionPoint = null;
            to.m_showBasicRecipies = from.m_showBasicRecipies;
            to.m_useDistance = Mathf.Max(from.m_useDistance, UseDistance);
            to.m_craftingSkill = from.m_craftingSkill;
            to.m_hoverOffset = from.m_hoverOffset;
            to.m_hasCraftTab = from.m_hasCraftTab;
            to.m_canRepair = from.m_canRepair;
            to.m_upgrader = false;
            CopyEffects(from, to);
        }

        private static void CopyEffects(CraftingStation from, CraftingStation to)
        {
            to.m_craftItemEffects = from.m_craftItemEffects;
            to.m_craftItemDoneEffects = from.m_craftItemDoneEffects;
            to.m_repairItemDoneEffects = from.m_repairItemDoneEffects;
            to.m_craftItemDoneFailEffects = from.m_craftItemDoneFailEffects;
        }
    }

    /// <summary>Every cart that gets a ZDO gets its station when the setting is on.</summary>
    [HarmonyPatch(typeof(Vagon), nameof(Vagon.Awake))]
    public static class CartAwakePatch
    {
        [HarmonyPostfix]
        public static void Postfix(Vagon __instance) => CartStation.Apply(__instance);
    }
}
