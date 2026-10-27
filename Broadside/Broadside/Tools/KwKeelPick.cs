using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>Where the keel tool's window builds a keel, the sizes it opens with, and its title.</summary>
    public sealed class KwKeelStart
    {
        public KwKeelStart(KwSite site, KwKeelSpec spec, string title, bool exact = false) => (Site, Spec, Title, Exact) = (site, spec, title, exact);

        public KwSite Site { get; }
        public KwKeelSpec Spec { get; }
        public string Title { get; }

        /// <summary>Sizes measured to meet another keel exactly (its fields step by a centimetre, not the usual half metre).</summary>
        public bool Exact { get; }
    }

    /// <summary>
    /// The keel tool's clicks. A click near a keel's open end picks it (for half a minute): a click near another keel's then
    /// joins the two, a new keel from the lower of their nearest open ends to the other, climbing to it by a curve (quick or
    /// oval, never a flat stern); a click near the same keel's end carries it on from that end. A click elsewhere on a keel
    /// edits it instead (<see cref="KwKeelEdit"/>, which forgets the keel picked). A click on a keel block (a rest) that a
    /// keel already starts from builds its twin: the same keel running the other way from the same start, so a ship's two
    /// ends match. Any other rest starts a new keel there, as before. The player is told what to do whenever nothing opens.
    /// </summary>
    public static class KwKeelPick
    {
        /// <summary>What the keel tool's click on nothing it takes says.</summary>
        public const string Hint = "Click a keel block on the dock or near a keel's open end; elsewhere on a keel changes it";

        private const float Forget = 30f, SameStart = 0.15f;
        private static Piece? picked;
        private static Vector3 pickedAt;
        private static float pickedTime;

        /// <summary>The keel the click starts, or null (the player told what next) while it waits for a second keel or finds nothing.</summary>
        public static KwKeelStart? Click(Player player, KwClick aim, KwKeelSpec last)
        {
            if (KwKeel.IsKeel(aim.Piece))
                return OnKeel(player, aim, last);
            picked = null;
            KwSite? rest = KwShapeSites.Keel(aim);
            if (rest == null)
            {
                Say(player, Hint);
                return null;
            }
            return Twin(player, rest) ?? new KwKeelStart(rest, last, "New keel");
        }

        // First keel: picked. Second: the same one carries on, another one is joined to it.
        private static KwKeelStart? OnKeel(Player player, KwClick aim, KwKeelSpec last)
        {
            Piece? first = Picked();
            if (first == null)
            {
                (picked, pickedAt, pickedTime) = (aim.Piece, aim.Point, Time.time);
                aim.Piece.GetComponent<WearNTear>()?.Highlight();
                Say(player, "Keel picked: click near another keel's open end to join them, or near this keel's end again to carry it on");
                return null;
            }
            picked = null;
            if (first != aim.Piece)
                return Join(player, first, pickedAt, aim.Piece, last);
            KwSite? end = KwShapeSites.Keel(aim);
            if (end == null)
                Say(player, "This keel has no open end to carry on from");
            return end == null ? null : new KwKeelStart(end, With(last, end.Body), "New keel");
        }

        private static Piece? Picked() => picked != null && Time.time - pickedTime < Forget ? picked : null;

        /// <summary>The keel picked is forgotten (a keel was clicked to edit it).</summary>
        public static void Drop() => picked = null;

        // A keel from the lower of the two keels' nearest open ends to the other's, climbing to it, as thick as the first.
        private static KwKeelStart? Join(Player player, Piece a, Vector3 near, Piece b, KwKeelSpec last)
        {
            Transform? ja = KwShapeSites.Joint(a, near), jb = ja != null ? KwShapeSites.Joint(b, ja.position) : null;
            if (ja == null || jb == null)
            {
                Say(player, "Both keels need an open end to join");
                return null;
            }
            (Vector3 from, Vector3 to) = ja.position.y <= jb.position.y + 0.01f ? (ja.position, jb.position) : (jb.position, ja.position);
            Vector3 run = new Vector3(to.x - from.x, 0f, to.z - from.z);
            float length = run.magnitude, rise = Mathf.Max(0f, to.y - from.y);
            if (length < KwKeelSpec.MinLength || length > KwKeelSpec.MaxLength || rise > KwKeelSpec.MaxRise)
            {
                Say(player, $"Those ends are {length:0.#} m apart and {rise:0.#} m apart in height: a keel joins ends {KwKeelSpec.MinLength:0} to {KwKeelSpec.MaxLength:0} m apart, up to {KwKeelSpec.MaxRise:0} m higher");
                return null;
            }
            KwKeelBody body = KwKeel.BodyOf(a);
            KwSite site = new KwSite(from, Quaternion.LookRotation(run / length), false, body);
            KwKeelCurve curve = last.Flat ? KwKeelCurve.Quick : last.Curve;
            return new KwKeelStart(site, new KwKeelSpec(length, rise, curve, false, last.PostAbove, body), "Join keels", true);
        }

        // The made keel that starts on this rest, mirrored: the same keel the other way from the same start. Null when none
        // starts here; told, and null too, when keels already run both ways from it.
        private static KwKeelStart? Twin(Player player, KwSite rest)
        {
            List<Piece> starting = Starting(rest.Point);
            if (starting.Count != 1)
            {
                if (starting.Count > 1)
                    Say(player, "This keel block already has a keel each way");
                return null;
            }
            Piece keel = starting[0];
            ZDO? zdo = keel.GetComponent<ZNetView>()?.GetZDO();
            if (zdo == null || !KwKeelSpec.TryParse(zdo.GetString(KwGenerated.Key), out KwKeelSpec spec))
                return null;
            Vector3 back = -keel.transform.forward;
            back.y = 0f;
            return new KwKeelStart(new KwSite(rest.Point, Quaternion.LookRotation(back.normalized), true, spec.Body), spec, "Mirrored keel");
        }

        // The made keels whose start sits on this point.
        private static List<Piece> Starting(Vector3 point)
        {
            List<Piece> near = new List<Piece>(), starting = new List<Piece>();
            Piece.GetAllPiecesInRadius(point, 1f, near);
            foreach (Piece piece in near)
            {
                if (Utils.GetPrefabName(piece.gameObject) == KwKeel.MadeName && (piece.transform.position - point).magnitude < SameStart)
                    starting.Add(piece);
            }
            return starting;
        }

        private static KwKeelSpec With(KwKeelSpec spec, KwKeelBody body) => new KwKeelSpec(spec.Length, spec.Rise, spec.Curve, spec.Post, spec.PostAbove, body);

        private static void Say(Player player, string text) => player.Message(MessageHud.MessageType.Center, text);
    }
}
