using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace Broadside
{
    /// <summary>
    /// A rudder's design being built to one post and linked to its helm (<see cref="KwRudderLay"/>): a mesh builder per
    /// material for the parts that stay on the post (in the piece's frame: the post's after face at x = 0, its foot at
    /// z = 0; the deck tiller's pivot post too), another set for the parts on the blade (in the blade's frame: the stock's
    /// axis at the origin, the blade toward -x) and a third for the deck tiller (in its own frame: its pivot at the origin,
    /// its handle toward +x), and the post's measures the design is drawn to. Kit axes: x forward, y left, z up, metres.
    /// Afterwards, the post's, the blade's and the deck tiller's parts as one mesh each with a submesh per material
    /// (<see cref="Combined"/>), each showing the clean part of its material's
    /// texture (<see cref="KwRudderWindows"/>).
    /// </summary>
    public sealed class KwRudderKit
    {
        private readonly Dictionary<KwLook, MeshBuilder> post = new Dictionary<KwLook, MeshBuilder>();
        private readonly Dictionary<KwLook, MeshBuilder> blade = new Dictionary<KwLook, MeshBuilder>();
        private readonly Dictionary<KwLook, MeshBuilder> helm = new Dictionary<KwLook, MeshBuilder>();
        private readonly MeshBuilder? only;

        /// <summary>A kit for a tier, fit and lay; with <paramref name="only"/>, every part goes into that one builder.</summary>
        public KwRudderKit(KwRudderTier tier, KwRudderFit fit, MeshBuilder? only = null, KwRudderLay? lay = null)
        {
            Tier = tier;
            Fit = fit;
            Lay = lay ?? KwRudderLay.Plain;
            Span = Lay.Geared ? tier.Span(fit, KwGearTrain.Ceiling(Lay)) : tier.Span(fit);
            this.only = only;
        }

        public KwRudderTier Tier { get; }
        public KwRudderFit Fit { get; }

        /// <summary>How the rudder is linked to its helm: a tiller to the hand, a yoke with lines to a deck tiller, or the steering gear's quadrant.</summary>
        public KwRudderLay Lay { get; }

        /// <summary>The foot of the rudder's head on the stock, over the post's top (just over it with the steering gear); and the yoke's height on it.</summary>
        public float HeadBase => Top + (Lay.Geared ? Mathf.Max(Lay.Lift, 0.02f) : Lay.Lift);
        public float YokeZ => HeadBase + 0.07f * T;

        /// <summary>A tier's tiller's end: the helmsman's grip when it is steered by a tiller, else the tier's own.</summary>
        public Vector3 TillerTo(Vector3 own) => Lay.Kind == KwRudderHelm.Tiller ? Lay.Grip - Pivot : own;

        /// <summary>The blade's place on the post (<see cref="KwRudderSpan"/>; with the steering gear, its top under the quadrant).</summary>
        public KwRudderSpan Span { get; }

        /// <summary>The post's measures: its scale (S) and thickness factor (T), for the head, tiller and straps; its top over its foot.</summary>
        public float S => Fit.Scale;
        public float T => Fit.Thick;
        public float Top => Fit.Height;

        /// <summary>The blade's measures: the scale of its chord and details (B) and its thickness factor (BT), as S and T are the post's.</summary>
        public float B => Span.Chord;
        public float BT => Span.Thick;

        /// <summary>The stock's radius, half the blade's thickness, and the blade's fore edge (just inside the stock).</summary>
        public float Stock => Tier.Stock(Fit);
        public float Half => Tier.BladeHalf * BT;
        public float Fore => -0.3f * Stock;

        /// <summary>The stock's axis in the piece's frame.</summary>
        public Vector3 Pivot => Tier.Pivot(Fit);

        /// <summary>The blade's foot and tip heights, and the stock's foot (the blade's, or the rudder point's when higher).</summary>
        public float Foot => Span.Foot;
        public float Tip => Span.Tip;
        public float StockFoot => Mathf.Min(0f, Foot);

        public float AftAt(float z) => Span.AftAt(z);

        /// <summary>
        /// The height on the blade that matches a share of the post's height on a flat stern (where a strap on the post
        /// sits): there on a flat stern, at the same place along the blade on one that hangs lower.
        /// </summary>
        public float OnBlade(float share) => Span.Z(share * KwRudderFit.Standard.Height);

        /// <summary>The highest point of the blade at least d aft of the stock; its foot when none is.</summary>
        public float Reach(float d)
        {
            for (float z = Tip; z > Foot; z -= 0.02f * B)
            {
                if (AftAt(z) >= d)
                    return z;
            }
            return Foot;
        }

        public MeshBuilder Fixed(KwLook look) => Part(post, look);

        public MeshBuilder Blade(KwLook look) => Part(blade, look);

        /// <summary>A builder for the deck tiller, which turns on its own post (<see cref="KwRudderLinkShapes"/>).</summary>
        public MeshBuilder Helm(KwLook look) => Part(helm, look);

        private MeshBuilder Part(Dictionary<KwLook, MeshBuilder> parts, KwLook look)
        {
            if (only != null)
                return only;
            if (!parts.TryGetValue(look, out MeshBuilder builder))
                parts[look] = builder = new MeshBuilder(Vector3.zero);
            return builder;
        }

        /// <summary>The post's parts or the blade's as one mesh, a submesh per material, and the materials in that order.</summary>
        public (Mesh Mesh, Material[] Materials) Combined(bool onBlade, string name) => Combined(onBlade ? blade : post, name);

        /// <summary>The deck tiller's parts as one mesh, a submesh per material, and the materials in that order.</summary>
        public (Mesh Mesh, Material[] Materials) CombinedHelm(string name) => Combined(helm, name);

        /// <summary>A drawn part of its own under a parent: parts by look as one mesh, a submesh per material, its own mesh destroyed with it.</summary>
        public static Transform Part(Transform parent, string name, Dictionary<KwLook, MeshBuilder> parts, string mesh)
        {
            GameObject part = new GameObject(name) { layer = parent.gameObject.layer };
            part.transform.SetParent(parent, false);
            (Mesh made, Material[] materials) = Combined(parts, mesh);
            part.AddComponent<MeshFilter>().sharedMesh = made;
            part.AddComponent<MeshRenderer>().sharedMaterials = materials;
            part.AddComponent<KwMeshOwner>();
            return part.transform;
        }

        private static (Mesh Mesh, Material[] Materials) Combined(Dictionary<KwLook, MeshBuilder> parts, string name)
        {
            List<CombineInstance> pieces = new List<CombineInstance>();
            List<Material> materials = new List<Material>();
            int vertices = 0;
            foreach (KeyValuePair<KwLook, MeshBuilder> part in parts)
            {
                Material? material = KwTierMaterials.Of(part.Key);
                if (material == null)
                    continue;
                Mesh piece = Made(part.Key, part.Value, name);
                vertices += piece.vertexCount;
                pieces.Add(new CombineInstance { mesh = piece });
                materials.Add(material);
            }
            Mesh mesh = new Mesh { name = name, indexFormat = vertices > 65000 ? IndexFormat.UInt32 : IndexFormat.UInt16 };
            mesh.CombineMeshes(pieces.ToArray(), false, false);
            mesh.RecalculateBounds();
            foreach (CombineInstance piece in pieces)
                Object.Destroy(piece.mesh);
            return (mesh, materials.ToArray());
        }

        // A look's part as a mesh, showing the clean part of its texture (KwRudderWindows).
        private static Mesh Made(KwLook look, MeshBuilder builder, string name)
        {
            Mesh mesh = builder.ToMesh(name);
            KwRudderWindows.Fit(mesh, look);
            return mesh;
        }
    }
}
