using System.Collections.Generic;
using System.Globalization;
using PatchGuard;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// A post cap carved from what its keel is made of. The Post cap tool's caps (<see cref="KwCapSpec"/>) get their keel's
    /// biome tier in their ZDO when built (<see cref="KwBiomeTiers.Key"/>); an old hammer cap placed onto a keel's "Post
    /// top" took it on the placing player's game. Every peer dresses the cap in its tier's look when it appears
    /// (<see cref="Look"/>: a carved head in the heads' look tinted by the tier, any other cap in the tier's keel look);
    /// saved in a blueprint like any other value. A cap placed before the tiers came, or on no keel, is Meadows. Its hover
    /// names its style, and for a tool's cap its facing, length, lean and base, read once from its saved text.
    /// </summary>
    public sealed class KwTierCap : MonoBehaviour, IPlaced, Hoverable
    {
        private const float Reach = 20f, Snapped = 0.1f;
        private string hover = "";

        private void Start() => Guard.Run("Broadside post cap tier", Dress);

        public void OnPlaced() => Guard.Run("Broadside post cap tier", Adopt);

        private void Dress()
        {
            ZDO? zdo = GetComponent<ZNetView>()?.GetZDO();
            if (zdo != null)
                KwBiomeDress.Wear(transform, Look(Utils.GetPrefabName(gameObject), zdo.GetString(KwGenerated.Key), KwBiomeTiers.Of(zdo)));
            hover = Words(zdo);
        }

        /// <summary>
        /// The look a cap wears in its tier, from its prefab and saved text (an old hammer cap has none): a carved head's
        /// where its model is drawn (<see cref="KwHeadLooks"/>), else the tier's keel look.
        /// </summary>
        public static Material? Look(string prefab, string? saved, KwBiomeTier tier)
        {
            string style = KwCapSpec.TryParse(saved ?? "", out KwCapSpec spec) ? spec.Style : KwCapSpec.Old(prefab, out string old) ? old : "";
            Material? head = KwCapHeads.Of(style) != null ? KwHeadLooks.For(tier) : null;
            return head ?? KwBiomeLooks.For(KwPart.Keel, tier);
        }

        /// <summary>Whether a material is a cap's look before its tier dresses it: the Meadows keel look or the heads' Meadows look.</summary>
        public static bool Undressed(Material material) =>
            material == KwBiomeLooks.For(KwPart.Keel, KwBiomeTiers.Meadows) || KwHeadLooks.IsMeadows(material);

        // "Post cap: dragon head", and for a tool's cap its length, lean and base on the next line.
        private string Words(ZDO? zdo)
        {
            if (zdo != null && KwCapSpec.TryParse(zdo.GetString(KwGenerated.Key), out KwCapSpec spec))
                return $"Post cap: {spec.Name.ToLowerInvariant()}{(spec.Inboard ? ", facing inboard" : "")}\n"
                    + $"Length {spec.Length.ToString("0.0", CultureInfo.InvariantCulture)}×{spec.Leaning}, "
                    + $"base {spec.Width.ToString("0.00", CultureInfo.InvariantCulture)} × {spec.Depth.ToString("0.00", CultureInfo.InvariantCulture)} m";
            return KwCapSpec.Old(Utils.GetPrefabName(gameObject), out string style) ? $"Post cap: {KwCaps.Find(style)!.Name.ToLowerInvariant()}" : "Post cap";
        }

        public string GetHoverText() => hover.Length > 0 ? hover : "Post cap";

        public string GetHoverName() => "Post cap";

        public float GetHoverOffset() => 0f;

        // The keel whose post top the cap stands on gives it its tier.
        private void Adopt()
        {
            ZNetView view = GetComponent<ZNetView>();
            Piece? keel = KeelUnder(transform.position);
            if (view == null || view.GetZDO() == null || keel == null)
                return;
            view.GetZDO().Set(KwBiomeTiers.Key, KwBiomeTiers.Of(keel.GetComponent<ZNetView>()?.GetZDO()).Id);
            Dress();
        }

        /// <summary>The keel loaded near the point with a "Post top" snap point on it (within 10 cm): the post a cap stands on.</summary>
        public static Piece? KeelUnder(Vector3 point)
        {
            List<Piece> near = new List<Piece>();
            Piece.GetAllPiecesInRadius(point, Reach, near);
            foreach (Piece piece in near)
            {
                if (!KwKeel.IsKeel(piece))
                    continue;
                foreach (Transform child in piece.transform)
                {
                    if (child.name == "Post top" && (child.position - point).sqrMagnitude < Snapped * Snapped)
                        return piece;
                }
            }
            return null;
        }
    }
}
