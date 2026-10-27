using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// Openings through a skin of strakes (<see cref="KwStrakes"/>): a planking part's, from its span's openings segment
    /// (<see cref="KwSpan.Openings"/>), and a cabin's raised sides: the openings that fit each side given to the part's
    /// definition (<see cref="KwDef.Openings"/>), the openings themselves (with their tunnels) to its measures
    /// (<see cref="KwGirth.Holes"/>), and each strake's board, or a painted band over it, cut round them along their tunnels
    /// (<see cref="KwOpeningRing"/>) as it is drawn. Pure.
    /// </summary>
    public static class KwOpeningSkin
    {
        /// <summary>A planking definition given the openings of its saved segment that fit its sides.</summary>
        public static KwDef Cut(KwDef skin, string segment)
        {
            List<KwOpening> openings = KwOpeningText.Read(segment);
            if (openings.Count == 0 || skin.Stations.Count < 2)
                return skin;
            foreach (KwOpeningWall wall in KwOpeningSurfaces.Planking(skin.Stations, openings))
                skin.Openings.AddRange(wall.Openings);
            return skin;
        }

        /// <summary>Some openings (fitted to their walls, with their tunnels) on the port side (planking's or a raised left side's) and on the starboard.</summary>
        public static (List<KwOpening>? Port, List<KwOpening>? Starboard) Holes(List<KwOpening> openings)
        {
            List<KwOpening>? port = null, starboard = null;
            foreach (KwOpening o in openings)
            {
                if (o.OnPort)
                    (port ??= new List<KwOpening>()).Add(o);
                else
                    (starboard ??= new List<KwOpening>()).Add(o);
            }
            return (port, starboard);
        }

        /// <summary>A saved openings segment for a part made again longer (along) or scaled (s: heights and sizes too); "" stays "".</summary>
        public static string Scaled(string segment, float along, float s) =>
            segment.Length == 0 ? segment : KwOpeningText.Write(KwOpeningText.Read(segment).ConvertAll(o => o.Scaled(along, s)));

        /// <summary>
        /// A strake's piece between two stations with the openings on its side cut out of it, as convex pieces, each cut
        /// running straight through the board along its opening's tunnel (<see cref="KwOpeningRing"/>).
        /// </summary>
        public static List<List<KwGirthPoint>> Apart(KwGirth girth, float side, List<KwGirthPoint> piece) =>
            KwOpeningRing.Apart(piece, girth.HolesOn(side));
    }
}
