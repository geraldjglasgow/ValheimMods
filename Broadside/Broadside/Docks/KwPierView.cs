using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Broadside
{
    /// <summary>
    /// The Dock tool's preview: the planned dock (<see cref="KwPierPlan"/>), every section as it will be built, pilings to
    /// their found feet, at its width and length, in one mesh made again whenever the plan changes (the aim, the wheel's
    /// width or material). It wears the material's own texture as the built dock does (<see cref="KwPierLooks"/>, its
    /// tiling put into the preview's UVs), see-through at <see cref="Opacity"/>, tinted red while it is refused. Drawn in
    /// two passes after everything else in the frame: depth only, then the texture where that depth is the nearest, so
    /// only the faces in front show (boards, joists and pilings behind one another never stack into a blur). Local to
    /// the player's screen, never networked, casting no shadow.
    /// </summary>
    public static class KwPierView
    {
        private const float Opacity = 0.55f;
        private const int DepthQueue = 3990;
        private static readonly Color Allowed = new Color(1f, 1f, 1f, Opacity), Refused = new Color(1f, 0.32f, 0.25f, Opacity);
        private static readonly Dictionary<(Material, bool), Material> films = new Dictionary<(Material, bool), Material>();
        private static Material? depth;
        private static bool sought;
        private static GameObject? shown;
        private static Mesh? mesh;
        private static KwPierPlan? drawn;

        public static void Show(KwPierPlan? plan)
        {
            if (plan == null)
            {
                Hide();
                return;
            }
            GameObject view = View();
            view.SetActive(true);
            if (plan == drawn)
                return;
            drawn = plan;
            view.transform.SetPositionAndRotation(plan.At, plan.Turn);
            Material? solid = KwPierLooks.Of(plan.Kind);
            if (mesh != null)
                Object.Destroy(mesh);
            mesh = Build(plan, solid);
            Dress(view, mesh, Film(solid, plan.Why != null));
        }

        public static void Hide()
        {
            if (shown != null && shown.activeSelf)
                shown.SetActive(false);
            drawn = null;
        }

        // Every section in the start's frame, each at its own place along the line, its UVs as the material tiles them.
        private static Mesh Build(KwPierPlan plan, Material? solid)
        {
            KwPierMesh builder = new KwPierMesh();
            for (int i = 0; i < plan.Sections.Count; i++)
            {
                builder.Offset = new Vector3(0f, 0f, plan.Offsets[i]);
                KwPierShape.Build(builder, plan.Sections[i], plan.Kind);
            }
            Mesh made = builder.ToMesh("bs_pier_preview");
            Tile(made, solid);
            return made;
        }

        // The material's tiling and offset put into the mesh's own UVs: the see-through shader reads none.
        private static void Tile(Mesh made, Material? solid)
        {
            if (solid == null || !solid.HasProperty("_MainTex"))
                return;
            Vector2 scale = solid.mainTextureScale, offset = solid.mainTextureOffset;
            if (scale == Vector2.one && offset == Vector2.zero)
                return;
            List<Vector2> uvs = new List<Vector2>(made.vertexCount);
            made.GetUVs(0, uvs);
            for (int i = 0; i < uvs.Count; i++)
                uvs[i] = Vector2.Scale(uvs[i], scale) + offset;
            made.SetUVs(0, uvs);
        }

        // The mesh on both passes: depth on the view (none when that shader is missing), the texture on its child.
        private static void Dress(GameObject view, Mesh made, Material? film)
        {
            Transform face = view.transform.GetChild(0);
            view.GetComponent<MeshFilter>().sharedMesh = made;
            face.GetComponent<MeshFilter>().sharedMesh = made;
            MeshRenderer pass = view.GetComponent<MeshRenderer>();
            pass.sharedMaterial = Depth();
            pass.enabled = pass.sharedMaterial != null && film != null;
            face.GetComponent<MeshRenderer>().sharedMaterial = film;
        }

        private static GameObject View()
        {
            if (shown != null)
                return shown;
            // a new one (the old went with the world's scene) shows nothing drawn yet
            (shown, drawn) = (Part("KwPierPreview", null), null);
            Part("film", shown.transform);
            return shown;
        }

        private static GameObject Part(string name, Transform? parent)
        {
            GameObject part = new GameObject(name);
            if (parent != null)
                part.transform.SetParent(parent, false);
            part.AddComponent<MeshFilter>();
            MeshRenderer renderer = part.AddComponent<MeshRenderer>();
            (renderer.shadowCastingMode, renderer.receiveShadows) = (ShadowCastingMode.Off, false);
            return part;
        }

        // The material's texture on a see-through shader the game ships (the sprite shader, else the UI's), white or red,
        // drawn after the depth pass; the solid material when neither shader is there.
        private static Material? Film(Material? solid, bool refused)
        {
            if (solid == null)
                return null;
            if (films.TryGetValue((solid, refused), out Material film) && film != null)
                return film;
            Shader? shader = Shader.Find("Sprites/Default") ?? Shader.Find("UI/Default");
            if (shader == null)
                return solid;
            film = new Material(shader) { name = "bs_pier_preview", color = refused ? Refused : Allowed, renderQueue = DepthQueue + 1 };
            film.mainTexture = solid.HasProperty("_MainTex") ? solid.mainTexture : null;
            films[(solid, refused)] = film;
            return film;
        }

        // Depth only (no colour: blend Zero, One), written and tested, front faces; null when the shader is missing.
        private static Material? Depth()
        {
            if (sought)
                return depth;
            sought = true;
            Shader? shader = Shader.Find("Hidden/Internal-Colored");
            if (shader == null)
                return null;
            depth = new Material(shader) { name = "bs_pier_preview_depth", renderQueue = DepthQueue };
            depth.SetInt("_SrcBlend", (int)BlendMode.Zero);
            depth.SetInt("_DstBlend", (int)BlendMode.One);
            depth.SetInt("_ZWrite", 1);
            depth.SetInt("_ZTest", (int)CompareFunction.LessEqual);
            depth.SetInt("_Cull", (int)CullMode.Back);
            return depth;
        }
    }
}
