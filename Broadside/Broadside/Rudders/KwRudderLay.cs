using System.Globalization;
using UnityEngine;

namespace Broadside
{
    /// <summary>How a rudder is steered from its ship (<see cref="KwRudderLay"/>).</summary>
    public enum KwRudderHelm
    {
        /// <summary>No deck before the post found yet: the tiller over the post's top.</summary>
        None,

        /// <summary>A tiller from the rudder's head over the post's top and forward to the helmsman's hand.</summary>
        Tiller,

        /// <summary>A yoke across the rudder's head, two lines or rods forward round the post to a deck tiller on its own post.</summary>
        Lines,

        /// <summary>A yoke across the head of a capped post with no deck before it yet: its lines not rigged.</summary>
        Yoke,

        /// <summary>
        /// Steered from a captain's wheel through the steering gear (<see cref="KwRudderGear"/>): no tiller or yoke, a
        /// toothed quadrant on the stock under the stern's deck (<see cref="Deck"/>), turned by a line shaft from the wheel.
        /// </summary>
        Gear,
    }

    /// <summary>
    /// How a rudder is linked to where its helmsman stands, picked from the stern it hangs on (<see cref="KwRudderLayFind"/>):
    /// its kind (<see cref="KwRudderHelm"/>), how high over the post's top its head sits (<see cref="Lift"/>), where the
    /// helm stands on the deck (<see cref="Reach"/> forward, the deck's top <see cref="Deck"/>) and how long the yokes'
    /// arms are (<see cref="Half"/>). The helmsman stands on the deck facing the bow, his right hand on the grip at
    /// <see cref="GripHeight"/> over it (a peg hangs from it to his idle hand); with a captain's wheel on the ship, the
    /// steering gear turns the rudder instead (<see cref="KwRudderHelm.Gear"/>: its <see cref="Deck"/> the stern's deck the
    /// line shaft hangs under, <see cref="KwGearTrain"/>) and the helmsman stands at the wheel. The rudder's owner saves it in the rudder's
    /// ZDO (<see cref="Key"/>), so it travels in a blueprint too, and every peer draws it from there. Kit axes of the
    /// piece: x forward from the post's after face, y left, z up from the rudder point.
    /// </summary>
    public readonly struct KwRudderLay
    {
        public const string Key = "kw_rudder_lay";
        private const string Tag = "lay";
        private static readonly int KeyHash = Key.GetStableHashCode();

        /// <summary>The grip over the deck; the idle hand's height; the deck tiller's handle; how far to port of the grip the helmsman stands.</summary>
        public const float GripHeight = 1f, Hand = 0.88f, Handle = 0.6f, Side = 0.3f;

        /// <summary>A bare post's look: the tiller over its top, nothing on the deck.</summary>
        public static readonly KwRudderLay Plain = new KwRudderLay(KwRudderHelm.None, 0.02f, 0f, 0f, 0f);

        public KwRudderLay(KwRudderHelm kind, float lift, float reach, float deck, float half)
        {
            Kind = kind;
            Lift = lift;
            Reach = reach;
            Deck = deck;
            Half = half;
        }

        public KwRudderHelm Kind { get; }

        /// <summary>How high over the post's top the rudder's head starts (with the steering gear, at least just over it).</summary>
        public float Lift { get; }

        /// <summary>A tiller's grip, or the deck tiller's pivot post, this far forward of the post's after face.</summary>
        public float Reach { get; }

        /// <summary>The deck's top where the helm stands (the steering gear's: over the shaft at the stern post), over the rudder point (negative under it).</summary>
        public float Deck { get; }

        /// <summary>Half the length of the yoke across the rudder's head, and of the deck tiller's.</summary>
        public float Half { get; }

        /// <summary>Whether a helmsman can stand at it: a tiller, or lines to a deck tiller.</summary>
        public bool Steered => Kind == KwRudderHelm.Tiller || Kind == KwRudderHelm.Lines;

        /// <summary>Whether the rudder's head carries a yoke rather than a tiller.</summary>
        public bool Yoked => Kind == KwRudderHelm.Lines || Kind == KwRudderHelm.Yoke;

        /// <summary>Whether it is steered from a captain's wheel through the steering gear under the deck.</summary>
        public bool Geared => Kind == KwRudderHelm.Gear;

        /// <summary>The deck tiller's pivot, at its bar's height over the deck.</summary>
        public Vector3 Pivot => new Vector3(Reach, 0f, Deck + GripHeight);

        /// <summary>Where the helmsman's hand holds it with the helm amidships: the tiller's end, or the deck tiller's.</summary>
        public Vector3 Grip => Kind == KwRudderHelm.Lines ? Pivot + new Vector3(Handle, 0f, 0f) : new Vector3(Reach, 0f, Deck + GripHeight);

        /// <summary>The helmsman's feet with the helm amidships: on the deck, to port of the grip.</summary>
        public Vector3 Feet => new Vector3(Grip.x, Side, Deck);

        public bool Same(KwRudderLay other) => ToString() == other.ToString();

        public override string ToString() =>
            string.Join("|", Tag, ((int)Kind).ToString(CultureInfo.InvariantCulture), F(Lift), F(Reach), F(Deck), F(Half));

        private static string F(float value) => value.ToString("0.###", CultureInfo.InvariantCulture);

        public static bool TryParse(string? text, out KwRudderLay lay)
        {
            lay = Plain;
            string[] p = (text ?? "").Split('|');
            if (p.Length != 6 || p[0] != Tag || !int.TryParse(p[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out int kind)
                || kind < 0 || kind > (int)KwRudderHelm.Gear)
                return false;
            if (!Num(p[2], out float lift) || !Num(p[3], out float reach) || !Num(p[4], out float deck) || !Num(p[5], out float half))
                return false;
            // an old steering gear's yoke sat below the post's top, under a deck lower than it
            float lowest = kind == (int)KwRudderHelm.Gear ? -KwKeelSpec.MaxPost : 0f;
            lay = new KwRudderLay((KwRudderHelm)kind, Mathf.Clamp(lift, lowest, 3f), reach, deck, Mathf.Clamp(half, 0f, 1.5f));
            return true;
        }

        /// <summary>The lay a placed rudder's ZDO holds; null without one.</summary>
        public static KwRudderLay? Of(ZDO? zdo) => zdo != null && TryParse(zdo.GetString(KeyHash), out KwRudderLay lay) ? lay : (KwRudderLay?)null;

        /// <summary>The lay a blueprint's rudder was saved with; null without one.</summary>
        public static KwRudderLay? Of(KwBpPiece piece) => TryParse(KwBpData.Text(piece.Data, KeyHash), out KwRudderLay lay) ? lay : (KwRudderLay?)null;

        /// <summary>Writes the lay into a rudder's ZDO when it differs from what is there (only its owner calls this).</summary>
        public void Save(ZDO zdo)
        {
            string text = ToString();
            if (zdo.GetString(KeyHash) != text)
                zdo.Set(KeyHash, text);
        }

        private static bool Num(string text, out float value) =>
            float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value) && !float.IsNaN(value) && !float.IsInfinity(value);
    }
}
