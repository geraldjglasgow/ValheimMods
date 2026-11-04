using System.Collections.Generic;
using PatchGuard;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// A dock lantern in the world (<c>BS_dock_lantern</c>, DOCKS.md "Dock lanterns"): the workshop's lantern head
    /// (<c>bs_dock_lanternhead</c>) on top of a post of the dock's own wood, standing just outside a dock's edge with the
    /// head's arm reaching in over the deck. The piece stands where the post's axis meets the deck's top; the head sits
    /// <see cref="KwDockLanternSpec.Rise"/> over it and the post runs down to its saved length (<see cref="KwDockLanternSpec"/>).
    /// Built once on every peer from its ZDO: the post's box everywhere, a dedicated server too, its look only where there is
    /// a screen (<see cref="KwDockLanternShape"/>, in the dock's material); its requirements set to what it cost, so the game
    /// gives it back when it is removed or broken. The placing game writes the text and builds in the same frame it places
    /// the piece (<see cref="Take"/>); a peer whose ZDO has no text yet looks again every second. Beside a dock section it is
    /// held up by that section (<see cref="Held"/>, <see cref="KwPierLadderSupport"/>) and falls with it; beside a game floor
    /// the game's own support holds it. Lit from dusk to dawn as the lantern pole (<see cref="KwDockLantern"/>). Nothing is sent.
    /// </summary>
    public sealed class KwDockLanternPiece : MonoBehaviour
    {
        public const string Prefab = KwDockside.Prefix + "lantern";
        public const string PostName = "bs_lantern_post", BoxName = "bs_lantern_post_box";
        private const float Shown = 0.6f, Boxed = 0.3f, Holds = 0.06f;
        private static readonly List<KwDockLanternPiece> loaded = new List<KwDockLanternPiece>();
        private static readonly Dictionary<WearNTear, KwDockLanternPiece> byWear = new Dictionary<WearNTear, KwDockLanternPiece>();
        private ZNetView? view;
        private WearNTear? wear;
        private Mesh? mesh;
        private float nextLook;

        /// <summary>Every dock lantern placed and loaded on this peer, built or not yet (never a placing ghost).</summary>
        public static IReadOnlyList<KwDockLanternPiece> Loaded => loaded;

        public KwDockLanternSpec? Spec { get; private set; }

        /// <summary>The dock lantern a piece's wear belongs to, or null.</summary>
        public static KwDockLanternPiece? Of(WearNTear wear) => byWear.TryGetValue(wear, out KwDockLanternPiece lantern) ? lantern : null;

        /// <summary>
        /// The prefab (<see cref="KwDockDef.Fit"/>): the head's parts lifted onto the post's top, a wood post shown 0.6 m under
        /// the deck and its box 0.3 m (what a placed lantern's own length replaces), the night light and this.
        /// </summary>
        public static void Fit(GameObject piece, GameObject look)
        {
            foreach (Transform part in look.transform)
                part.localPosition += Vector3.up * KwDockLanternSpec.Rise;
            GameObject post = new GameObject(PostName) { layer = piece.layer };
            post.transform.SetParent(look.transform, false);
            post.AddComponent<MeshFilter>().sharedMesh = KwIcons.CanRender ? KwDockLanternShape.Mesh(KwPierKinds.Wood, Shown, 1, Prefab) : null;
            post.AddComponent<MeshRenderer>().sharedMaterial = KwPierLooks.Of(KwPierKinds.Wood);
            GameObject box = new GameObject(BoxName) { layer = piece.layer };
            box.transform.SetParent(look.transform, false);
            Size(box.AddComponent<BoxCollider>(), KwPierKinds.Wood, Boxed);
            piece.AddComponent<KwDockLantern>();
            piece.AddComponent<KwDockLanternPiece>();
        }

        // The post's box from the head down to its foot, as wide as the post.
        private static void Size(BoxCollider box, KwPierKind kind, float length)
        {
            float wide = KwDockLanternShape.Half(kind) * 2f, rise = KwDockLanternSpec.Rise;
            (box.center, box.size) = (new Vector3(0f, (rise - length) * 0.5f, 0f), new Vector3(wide, rise + length, wide));
        }

        private void Awake()
        {
            view = GetComponent<ZNetView>();
            wear = GetComponent<WearNTear>();
        }

        // Listed once it is a real piece (a placing ghost has no valid network view), then built from its ZDO.
        private void Start()
        {
            if (view == null || !view.IsValid())
            {
                enabled = false;
                return;
            }
            loaded.Add(this);
            if (wear != null)
                byWear[wear] = this;
            Guard.Run("Broadside dock lantern", Look);
        }

        private void Update()
        {
            if (Spec != null)
                enabled = false;
            else if (Time.time >= nextLook)
                Guard.Run("Broadside dock lantern", Look);
        }

        private void Look()
        {
            nextLook = Time.time + 1f;
            ZDO? zdo = view != null && view.IsValid() ? view.GetZDO() : null;
            if (Spec == null && zdo != null && KwDockLanternSpec.TryParse(zdo.GetString(KwDockLanternSpec.Key), out KwDockLanternSpec spec))
                Build(spec);
        }

        /// <summary>A lantern just placed on this game: its post written to its new ZDO and built at once.</summary>
        public static void Take(GameObject piece, KwDockLanternSpec spec)
        {
            piece.GetComponent<ZNetView>()?.GetZDO()?.Set(KwDockLanternSpec.Key, spec.ToString());
            KwDockLanternPiece? lantern = piece.GetComponent<KwDockLanternPiece>();
            if (lantern != null && lantern.Spec == null)
                lantern.Build(spec);
        }

        private void Build(KwDockLanternSpec spec)
        {
            Spec = spec;
            enabled = false;
            BoxCollider? box = transform.Find(KwDockModel.LookName + "/" + BoxName)?.GetComponent<BoxCollider>();
            if (box != null)
                Size(box, spec.Kind, spec.Length);
            GetComponent<Piece>().m_resources = spec.Requirements(true);
            KwPierBody.Resupport(wear);
            if (KwIcons.CanRender)
                Draw(spec);
        }

        private void Draw(KwDockLanternSpec spec)
        {
            Transform? post = transform.Find(KwDockModel.LookName + "/" + PostName);
            if (post == null)
                return;
            mesh = KwDockLanternShape.Mesh(spec.Kind, spec.Length, spec.Seed, Prefab);
            post.GetComponent<MeshFilter>().sharedMesh = mesh;
            Material? look = KwPierLooks.Of(spec.Kind);
            if (look != null)
                post.GetComponent<MeshRenderer>().sharedMaterial = look;
        }

        /// <summary>
        /// Whether a dock section loaded here holds it: the deck's edge just inside its post on a built section (true too
        /// while a section near it has not built yet, so a lantern loading before its dock is never dropped).
        /// </summary>
        public bool Held()
        {
            KwPierKind kind = Spec != null ? Spec.Kind : KwPierKinds.Wood;
            Vector3 way = new Vector3(transform.forward.x, 0f, transform.forward.z).normalized;
            Vector3 hook = transform.position + way * (KwDockLanternShape.Off(kind) + Holds);
            foreach (KwPier pier in KwPier.Loaded)
            {
                KwPierSpec? spec = pier != null ? pier.Spec : null;
                Vector3 local = pier != null ? pier.transform.InverseTransformPoint(hook) : Vector3.positiveInfinity;
                if (pier != null && spec == null && local.sqrMagnitude < 15f * 15f)
                    return true;
                if (spec != null && Mathf.Abs(local.x) <= spec.Width * 0.5f + 0.02f && local.z >= -0.02f && local.z <= spec.Length + 0.02f && Mathf.Abs(local.y) <= 0.05f)
                    return true;
            }
            return false;
        }

        private void OnDestroy()
        {
            loaded.Remove(this);
            if (wear != null)
                byWear.Remove(wear);
            if (mesh != null)
                Destroy(mesh);
        }
    }
}
