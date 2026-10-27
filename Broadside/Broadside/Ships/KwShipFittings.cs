using PatchGuard;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// A ship's working fittings, made from its blueprint on every peer. Its rudder (the blueprint's rudder, built to its
    /// post and linked to its helm as it was on the dock, <see cref="KwRudderLay"/>) turns as the game's ships turn theirs
    /// (the Ship's rudder object), its deck tiller and links with it, or its steering gear to the captain's wheel, which
    /// turns with the helm (<see cref="KwShipGear"/>, <see cref="KwShipWheel"/>); its hatches open and shut (<see cref="KwShipHatch"/>), its openings' covers and doors too (<see cref="KwShipOpenings"/>, made everywhere as the hatches are);
    /// a ladder below deck is climbed as a placed one; its sails' cloth and ropes are raised and lowered by the ship's speed
    /// (<see cref="KwShipSail"/>), a bowsprit's jibs with them (<see cref="KwShipJibs"/>); the helm is set where the ship's own pieces put it (<see cref="KwShipHelm"/>), the ladder
    /// over the side too (<see cref="KwShipLadder"/>, placed everywhere, drawn only on screens); the onboard trigger and the buoyancy box are sized to the hull. The
    /// rudder and the sails are only drawn, so a dedicated server leaves them out
    /// (<see cref="KwShipBody.Drawn"/>): the ship steers by the game's rudder value, which needs no blade, and its helm sits
    /// at the stern there, where nobody takes it from. The hatches are made everywhere: the ship's owner, the server too,
    /// answers their calls. So are a railing's shield hooks and a name board's name (<see cref="KwShipDress"/>).
    /// </summary>
    public static class KwShipFittings
    {
        /// <summary>How far over the sheer the onboard trigger reaches (as far as the break-up's aboard check, KwShipBreakRules).</summary>
        public const float Aloft = 25f;

        /// <summary>
        /// The pieces that move on a ship, kept out of its combined look: rudders, hatches, anchor windlasses (<see cref="KwShipSailing"/>) and captain's wheels;
        /// and masts, bowsprits and post caps, which can be shot down and are drawn on their own (<see cref="KwRigLook"/>).
        /// </summary>
        public static bool Moves(string prefab) => KwRudder.Is(prefab) || prefab.StartsWith(KwHatch.Prefix) || KwAnchor.Is(prefab) || KwWheel.Is(prefab)
            || prefab == KwSailTool.Made || prefab == KwBowsprit.Made || KwCaps.Is(prefab);

        public static void Add(Transform ship, KwBlueprint blueprint, KwBpHull hull)
        {
            Transform holder = ship.Find(KwShipPrefab.Fittings);
            Ship game = ship.GetComponent<Ship>();
            int numbered = 0;
            Physics.SyncTransforms();
            foreach (KwBpPiece piece in blueprint.Pieces)
                Fit(ship, holder, blueprint, piece, ref numbered);
            KwShipBody.Step("Broadside ship openings", () => KwShipOpenings.Add(ship, holder, blueprint, hull));
            KwShipBody.Step("Broadside ship helm", () => KwShipHelm.Place(ship, hull, game.m_rudderObject));
            KwShipBody.Step("Broadside ship ladder", () => KwShipLadder.Place(ship, blueprint, hull));
            KwShipBody.Step("Broadside ship rig damage", () => KwRigDamage.Add(ship, blueprint));
            Size(ship, hull);
        }

        // One piece's working part, if it has one: a rudder, a sail, a ladder below deck, a captain's wheel, or a hatch
        // (numbered among the ship's hatches, whose state is kept in the ship's ZDO).
        private static void Fit(Transform ship, Transform holder, KwBlueprint blueprint, KwBpPiece piece, ref int numbered)
        {
            Ship game = ship.GetComponent<Ship>();
            KwShipDress.Fit(ship, holder, piece);
            if (KwRudder.Is(piece.Prefab) && KwShipBody.Drawn)
                KwShipBody.Step("Broadside ship rudder", () => game.m_rudderObject = Rudder(ship, holder, blueprint, piece));
            else if (piece.Prefab == KwSailTool.Made && KwShipBody.Drawn)
                KwShipBody.Step("Broadside ship sail", () => Sail(holder, piece));
            else if (piece.Prefab == KwBowsprit.Made && KwShipBody.Drawn)
                KwShipBody.Step("Broadside ship jibs", () => KwShipJibs.Add(holder, blueprint, piece));
            else if (piece.Prefab == KwToolPieces.PartPrefix + KwLadderTool.Tool)
                KwShipBody.Step("Broadside ship ladder below", () => Climb(holder, piece));
            else if (KwWheel.Is(piece.Prefab) && KwShipBody.Drawn)
                KwShipBody.Step("Broadside ship wheel", () => KwShipWheel.Add(holder, piece));
            else if (piece.Prefab.StartsWith(KwHatch.Prefix))
                Hatch(ship, holder, piece, numbered++);
        }

        // A hatch: a copy of its piece, whether it is open kept in the ship's ZDO under its number.
        private static void Hatch(Transform ship, Transform holder, KwBpPiece piece, int index)
        {
            ZNetView view = ship.GetComponent<ZNetView>();
            KwShipBody.Step("Broadside ship hatch", () =>
            {
                Transform made = KwShipParts.Copy(holder, piece, "hatch" + index);
                KwShipHatch.Make(made, piece.Prefab, view, index);
                KwHoverForward.OnColliders(made);
            });
        }

        // A ladder below a hatch or down from a deck's or cabin roof's open edge (its gap in a roof railing is in that
        // railing's text): an unseen place to climb it, as long as it was made, which E climbs as a placed ladder does, and
        // its look as its child, drawn on its own so a sea edit redraws only it (KwSeaLadderLook).
        private static void Climb(Transform holder, KwBpPiece piece) => KwSeaLadderLook.Make(holder, piece);

        // The rudder's parts built to its post and linked to its helm as it was saved, or to the ship's captain's wheel by its
        // steering gear (one saved before it was linked: to the first deck forward of its post); its blade is what the ship
        // turns.
        private static GameObject? Rudder(Transform ship, Transform holder, KwBlueprint blueprint, KwBpPiece piece)
        {
            Transform made = KwShipParts.Copy(holder, piece, "rudder");
            KwRudderFit fit = KwShipParts.Fit(blueprint, piece);
            KwShipGear.Refit(ship, made, blueprint, piece, fit);
            Transform? blade = made.Find(KwRudder.BladeName);
            return blade != null ? blade.gameObject : null;
        }

        /// <summary>
        /// A mast's sails' cloth, lashings and ropes (its mast and spars are in the ship's look, but for a boom, which swings
        /// with its sail to the wind, <see cref="KwBoomSwing"/>), raised by the ship's speed; also made again for a mast
        /// changed at sea (<see cref="KwSeaRigLayers"/>).
        /// </summary>
        internal static void Sail(Transform holder, KwBpPiece piece)
        {
            Transform made = new GameObject("sail") { layer = holder.gameObject.layer }.transform;
            made.SetParent(holder, false);
            made.SetLocalPositionAndRotation(piece.Position, piece.Rotation);
            string saved = KwShipParts.Saved(piece);
            KwBoomSwing.Dress(made, saved);
            made.gameObject.AddComponent<KwShipSail>().Saved = saved;
        }

        // The onboard trigger round the hull, from a metre under the keel line to Aloft over the sheer (cabin roofs, masts
        // and their tops: a ship with nobody counted aboard is braked to a stop, KwShipDrive.Drag, so a player in the crow's
        // nest must count), and the buoyancy box under it.
        private static void Size(Transform ship, KwBpHull hull)
        {
            Transform? onboard = ship.Find("OnboardTrigger"), floating = ship.Find("FloatCollider");
            if (onboard != null)
            {
                float tall = hull.Depth + 1f + Aloft;
                onboard.localPosition = new Vector3(0f, tall / 2f - 1f, 0f);
                onboard.localScale = new Vector3(hull.Beam + 1f, tall, hull.Length + 1f);
            }
            BoxCollider? box = floating != null ? floating.GetComponent<BoxCollider>() : null;
            if (box == null)
                return;
            floating!.localPosition = Vector3.zero;
            box.size = new Vector3(hull.Beam * 0.85f, 1f, hull.Length * 0.8f);
        }
    }
}
