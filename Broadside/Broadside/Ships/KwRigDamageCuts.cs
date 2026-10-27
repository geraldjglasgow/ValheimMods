using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// The marks a shot leaves on a launched ship's rig (<see cref="KwRigMark"/>), written on the ship's owner through
    /// <see cref="KwRigDamage.Edit"/>: a notch cut into a spar, deepening one already there on the same side (within
    /// <see cref="Merge"/> degrees and overlapping); a mast, bowsprit or post cap fallen once more than half its section is
    /// gone at one place, toward the way the shot flew (<see cref="KwRigSection.Breaks"/>); a hole in a sail (one the same
    /// shot already made, through the router and the shooter's game both, counted once); a chip off a cap; a chunk off a
    /// rudder's blade. A fallen piece takes no more marks.
    /// </summary>
    public static class KwRigDamageCuts
    {
        private const float Graze = 0.02f, Deepest = 0.98f, Merge = 70f, Again = 1.5f, Apart = 0.4f;
        private const int MostHoles = 30, MostChips = 12, MostChunks = 16;
        private static readonly List<(int Ship, int Piece, int Sail, Vector2 Metres, float At)> recent = new List<(int, int, int, Vector2, float)>();
        private static readonly System.Random seeds = new System.Random();

        /// <summary>A hole's radius in a sail for a shot's damage: the ball's bite and the cloth torn round it.</summary>
        public static float HoleRadius(float damage) => 1.6f * KwRigDamageTakers.Bite(damage) + 0.04f;

        /// <summary>
        /// A notch cut into a spar by a shot met at a point flying a way (the piece's kit axes), its place and length along
        /// the spar stretched (a gaff's, kept as set full); the energy it took.
        /// </summary>
        public static float Notch(KwRigDamage damage, KwRigPiece piece, KwRigSpar spar, Vector3 kit, Vector3 way, KwShipHit hit, float stretch = 1f)
        {
            (float s, float angle, float off) = spar.Shot(kit, way);
            float r = Mathf.Max(spar.Radius(s), 0.01f), b = KwRigDamageTakers.Bite(hit.Damage), area = Mathf.PI * r * r;
            float cut = Mathf.Min(KwRigSection.Band(off, b, r), hit.Energy / (area * KwRigDamageTakers.Wood), Deepest);
            if (cut < Graze)
                return Mathf.Min(hit.Energy, KwRigDamageTakers.GrazeTakes);
            float heading = Heading(damage, hit.Direction);
            KwRigMark notch = new KwRigMark(KwRigMark.Notch, piece.Index, spar.Part, s * stretch, angle, cut, b * stretch, Seed());
            damage.Edit(marks => Cut(marks, piece, notch, heading));
            return Mathf.Min(hit.Energy, cut * area * KwRigDamageTakers.Wood);
        }

        // The notch added, or deepening one on the same side that it overlaps; then whether its piece falls.
        private static bool Cut(List<KwRigMark> marks, KwRigPiece piece, KwRigMark add, float heading)
        {
            if (marks.Exists(m => m.Type == KwRigMark.Fall && m.Piece == piece.Index))
                return false;
            int near = marks.FindIndex(m => m.On(KwRigMark.Notch, add.Piece, add.Part) && Mathf.Abs(m.A - add.A) < (m.D + add.D) * 0.8f
                && Mathf.Abs(Mathf.DeltaAngle(m.B, add.B)) < Merge);
            if (near >= 0)
                marks[near] = Deeper(marks[near], add);
            else
                marks.Add(add);
            if (add.Part == KwRigSpar.Column && piece.Kind != KwRigPiece.RudderKind)
                Falls(marks, piece, heading);
            return true;
        }

        // Two notches on one side as one: their cuts added (never quite all), its middle between theirs by their cuts, a little longer.
        private static KwRigMark Deeper(KwRigMark was, KwRigMark add)
        {
            float both = Mathf.Max(was.C + add.C, 1e-4f);
            float at = (was.A * was.C + add.A * add.C) / both;
            return was.With(at, was.B, Mathf.Min(Deepest, was.C + add.C), Mathf.Max(was.D, add.D) + 0.2f * Mathf.Min(was.D, add.D));
        }

        // A mast, bowsprit or cap whose notches take more than half its section at one place falls there.
        private static void Falls(List<KwRigMark> marks, KwRigPiece piece, float heading)
        {
            List<KwRigMark> cuts = marks.FindAll(m => m.On(KwRigMark.Notch, piece.Index, KwRigSpar.Column));
            (float at, float lost) = KwRigSection.Worst(cuts);
            if (lost > KwRigSection.Breaks)
                marks.Add(new KwRigMark(KwRigMark.Fall, piece.Index, KwRigSpar.Column, at, heading, lost, 0f, Seed()));
        }

        /// <summary>A hole in a sail at a place in its cloth's texture, of a radius (m); one the same shot made just now is not made twice.</summary>
        public static void Hole(KwRigDamage damage, int piece, int sail, Vector2 uv, float radius)
        {
            KwSailLayout? s = damage.Parts.Get(piece) is KwRigPiece mast && sail >= 0 && sail < mast.Sails.Count ? mast.Sails[sail] : null;
            if (s == null || Recent(damage, piece, sail, new Vector2(uv.x * s.Width, uv.y * s.Height)))
                return;
            KwRigMark hole = new KwRigMark(KwRigMark.Hole, piece, sail, uv.x, uv.y, radius, 0f, Seed());
            damage.Edit(marks =>
            {
                if (marks.Exists(m => m.Type == KwRigMark.Fall && m.Piece == piece))
                    return false;
                Limit(marks, m => m.On(KwRigMark.Hole, piece, sail), MostHoles);
                marks.Add(hole);
                return true;
            });
        }

        // Whether a hole this near was made in this sail in the last moment (the same shot through the router and the shooter's game).
        private static bool Recent(KwRigDamage damage, int piece, int sail, Vector2 metres)
        {
            int ship = damage.GetInstanceID();
            recent.RemoveAll(r => Time.time - r.At > Again);
            if (recent.Exists(r => r.Ship == ship && r.Piece == piece && r.Sail == sail && (r.Metres - metres).magnitude < Apart))
                return true;
            recent.Add((ship, piece, sail, metres, Time.time));
            return false;
        }

        /// <summary>A chip off a post cap where a shot struck it (its kit axes); low on it, its base cut too. The energy its base took.</summary>
        public static float Chip(KwRigDamage damage, KwRigPiece cap, Vector3 kit, Vector3 way, KwShipHit hit)
        {
            float b = KwRigDamageTakers.Bite(hit.Damage), heading = Heading(damage, hit.Direction);
            Vector3 at = kit + way * (0.3f * b);
            KwRigMark chip = new KwRigMark(KwRigMark.Chip, cap.Index, -1, at.x, at.y, at.z, 1.3f * b, Seed());
            (float s, float angle, float off) = cap.Main.Shot(kit, way);
            float r = Mathf.Max(cap.Main.Radius(s), 0.01f), area = Mathf.PI * r * r;
            float cut = s <= Base(cap) ? Mathf.Min(KwRigSection.Band(off, b, r), hit.Energy / (area * KwRigDamageTakers.Wood), Deepest) : 0f;
            KwRigMark notch = new KwRigMark(KwRigMark.Notch, cap.Index, KwRigSpar.Column, s, angle, cut, b, Seed());
            damage.Edit(marks =>
            {
                Limit(marks, m => m.On(KwRigMark.Chip, cap.Index, -1), MostChips);
                marks.Add(chip);
                if (cut >= Graze)
                    Cut(marks, cap, notch, heading);
                return true;
            });
            return cut * area * KwRigDamageTakers.Wood;
        }

        /// <summary>How high on a post cap a shot still cuts at its base: the lower part of it.</summary>
        public static float Base(KwRigPiece cap) => Mathf.Max(0.35f, 0.4f * cap.Main.Length);

        /// <summary>A chunk off a rudder's blade where a shot struck it (the blade's kit axes); at its stock with a heavy blow, all of it.</summary>
        public static void Chunk(KwRigDamage damage, KwRigPiece rudder, Vector3 kit, float blow, bool heavy)
        {
            float b = KwRigDamageTakers.Bite(blow), stock = rudder.Tier!.Stock(rudder.Fit);
            bool whole = heavy && Mathf.Abs(kit.x) < stock + 0.12f;
            KwRigMark chunk = new KwRigMark(KwRigMark.Chunk, rudder.Index, -1, kit.z, Mathf.Min(kit.x + b, -stock - 0.02f), 1.5f * b, whole ? 1f : 0f, Seed());
            damage.Edit(marks =>
            {
                Limit(marks, m => m.On(KwRigMark.Chunk, rudder.Index, -1), MostChunks);
                marks.Add(chunk);
                return true;
            });
        }

        // The oldest of some marks let go while there are as many as are kept.
        private static void Limit(List<KwRigMark> marks, System.Predicate<KwRigMark> which, int most)
        {
            while (marks.FindAll(which).Count >= most)
                marks.RemoveAt(marks.FindIndex(which));
        }

        // A shot's heading in the ship's frame, degrees round its up from its bow: the way a piece it fells falls.
        private static float Heading(KwRigDamage damage, Vector3 way)
        {
            Vector3 local = damage.Frame.InverseTransformDirection(way);
            return Mathf.Atan2(local.x, local.z) * Mathf.Rad2Deg;
        }

        // A mark's own seed (its outline's and splinters' pattern), the same for every peer once written.
        private static int Seed() => seeds.Next(1, int.MaxValue);
    }
}
