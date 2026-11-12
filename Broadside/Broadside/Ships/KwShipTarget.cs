using System.Collections.Generic;
using HarmonyLib;
using PatchGuard;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// A launched ship as something a creature attacks, answered cheaply (found on the way to a player's report,
    /// 2026-11-08, of frame drops with leeches or pirates by a ship; the drops themselves were the ship's sync,
    /// <see cref="KwShipSync"/>). A creature that cannot reach its enemy turns on what players built near it, and a ship
    /// is one piece (the game's <c>StaticTarget</c>): twenty times a second the game asked for the ship's nearest point
    /// by measuring every collider it has (<c>StaticTarget.FindClosestPoint</c>: measured 0.3 ms a call at 2300
    /// colliders, 1.1 ms at 8500) and whether it sees it by looking each thing in the way up in the list of them all
    /// (<c>BaseAI.CanSeeTarget</c>; every two seconds that once for each of up to 128 colliders near the creature). The
    /// game's ships have a dozen colliders; ours has thousands of boxes.
    /// Here the nearest point is taken from the few boxes round the asker (<see cref="Closest"/>), or far off from the
    /// hull's own box; a thing in the way is the ship's when it is of the ship's body (<see cref="Seen"/>, asked once a
    /// step by a creature); and the ship's middle is the hull box's, set once, so the game never lists the colliders.
    /// What creatures do is as before: they come up to the hull and strike it. On every peer (a creature thinks on its
    /// owner, a dedicated server too); nothing is sent or saved.
    /// </summary>
    public sealed class KwShipTarget : MonoBehaviour
    {
        // Past this far from the hull's box the box's own nearest point serves: the creature is only walking up to it.
        private const float Far = 6f;

        private static readonly Dictionary<int, KwShipTarget> Ships = new Dictionary<int, KwShipTarget>();

        // How much further than the hull's box is the boxes are looked for, each tried only when the one before found none.
        private static readonly float[] Reach = { 1.5f, 4f, 8f };
        private static readonly Collider[] Near = new Collider[256];
        private static readonly RaycastHit[] Hits = new RaycastHit[128];
        private static int mask;

        private Rigidbody? body;
        private StaticTarget? target;
        private Transform? frame;
        private Vector3 centre, half;
        private int id;
        private BaseAI? asker;
        private float asked = -1f;
        private bool seen;

        // The ship's own boxes are all on the vehicle layer, like the game's ships' (KwShipColliders).
        private static int Mask => mask != 0 ? mask : mask = LayerMask.GetMask("vehicle");

        /// <summary>A launched ship given its cheap answers, from its hull's measure (in its colliders' frame, the blueprint's).</summary>
        public static void Add(Transform ship, KwBpHull hull)
        {
            StaticTarget? target = ship.GetComponent<StaticTarget>();
            Transform? frame = ship.Find(KwShipPrefab.Colliders);
            if (target == null || frame == null || ship.GetComponent<KwShipTarget>() != null)
                return;
            KwShipTarget made = ship.gameObject.AddComponent<KwShipTarget>();
            (made.target, made.frame, made.body, made.id) = (target, frame, ship.GetComponent<Rigidbody>(), target.GetInstanceID());
            made.centre = new Vector3(0f, hull.Keel + hull.Depth / 2f, (hull.Fore + hull.Aft) / 2f);
            made.half = new Vector3(hull.Beam, hull.Depth, hull.Length) / 2f;
            // the game works a target's middle out once from every collider; the hull box's is given it instead
            (target.m_localCenter, target.m_haveCenter) = (ship.InverseTransformPoint(frame.TransformPoint(made.centre)), true);
            Ships[made.id] = made;
        }

        private void OnDestroy()
        {
            if (Ships.TryGetValue(id, out KwShipTarget kept) && ReferenceEquals(kept, this))
                Ships.Remove(id);
        }

        /// <summary>The launched ship a target is, if it is one; one test when none is loaded.</summary>
        public static bool Of(StaticTarget target, out KwShipTarget ship)
        {
            ship = null!;
            return Ships.Count != 0 && Ships.TryGetValue(target.GetInstanceID(), out ship);
        }

        /// <summary>
        /// The ship's nearest point to a place: of the boxes round the place when it is near the hull, else the nearest
        /// point of the hull's box (a creature that far off is only told which way to go; one inside the box with no box
        /// of the ship in reach is sent to the ship's middle).
        /// </summary>
        public Vector3 Closest(Vector3 point)
        {
            Vector3 local = frame!.InverseTransformPoint(point) - centre;
            local = new Vector3(Mathf.Clamp(local.x, -half.x, half.x), Mathf.Clamp(local.y, -half.y, half.y), Mathf.Clamp(local.z, -half.z, half.z));
            Vector3 box = frame.TransformPoint(local + centre);
            float clear = Vector3.Distance(point, box);
            if (clear > Far)
                return box;
            foreach (float reach in Reach)
            {
                if (Nearest(point, clear + reach, out Vector3 found))
                    return found;
            }
            return clear > 0.01f ? box : frame.TransformPoint(centre);
        }

        // The nearest point of the ship's own boxes within a reach of a place; false with none there.
        private bool Nearest(Vector3 point, float reach, out Vector3 found)
        {
            found = point;
            float best = float.MaxValue;
            int count = Physics.OverlapSphereNonAlloc(point, reach, Near, Mask, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < count; i++)
            {
                Collider part = Near[i];
                if (part.attachedRigidbody != body)
                    continue;
                Vector3 at = part is MeshCollider { convex: false } ? part.ClosestPointOnBounds(point) : part.ClosestPoint(point);
                float away = (at - point).sqrMagnitude;
                if (away < best)
                    (best, found) = (away, at);
            }
            return best < float.MaxValue;
        }

        /// <summary>
        /// Whether a creature sees the ship, by the game's own rules (in view range, before it unless alerted, nothing
        /// but the ship between its eye and the ship's middle, no mist); the same creature asking again in one physics
        /// step is told the same (the game asks once for each collider near it).
        /// </summary>
        public bool Seen(BaseAI ai)
        {
            if (ReferenceEquals(ai, asker) && asked == Time.fixedTime)
                return seen;
            (asker, asked) = (ai, Time.fixedTime);
            return seen = Looks(ai);
        }

        private bool Looks(BaseAI ai)
        {
            Vector3 middle = target!.GetCenter(), eye = ai.m_character.m_eye.position, to = middle - eye;
            if (Vector3.Distance(middle, ai.transform.position) > ai.m_viewRange)
                return false;
            if (ai.m_viewRange > 0f && !ai.IsAlerted() && Vector3.Dot(ai.transform.forward, to) < 0f)
                return false;
            int count = Physics.RaycastNonAlloc(eye, to.normalized, Hits, to.magnitude, BaseAI.m_viewBlockMask);
            for (int i = 0; i < count; i++)
            {
                // the ship's boxes and the pieces riding it are all colliders of its body
                if (Hits[i].rigidbody != body)
                    return false;
            }
            return ai.m_mistVision || !ParticleMist.IsMistBlocked(eye, middle);
        }
    }

    /// <summary>
    /// The game's two questions about a static target answered by a launched ship itself (<see cref="KwShipTarget"/>);
    /// every other target, and every target while no launched ship is loaded, is the game's as it was.
    /// </summary>
    [HarmonyPatch]
    public static class KwShipTargetPatches
    {
        [HarmonyPatch(typeof(StaticTarget), nameof(StaticTarget.FindClosestPoint)), HarmonyPrefix]
        private static bool Closest(StaticTarget __instance, Vector3 point, ref Vector3 __result)
        {
            if (!KwShipTarget.Of(__instance, out KwShipTarget ship))
                return true;
            __result = Guard.Run("Broadside ship target", static (s, p) => s.Closest(p), ship, point);
            return false;
        }

        [HarmonyPatch(typeof(BaseAI), "CanSeeTarget", typeof(StaticTarget)), HarmonyPrefix]
        private static bool Seen(BaseAI __instance, StaticTarget target, ref bool __result)
        {
            if (target == null || !KwShipTarget.Of(target, out KwShipTarget ship))
                return true;
            __result = Guard.Run("Broadside ship sight", static (s, a) => s.Seen(a), ship, __instance);
            return false;
        }
    }
}
