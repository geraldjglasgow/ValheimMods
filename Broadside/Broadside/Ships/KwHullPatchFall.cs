using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// A patch coming on or off, drawn (<see cref="KwHullPatchLooks"/>; the user, 2026-10-26: "these wooden repair kits should
    /// just go away: a wood-breaking noise, they fall off the ship, and that hull piece is actually repaired"). Knocked on:
    /// the game's wood hammering twice, the patch pressed onto the hull from a little proud over <see cref="Settle"/> s.
    /// Dropped: the game's wood breaking, then the patch let go of the ship where it is, pushed off the hull and falling under
    /// gravity, turning, shrinking away over its last <see cref="Fade"/> s and gone after <see cref="Life"/> s (a deck's
    /// patch pops up off the deck and is gone after <see cref="DeckLife"/> s, before it would fall back through it). Sounds
    /// are local copies (<see cref="KwRigSound"/>); nothing is sent.
    /// </summary>
    public sealed class KwHullPatchFall : MonoBehaviour
    {
        public const string Hammer = "sfx_build_hammer_wood";
        // a deck's patch thrown up at Lift m/s is back down at the deck after 2 Lift / g = 0.71 s: gone at DeckLife, just before
        public const float Settle = 0.3f, Life = 1.6f, DeckLife = 0.7f, Fade = 0.5f, Push = 1.4f, Lift = 3.5f, Spin = 220f, Proud = 0.05f;
        private Vector3 velocity, axis, rest, size;
        private float time, life = Life, knocked = -1f;
        private bool falling;

        /// <summary>A new patch hammered on.</summary>
        public static void Knock(GameObject board)
        {
            KwHullPatchFall made = board.AddComponent<KwHullPatchFall>();
            (made.rest, made.size) = (board.transform.localPosition, board.transform.localScale);
            KwRigSound.Play(Hammer, board.transform.position);
            made.Update();
        }

        /// <summary>A patch broken away and falling off the ship (one on a deck pops up, turns and is gone before it lands).</summary>
        public static void Drop(GameObject board)
        {
            KwHullPatchFall? made = board.GetComponent<KwHullPatchFall>();
            if (made == null)
                made = board.AddComponent<KwHullPatchFall>();
            else if (made.size != Vector3.zero)
                board.transform.localPosition = made.rest;
            Transform t = board.transform;
            KwRigSound.Play(KwRigSound.Crack, t.position);
            t.SetParent(null, true);
            Vector3 outward = t.forward;
            bool deck = outward.y > 0.7f;
            (made.falling, made.time, made.size, made.life) = (true, 0f, t.localScale, deck ? DeckLife : Life);
            made.velocity = deck ? Vector3.up * Lift + Vector3.Cross(outward, Vector3.forward) * 0.4f : outward * Push + Vector3.up * 0.3f;
            made.axis = Vector3.Cross(outward, Vector3.up).sqrMagnitude > 1e-4f ? Vector3.Cross(outward, Vector3.up).normalized : Vector3.right;
        }

        private void Update()
        {
            time += Time.deltaTime;
            if (falling)
                Fall();
            else
                Press();
        }

        // Pressed on from a little proud of the hull, knocked a second time halfway; then left alone.
        private void Press()
        {
            float share = Mathf.Clamp01(time / Settle);
            transform.localPosition = rest + transform.localRotation * Vector3.forward * (Proud * (1f - share));
            if (share >= 0.5f && knocked < 0f)
            {
                knocked = time;
                KwRigSound.Play(Hammer, transform.position);
            }
            if (share >= 1f)
                Destroy(this);
        }

        // Off the ship under gravity, turning, shrinking away at the end, then gone.
        private void Fall()
        {
            float dt = Time.deltaTime;
            velocity += Physics.gravity * dt;
            transform.position += velocity * dt;
            transform.rotation = Quaternion.AngleAxis(Spin * dt, axis) * transform.rotation;
            transform.localScale = size * Mathf.Clamp01((life - time) / Fade);
            if (time >= life)
                Destroy(gameObject);
        }
    }
}
