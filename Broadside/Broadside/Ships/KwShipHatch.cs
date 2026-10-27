using System.Collections.Generic;
using PatchGuard;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// A hatch on a launched ship: a working copy of the blueprint's hatch (its coaming and its leaves, the leaves' boxes
    /// to walk on while shut). It has no ZDO of its own: whether it is open lives in the ship's, under the hatch's number
    /// on the ship (<c>kw_hatch3_open</c>), flipped by the ship's owner on a call named for it (<c>KW_Hatch3_Toggle</c>),
    /// and every peer swings the leaves toward it, as a placed hatch does (<see cref="KwHatchCover"/>). The deck under it
    /// is cut open in the ship's look and its colliders (<see cref="KwShipDeckHoles"/>).
    /// </summary>
    public sealed class KwShipHatch : MonoBehaviour, Interactable, Hoverable
    {
        private const float SwingPerSecond = 2f, OpenAngle = 110f;
        private static readonly List<KwShipHatch> all = new List<KwShipHatch>();

        private ZNetView? ship;
        private int key;
        private string call = "";
        private Transform? leaf0, leaf1;
        private bool doors;
        private float stage;

        public bool IsOpen => ship != null && ship.IsValid() && ship.GetZDO().GetBool(key);

        /// <summary>Makes a working hatch of a ship's copy of a hatch piece, numbered among the ship's numbered fittings.</summary>
        public static void Make(Transform made, string prefab, ZNetView ship, int index)
        {
            KwShipHatch hatch = made.gameObject.AddComponent<KwShipHatch>();
            KwHatchCover? kind = ZNetScene.instance.GetPrefab(prefab)?.GetComponent<KwHatchCover>();
            hatch.ship = ship;
            hatch.doors = kind != null && kind.Doors;
            hatch.key = ("kw_hatch" + index + "_open").GetStableHashCode();
            hatch.call = "KW_Hatch" + index + "_Toggle";
            hatch.leaf0 = made.Find("leaf0");
            hatch.leaf1 = made.Find("leaf1");
            ship.Register(hatch.call, sender => Guard.Run("Broadside ship hatch", hatch.Flip));
            hatch.stage = hatch.IsOpen ? 1f : 0f;
            hatch.Pose();
            all.Add(hatch);
        }

        /// <summary>The nearest ship hatch within range of a point, or null.</summary>
        public static KwShipHatch? Nearest(Vector3 point, float range)
        {
            KwShipHatch? best = null;
            float bestSq = range * range;
            foreach (KwShipHatch hatch in all)
            {
                float sq = hatch != null ? (hatch.transform.position - point).sqrMagnitude : float.MaxValue;
                if (sq < bestSq)
                    (best, bestSq) = (hatch, sq);
            }
            return best;
        }

        private void OnDestroy()
        {
            all.Remove(this);
            if (ship != null && call.Length > 0)
                ship.Unregister(call);
        }

        private void Update()
        {
            float target = IsOpen ? 1f : 0f;
            if (Mathf.Approximately(stage, target))
                return;
            stage = Mathf.MoveTowards(stage, target, SwingPerSecond * Time.deltaTime);
            Pose();
        }

        // As a placed hatch: one leaf lifts its fore edge about its aft hinge; two doors lift their middle edges.
        private void Pose()
        {
            float angle = Mathf.SmoothStep(0f, OpenAngle, stage);
            if (leaf0 != null)
                leaf0.localRotation = doors ? Quaternion.Euler(0f, 0f, angle) : Quaternion.Euler(-angle, 0f, 0f);
            if (leaf1 != null)
                leaf1.localRotation = Quaternion.Euler(0f, 0f, -angle);
        }

        // On the ship's owner: open becomes shut and shut open.
        private void Flip()
        {
            if (ship != null && ship.IsValid() && ship.IsOwner())
                ship.GetZDO().Set(key, !IsOpen);
        }

        /// <summary>Asks for the hatch to open, if it is shut (a player climbing up to it).</summary>
        public void OpenUp()
        {
            if (!IsOpen && ship != null && ship.IsValid())
                ship.InvokeRPC(call);
        }

        public bool Interact(Humanoid user, bool hold, bool alt)
        {
            if (hold || ship == null || !ship.IsValid())
                return false;
            ship.InvokeRPC(call);
            return true;
        }

        public bool UseItem(Humanoid user, ItemDrop.ItemData item) => false;

        public string GetHoverText() =>
            Localization.instance.Localize($"Hatch\n[<color=yellow><b>$KEY_Use</b></color>] {(IsOpen ? "Shut" : "Open")}");

        public string GetHoverName() => "Hatch";

        public float GetHoverOffset() => 0f;
    }
}
