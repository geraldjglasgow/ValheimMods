using HarmonyLib;
using PatchGuard;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// A launched ship in the world (<see cref="KwShipPrefab"/>): on every peer it reads the blueprint its ZDO carries
    /// (<see cref="Key"/>, <see cref="KwShipData"/>) and builds the ship from it once, on its first frame: its colliders to
    /// walk on (<see cref="KwShipColliders"/>), how it floats and sails (<see cref="KwShipTuning"/>), its working fittings
    /// (<see cref="KwShipFittings"/>: the rudder, the helm, the hatches, the sails, the ladder) and, only where there is a
    /// screen (<see cref="Drawn"/>, never on a dedicated server), its look (every piece's meshes combined, one mesh per
    /// material, <see cref="KwShipLook"/>), its shadow, its ship materials and its water mask. The game's own Ship does
    /// the rest: steering, sailing, rowing, its owner and its sync. A ghost (no ZDO) builds nothing.
    /// </summary>
    public sealed class KwShipBody : MonoBehaviour
    {
        public const string Key = "kw_ship";

        /// <summary>Whether this peer draws (false on a dedicated server, which builds only what is not drawn).</summary>
        public static readonly bool Drawn = SystemInfo.graphicsDeviceType != UnityEngine.Rendering.GraphicsDeviceType.Null;

        public KwBpHull? Hull { get; private set; }

        /// <summary>The blueprint it was built from (its masts' ropes tie on to its pieces, <see cref="KwRigSource"/>).</summary>
        public KwBlueprint? Blueprint { get; private set; }

        private void Start()
        {
            gameObject.AddComponent<KwShipNudge>();
            Guard.Run("Broadside ship", Build);
        }

        /// <summary>One step of building a ship: a throw is reported (the guard logs it) and the next step goes on.</summary>
        public static void Step(string what, System.Action action)
        {
            try
            {
                Guard.Run(what, action);
            }
            catch (System.Exception)
            {
                // reported by the guard; the ship is built without this part
            }
        }

        private void Build()
        {
            ZDO? zdo = GetComponent<ZNetView>()?.GetZDO();
            KwBlueprint? blueprint = zdo != null ? KwShipData.Unpack(zdo.GetString(Key)) : null;
            if (blueprint == null)
                return;
            KwBpHull hull = Hull = KwBpHull.Measure(blueprint);
            Blueprint = blueprint;
            // each step on its own, so one that fails leaves the others standing (and the log says which)
            Step("Broadside ship colliders", () => KwShipColliders.Add(transform.Find(KwShipPrefab.Colliders), blueprint));
            Step("Broadside ship tuning", () => KwShipTuning.Apply(transform, blueprint, hull));
            Step("Broadside ship fittings", () => KwShipFittings.Add(transform, blueprint, hull));
            Step("Broadside ship sailing", () => KwShipSailing.Add(transform, blueprint, hull));
            if (Drawn)
                Draw(blueprint);
            Step("Broadside ship damage", () => KwShipDamage.Add(transform, blueprint, hull));
            Step("Broadside ship sea edits", () => KwSeaShip.Add(this, blueprint, hull));
        }

        // What only a screen shows: the look, the shadow cast by plain boxes, the ship materials and the water mask.
        private void Draw(KwBlueprint blueprint)
        {
            Step("Broadside ship look", () => KwShipLook.Add(transform.Find(KwShipPrefab.Visual), blueprint));
            Step("Broadside ship shadow", () => KwShipShadow.Add(transform, blueprint));
            Step("Broadside ship materials", () => KwShipMaterials.Apply(transform));
            Step("Broadside ship water mask", () => KwShipWaterMask.Add(transform, KwHullSlices.Rings(blueprint, Hull!), GetComponent<KwShipHydro>().Waterline));
        }
    }

    /// <summary>
    /// The game's ship moves its sail by its own cloth, which a launched ship does not have: there its sails follow the
    /// ship's speed themselves (<see cref="KwShipSail"/>), and the game's sail step is skipped.
    /// </summary>
    [HarmonyPatch(typeof(Ship), "UpdateSailSize")]
    public static class KwShipSailPatch
    {
        [HarmonyPrefix]
        private static bool Prefix(Ship __instance) => __instance.GetComponent<KwShipBody>() == null;
    }
}
