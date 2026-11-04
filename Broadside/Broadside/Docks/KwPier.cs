using System.Collections.Generic;
using PatchGuard;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// A dock section in the world (<c>BS_pier_&lt;material&gt;</c>): its shape is read from its ZDO
    /// (<see cref="KwPierSpec"/>) and built once on every peer: the solid parts and snap points everywhere, a dedicated
    /// server too (<see cref="KwPierBody"/>), the timbers only where there is a screen (<see cref="KwPierShape"/>, in its
    /// material's own atlas, <see cref="KwPierLooks"/>); and its requirements are set to what it cost
    /// (<see cref="KwPierCost"/>), so the game gives all of it back, as it does any piece's, when it is removed with the
    /// hammer or broken. The placing game writes the text and builds in the same frame it places the piece
    /// (<see cref="Take"/>); a peer whose ZDO has no text yet looks again every second. Where another section meets one of
    /// its edges (a branch off its side) its curb is left out (<see cref="KwPierGaps"/>), worked out on each peer from the
    /// sections loaded there when it builds, and again, the curbs and timbers made anew, the frame after one meeting it
    /// comes or goes. Nothing is sent. Other code reads a loaded section's deck through <see cref="KwPierDeck"/> (DOCKS.md,
    /// "Reading a dock").
    /// </summary>
    public sealed class KwPier : MonoBehaviour
    {
        /// <summary>The material's id, set on each of the three prefabs.</summary>
        public string Kind = "wood";

        private static readonly List<KwPier> loaded = new List<KwPier>();
        private ZNetView? view;
        private Mesh? mesh;
        private float nextLook;
        private bool regap;

        /// <summary>Every dock section loaded on this peer, built or not yet.</summary>
        public static IReadOnlyList<KwPier> Loaded => loaded;

        /// <summary>The section's shape, once built; null until then.</summary>
        public KwPierSpec? Spec { get; private set; }

        public KwPierKind Material => KwPierKinds.Of(Kind);

        /// <summary>Its deck's middle in the world and the level distance from there to its corners, once built (for a quick look at what is near it).</summary>
        public Vector3 Middle { get; private set; }
        public float Reach { get; private set; }

        /// <summary>Where other loaded sections meet its edges, as last built; none until it has built.</summary>
        public KwPierGaps Gaps { get; private set; } = KwPierGaps.None;

        private void Awake()
        {
            view = GetComponent<ZNetView>();
            loaded.Add(this);
        }

        private void Start() => Guard.Run("Broadside dock", Look);

        private void Update()
        {
            if (Spec == null)
            {
                if (Time.time >= nextLook)
                    Guard.Run("Broadside dock", Look);
                return;
            }
            enabled = false;
            if (regap)
                Guard.Run("Broadside dock", Refit);
        }

        /// <summary>A section meeting this one came or went: its gaps are looked at again next frame.</summary>
        public void Regap()
        {
            regap = true;
            enabled = true;
        }

        // The gaps worked out again; the curbs' solid parts and the timbers made again when a side's changed.
        private void Refit()
        {
            regap = false;
            KwPierGaps gaps = KwPierGaps.Of(this);
            bool reshaped = gaps.ShapeKey != Gaps.ShapeKey;
            Gaps = gaps;
            if (Spec == null || !reshaped)
                return;
            KwPierBody.Curbs(transform, Spec, Material, gaps);
            KwPierBody.Resupport(GetComponent<WearNTear>());
            if (KwIcons.CanRender)
                Draw(Spec, Material);
        }

        // The saved shape built, once it is there.
        private void Look()
        {
            nextLook = Time.time + 1f;
            ZDO? zdo = view != null && view.IsValid() ? view.GetZDO() : null;
            if (Spec == null && zdo != null && KwPierSpec.TryParse(zdo.GetString(KwPierSpec.Key), out KwPierSpec spec))
                Build(spec);
        }

        /// <summary>A section just placed on this game: its shape written to its new ZDO and built at once, before the game's support check can run.</summary>
        public static void Take(GameObject piece, KwPierSpec spec)
        {
            piece.GetComponent<ZNetView>()?.GetZDO()?.Set(KwPierSpec.Key, spec.ToString());
            KwPier? pier = piece.GetComponent<KwPier>();
            if (pier != null && pier.Spec == null)
                pier.Build(spec);
        }

        private void Build(KwPierSpec spec)
        {
            (Middle, Reach) = (transform.position + transform.forward * (spec.Length * 0.5f), new Vector2(spec.Length, spec.Width).magnitude * 0.5f);
            Spec = spec;
            enabled = false;
            KwPierKind kind = Material;
            Gaps = KwPierGaps.Of(this);
            KwPierBody.Make(transform, spec, kind, Gaps);
            Piece piece = GetComponent<Piece>();
            piece.m_resources = KwPierCost.Requirements(kind, KwPierCost.Amount(spec), true);
            KwPierBody.Resupport(GetComponent<WearNTear>());
            if (KwIcons.CanRender)
                Draw(spec, kind);
            KwPierGaps.Touch(this);
        }

        // The timbers in the material's look on the model the prefab carries (the last ones dropped).
        private void Draw(KwPierSpec spec, KwPierKind kind)
        {
            Transform? model = transform.Find("model");
            if (model == null)
                return;
            KwPierMesh builder = new KwPierMesh();
            KwPierShape.Build(builder, spec, kind, Gaps);
            if (mesh != null)
                Destroy(mesh);
            mesh = builder.ToMesh("bs_pier_" + kind.Id);
            model.GetComponent<MeshFilter>().sharedMesh = mesh;
            Material? look = KwPierLooks.Of(kind);
            if (look != null)
                model.GetComponent<MeshRenderer>().sharedMaterial = look;
        }

        // Gone (removed, broken or out of the loaded area): the sections it met close their curbs again.
        private void OnDestroy()
        {
            loaded.Remove(this);
            Guard.Run<KwPier>("Broadside dock", KwPierGaps.Touch, this);
            if (mesh != null)
                Destroy(mesh);
        }
    }
}
