using System;

namespace Broadside
{
    /// <summary>
    /// Which hammer pieces are built on a launched ship and ride it (<see cref="KwShipRider"/>). First (2026-10-26) any
    /// piece of the game's or another mod's ("I want a way to build things like cannons and the weapon mounts on the ship
    /// after it is built ... I really want the keel guide and keel blocks to create the entirety of the ship"); since the
    /// ship cannons came into Broadside the same day, only those (<see cref="KwArmDef.CannonPrefix"/>; the user's words:
    /// "We also need to not allow building with the vanilla Valheim pieces on the ship, but you should be allowed to build
    /// cannons on your ship"). Any other piece aimed at a launched ship is refused there (<see cref="Refused"/>: its ghost
    /// stands on the deck, red, and placing it says <see cref="OnlyCannons"/>): the game's own and every other mod's,
    /// but not Broadside's own dock pieces (its tools work at sea their own way, <see cref="KwSeaTools"/>) nor the hoe's
    /// and cultivator's ground work, which never stand on a ship. Pieces already riding a ship stay: the rule is for
    /// building only.
    /// </summary>
    public static class KwShipRiderRules
    {
        /// <summary>How long a parked rider's owner waits for its ship to be anywhere near (with the area loaded) before the piece is made ordinary.</summary>
        public const float Patience = 60f;

        /// <summary>What placing a refused piece on a launched ship says.</summary>
        public const string OnlyCannons = "Only ship cannons are built on a launched ship";

        /// <summary>The rule as plain data, for offline checks: Broadside's own ship cannons, nothing else.</summary>
        public static bool Rides(string prefab, bool waterPiece, bool groundPiece, bool movesItself) =>
            prefab.StartsWith(KwArmDef.CannonPrefix, StringComparison.Ordinal) && !waterPiece && !groundPiece && !movesItself;

        /// <summary>Whether a piece (its prefab or its placement ghost) may be built on a launched ship.</summary>
        public static bool Rides(Piece piece) =>
            Rides(Utils.GetPrefabName(piece.gameObject), piece.m_waterPiece, Ground(piece), Moves(piece));

        /// <summary>The refusal as plain data: any piece but Broadside's own and the ground's that does not ride.</summary>
        public static bool Refused(string prefab, bool groundPiece, bool rides) =>
            !rides && !groundPiece && !prefab.StartsWith("KW_", StringComparison.Ordinal);

        /// <summary>Whether a piece aimed at a launched ship is refused there, its ghost standing on the deck to say so.</summary>
        public static bool Refused(Piece piece) => Refused(Utils.GetPrefabName(piece.gameObject), Ground(piece), Rides(piece));

        /// <summary>Whether the placement ray takes a launched ship for this piece: one that rides, or one refused there.</summary>
        public static bool Aimed(Piece piece) => Rides(piece) || Refused(piece);

        /// <summary>
        /// Whether a parked rider (its ship not in this game's scene and no ZDO of its ship near) is made an ordinary piece
        /// by its owner: its ship destroyed (seen by this game), or missing for <see cref="Patience"/> seconds with the area
        /// round the piece loaded. A rider aboard its ship, or near its ship's ZDO, never is.
        /// </summary>
        public static bool Released(bool shipDestroyed, float missingFor, bool areaReady) => shipDestroyed || missingFor > Patience && areaReady;

        private static bool Ground(Piece piece) =>
            piece.m_groundPiece || piece.m_groundOnly || piece.m_cultivatedGroundOnly || piece.m_vegetationGroundOnly;

        private static bool Moves(Piece piece) =>
            piece.GetComponent<Ship>() != null || piece.GetComponent<Vagon>() != null || piece.GetComponent<ItemDrop>() != null;
    }
}
