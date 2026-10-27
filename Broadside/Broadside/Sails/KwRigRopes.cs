using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>One rope of a mast's rigging (<see cref="KwRigRopes"/>): its name, width and points (the piece's kit), and what holds its foot.</summary>
    public sealed class KwRigRope
    {
        public KwRigRope(string name, float width, List<Vector3> points, KwRigTie? tie) => (Name, Width, Points, Tie) = (name, width, points, tie);

        public string Name { get; }
        public float Width { get; }
        public List<Vector3> Points { get; }
        public KwRigTie? Tie { get; }
    }

    /// <summary>
    /// A rigged piece's ship (<see cref="KwRigShip"/>, null where nothing is drawn) and the places its ropes tie on, each worked
    /// out once by its rope's name (<see cref="KwRigTies"/>), so a sheet that follows its sail up and down keeps its foot.
    /// </summary>
    public sealed class KwRigPlan
    {
        private readonly Dictionary<string, KwRigTie> ties = new Dictionary<string, KwRigTie>();
        private readonly List<Vector3> taken = new List<Vector3>();

        public KwRigPlan(KwRigShip? ship, List<KwSailLayout>? sails)
        {
            Ship = ship;
            if (ship != null && sails != null)
                ship.Own(sails);
        }

        public KwRigShip? Ship { get; }

        /// <summary>Every tie worked out so far, each drawn as its fitting.</summary>
        public IEnumerable<KwRigTie> Ties => ties.Values;

        /// <summary>A rope's tie to the ship's side its aim is on (worked out the first time); null with no ship.</summary>
        public KwRigTie? Side(string name, Vector3 from, Vector3 aim, KwTieKind kind) =>
            Ship == null ? null : Keep(name, () => Took(KwRigTies.Side(Ship, from, aim, kind, taken)));

        // A tie's foot kept, so the next rope's keeps away from it.
        private KwRigTie Took(KwRigTie tie)
        {
            taken.Add(tie.End);
            return tie;
        }

        /// <summary>A rope's tie to the deck or roof under its aim; null with no ship.</summary>
        public KwRigTie? Down(string name, Vector3 from, Vector3 aim) => Ship == null ? null : Keep(name, () => KwRigTies.Down(Ship, from, aim));

        /// <summary>A rope's tie to a cleat on the mast at its end; null with no ship.</summary>
        public KwRigTie? Mast(string name, Vector3 end, float radius) => Ship == null ? null : Keep(name, () => KwRigTie.OnMast(end, radius, Ship.Tier));

        // A rope's tie, worked out the first time; a throw is reported once and leaves the rope loose.
        private KwRigTie? Keep(string name, System.Func<KwRigTie> make)
        {
            if (ties.TryGetValue(name, out KwRigTie tie))
                return tie;
            try
            {
                return ties[name] = make();
            }
            catch (System.Exception e)
            {
                if (!warned)
                    Log.Warn($"Broadside: the rope {name} could not be tied on to its ship: {e}");
                warned = true;
                return null;
            }
        }

        private static bool warned;
    }

    /// <summary>
    /// A mast's ropes (<see cref="KwSailRigging"/> draws them), in its kit axes. Standing rigging from the masthead: a shroud to
    /// each side a little abaft the mast, made fast with deadeyes; a forestay forward to the deck on a mast with no square
    /// sail (on one with, it would cut through the sails' bellies). Running rigging: the highest square sail's halyard over
    /// the masthead block and down the mast (down its side past yards below) to a cleat there; each yard's braces from its
    /// ends aft to the ship's sides, the higher ones further aft; each square sail's sheets from its clews, the course's aft
    /// to the sides, a higher sail's to the ends of the yard below; a sail along the ship's halyard, its boom's sheet to the
    /// deck under the boom's end, its topping lift or its gaff's peak halyard. Every rope to the ship ties on as
    /// <see cref="KwRigTies"/> says (<see cref="KwRigPlan"/>), its fitting at its foot. A long rope sags a little.
    /// </summary>
    public static class KwRigRopes
    {
        private static readonly float[] Sides = { -1f, 1f };

        /// <summary>The ropes fixed to the mast and its yards.</summary>
        public static List<KwRigRope> Fixed(KwRigPlan plan, List<KwSailLayout> sails)
        {
            List<KwRigRope> ropes = new List<KwRigRope>();
            KwSailLayout m = sails[0];
            Vector3 head = new Vector3(0f, 0f, m.Mast - 0.32f);
            foreach (float side in Sides)
                ropes.Add(ToSide(plan, "shroud_" + Name(side), KwSailRigging.Stout, head + new Vector3(0f, side * m.Radius, 0f), new Vector3(-Abaft(m), side * Wide(m), 0f), KwTieKind.Deadeye));
            int squares = sails.FindAll(s => s.Square).Count;
            if (squares == 0)
                ropes.Add(ToDeck(plan, "forestay", KwSailRigging.Stout, head + new Vector3(m.Radius, 0f, 0f), new Vector3(0.4f * m.Mast, 0f, 0f)));
            for (int i = 0; i < sails.Count; i++)
            {
                if (!sails[i].Square)
                    Along(plan, ropes, sails[i], i);
                else
                    Square(plan, ropes, sails[i], i, i == squares - 1, squares > 1);
            }
            return ropes;
        }

        // A yard's braces; the highest yard's halyard too, down the mast's front (or its side, past the yards below).
        private static void Square(KwRigPlan plan, List<KwRigRope> ropes, KwSailLayout s, int i, bool highest, bool stacked)
        {
            if (highest)
            {
                Vector3 side = new Vector3(0f, s.Radius + 0.03f, 0f), round = new Vector3(0.71f, 0.71f, 0f) * (s.Radius + 0.07f);
                List<Vector3> fall = stacked
                    ? new List<Vector3> { round + new Vector3(0f, 0f, s.Block.z), side + new Vector3(0f, 0f, s.Block.z), side + new Vector3(0f, 0f, 0.3f) }
                    : new List<Vector3> { new Vector3(s.Block.x, 0f, 0.3f) };
                fall.InsertRange(0, new[] { new Vector3(s.YardX, 0f, s.At + s.Spar), s.Block });
                ropes.Add(Belayed(plan, "halyard", fall, s.Radius));
            }
            foreach (float side in Sides)
            {
                Vector3 end = new Vector3(s.YardX, side * (s.Width / 2f + 0.15f), s.At);
                Vector3 aim = new Vector3(-0.3f * s.Mast - 0.4f * s.Spec.Tier, side * Wide(s), 0f);
                ropes.Add(ToSide(plan, $"brace{i}_{Name(side)}", KwSailRigging.Thin, end, aim, KwTieKind.Cleat));
            }
        }

        private static void Along(KwRigPlan plan, List<KwRigRope> ropes, KwSailLayout s, int i)
        {
            Vector3 boomEnd = new Vector3(-s.Width - 0.1f, 0f, s.At);
            ropes.Add(ToDeck(plan, "sheet_boom" + i, KwSailRigging.Thin, boomEnd, new Vector3(-s.Width * 0.85f, 0f, 0f)));
            if (s.Spec.Shape == 1)
                ropes.Add(new KwRigRope("lift", KwSailRigging.Thin, KwSailRigging.Sag(boomEnd, new Vector3(-s.Radius, 0f, s.Block.z + 0.1f)), null));
        }

        /// <summary>
        /// The ropes on what moves of sail i, at hoist f: a square sail's sheets from its clews, the course's to the ship's
        /// sides (tied on as the sail is fully set), a higher one's to the ends of the yard below; a sail along the ship's
        /// halyard from its head over its block and down the mast beside its boom to a cleat, and a gaff's peak halyard
        /// (taut, over the gaff).
        /// </summary>
        public static List<KwRigRope> Moving(KwRigPlan plan, List<KwSailLayout> sails, int i, float f)
        {
            KwSailLayout s = sails[i];
            Vector3 origin = s.ClothOrigin;
            List<KwRigRope> ropes = new List<KwRigRope>();
            if (s.Square)
            {
                Vector3[] clews = KwSailCanvas.Clews(s, f), set = f >= 1f ? clews : KwSailCanvas.Clews(s, 1f);
                for (int c = 0; c < clews.Length; c++)
                    ropes.Add(Sheet(plan, sails, i, c, origin + clews[c], origin + set[c]));
                return ropes;
            }
            (Vector3 head, Vector3 peak) = KwSailCanvas.Top(s, f);
            Vector3 aside = new Vector3(s.Block.x, s.Spar + 0.05f, 0f);
            ropes.Add(Belayed(plan, "halyard" + i, new List<Vector3> { origin + head, s.Block, aside + new Vector3(0f, 0f, s.Block.z), aside + new Vector3(0f, 0f, 0.3f) }, s.Radius));
            if (s.Spec.Shape == 2)
                ropes.Add(new KwRigRope("peak" + i, KwSailRigging.Thin, new List<Vector3> { origin + peak + new Vector3(-0.1f, 0f, 0.04f), new Vector3(-s.Radius, 0f, s.Block.z + 0.1f) }, null));
            return ropes;
        }

        // A square sail's sheet from a clew: the course's to the ship aft of the mast (tied on from where the clew is with the
        // sail set), a higher sail's out to the end of the yard below it (whose sail is the next one down the stack).
        private static KwRigRope Sheet(KwRigPlan plan, List<KwSailLayout> sails, int i, int c, Vector3 clew, Vector3 set)
        {
            KwSailLayout s = sails[i];
            string name = $"sheet{i}_{c}";
            float side = Mathf.Sign(set.y);
            if (s.Spec.Tier == 0 || i == 0)
            {
                Vector3 aim = new Vector3(-Mathf.Max(0.8f, 0.15f * s.Mast), side * Wide(s), 0f);
                KwRigTie? tie = plan.Side(name, set, aim, KwTieKind.Cleat);
                return new KwRigRope(name, KwSailRigging.Thin, KwSailRigging.Sag(clew, tie?.End ?? Loose(aim)), tie);
            }
            KwSailLayout below = sails[i - 1];
            return new KwRigRope(name, KwSailRigging.Thin, KwSailRigging.Sag(clew, new Vector3(below.YardX, side * below.Width / 2f, below.At + below.Spar * 0.8f)), null);
        }

        /// <summary>Every rope of the mast with its sails fully set (a saved ship's ghost, the sail tool's preview).</summary>
        public static List<KwRigRope> All(KwRigPlan plan, List<KwSailLayout> sails)
        {
            List<KwRigRope> ropes = Fixed(plan, sails);
            for (int i = 0; i < sails.Count; i++)
                ropes.AddRange(Moving(plan, sails, i, 1f));
            return ropes;
        }

        // A halyard down the mast to a cleat on it, its last point at the cleat's horns.
        private static KwRigRope Belayed(KwRigPlan plan, string name, List<Vector3> fall, float radius)
        {
            KwRigTie? tie = plan.Mast(name, fall[fall.Count - 1], radius);
            if (tie != null)
                fall[fall.Count - 1] = tie.End;
            return new KwRigRope(name, KwSailRigging.Thin, fall, tie);
        }

        private static KwRigRope ToSide(KwRigPlan plan, string name, float width, Vector3 from, Vector3 aim, KwTieKind kind)
        {
            KwRigTie? tie = plan.Side(name, from, aim, kind);
            return new KwRigRope(name, width, KwSailRigging.Sag(from, tie?.End ?? Loose(aim)), tie);
        }

        private static KwRigRope ToDeck(KwRigPlan plan, string name, float width, Vector3 from, Vector3 aim)
        {
            KwRigTie? tie = plan.Down(name, from, aim);
            return new KwRigRope(name, width, KwSailRigging.Sag(from, tie?.End ?? Loose(aim)), tie);
        }

        // With no ship to tie to (nothing is drawn): the aim, a little over the deck.
        private static Vector3 Loose(Vector3 aim) => new Vector3(aim.x, aim.y, 0.18f);

        private static string Name(float side) => side < 0f ? "r" : "l";

        // How far abaft the mast its shrouds come down: a little, more on a taller mast.
        private static float Abaft(KwSailLayout m) => Mathf.Clamp(0.08f * m.Mast, 0.4f, 1.6f);

        // How far out to each side the ropes aim: wide, past any hull's side (only the side counts).
        private static float Wide(KwSailLayout s) => Mathf.Max(s.Side * 1.8f, s.Width * 0.6f, 2.5f);
    }
}
