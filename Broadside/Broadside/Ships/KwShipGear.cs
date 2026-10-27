using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// A launched ship's rudder built with its steering gear (<see cref="KwRudderGear"/>), on every peer that draws. With a
    /// captain's wheel in the blueprint, the gear the rudder was saved with on the dock (its lay and its shafts), or, when
    /// it was saved without one (an old rope gear's text reads as none, or its lay does not match it), one laid out from the
    /// blueprint now (<see cref="KwRudderGearFind"/>, the same on every peer from the same blueprint). Linked, the rudder
    /// carries the gear's lay (no deck tiller, the quadrant on its stock), its shafts are drawn turning with the ship's
    /// helm (<see cref="KwRudderGearLook"/>) and the helm goes to the wheel (<see cref="KwShipWheel.TakesHelm"/>); refused,
    /// the rudder keeps its deck tiller and the wheel's hover says why.
    /// Without a wheel the rudder is built with the lay it was saved with, as before; a rudder saved with none (or with a
    /// gear's, its wheel gone) is linked to the first deck forward of its post (<see cref="KwShipTiller.Lay"/>).
    /// </summary>
    public static class KwShipGear
    {
        /// <summary>A ship's rudder copy built to its post, linked to its helm, its gear drawn when it has one.</summary>
        public static void Refit(Transform ship, Transform rudder, KwBlueprint blueprint, KwBpPiece piece, KwRudderFit fit)
        {
            KwRudderGear? gear = Gear(blueprint, piece, fit, out KwRudderLay? lay);
            KwRudder.Refit(rudder.gameObject, fit, lay ?? KwShipTiller.Lay(ship, rudder, fit));
            KwRudderGearLook.Set(rudder.gameObject, gear);
        }

        // The gear for the ship's rudder (null without a wheel) and the lay it is built with (null: none to use, the ship
        // finds it a deck tiller).
        private static KwRudderGear? Gear(KwBlueprint blueprint, KwBpPiece piece, KwRudderFit fit, out KwRudderLay? lay)
        {
            KwRudderLay? saved = KwRudderLay.Of(piece);
            bool geared = saved != null && saved.Value.Geared;
            lay = geared ? null : saved;
            if (!blueprint.Pieces.Exists(p => KwWheel.Is(p.Prefab)))
                return null;
            KwRudderGear? gear = KwRudderGear.Of(piece);
            if (gear != null && gear.Linked == geared)
            {
                lay = saved;
                return gear;
            }
            KwRudderTier tier = KwRudderTiers.Of(piece.Prefab) ?? KwRudderTiers.All[0];
            gear = KwRudderGearFind.Find(new KwGearShip(blueprint.Pieces, piece), tier, fit, out KwRudderLay found);
            if (gear != null && gear.Linked)
                lay = found;
            return gear;
        }
    }
}
