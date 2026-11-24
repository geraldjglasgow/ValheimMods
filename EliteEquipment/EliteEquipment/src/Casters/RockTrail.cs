using LocalEffects;
using UnityEngine;

namespace EliteEquipment.Casters
{
    /// <summary>
    /// The dark green spell trail on a flying Greywood rock and on each of its pieces (user, 2026-11-02: "there needs to be
    /// some like dark greenish spell particle or something for the rock"): on the rock's prefab (<see cref="WandRock"/>), so
    /// every machine that has the networked rock, the caster's and every other player's, attaches a local copy of the
    /// workshop's <see cref="Effect"/> (<c>ee_casterfx_meadows</c>) at the rock's centre as the rock appears, its +Z along
    /// the flight, scaled with the rock (as authored for a rock the stone's size: 1 a tap, 2.5 at full charge, 0.45 a piece),
    /// gone with the rock. None while the bundle lacks it, and none on a dedicated server.
    /// </summary>
    public sealed class RockTrail : MonoBehaviour
    {
        public const string Effect = "ee_fx_rock_trail";

        private static GameObject trail;
        private static bool prepared;

        private void Start()
        {
            if (!CastersSettings.On || WandRock.Rock == null)
                return;
            GameObject copy = LocalEffect.Attach(Trail(), transform, transform.position, endless: true);
            if (copy == null)
                return;
            copy.transform.localRotation = Quaternion.identity;
            copy.transform.localScale = Vector3.one / Mathf.Max(0.001f, WandRock.Rock.transform.localScale.x);
        }

        /// <summary>The prepared trail, its particles set once to follow the rock's scale; null without it.</summary>
        private static GameObject Trail()
        {
            if (prepared)
                return trail;
            prepared = true;
            trail = CasterFx.Prepared(Effect);
            if (trail != null)
                ChargeOrb.ScaleWithParent(trail);
            return trail;
        }
    }
}
