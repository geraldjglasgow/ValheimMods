using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// A door leaf's iron (<see cref="KwDoorMesh"/>), in the door space (<see cref="KwDoorSpace"/>): a hinge knuckle on the
    /// hinge line at the foot ledge's and the top ledge's heights; on the outer face, by its tier's style
    /// (<see cref="KwDoorStyle"/>), two short strap hinges with spear ends (plain), three long nailed straps (strapped), or
    /// three bands across, stopping short of the free edge (so a double door's two leaves read apart), studded along them,
    /// with a stud on every board between them (banded); and a ring pull on each face near the free edge: inside on the
    /// middle ledge, outside just under the middle strap or band, on the wood.
    /// </summary>
    public static class KwDoorIron
    {
        /// <summary>An iron strap's thickness, a strap's and a band's width, a knuckle's radius and length.</summary>
        private const float Iron = 0.008f, Strap = 0.055f, Band = 0.075f, Knuckle = 0.036f, KnuckleLong = 0.14f;

        /// <summary>A stud's foot and top, how far it stands; studs' spacing along a strap or band.</summary>
        private const float Stud = 0.02f, StudTop = 0.011f, StudHigh = 0.009f, Spacing = 0.13f;

        /// <summary>How far the ring pull is in from the free edge, its plate's radius and thickness, the ring's radius and its bar's half width.</summary>
        private const float PullIn = 0.13f, Plate = 0.055f, PlateThick = 0.007f, Ring = 0.06f, Bar = 0.008f;

        /// <summary>How far under the middle ledge's line the outer ring pull's plate sits; how far short of the free edge a band stops.</summary>
        private const float PullDrop = 0.12f, Short = 0.035f;

        private const int Sides = 8, RingSides = 14;

        /// <summary>The iron of a leaf by its style.</summary>
        public static void Draw(MeshBuilder b, KwDoorSpace door, KwDoorStyle style)
        {
            float[] at = KwDoorMesh.Heights(door.High);
            Knuckles(b, door, at[0]);
            Knuckles(b, door, at[2]);
            if (style == KwDoorStyle.Banded)
                Bands(b, door, at);
            else
                Straps(b, door, at, style == KwDoorStyle.Strapped);
            Pulls(b, door, at[1]);
        }

        // A hinge's knuckle: a short round barrel on the hinge line, standing a little proud of both faces.
        private static void Knuckles(MeshBuilder b, KwDoorSpace door, float h)
        {
            List<Vector3> ring = new List<Vector3>();
            for (int i = 0; i < Sides; i++)
            {
                float t = (i + 0.5f) * 2f * Mathf.PI / Sides;
                ring.Add(new Vector3(Mathf.Cos(t) * Knuckle, h - KnuckleLong / 2f, Mathf.Sin(t) * Knuckle));
            }
            door.Prism(b, ring, new Vector3(0f, KnuckleLong, 0f), Vector3.up, Vector2.zero);
        }

        // Strap hinges from the hinge edge at the foot and top ledges' heights: short ones on a plain door; long nailed ones
        // and a third across the middle on a strapped one.
        private static void Straps(MeshBuilder b, KwDoorSpace door, float[] at, bool strapped)
        {
            float reach = door.Wide * (strapped ? 0.86f : 0.45f);
            Hinge(b, door, at[0], reach, strapped);
            Hinge(b, door, at[2], reach, strapped);
            if (strapped)
                Hinge(b, door, at[1], reach, true);
        }

        // One strap from the hinge edge, its far end a spear point, nailed along it (only near the hinge on a plain door).
        private static void Hinge(MeshBuilder b, KwDoorSpace door, float h, float reach, bool nailed)
        {
            float d = door.Half, w = Strap / 2f, tip = Mathf.Min(Strap * 1.2f, reach * 0.3f);
            door.Box(b, new Vector3(0f, h - w, d), new Vector3(reach - tip, h + w, d + Iron), Vector3.right, Vector2.zero);
            List<Vector3> head = new List<Vector3> { new Vector3(reach - tip, h - w * 1.5f, d), new Vector3(reach, h, d), new Vector3(reach - tip, h + w * 1.5f, d) };
            door.Prism(b, head, new Vector3(0f, 0f, Iron), Vector3.right, Vector2.zero);
            float last = nailed ? reach - tip : Mathf.Min(reach - tip, 0.2f);
            for (float a = 0.07f; a <= last + 1e-4f; a += Spacing)
                Studs(b, door, a, h, d + Iron);
        }

        // Three bands right across the outer face at the ledges' heights, studded along them, and a stud on every board
        // halfway between each two bands.
        private static void Bands(MeshBuilder b, KwDoorSpace door, float[] at)
        {
            float d = door.Half, w = Mathf.Min(Band, door.High / 10f) / 2f;
            foreach (float h in at)
            {
                door.Box(b, new Vector3(0f, h - w, d), new Vector3(door.Wide - Short, h + w, d + Iron), Vector3.right, Vector2.zero);
                int count = Mathf.Max(2, Mathf.RoundToInt((door.Wide - 0.15f) / Spacing) + 1);
                for (int i = 0; i < count; i++)
                    Studs(b, door, Mathf.Lerp(0.06f, door.Wide - Short - 0.04f, i / (count - 1f)), h, d + Iron);
            }
            foreach (float a in KwDoorMesh.Middles(door.Wide))
            {
                Studs(b, door, a, (at[0] + at[1]) / 2f, d);
                Studs(b, door, a, (at[1] + at[2]) / 2f, d);
            }
        }

        // A square stud standing out of a face at a depth (way 1 the outer face, -1 the inner), its top smaller.
        private static void Studs(MeshBuilder b, KwDoorSpace door, float a, float h, float d, float way = 1f)
        {
            List<Vector3> foot = Square(a, h, d, Stud / 2f), top = Square(a, h, d + way * StudHigh, StudTop / 2f);
            door.Solid(b, foot, top, Vector3.right, Vector2.zero);
        }

        private static List<Vector3> Square(float a, float h, float d, float r) => new List<Vector3>
        {
            new Vector3(a - r, h - r, d), new Vector3(a + r, h - r, d), new Vector3(a + r, h + r, d), new Vector3(a - r, h + r, d),
        };

        // A ring pull on each face near the free edge: inside on the middle ledge, outside on the wood just under the middle
        // strap or band.
        private static void Pulls(MeshBuilder b, KwDoorSpace door, float h)
        {
            float a = Mathf.Max(door.Wide - PullIn, door.Wide / 2f);
            Pull(b, door, a, h - PullDrop, door.Half, 1f);
            Pull(b, door, a, h, -door.Half - KwDoorMesh.LedgeThick, -1f);
        }

        // A round plate on a face (way 1 the outer, -1 the inner) and a ring hanging from its middle, lying against it.
        private static void Pull(MeshBuilder b, KwDoorSpace door, float a, float h, float d, float way)
        {
            door.Prism(b, KwDoorSpace.Circle(a, h, d, Plate, Sides), new Vector3(0f, 0f, way * PlateThick), Vector3.right, Vector2.zero);
            Studs(b, door, a, h, d + way * PlateThick, way);
            Loop(b, door, new Vector3(a, h - Ring, d + way * (PlateThick + Bar)));
        }

        // The ring: a square bar bent round in the face's plane.
        private static void Loop(MeshBuilder b, KwDoorSpace door, Vector3 centre)
        {
            List<Vector3>? last = null;
            for (int i = 0; i <= RingSides; i++)
            {
                float t = i * 2f * Mathf.PI / RingSides;
                Vector3 radial = new Vector3(Mathf.Cos(t), Mathf.Sin(t), 0f), at = centre + radial * Ring, deep = new Vector3(0f, 0f, Bar);
                List<Vector3> square = new List<Vector3> { at - radial * Bar - deep, at + radial * Bar - deep, at + radial * Bar + deep, at - radial * Bar + deep };
                for (int k = 0; last != null && k < 4; k++)
                    door.Face(b, new List<Vector3> { last[k], last[(k + 1) % 4], square[(k + 1) % 4], square[k] }, Vector3.right, Vector2.zero);
                last = square;
            }
        }
    }
}
