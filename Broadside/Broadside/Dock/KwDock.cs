using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// The dry dock set, built on land in the game's plank wood: the keel block guide (a 4 m timber laid flush on the
    /// ground, marked every metre with a seat a keel block snaps onto; guides snap end to end, and a line of them with
    /// their blocks is a dry dock for a ship of any width) and the keel block (a notched cap on a crib, on a sill board; a
    /// row of loose blocks is a dock too). Kit axes; each piece's origin is on the ground at its aft end's centre (a
    /// block's at its foot). Nothing on the dock moves: it is for designing.
    /// </summary>
    public static class KwDock
    {
        public const float CradleTop = 0.9f, GuideLength = 4f;

        // A keel block's cap: 1.2 m across, 0.3 m high, notched 0.32 m wide and 0.1 m deep for the keel. It stands on a crib
        // of stacked timbers (KwSupport) whose top is CapLift up by default, so the notch's floor is CradleTop up.
        public const float CapHeight = 0.3f, NotchDepth = 0.1f, CapLift = CradleTop - (CapHeight - NotchDepth);
        private static readonly Vector2[] CapOutline =
        {
            new Vector2(-0.6f, 0f), new Vector2(0.6f, 0f), new Vector2(0.6f, CapHeight), new Vector2(0.16f, CapHeight),
            new Vector2(0.16f, CapHeight - NotchDepth), new Vector2(-0.16f, CapHeight - NotchDepth),
            new Vector2(-0.16f, CapHeight), new Vector2(-0.6f, CapHeight),
        };

        // declared after the cap above: static fields start in the order they are written
        public static readonly KwDef Cradle = LooseBlock();
        public static readonly KwDef Guide = GuideDef();

        public static IEnumerable<KwDef> All => new[] { Guide, Cradle };

        // A keel block: a sill board on the ground and a capped crib, its cap a part of its own so it can fit the keel
        // resting on it (KwSupport).
        private static KwDef LooseBlock()
        {
            KwDef def = Def("KW_keelcradle", "Keel block", SillShape,
                new List<(string, Vector3)> { ("Cradle foot", Vector3.zero), ("Keel rest", new Vector3(0f, 0f, CradleTop)) });
            def.Parts.Add(("support", new Vector3(0f, 0f, CapLift), CapShape));
            return def;
        }

        // The guide: its two ends (guides join there, lined up, KwGuideSnap) and a block seat every metre, on the ground
        // between its rails, where a keel block's foot goes, so the keel rests at the blocks' height.
        private static KwDef GuideDef()
        {
            List<(string, Vector3)> snaps = new List<(string, Vector3)> { ("Guide end", Vector3.zero), ("Guide end", new Vector3(GuideLength, 0f, 0f)) };
            for (float x = 0.5f; x < GuideLength; x += 1f)
                snaps.Add(("Block seat", new Vector3(x, 0f, 0f)));
            return Def("KW_dockguide", $"Keel block guide ({GuideLength:0} m)", GuideShape, snaps);
        }

        // A heavy timber sole along the middle, two raised side rails outside the keel blocks' sills (the blocks stand
        // between them, their seats on the ground as before, so a saved ship sits on them as it always did), cross ties
        // at the ends and the middle between the blocks, and a chevron on the sole's fore end showing which way the bow is.
        private static void GuideShape(MeshBuilder b)
        {
            Solid.Box(b, new Vector3(0f, -0.15f, 0f), new Vector3(GuideLength, 0.15f, 0.08f));
            foreach (float side in new[] { 1f, -1f })
                Solid.Box(b, new Vector3(0f, side * RailIn, 0f), new Vector3(GuideLength, side * (RailIn + RailWidth), RailHeight));
            foreach (float x in new[] { 0.06f, GuideLength / 2f, GuideLength - 0.06f })
                Solid.Box(b, new Vector3(x - 0.06f, -(RailIn + RailWidth), 0f), new Vector3(x + 0.06f, RailIn + RailWidth, 0.12f));
            foreach (float side in new[] { 1f, -1f })
                Solid.Box(b, new Vector3(GuideLength - 0.45f, side * 0.04f, 0.08f), new Vector3(GuideLength - 0.12f, side * 0.04f + side * 0.11f, 0.11f));
        }

        // The side rails: from just outside a keel block's sill (0.7 m off the middle), this wide and high.
        private const float RailIn = 0.78f, RailWidth = 0.25f, RailHeight = 0.3f;

        private static KwDef Def(string name, string label, System.Action<MeshBuilder> shape, List<(string, Vector3)> snaps)
        {
            KwDef def = new KwDef(name, label, "dock") { Shape = shape, Wood = true };
            def.Snaps.AddRange(snaps);
            return def;
        }

        private static void CapShape(MeshBuilder b) => Solid.Prism(b, CapOutline, -0.25f, 0.25f);

        private static void SillShape(MeshBuilder b) => Solid.Box(b, new Vector3(-0.35f, -0.7f, 0f), new Vector3(0.35f, 0.7f, 0.06f));

        /// <summary>A crib of four crossed timber layers, one metre high (kit z -1 to 0, under a cap), stretched to fit by KwSupport.</summary>
        public static void CribShape(MeshBuilder b)
        {
            for (int layer = 0; layer < 4; layer++)
            {
                float z0 = -1f + layer * 0.25f, z1 = z0 + 0.25f;
                foreach (float s in new[] { -1f, 1f })
                {
                    if (layer % 2 == 0)
                        Solid.Box(b, new Vector3(-0.25f, s * 0.42f - 0.1f, z0), new Vector3(0.25f, s * 0.42f + 0.1f, z1));
                    else
                        Solid.Box(b, new Vector3(s * 0.15f - 0.08f, -0.55f, z0), new Vector3(s * 0.15f + 0.08f, 0.55f, z1));
                }
            }
        }
    }
}
