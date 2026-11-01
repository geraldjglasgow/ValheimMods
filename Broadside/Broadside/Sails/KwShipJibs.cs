using System.Collections.Generic;
using PatchGuard;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// A launched ship's jibs (<see cref="KwJibDress"/>), a fitting made from its blueprint (<see cref="KwShipFittings"/>):
    /// the stay found among the blueprint's own masts (<see cref="KwJibStay.InBlueprint"/>), the jibs set and furled with
    /// their foremast's lower sails by the ship's speed, as the game's ships set theirs (<see cref="KwShipSail.Share"/>),
    /// each easing over a second or so, and sheeted to leeward of the apparent wind (<see cref="KwJibSwing"/>). Every peer
    /// reads the speed the game syncs; only drawn, so never on a dedicated server. The bowsprit's spar is in the ship's look.
    /// The jibs are dressed as the speed sets them when the ship is built, so the first look at the speed sets them again
    /// only when it changed since (as <see cref="KwShipSail"/>'s sails).
    /// </summary>
    public sealed class KwShipJibs : MonoBehaviour
    {
        private const float PerSecond = 0.7f;

        private Ship? ship;
        private KwMastSpec? mast;
        private List<KwJib> jibs = new List<KwJib>();
        private float stage = -1f, dressed = -1f;
        private bool broken;

        /// <summary>A blueprint's bowsprit's jibs on the ship, under its fittings; nothing with no jib or no foremast.</summary>
        public static void Add(Transform holder, KwBlueprint blueprint, KwBpPiece piece)
        {
            if (!KwBowsprit.TryParse(KwShipParts.Saved(piece), out KwBowsprit spec) || spec.Jibs == 0)
                return;
            KwJibStay? stay = KwJibStay.InBlueprint(blueprint, piece);
            if (stay == null)
                return;
            Transform made = new GameObject("bowsprit") { layer = holder.gameObject.layer }.transform;
            made.SetParent(holder, false);
            made.SetLocalPositionAndRotation(piece.Position, piece.Rotation);
            KwShipJibs set = made.gameObject.AddComponent<KwShipJibs>();
            Ship? game = holder.GetComponentInParent<Ship>();
            set.dressed = game != null ? KwShipSail.Share(stay.Mast, 0, game.GetSpeedSetting()) : 1f;
            (set.mast, set.jibs) = (stay.Mast, KwJibDress.Dress(made, spec, stay, KwShipSail.HoistOf(set.dressed)));
            KwJibSwing.Add(made, set.jibs);
        }

        private void Start() => ship = GetComponentInParent<Ship>();

        // A throw is reported once and stops these jibs, rather than once a frame.
        private void Update()
        {
            if (broken)
                return;
            broken = true;
            Guard.Run("Broadside ship jibs", static s => s.Hoist(), this);
            broken = false;
        }

        // The jibs eased to their share at the speed; the first time set there at once (nothing to do when dressed there).
        private void Hoist()
        {
            if (ship == null || mast == null || jibs.Count == 0)
                return;
            float target = KwShipSail.Share(mast, 0, ship.GetSpeedSetting());
            if (stage >= 0f && Mathf.Approximately(stage, target))
                return;
            bool first = stage < 0f;
            stage = first ? target : Mathf.MoveTowards(stage, target, PerSecond * Time.deltaTime);
            if (first && Mathf.Approximately(dressed, target))
                return;
            foreach (KwJib jib in jibs)
                KwJibDress.Pose(jib, KwShipSail.HoistOf(stage));
        }
    }
}
