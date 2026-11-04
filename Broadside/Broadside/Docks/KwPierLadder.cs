using System.Collections.Generic;
using PatchGuard;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// A dock ladder in the world (<c>BS_pier_ladder_&lt;material&gt;</c>, DOCKS.md "Dock ladders"): it stands on a dock's
    /// (or a game floor's) edge at the deck's top, facing out over the water, and hangs straight down to its saved length
    /// (<see cref="KwPierLadderSpec"/>). Built once on every peer from its ZDO: its climb box everywhere, a dedicated server
    /// too (<see cref="KwPierLadderBody"/>), its look only where there is a screen (<see cref="KwPierLadderShape"/>, in its
    /// dock's material); its requirements set to what it cost, so the game gives it back when it is removed or broken; its
    /// climb (<see cref="KwLadderClimb"/>, as the kit's ladders are climbed) given its length. The placing game writes the
    /// text and builds in the same frame it places the piece (<see cref="Take"/>); a peer whose ZDO has no text yet looks
    /// again every second. The dock section it hangs from leaves its curb out round it (<see cref="KwPierGaps"/>), told when
    /// the ladder builds or goes; that section holds it up (<see cref="Held"/>, <see cref="KwPierLadderSupport"/>). Nothing
    /// is sent.
    /// </summary>
    public sealed class KwPierLadder : MonoBehaviour
    {
        /// <summary>The material's id, set on each of the three prefabs.</summary>
        public string Kind = "wood";

        private const float Holds = 0.12f;
        private static readonly List<KwPierLadder> loaded = new List<KwPierLadder>();
        private static readonly Dictionary<WearNTear, KwPierLadder> byWear = new Dictionary<WearNTear, KwPierLadder>();
        private ZNetView? view;
        private WearNTear? wear;
        private Mesh? mesh;
        private float nextLook;

        /// <summary>Every dock ladder loaded on this peer, built or not yet.</summary>
        public static IReadOnlyList<KwPierLadder> Loaded => loaded;

        /// <summary>Its saved shape, once built; null until then.</summary>
        public KwPierLadderSpec? Spec { get; private set; }

        public KwPierKind Material => KwPierKinds.Of(Kind);

        /// <summary>The dock ladder a piece's wear belongs to, or null.</summary>
        public static KwPierLadder? Of(WearNTear wear) => byWear.TryGetValue(wear, out KwPierLadder ladder) ? ladder : null;

        private void Awake()
        {
            view = GetComponent<ZNetView>();
            wear = GetComponent<WearNTear>();
            loaded.Add(this);
            if (wear != null)
                byWear[wear] = this;
        }

        private void Start() => Guard.Run("Broadside dock ladder", Look);

        private void Update()
        {
            if (Spec != null)
                enabled = false;
            else if (Time.time >= nextLook)
                Guard.Run("Broadside dock ladder", Look);
        }

        private void Look()
        {
            nextLook = Time.time + 1f;
            ZDO? zdo = view != null && view.IsValid() ? view.GetZDO() : null;
            if (Spec == null && zdo != null && KwPierLadderSpec.TryParse(zdo.GetString(KwPierLadderSpec.Key), out KwPierLadderSpec spec))
                Build(spec);
        }

        /// <summary>A ladder just placed on this game: its shape written to its new ZDO and built at once.</summary>
        public static void Take(GameObject piece, KwPierLadderSpec spec)
        {
            piece.GetComponent<ZNetView>()?.GetZDO()?.Set(KwPierLadderSpec.Key, spec.ToString());
            KwPierLadder? ladder = piece.GetComponent<KwPierLadder>();
            if (ladder != null && ladder.Spec == null)
                ladder.Build(spec);
        }

        private void Build(KwPierLadderSpec spec)
        {
            Spec = spec;
            enabled = false;
            KwPierKind kind = Material;
            KwPierLadderBody.Make(transform, spec.Length);
            GetComponent<Piece>().m_resources = KwPierCost.Requirements(kind, spec.Cost, true);
            KwLadderClimb? climb = GetComponent<KwLadderClimb>();
            if (climb != null)
                (climb.FixedLength, climb.IntoWater) = (spec.Length, true);
            KwPierBody.Resupport(wear);
            if (KwIcons.CanRender)
                Draw(spec, kind);
            KwPierGaps.TouchLadder(transform.position);
        }

        private void Draw(KwPierLadderSpec spec, KwPierKind kind)
        {
            Transform? model = transform.Find("model");
            if (model == null)
                return;
            mesh = KwPierLadderShape.Mesh(kind, spec.Length, spec.Seed, "bs_pier_ladder_" + kind.Id);
            model.GetComponent<MeshFilter>().sharedMesh = mesh;
            Material? look = KwPierLooks.Of(kind);
            if (look != null)
                model.GetComponent<MeshRenderer>().sharedMaterial = look;
        }

        /// <summary>
        /// Whether a dock section loaded here holds it: its top on a built section's deck edge (true too while a section
        /// near it has not built yet, so a ladder loading before its dock is never dropped).
        /// </summary>
        public bool Held()
        {
            Vector3 hook = transform.position - Flat(transform.forward) * Holds;
            foreach (KwPier pier in KwPier.Loaded)
            {
                if (pier == null)
                    continue;
                KwPierSpec? spec = pier.Spec;
                Vector3 local = pier.transform.InverseTransformPoint(hook);
                if (spec == null && local.sqrMagnitude < 15f * 15f)
                    return true;
                if (spec != null && Mathf.Abs(local.x) <= spec.Width * 0.5f + 0.02f && local.z >= -0.02f && local.z <= spec.Length + 0.02f && Mathf.Abs(local.y) <= 0.05f)
                    return true;
            }
            return false;
        }

        private static Vector3 Flat(Vector3 v) => new Vector3(v.x, 0f, v.z).normalized;

        // Gone (removed, broken or out of the loaded area): the section it hung from closes its curb again.
        private void OnDestroy()
        {
            loaded.Remove(this);
            if (wear != null)
                byWear.Remove(wear);
            if (Spec != null && Spec.OnDock)
                Guard.Run<Vector3>("Broadside dock ladder", KwPierGaps.TouchLadder, transform.position);
            if (mesh != null)
                Destroy(mesh);
        }
    }
}
