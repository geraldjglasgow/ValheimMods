using System.Collections;
using System.Collections.Generic;
using HarmonyLib;
using PatchGuard;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// A launched ship in the world (<see cref="KwShipPrefab"/>): on every peer it reads the blueprint its ZDO carries
    /// (<see cref="Key"/>, <see cref="KwShipData"/>) and builds the ship from it once (where there is a screen over a few
    /// frames, <see cref="Staged"/>; its colliders and tuning always on the first): its colliders to
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

        /// <summary>Whether every step of building it has run (where there is a screen they run over a few frames).</summary>
        public bool Built { get; private set; }

        private void Start()
        {
            gameObject.AddComponent<KwShipNudge>();
            if (Drawn)
                StartCoroutine(Staged());
            else
                Guard.Run("Broadside ship", () => { foreach (bool _ in Steps()) { } });
        }

        // The steps over frames, about 4 ms of them a frame (one step may take more), so placing or loading a ship is a few
        // short frames rather than one long one (2026-10-27: 340 to 470 ms in one frame); the first frame always has its
        // colliders and tuning, so it floats and is walked on at once. The order is the steps' own.
        private IEnumerator Staged()
        {
            IEnumerator<bool> steps = Steps().GetEnumerator();
            double until = KwBpRunner.Until();
            while (Next(steps))
            {
                if (!KwBpRunner.Over(until))
                    continue;
                yield return null;
                until = KwBpRunner.Until();
            }
        }

        private static bool Next(IEnumerator<bool> steps)
        {
            try
            {
                return Guard.Run<bool>("Broadside ship", steps.MoveNext);
            }
            catch (System.Exception)
            {
                // reported by the guard; the ship stays as far as it was built
                return false;
            }
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

        // Every step of building the ship, in order, each on its own, so one that fails leaves the others standing (and the
        // log says which); each yield is a place it may go on in the next frame.
        private IEnumerable<bool> Steps()
        {
            ZDO? zdo = GetComponent<ZNetView>()?.GetZDO();
            KwBlueprint? blueprint = zdo != null ? KwShipData.Unpack(zdo.GetString(Key)) : null;
            if (blueprint == null)
                yield break;
            KwBpHull hull = Hull = KwBpHull.Measure(blueprint);
            Blueprint = blueprint;
            Manual(GetComponent<Rigidbody>());
            Step("Broadside ship colliders", () => KwShipColliders.Add(transform.Find(KwShipPrefab.Colliders), blueprint));
            Step("Broadside ship tuning", () => KwShipTuning.Apply(transform, blueprint, hull));
            yield return true;
            foreach (bool step in KwShipFittings.Steps(transform, blueprint, hull))
                yield return step;
            Step("Broadside ship sailing", () => KwShipSailing.Add(transform, blueprint, hull));
            yield return true;
            foreach (bool step in Drawn ? Draw(blueprint) : new bool[0])
                yield return step;
            Step("Broadside ship damage", () => KwShipDamage.Add(transform, blueprint, hull));
            yield return true;
            foreach (bool step in KwSeaShip.Steps(this, blueprint, hull))
                yield return step;
            Built = true;
        }

        // The body's centre of mass and inertia taken off automatic before a box is added: on automatic Unity works them
        // out again from every collider as each is added, and a ship has a thousand boxes and their item-floor copies
        // (measured live, 2026-10-27: 809 ms against 31 ms for the copies alone). Its tuning sets both a few steps on.
        private static void Manual(Rigidbody? body)
        {
            if (body == null)
                return;
            body.automaticCenterOfMass = false;
            body.automaticInertiaTensor = false;
        }

        // What only a screen shows: the look, the shadow cast by plain boxes, the ship materials and the water mask.
        private IEnumerable<bool> Draw(KwBlueprint blueprint)
        {
            Step("Broadside ship look", () => KwShipLook.Add(transform.Find(KwShipPrefab.Visual), blueprint));
            yield return true;
            Step("Broadside ship shadow", () => KwShipShadow.Add(transform, blueprint));
            Step("Broadside ship materials", () => KwShipMaterials.Apply(transform));
            Step("Broadside ship water mask", () => KwShipWaterMask.Add(transform, KwHullSlices.Rings(blueprint, Hull!), GetComponent<KwShipHydro>().Waterline));
            yield return true;
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
