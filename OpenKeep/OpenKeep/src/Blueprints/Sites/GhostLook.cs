using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.Rendering;

namespace OpenKeep.Blueprints.Sites
{
    /// <summary>
    /// The look of a construction site's ghost: a faint, light copy of the nearest surface with the piece's own texture
    /// showing through, so players see what will stand there and still see through it (the user: "ghost like ... you
    /// can still make out the details ... barely visible at all"). See-through faces alone stack up (a hut's walls,
    /// floor and roof behind one another drew a solid white block), so every renderer of a copy draws twice, after
    /// everything else in the frame: first depth only (Unity's Hidden/Internal-Colored, blend Zero/One), then, as a
    /// child renderer on the same mesh, the piece's texture on the game's Sprites/Default shader (unlit, alpha-blended,
    /// depth tested) where that depth is the nearest. No shadows. The colour (<see cref="Pale"/>: brightness and
    /// opacity, or a glow colour) is set per renderer by the ghost; <see cref="Tune"/> changes the look live (DevBridge
    /// eval). When a shader is missing the copy keeps its materials and only the tint shows.
    /// </summary>
    public static class GhostLook
    {
        /// <summary>
        /// The plain ghost: the texture lightened and cooled by the colour (blue over red cancels the wood and thatch's
        /// yellow, which read green over the scene), at 4 % opacity; tuned live with the user in game.
        /// </summary>
        public static Color Pale { get; private set; } = new Color(1.01f, 1.34f, 2.4f, 0.04f);

        /// <summary>A glowing ghost piece (the planner's selection, a hovered queue row) is this opaque.</summary>
        public const float GlowAlpha = 0.4f;

        /// <summary>After every transparent thing of the frame (water, smoke), so the depth pass hides none of them.</summary>
        private const int DepthQueue = 3990;

        /// <summary>Each game material's film copy (its texture), made at first use.</summary>
        private static readonly Dictionary<Material, Material> films = new Dictionary<Material, Material>();
        private static Material depth;
        private static Shader filmShader;
        private static bool textured = true;
        private static bool tried;

        /// <summary>Gives each renderer of the copy (found into <paramref name="renderers"/>) the depth pass and a film child.</summary>
        public static void Apply(GameObject copy, List<Renderer> renderers)
        {
            if (!Ready())
                return;
            copy.GetComponentsInChildren(renderers);
            foreach (Renderer renderer in renderers)
            {
                if (renderer is MeshRenderer mesh)
                    Split(mesh);
            }
        }

        /// <summary>The colour of a ghost piece: pale, or the glow's colour made more solid.</summary>
        public static Color ColourOf(Color? glow) => glow.HasValue ? new Color(glow.Value.r, glow.Value.g, glow.Value.b, GlowAlpha) : Pale;

        /// <summary>
        /// Live tuning (DevBridge: <c>OpenKeep.Blueprints.Sites.GhostLook.Tune(0.1, 1.5, true)</c>): opacity, how much the
        /// texture is lightened (1 = as is), and whether the texture shows; every ghost is painted again at once.
        /// </summary>
        public static string Tune(double alpha, double brightness, bool texture)
        {
            float b = (float)brightness;
            Pale = new Color(b, b, b, (float)alpha);
            textured = texture;
            foreach (KeyValuePair<Material, Material> pair in films)
                Dress(pair.Value, pair.Key);
            SiteGhost.RepaintAll();
            return string.Format(CultureInfo.InvariantCulture, "ghost: alpha {0}, brightness {1}, texture {2}", alpha, brightness, texture);
        }

        /// <summary>The renderer writes only depth; a child renderer on the same mesh draws the film.</summary>
        private static void Split(MeshRenderer renderer)
        {
            Material[] originals = renderer.sharedMaterials;
            Material[] depths = new Material[originals.Length];
            Material[] faces = new Material[originals.Length];
            for (int k = 0; k < originals.Length; k++)
            {
                depths[k] = depth;
                faces[k] = FilmOf(originals[k]);
            }
            renderer.sharedMaterials = depths;
            Quiet(renderer);
            MeshFilter filter = renderer.GetComponent<MeshFilter>();
            if (filter == null || filter.sharedMesh == null)
                return;
            GameObject face = new GameObject("ghost film");
            face.transform.SetParent(renderer.transform, false);
            face.AddComponent<MeshFilter>().sharedMesh = filter.sharedMesh;
            MeshRenderer drawn = face.AddComponent<MeshRenderer>();
            drawn.sharedMaterials = faces;
            Quiet(drawn);
        }

        private static Material FilmOf(Material original)
        {
            if (original != null && films.TryGetValue(original, out Material film) && film != null)
                return film;
            film = new Material(filmShader) { name = "OpenKeep site ghost film", renderQueue = DepthQueue + 1 };
            Dress(film, original);
            if (original != null)
                films[original] = film;
            return film;
        }

        private static void Dress(Material film, Material original)
        {
            film.color = Pale;
            film.mainTexture = textured && original != null && original.HasProperty("_MainTex") ? original.mainTexture : null;
        }

        private static void Quiet(Renderer renderer)
        {
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
        }

        /// <summary>The depth material and the film shader, found at first use; false when either shader is missing.</summary>
        private static bool Ready()
        {
            if (!tried)
            {
                tried = true;
                Shader depthShader = Shader.Find("Hidden/Internal-Colored");
                filmShader = Shader.Find("Sprites/Default");
                if (depthShader != null && filmShader != null)
                    depth = MakeDepth(depthShader);
            }
            return depth != null;
        }

        /// <summary>Depth only: no colour (blend Zero/One), depth written and tested, both sides.</summary>
        private static Material MakeDepth(Shader shader)
        {
            Material material = new Material(shader) { name = "OpenKeep site ghost depth", renderQueue = DepthQueue };
            material.SetInt("_SrcBlend", (int)BlendMode.Zero);
            material.SetInt("_DstBlend", (int)BlendMode.One);
            material.SetInt("_ZWrite", 1);
            material.SetInt("_ZTest", (int)CompareFunction.LessEqual);
            material.SetInt("_Cull", (int)CullMode.Off);
            return material;
        }
    }
}
