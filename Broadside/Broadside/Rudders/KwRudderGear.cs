using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace Broadside
{
    /// <summary>Why a captain's wheel does not steer its ship's rudder (<see cref="KwRudderGear.Why"/>); None when it does. 5 was an old refusal, no longer made.</summary>
    public enum KwGearWhy
    {
        None = 0,
        Far = 1,
        Deck = 2,
        Hull = 3,
        Stern = 4,
        Route = 6,
        Low = 7,
        Climb = 8,
        Ceiling = 9,
    }

    /// <summary>
    /// A rudder's steering gear to a captain's wheel (<see cref="KwRudderGearFind"/>), saved by the rudder's owner in the
    /// rudder's ZDO (<see cref="Key"/>) beside its lay (<see cref="KwRudderLay"/>, of the gear's kind), so it travels in a
    /// blueprint and every peer draws it from there (<see cref="KwRudderGearLook"/>): the wheel it turns with, the line
    /// shaft's corners from the pinion over the rudder's quadrant to the bevel box under the wheel (<see cref="KwGearTrain"/>),
    /// and its hangers, each a point on the shaft and the deck's (or a cabin roof's) top over it; for a wheel on a cabin's
    /// roof the shaft climbs the cabin's back wall (<see cref="KwGearClimb"/>), held to it by brackets. Or why the wheel near
    /// it does not steer it, the rudder then keeping its deck tiller. All in the rudder's kit frame (x forward from the post's
    /// after face, y left, z up from the rudder point). Its text is "shaft|wheel|corners|hangers", with "|brackets" after it
    /// when it climbs (a corner "x,y,z", a hanger "x,y,z,top", a bracket "x,y,z,dx,dy", each list joined by ";"), or
    /// "nogear|wheel|why|metres". The rope gear's old "gear|..." text reads as none, so it is measured again.
    /// </summary>
    public sealed class KwRudderGear
    {
        public const string Key = "kw_rudder_gear";
        public static readonly int Hash = Key.GetStableHashCode();
        private const string Tag = "shaft", Refusal = "nogear";

        private KwRudderGear(Vector3 wheel, Vector3[] corners, Vector4[] hangers, KwGearBracket[] brackets, KwGearWhy why, float far) =>
            (Wheel, Corners, Hangers, Brackets, Why, Far) = (wheel, corners, hangers, brackets, why, far);

        /// <summary>The wheel's place (the foot of its stand), in the rudder's kit frame.</summary>
        public Vector3 Wheel { get; }

        /// <summary>The line shaft's corners: the pinion's middle first, the bevel box under the wheel last.</summary>
        public Vector3[] Corners { get; }

        /// <summary>The hangers: a point on the shaft (x, y, z) and the deck's top over it (w).</summary>
        public Vector4[] Hangers { get; }

        /// <summary>The brackets holding its climbing stretches to a cabin's walls (none for a wheel on a deck).</summary>
        public KwGearBracket[] Brackets { get; }

        /// <summary>Whether its shaft climbs a cabin's wall to a wheel on the cabin's roof.</summary>
        public bool Climbs
        {
            get
            {
                for (int i = 0; i + 1 < Corners.Length; i++)
                {
                    Vector3 run = Corners[i + 1] - Corners[i];
                    if (run.z > 0.3f && new Vector2(run.x, run.y).magnitude < 0.01f)
                        return true;
                }
                return false;
            }
        }

        public KwGearWhy Why { get; }

        /// <summary>How far from the rudder the wheel stands, when it is refused as too far.</summary>
        public float Far { get; }

        /// <summary>Whether the wheel steers the rudder through the gear.</summary>
        public bool Linked => Why == KwGearWhy.None;

        /// <summary>How long its shafts are, the short one up into the wheel's stand included.</summary>
        public float Length
        {
            get
            {
                float length = Corners.Length > 0 ? Mathf.Max(0f, Wheel.z - Corners[Corners.Length - 1].z) : 0f;
                for (int i = 0; i + 1 < Corners.Length; i++)
                    length += (Corners[i + 1] - Corners[i]).magnitude;
                return length;
            }
        }

        public static KwRudderGear Shafted(Vector3 wheel, Vector3[] corners, Vector4[] hangers, KwGearBracket[]? brackets = null) =>
            new KwRudderGear(wheel, corners, hangers, brackets ?? new KwGearBracket[0], KwGearWhy.None, 0f);

        public static KwRudderGear Refused(Vector3 wheel, KwGearWhy why, float far = 0f) =>
            new KwRudderGear(wheel, new Vector3[0], new Vector4[0], new KwGearBracket[0], why, far);

        /// <summary>Why the wheel does not steer the rudder, in words for its hover; empty when it does.</summary>
        public string Words => Why switch
        {
            KwGearWhy.Far => $"The rudder is {Far:0} m away; a wheel steers one at most {KwRudderGearFind.Reach:0} m off.",
            KwGearWhy.Deck => "Stand it on a deck or a cabin's roof to link it to the rudder.",
            KwGearWhy.Hull => "It does not stand inside the rudder's hull.",
            KwGearWhy.Stern => "Its deck does not reach back to the stern post, where the steering shaft comes through.",
            KwGearWhy.Route => "No clear way under its deck for the steering shaft: a hatch, a ladder, a mast or a cabin is in the way.",
            KwGearWhy.Low => "Its deck is too low over the rudder's blade for the quadrant on the rudder's stock.",
            KwGearWhy.Climb => "No clear way up inside the cabin for the steering shaft: doorways, windows, corner posts, ladders, masts or fittings take its back wall and its sides near the back.",
            KwGearWhy.Ceiling => "No clear way under the cabin's roof to the wheel for the steering shaft: a wall, a post, a doorway, a ladder or a mast is in the way, or the wheel stands too near the roof's edge.",
            _ => "",
        };

        public override string ToString() => !Linked
            ? string.Join("|", Refusal, P(Wheel), ((int)Why).ToString(CultureInfo.InvariantCulture), F(Far))
            : string.Join("|", Tag, P(Wheel), Join(Corners, P), Join(Hangers, H)) + (Brackets.Length > 0 ? "|" + Join(Brackets, b => b.ToString()) : "");

        /// <summary>A gear's text read back; null for none, an old rope gear's, or one that does not read.</summary>
        public static KwRudderGear? Parse(string? text)
        {
            string[] p = (text ?? "").Split('|');
            if ((p.Length != 4 && (p.Length != 5 || p[0] != Tag)) || !Point(p[1], out Vector3 wheel))
                return null;
            if (p[0] == Refusal)
                return int.TryParse(p[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out int why) && Known(why)
                    && Num(p[3], out float far) ? Refused(wheel, (KwGearWhy)why, far) : null;
            Vector3[]? corners = p[0] == Tag ? Points(p[2]) : null;
            Vector4[]? hangers = corners != null ? Hangs(p[3]) : null;
            KwGearBracket[]? brackets = hangers != null ? KwGearBracket.Read(p.Length == 5 ? p[4] : "") : null;
            return corners != null && hangers != null && brackets != null ? Shafted(wheel, corners, hangers, brackets) : null;
        }

        /// <summary>The gear a placed rudder's ZDO holds; null without one.</summary>
        public static KwRudderGear? Of(ZDO? zdo) => zdo != null ? Parse(zdo.GetString(Hash)) : null;

        /// <summary>The gear a blueprint's rudder was saved with; null without one.</summary>
        public static KwRudderGear? Of(KwBpPiece piece) => Parse(KwBpData.Text(piece.Data, Hash));

        /// <summary>Writes a gear (none: an empty text) into a rudder's ZDO when it differs from what is there (only its owner calls this).</summary>
        public static void Save(ZDO zdo, KwRudderGear? gear)
        {
            string text = gear?.ToString() ?? "";
            if (zdo.GetString(Hash) != text)
                zdo.Set(Hash, text);
        }

        /// <summary>Whether two gears (either none) are the same, as their texts are.</summary>
        public static bool Same(KwRudderGear? a, KwRudderGear? b) => (a?.ToString() ?? "") == (b?.ToString() ?? "");

        /// <summary>
        /// A saved rudder's cost from the hammer: its biome's recipe, and with a steering gear its shafts' timber, its
        /// recipe's first item a metre per two (at least 2), and their fittings, its second a metre per five (at least 1).
        /// </summary>
        public static List<(string Item, int Amount)> Cost(KwRudderTier tier, KwBpPiece piece)
        {
            List<(string Item, int Amount)> cost = new List<(string Item, int Amount)>(tier.Recipe);
            KwRudderGear? gear = Of(piece);
            if (gear == null || !gear.Linked || tier.Recipe.Length < 2)
                return cost;
            float length = gear.Length;
            KwBpCost.Add(cost, tier.Recipe[0].Item, Mathf.Max(2, Mathf.CeilToInt(length / 2f)));
            KwBpCost.Add(cost, tier.Recipe[1].Item, Mathf.Max(1, Mathf.CeilToInt(length / 5f)));
            return cost;
        }

        private static bool Known(int why) => why > 0 && Enum.IsDefined(typeof(KwGearWhy), why);

        private static string Join<T>(T[] items, Func<T, string> one)
        {
            List<string> each = new List<string>(items.Length);
            foreach (T item in items)
                each.Add(one(item));
            return string.Join(";", each);
        }

        // The shaft's corners, at least the pinion and the box under the wheel.
        private static Vector3[]? Points(string text)
        {
            string[] each = text.Split(';');
            Vector3[] points = new Vector3[each.Length];
            for (int i = 0; i < each.Length; i++)
            {
                if (!Point(each[i], out points[i]))
                    return null;
            }
            return points.Length >= 2 ? points : null;
        }

        // The hangers (none is fine).
        private static Vector4[]? Hangs(string text)
        {
            if (text.Length == 0)
                return new Vector4[0];
            string[] each = text.Split(';');
            Vector4[] hangers = new Vector4[each.Length];
            for (int i = 0; i < each.Length; i++)
            {
                string[] c = each[i].Split(',');
                if (c.Length != 4 || !Point(string.Join(",", c[0], c[1], c[2]), out Vector3 at) || !Num(c[3], out float top))
                    return null;
                hangers[i] = new Vector4(at.x, at.y, at.z, top);
            }
            return hangers;
        }

        private static string P(Vector3 v) => string.Join(",", F(v.x), F(v.y), F(v.z));

        private static string H(Vector4 v) => string.Join(",", F(v.x), F(v.y), F(v.z), F(v.w));

        private static bool Point(string text, out Vector3 point)
        {
            string[] c = text.Split(',');
            point = Vector3.zero;
            if (c.Length != 3 || !Num(c[0], out float x) || !Num(c[1], out float y) || !Num(c[2], out float z))
                return false;
            point = new Vector3(x, y, z);
            return true;
        }

        private static string F(float value) => value.ToString("0.###", CultureInfo.InvariantCulture);

        private static bool Num(string text, out float value) =>
            float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value) && !float.IsNaN(value) && !float.IsInfinity(value)
            && Mathf.Abs(value) < 1000f;
    }
}
