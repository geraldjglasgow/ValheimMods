using PatchGuard;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// A slatted crate's own marks (the user's words: "have a texture like the cows, where every crate has a different
    /// pattern ... dark spots, wood knots"): a seed in its ZDO (<see cref="SeedKey"/>), rolled once by the owner as the
    /// crate is placed, so every peer draws the same crate the same way, and its albedo made from it
    /// (<see cref="KwCratePattern"/>) and worn by the body and the lid through materials of its own (copies of the shared
    /// moving-object ones, so a ship carrying it adds no copies). At most one crate is made a frame, so a base loading
    /// many does not stall; until then, and on a placement ghost, it wears the bundle's albedo (the workshop's default
    /// seed). Off on a dedicated server; nothing is sent but the seed's one write.
    /// </summary>
    public sealed class KwCrateCoat : MonoBehaviour
    {
        public const string SeedName = "bs_crate_seed";
        public static readonly int SeedKey = SeedName.GetStableHashCode();
        private const int PerFrame = 1;
        private const float Wait = 1f;
        private static int frame = -1, madeThisFrame;

        /// <summary>The bundle the crate's sources are in; set on the prefab (a public field, so the game's copies keep it).</summary>
        public string Bundle = "";

        private ZNetView? view;
        private int seed;
        private float nextLook;
        private Material[]? own;

        private void Awake()
        {
            view = GetComponent<ZNetView>();
            Roll();
        }

        private void Start()
        {
            if (view == null || view.GetZDO() == null || !KwIcons.CanRender)
                enabled = false;
        }

        // Waits for the seed (a peer that is not the owner may see the crate before it), then dresses once, within the frame's budget.
        private void Update()
        {
            if (Time.time < nextLook || !Turn())
                return;
            int rolled = Roll();
            if (rolled == 0)
            {
                nextLook = Time.time + Wait;
                return;
            }
            enabled = false;
            Guard.Run("Broadside crate marks", static (c, s) => c.Dress(s), this, rolled);
        }

        // The owner rolls the seed once; anyone reads it (0: not rolled yet).
        private int Roll()
        {
            ZDO? zdo = view != null && view.IsValid() ? view.GetZDO() : null;
            if (zdo == null)
                return 0;
            int found = zdo.GetInt(SeedKey);
            if (found == 0 && view!.IsOwner())
                zdo.Set(SeedKey, found = Random.Range(1, KwCrateMarks.SeedMask + 1));
            return found;
        }

        private static bool Turn()
        {
            if (frame != Time.frameCount)
                (frame, madeThisFrame) = (Time.frameCount, 0);
            return madeThisFrame++ < PerFrame;
        }

        private void Dress(int rolled)
        {
            // the model's renderers only (a crane's rope sling under the crate is not the crate's wood)
            Transform? look = transform.Find(KwCrateModel.LookName);
            Renderer[] renderers = (look != null ? look : transform).GetComponentsInChildren<Renderer>(true);
            Texture2D? albedo = renderers.Length > 0 ? KwCratePattern.Take(Bundle, rolled, renderers[0].sharedMaterial?.mainTexture) : null;
            if (albedo == null)
                return;
            seed = rolled;
            own = new Material[renderers.Length];
            for (int i = 0; i < renderers.Length; i++)
            {
                own[i] = new Material(renderers[i].sharedMaterial) { name = renderers[i].sharedMaterial.name + " (crate " + rolled + ")", mainTexture = albedo };
                renderers[i].sharedMaterial = own[i];
            }
        }

        private void OnDestroy()
        {
            if (seed != 0)
                KwCratePattern.Release(Bundle, seed);
            foreach (Material material in own ?? new Material[0])
            {
                if (material != null)
                    Destroy(material);
            }
        }
    }
}
