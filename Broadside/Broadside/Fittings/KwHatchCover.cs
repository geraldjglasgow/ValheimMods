using System.Collections.Generic;
using PatchGuard;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// A hatch in the world. Whether it is open lives in its ZDO (<see cref="Key"/>, shut when unset), written only by the
    /// ZDO's owner: E asks the owner by RPC (claiming the hatch first when nobody owns it, as the game's Fireplace does),
    /// the owner flips it, and every peer swings the leaves toward the ZDO's state. Every hatch loaded on a peer is listed
    /// (<see cref="All"/>) for the decks to cut their openings from; one coming or going makes the decks round it build
    /// again (<see cref="KwGenerated.Recut"/>). A placement ghost does nothing and cuts nothing.
    /// </summary>
    public sealed class KwHatchCover : MonoBehaviour, Interactable, Hoverable
    {
        public const string Key = "kw_hatch_open";
        private const string RpcAsk = "KW_HatchToggle";
        private const float Reach = 16f, SwingPerSecond = 2f, OpenAngle = 110f;

        /// <summary>Every hatch loaded on this peer, ghosts left out.</summary>
        public static readonly List<KwHatchCover> All = new List<KwHatchCover>();

        /// <summary>The opening: its length along the deck and its width across it; set on the bench copy.</summary>
        public Vector2 Opening = Vector2.one;

        /// <summary>Two doors hinged at the sides (the large hatch) rather than one leaf hinged aft.</summary>
        public bool Doors;

        private ZNetView? view;
        private Transform? leaf0, leaf1;
        private float stage;
        private bool listed, broken;

        public bool IsOpen => view != null && view.IsValid() && view.GetZDO().GetBool(Key);

        /// <summary>The nearest hatch loaded on this peer within range of a point, or null.</summary>
        public static KwHatchCover? Nearest(Vector3 point, float range)
        {
            KwHatchCover? best = null;
            float bestSq = range * range;
            foreach (KwHatchCover hatch in All)
            {
                float sq = (hatch.transform.position - point).sqrMagnitude;
                if (sq < bestSq)
                {
                    best = hatch;
                    bestSq = sq;
                }
            }
            return best;
        }

        /// <summary>The opening's four corners on the deck's top, in the world.</summary>
        public Vector3[] Corners()
        {
            float hx = Opening.x / 2f, hy = Opening.y / 2f;
            return new[] { Kit(-hx, -hy), Kit(hx, -hy), Kit(hx, hy), Kit(-hx, hy) };
        }

        private Vector3 Kit(float x, float y) => transform.TransformPoint(KwMesh.ToUnity(new Vector3(x, y, 0f)));

        private void Start() => Guard.Run("Broadside hatch", static h => h.Init(), this);

        private void Init()
        {
            view = GetComponent<ZNetView>();
            leaf0 = transform.Find("leaf0");
            leaf1 = transform.Find("leaf1");
            if (view == null || view.GetZDO() == null || leaf0 == null)
            {
                enabled = false;
                return;
            }
            view.Register(RpcAsk, sender => Guard.Run("Broadside hatch toggle", Flip));
            stage = IsOpen ? 1f : 0f;
            Pose();
            All.Add(this);
            listed = true;
            KwGenerated.Recut(transform.position, Reach);
        }

        private void OnDestroy()
        {
            if (!listed)
                return;
            All.Remove(this);
            KwGenerated.Recut(transform.position, Reach);
        }

        // A throw is reported once and stops this hatch, rather than once a frame.
        private void Update()
        {
            if (broken)
                return;
            broken = true;
            Guard.Run("Broadside hatch", static h => h.Swing(), this);
            broken = false;
        }

        private void Swing()
        {
            float target = IsOpen ? 1f : 0f;
            if (Mathf.Approximately(stage, target))
                return;
            stage = Mathf.MoveTowards(stage, target, SwingPerSecond * Time.deltaTime);
            Pose();
        }

        // One leaf lifts its fore edge about its aft hinge; two doors lift their middle edges about their side hinges.
        private void Pose()
        {
            float angle = Mathf.SmoothStep(0f, OpenAngle, stage);
            leaf0!.localRotation = Doors ? Quaternion.Euler(0f, 0f, angle) : Quaternion.Euler(-angle, 0f, 0f);
            if (leaf1 != null)
                leaf1.localRotation = Quaternion.Euler(0f, 0f, -angle);
        }

        // On the owner: open becomes shut and shut open.
        private void Flip()
        {
            if (view != null && view.IsValid() && view.IsOwner())
                view.GetZDO().Set(Key, !IsOpen);
        }

        /// <summary>Asks for the hatch to open, if it is shut (a player climbing up to it).</summary>
        public void OpenUp()
        {
            if (!IsOpen)
                Ask();
        }

        private void Ask()
        {
            if (!view!.HasOwner())
                view.ClaimOwnership();
            view.InvokeRPC(RpcAsk);
        }

        public bool Interact(Humanoid user, bool hold, bool alt)
        {
            if (hold || view == null || !view.IsValid())
                return false;
            if (PrivateArea.CheckAccess(transform.position))
                Ask();
            return true;
        }

        public bool UseItem(Humanoid user, ItemDrop.ItemData item) => false;

        public string GetHoverText() =>
            Localization.instance.Localize($"Hatch\n[<color=yellow><b>$KEY_Use</b></color>] {(IsOpen ? "Shut" : "Open")}");

        public string GetHoverName() => "Hatch";

        public float GetHoverOffset() => 0f;
    }
}
