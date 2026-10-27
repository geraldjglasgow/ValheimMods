using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// Where a mast's platforms (<see cref="KwMastTops"/>) go clear of its sails, for the sail window: the height a crow's
    /// nest is first offered at (just over the highest yard or sail head, or 2 m over a fighting top), the height a fighting
    /// top is (just over the course's yard; on a mast with no square sail three fifths of its height), and the note the
    /// window shows when one is set among the sails. A crow's nest goes all round its mast, so every sail counts; a fighting
    /// top lies to one side of it (<see cref="KwMastTops.TopWay"/>), so only the sail along the ship counts behind it.
    /// Nothing is moved: the player's heights stand.
    /// </summary>
    public static class KwMastTopFit
    {
        private const float Over = 0.12f;

        /// <summary>The height a crow's nest is offered at on this mast.</summary>
        public static float Nest(KwMastSpec mast)
        {
            float at = 0f;
            foreach (KwSailLayout s in KwSailLayout.Of(mast))
                at = Mathf.Max(at, High(s) + Over);
            if (mast.Tops.HasTop)
                at = Mathf.Max(at, mast.Tops.TopAt + 2f);
            return Mathf.Clamp(Snap(at), KwMastTops.MinAt, mast.Mast - 0.15f);
        }

        /// <summary>The height a fighting top is offered at on this mast: over the course's yard.</summary>
        public static float Top(KwMastSpec mast)
        {
            if (mast.Squares == 0)
                return Mathf.Clamp(Snap(0.6f * mast.Mast), KwMastTops.MinAt, mast.Mast - 0.6f);
            KwSailLayout course = new KwSailLayout(mast.Sails[0]);
            return Mathf.Clamp(Snap(course.At + course.Spar + Over), KwMastTops.MinAt, mast.Mast - 0.6f);
        }

        /// <summary>What the window says of the platforms: one set among the sails, and where it would clear them; empty when none is.</summary>
        public static string Note(KwMastSpec mast)
        {
            KwMastTops tops = mast.Tops;
            string said = "";
            if (tops.Nest && Among(mast, tops.NestAt - 0.1f, tops.NestAt + KwMastTops.NestRail, false))
                said = $"The crow's nest is among the sails: {Nest(mast):0.##} m clears them.";
            if (tops.HasTop && Among(mast, tops.TopAt - 0.15f, tops.TopAt + KwMastTops.TopRail, true))
                said += (said.Length > 0 ? " " : "") + $"The fighting top is in the sail along the ship: {Top(mast):0.##} m clears it.";
            return said;
        }

        // Whether a sail stands between the two heights: any sail round the mast, or only the one along the ship behind it.
        private static bool Among(KwMastSpec mast, float low, float high, bool alongOnly)
        {
            foreach (KwSailLayout s in KwSailLayout.Of(mast))
            {
                if (alongOnly && (s.Square || KwMastTops.TopWay(mast) > 0f))
                    continue;
                if (high > Low(s) && low < High(s))
                    return true;
            }
            return false;
        }

        // A sail's lowest and highest points: a square sail from its foot to its yard's top, one along the ship from its boom's
        // underside to its head.
        private static float Low(KwSailLayout s) => s.Square ? s.At - s.Spar - s.Height : s.At - s.Spar;

        private static float High(KwSailLayout s) => s.Square ? s.At + s.Spar : s.Spec.Top + 0.1f;

        // Up to the window's step of a quarter metre.
        private static float Snap(float at) => Mathf.Ceil(at * 4f - 0.001f) / 4f;
    }
}
