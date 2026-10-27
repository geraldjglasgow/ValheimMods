using System.Collections.Generic;
using PatchGuard;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// A mast and its sails in the world. Its sails are raised together in four steps: furled, a quarter, half and fully
    /// raised; a square sail's foot is hauled up to its yard and let down again, a sail
    /// along the ship comes down onto its boom (<see cref="KwSailCanvas"/>), never stretched, each on its own yard or boom.
    /// E raises them a step, Shift and E lowers them one, the same for every player: the step lives in the piece's ZDO
    /// (<see cref="Key"/>, fully raised when unset), written only by the ZDO's owner, whom the player asks by RPC (claiming
    /// the piece first when nobody owns it); every peer folds each cloth up toward its yard (or down to its boom) from the
    /// ZDO, its bolt rope, its gaff and the ropes on them with it (<see cref="KwSailDress.Hoisted"/>). Its ropes tie on to the
    /// ship round it (<see cref="KwRigTies"/>), again a few seconds after it loads, whenever a hull part near it comes, and
    /// whenever a piece round it comes or goes (<see cref="KwRigState"/>). The rig's parts are made from what the piece saves (<see cref="KwSailDress.Dress"/>). A ghost does nothing.
    /// </summary>
    public sealed class KwSailRig : MonoBehaviour, Interactable, Hoverable
    {
        public const string Key = "kw_sail_hoist";
        private const string RpcAsk = "KW_SailHoist";
        private const float PerSecond = 0.7f;
        private static readonly string[] Steps = { "furled", "a quarter raised", "half raised", "fully raised" };

        // how much of the sail each step lets out
        private static readonly float[] Shares = { 0f, 0.25f, 0.5f, 1f };
        private const int Full = 3;

        private const float SettleFor = 4f;
        private static readonly List<KwSailRig> All = new List<KwSailRig>();

        private ZNetView? view;
        private Transform?[] cloths = new Transform?[0];
        private List<KwSailLayout>? sails;
        private float stage = 1f, rigAt;
        private bool posed, broken;

        /// <summary>Every mast loaded within reach of a point ties its ropes on again next frame: the hull there changed.</summary>
        public static void Rerig(Vector3 point, float reach)
        {
            foreach (KwSailRig sail in All)
            {
                if (sail != null && (sail.transform.position - point).sqrMagnitude < reach * reach)
                    sail.rigAt = Time.time;
            }
        }

        /// <summary>Every mast loaded on this peer (a bowsprit finds its foremast among them, <see cref="KwJibStay"/>).</summary>
        public static IReadOnlyList<KwSailRig> Loaded => All;

        /// <summary>The mast and sails its piece saves; null when the text does not read.</summary>
        public KwMastSpec? Spec { get; private set; }

        /// <summary>How far the sails are raised: 0 furled, 1 a quarter, 2 half, 3 full.</summary>
        public int Hoist => view != null && view.IsValid() ? Mathf.Clamp(view.GetZDO().GetInt(Key, Full), 0, Full) : Full;

        /// <summary>The share of each full sail let out: none, a quarter, a half or all of it.</summary>
        public float Drive => Shares[Hoist];

        private void Start()
        {
            view = GetComponent<ZNetView>();
            if (view == null || view.GetZDO() == null)
            {
                enabled = false;
                return;
            }
            view.Register<int>(RpcAsk, (sender, step) => Guard.Run("Broadside sail hoist", () => Step(step)));
            if (KwMastSpec.TryParse(view.GetZDO().GetString(KwGenerated.Key), out KwMastSpec mast))
                (Spec, sails, cloths) = (mast, KwSailLayout.Of(mast), new Transform?[mast.Sails.Count]);
            stage = Drive;
            rigAt = Time.time + SettleFor;
            All.Add(this);
        }

        private void OnDestroy() => All.Remove(this);

        // Once the ship round it has loaded (and whenever a hull part near it comes), its ropes tie on to it again.
        private void Retie()
        {
            if (rigAt <= 0f || Time.time < rigAt || sails == null)
                return;
            rigAt = 0f;
            KwSailRigging.Rig(transform, sails, KwSailCloth.Rope);
            posed = false;
        }

        // A throw is reported once and stops this mast, rather than once a frame.
        private void Update()
        {
            if (broken)
                return;
            broken = true;
            Guard.Run("Broadside sail", static s => { s.Retie(); s.Fold(); }, this);
            broken = false;
        }

        // The cloths may come (or come again, rebuilt) after this starts: they are posed once when found, then eased.
        private void Fold()
        {
            if (sails == null || !Found())
                return;
            float target = Drive;
            if (posed && Mathf.Approximately(stage, target))
                return;
            posed = true;
            stage = Mathf.MoveTowards(stage, target, PerSecond * Time.deltaTime);
            for (int i = 0; i < cloths.Length; i++)
                KwSailDress.Hoisted(transform, cloths[i]!, sails, i, Mathf.SmoothStep(0f, 1f, stage));
        }

        // Every sail's cloth found (one newly found is posed again); false while one is not there yet.
        private bool Found()
        {
            for (int i = 0; i < cloths.Length; i++)
            {
                if (cloths[i] != null)
                    continue;
                cloths[i] = transform.Find(KwSailDress.Cloth(i));
                posed = false;
                if (cloths[i] == null)
                    return false;
            }
            return true;
        }

        // On the owner: a step up or down, kept between furled and fully raised.
        private void Step(int step)
        {
            if (view != null && view.IsValid() && view.IsOwner())
                view.GetZDO().Set(Key, Mathf.Clamp(Hoist + step, 0, Full));
        }

        public bool Interact(Humanoid user, bool hold, bool alt)
        {
            if (hold || view == null || !view.IsValid())
                return false;
            if (!PrivateArea.CheckAccess(transform.position))
                return true;
            if (!view.HasOwner())
                view.ClaimOwnership();
            view.InvokeRPC(RpcAsk, alt ? -1 : 1);
            return true;
        }

        public bool UseItem(Humanoid user, ItemDrop.ItemData item) => false;

        public string GetHoverText()
        {
            bool one = sails == null || sails.Count == 1;
            string what = one ? "Sail" : $"Mast, {sails!.Count} sails";
            return Localization.instance.Localize(
                $"{what} ({Steps[Hoist]})\n[<color=yellow><b>$KEY_Use</b></color>] Raise {(one ? "it" : "them")} a step\n" +
                $"[<color=yellow><b>$KEY_AltPlace + $KEY_Use</b></color>] Lower {(one ? "it" : "them")} a step");
        }

        public string GetHoverName() => sails == null || sails.Count == 1 ? "Sail" : "Mast";

        public float GetHoverOffset() => 0f;
    }

    /// <summary>Destroys the mesh its object's filter holds when the object goes: a mesh made for this object alone.</summary>
    public sealed class KwMeshOwner : MonoBehaviour
    {
        private void OnDestroy()
        {
            MeshFilter? filter = GetComponent<MeshFilter>();
            if (filter != null && filter.sharedMesh != null)
                Destroy(filter.sharedMesh);
        }
    }
}
