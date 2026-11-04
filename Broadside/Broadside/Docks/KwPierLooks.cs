using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// What the docks wear: a copy of the kit's plank wood material (<see cref="KwPrefabs.Planks"/>: the game's Custom/Piece
    /// from the wood wall's body renderer, never its snow overlay, so rain, snow, value noise and the build highlight still
    /// work), drawn white, dressed in each dock material's own atlas painted in the asset workshop
    /// (<c>ValheimAssets/Assets/Ships/Keelwright/PierPlanks</c>): the albedo <c>bs_pier_&lt;material&gt;.png</c> and its normal
    /// map <c>bs_pier_&lt;material&gt;_n.png</c>, embedded, point filtered as the game's own textures, clamped
    /// (<see cref="KwPierAtlas"/>, mapped by <see cref="KwPierUv"/>). Made once a session when first asked for, shared by
    /// every section, the preview and the tool; dressed only where there is a screen (a dedicated server draws nothing). A
    /// material whose albedo is not embedded keeps the plank wood's texture, logged.
    /// </summary>
    public static class KwPierLooks
    {
        // Gloss: plain and core wood as the game's planks (paint.md 0.18), the planed fine wood a soft sheen over it.
        private static readonly Dictionary<string, float> Gloss = new Dictionary<string, float>
        {
            ["wood"] = 0.18f,
            ["corewood"] = 0.2f,
            ["finewood"] = 0.26f,
        };

        private static readonly Dictionary<string, Material> made = new Dictionary<string, Material>();

        /// <summary>The material's look; null before the kit has found the plank wood.</summary>
        public static Material? Of(KwPierKind kind)
        {
            if (made.TryGetValue(kind.Id, out Material look))
                return look;
            Material? planks = KwPrefabs.Planks;
            if (planks == null)
                return null;
            look = new Material(planks) { name = "bs_pier_" + kind.Id, color = Color.white, mainTextureScale = Vector2.one, mainTextureOffset = Vector2.zero };
            if (KwIcons.CanRender)
                Dress(look, kind);
            made[kind.Id] = look;
            return look;
        }

        private static void Dress(Material look, KwPierKind kind)
        {
            Texture2D? albedo = Load(look.name + ".png", false);
            if (albedo == null)
            {
                Log.Warn($"Broadside docks: {look.name}.png is not embedded; those docks wear the plank wood.");
                return;
            }
            look.mainTexture = albedo;
            Texture2D? normals = Load(look.name + "_n.png", true);
            if (normals != null && look.HasProperty("_BumpMap"))
                look.SetTexture("_BumpMap", normals);
            if (look.HasProperty("_Glossiness"))
                look.SetFloat("_Glossiness", Gloss.TryGetValue(kind.Id, out float gloss) ? gloss : 0.18f);
        }

        // An embedded texture, compressed as the kit's big ones (KwTextures), sampled nearest-texel.
        private static Texture2D? Load(string file, bool linear)
        {
            Texture2D? texture = KwTextures.Load(file, linear, TextureWrapMode.Clamp, TextureWrapMode.Clamp);
            if (texture != null)
                texture.filterMode = FilterMode.Point;
            return texture;
        }
    }
}
