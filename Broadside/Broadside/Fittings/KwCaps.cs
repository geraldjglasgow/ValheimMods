using System;
using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// Carved caps for the top of a stem or stern post, in the keel's biome tier (<see cref="KwTierCap"/>): five carved heads
    /// on long necks from the workshop's <c>kw_postheads</c> bundle (a dragon after the longships' prows, a serpent, a wolf,
    /// a raven and a horse, <see cref="KwCapHeads"/>), then the code's own: a scroll rolling outward, a slender tail curling
    /// back over the post, a turned finial and a plain sloped cap board (<see cref="Styles"/>, in the order the Post cap
    /// tool's wheel steps through them). The Post cap tool builds them as generated parts sized to their post
    /// (<see cref="KwCapTool"/>, <see cref="KwCapSpec"/>, <see cref="KwCapMesh"/>). The five hammer pieces placed before it
    /// (<see cref="Prefix"/>: scroll, dragon, tail, finial, plain) are no longer in the hammer; their prefabs stay, so caps
    /// already built load and save (the old dragon wears the carved dragon head): each snaps by its "Cap foot" onto a post's
    /// "Post top", its origin the middle of the post's top face, its foot (0.32 by 0.42 m) covering the standard post's top.
    /// A cap's outward side, where a curl rolls and a head looks, is +x. Kit axes: x forward, y left, z up, metres.
    /// </summary>
    public static class KwCaps
    {
        public const string Prefix = "KW_cap_";

        /// <summary>Half the foot's size at its underside on the standard post: it covers a post 0.3 m deep and 0.4 m wide.</summary>
        public const float FootHalfX = 0.16f, FootHalfY = 0.21f;

        /// <summary>The styles, in the wheel's order: the carved heads first.</summary>
        public static readonly KwCapStyle[] Styles =
        {
            Head("dragon", "Dragon head"),
            Head("serpent", "Serpent head"),
            Head("wolf", "Wolf head"),
            Head("raven", "Raven head"),
            Head("horse", "Horse head"),
            new KwCapStyle("scroll", "Scroll", KwCapCurls.Scroll, KwCapCurls.ScrollFoot, stem: true),
            new KwCapStyle("tail", "Tail curl", KwCapCurls.Tail, KwCapCurls.TailFoot, stem: true),
            new KwCapStyle("finial", "Finial", KwCapSmall.Finial, KwCapSmall.FinialFoot),
            new KwCapStyle("plain", "Plain", KwCapSmall.Plain, KwCapSmall.Board, flat: true),
        };

        // The styles the hammer had before the Post cap tool: their prefabs stay so caps already built load.
        private static readonly string[] Hammered = { "scroll", "dragon", "tail", "finial", "plain" };

        /// <summary>The old hammer pieces: kept registered, never in the hammer (<see cref="KwHammer"/>).</summary>
        public static IEnumerable<KwDef> All()
        {
            foreach (string id in Hammered)
                yield return Def(Find(id)!);
        }

        /// <summary>A style by its id; null for none.</summary>
        public static KwCapStyle? Find(string id) => Array.Find(Styles, s => s.Id == id);

        /// <summary>A style's place in the wheel's order; -1 for none.</summary>
        public static int IndexOf(string id) => Array.FindIndex(Styles, s => s.Id == id);

        /// <summary>Whether a prefab is a post cap: one the Post cap tool built, or an old hammer piece.</summary>
        public static bool Is(string prefab) => prefab == KwCapSpec.Made || prefab.StartsWith(Prefix, StringComparison.Ordinal);

        /// <summary>
        /// A cap's mesh drawn by the code into a builder: its style's shapes carried through its form and turned to face
        /// inboard when it does, on the keel texture's carving swatch; a style with a stem makes the stem longer or shorter
        /// and bends it by the lean itself, its curl kept (<see cref="KwCapPen"/>), the others are stretched. A carved head
        /// draws here only where its model is not loaded (a scroll in its place, <see cref="KwCapHeads"/>).
        /// </summary>
        public static void Draw(MeshBuilder b, KwCapSpec spec)
        {
            KwCapStyle style = Find(spec.Style) ?? Styles[0];
            KwCapForm form = new KwCapForm(spec.Width, spec.Depth, style.Stem ? 1f : spec.Length, style.Foot, style.Flat);
            Func<Vector3, Vector3> map = spec.Inboard ? p => Turned(form.Map(p)) : form.Map;
            style.Shape(new KwCapPen(b, map, style.Stem ? spec.Length : 1f, style.Leans ? spec.Lean : 0f));
            b.MapUv(KwKeelUv.Carving);
        }

        // A point turned half round about the post's middle, for a cap that looks inboard (kit axes).
        private static Vector3 Turned(Vector3 p) => new Vector3(-p.x, -p.y, p.z);

        // A carved head: drawn as a scroll where its model is not loaded (a dedicated server; a missing bundle).
        private static KwCapStyle Head(string id, string name) =>
            new KwCapStyle(id, name, KwCapCurls.Scroll, KwCapCurls.ScrollFoot, stem: true, head: KwCapHeads.Prefix + id);

        // An old hammer piece: the style at its standard size, its planar UVs moved onto the keel texture's carving swatch
        // (KwKeelUv), so a cap wears its keel's tier; the old dragon wears the carved head where it is loaded.
        private static KwDef Def(KwCapStyle style)
        {
            Action<MeshBuilder> carved = b =>
            {
                style.Shape(new KwCapPen(b));
                b.MapUv(KwKeelUv.Carving);
            };
            KwDef def = new KwDef(Prefix + style.Id, "Post cap, " + style.Name.ToLowerInvariant(), "fitting") { Shape = carved, Wood = true, Setup = Describe };
            if (style.Head != null)
                def.Model = (scene, piece) => KwCapHeads.Model(piece, new KwCapSpec(style.Id, KwCapForm.PostWidth, KwCapForm.PostDepth, 1f));
            def.Snaps.Add(("Cap foot", Vector3.zero));
            return def;
        }

        private static void Describe(GameObject prefab)
        {
            prefab.GetComponent<Piece>().m_description = "A carved cap for a stem or stern post, carved from what its keel is made of. The Post cap tool changes it.";
            prefab.AddComponent<KwTierCap>();
        }

        /// <summary>The block a cap stands on: the whole foot at z = 0, narrowing to half topX by topY at its top.</summary>
        public static void Foot(KwCapPen pen, float height, float topX, float topY) => pen.Hexa(new[]
        {
            new Vector3(-FootHalfX, -FootHalfY, 0f), new Vector3(FootHalfX, -FootHalfY, 0f), new Vector3(FootHalfX, FootHalfY, 0f), new Vector3(-FootHalfX, FootHalfY, 0f),
            new Vector3(-topX, -topY, height), new Vector3(topX, -topY, height), new Vector3(topX, topY, height), new Vector3(-topX, topY, height),
        });
    }

    /// <summary>
    /// One post cap style: its saved id, its name, its shapes (drawn for the standard post) and the height of its foot,
    /// where the carving over it starts (<see cref="KwCapForm"/>); a flat style (the plain cap) is a board on the post; a
    /// style with a stem (a scroll's, a tail's) makes it longer and leans it itself (<see cref="KwCapPen.Length"/>,
    /// <see cref="KwCapPen.Lean"/>); a carved head (<see cref="Head"/>, its prefab in the <c>kw_postheads</c> bundle) is
    /// fitted to its post from its model (<see cref="KwCapWarp"/>), its shapes only where that is not loaded.
    /// </summary>
    public sealed class KwCapStyle
    {
        public KwCapStyle(string id, string name, Action<KwCapPen> shape, float foot, bool flat = false, bool stem = false, string? head = null)
        {
            Id = id;
            Name = name;
            Shape = shape;
            Foot = foot;
            Flat = flat;
            Stem = stem;
            Head = head;
        }

        public string Id { get; }
        public string Name { get; }
        public Action<KwCapPen> Shape { get; }
        public float Foot { get; }
        public bool Flat { get; }

        /// <summary>The carved head's prefab in the bundle ("kw_head_dragon"); null for a style the code draws.</summary>
        public string? Head { get; }

        public bool Stem { get; }

        /// <summary>Whether its neck leans (<see cref="KwCapSpec.Lean"/>): the carved heads, the scroll and the tail curl.</summary>
        public bool Leans => Stem;
    }
}
