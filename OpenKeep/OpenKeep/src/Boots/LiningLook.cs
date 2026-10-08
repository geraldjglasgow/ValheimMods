using UnityEngine;

namespace OpenKeep.Boots
{
    /// <summary>
    /// The workshop's coverage cloth (v008): inside each pair of trousers, a copy of the body's own legs in the garment's
    /// colour between waist and ankle, so no skin shows where the trousers lie close. It shares the body's surface, so it
    /// is drawn with the bundle's own shader (<see cref="ShaderName"/>: a fixed depth offset gives the cloth the pixels it
    /// shares with the body, never the trousers'); the game has no shader like it. Where that shader cannot run on the
    /// machine's graphics API the cloth draws nothing, and the leggings' body paint underneath (<see cref="LegsPaint"/>)
    /// covers the legs as before.
    /// </summary>
    internal static class LiningLook
    {
        public const string ShaderName = "Workshop/LegArmorLining";

        private static Material hidden;

        public static bool Is(Material placeholder) =>
            placeholder != null && placeholder.shader != null && placeholder.shader.name == ShaderName;

        /// <summary>The bundle's own material when its shader runs here, else one that draws nothing.</summary>
        public static Material For(Material placeholder, Material basis) => placeholder.shader.isSupported ? placeholder : Hidden(basis);

        /// <summary>A cut-out copy of the set's material that cuts out every pixel (a clear texture).</summary>
        private static Material Hidden(Material basis)
        {
            if (hidden != null)
                return hidden;
            Plugin.Log.LogWarning("OpenKeep: the trousers' cloth backing cannot be drawn here; the leggings' body paint covers the legs instead");
            hidden = new Material(basis) { name = "OpenKeep_LiningHidden", mainTexture = Texture2D.blackTexture };
            if (hidden.HasProperty("_Cutoff"))
                hidden.SetFloat("_Cutoff", 0.5f);
            return hidden;
        }
    }
}
