using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// A rig piece put back with the hammer growing back in place (the user, 2026-10-26: "as they repair the ship with the
    /// hammer, if things like sails and anything that was shot and fell off the ship incrementally start generating back"),
    /// on every screen that sees it come back (<see cref="KwRigDamageLooks"/>, <see cref="KwRigDamageRudder"/>): a mast,
    /// bowsprit or post cap's look and what goes with it (a mast's sails and ropes, a bowsprit's jibs) scaled up about its
    /// foot from <see cref="From"/> of its size to whole over <see cref="Seconds"/> s, easing out, with the game's wood
    /// hammering at its foot; a rudder's blade the same about its hinge, from the share of it that was left. Stopped at
    /// whole if the piece falls again meanwhile. Only drawn; nothing is sent.
    /// </summary>
    public sealed class KwRigRise : MonoBehaviour
    {
        public const float Seconds = 1f, From = 0.15f;
        private KwRigLook? look;
        private Transform? fitting;
        private Vector3 foot, fittingAt, size = Vector3.one;
        private float time, from = From;

        /// <summary>A mast, bowsprit or cap put back: its look and fitting grow from its foot.</summary>
        public static void Begin(KwRigLook look)
        {
            KwRigRise made = Made(look.gameObject);
            (made.look, made.fitting) = (look, look.Fitting);
            made.foot = look.Piece.ToShip(look.Piece.Main.A);
            made.fittingAt = look.Piece.Piece.Position;
            KwRigSound.Play(KwHullPatchFall.Hammer, look.transform.parent.TransformPoint(made.foot));
            made.Show(From);
        }

        /// <summary>A rudder's blade mended: it grows about its hinge from the share of it that was left.</summary>
        public static void Grow(Transform blade, float left)
        {
            KwRigRise made = Made(blade.gameObject);
            made.from = Mathf.Clamp(left, From, 1f);
            KwRigSound.Play(KwHullPatchFall.Hammer, blade.position);
            made.Show(made.from);
        }

        // One on the object, started again; a blade keeps the size it had at rest.
        private static KwRigRise Made(GameObject on)
        {
            KwRigRise? made = on.GetComponent<KwRigRise>();
            if (made != null)
                made.Show(1f);
            else
                (made = on.AddComponent<KwRigRise>()).size = on.transform.localScale;
            made.time = 0f;
            return made;
        }

        private void LateUpdate()
        {
            time += Time.deltaTime;
            float share = Mathf.Clamp01(time / Seconds);
            if (share >= 1f || (look != null && look.GetComponent<KwMastFall>() != null))
            {
                Show(1f);
                Destroy(this);
                return;
            }
            float eased = 1f - (1f - share) * (1f - share) * (1f - share);
            Show(Mathf.Lerp(from, 1f, eased));
        }

        // The piece at a share of its size, about its foot (a look and its fitting) or its own origin (a blade).
        private void Show(float scale)
        {
            transform.localScale = size * scale;
            if (look == null)
                return;
            transform.localPosition = foot * (1f - scale);
            if (fitting != null)
            {
                fitting.localPosition = foot + (fittingAt - foot) * scale;
                fitting.localScale = Vector3.one * scale;
            }
        }
    }
}
