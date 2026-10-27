using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// How a cannon fires (<see cref="KwCannonWork"/> gives it the rest of its motion), from its <see cref="KwBreech"/>:
    /// the gunner's right fist draws the T-handle out of the boss in the breech door, steadily back and out toward his
    /// right hip, on a cord that comes out of the door; halfway through the pull the shot leaves
    /// (<see cref="KwArmDef.FireDelay"/>) and the barrel kicks back under it, past its stop, a bounce and a settle; he holds
    /// the full pull a beat and lets go, and the handle is reeled back fast into the boss, its stem along its cord and the
    /// cord drawn in after it. The handle rides on the door (so on the barrel, its keys less the barrel's kick, to keep to
    /// the pull as the door jumps back); his fist follows a moving part of no mesh that goes with the handle up to the
    /// release and stays there, so his hand stays where it let go, relaxing a little, while the handle flies home.
    /// </summary>
    public static class KwCannonFiring
    {
        /// <summary>The shot's motion, seconds; the moving part the gunner's right fist follows.</summary>
        public const float FireTime = 1.2f;
        public const string Fist = "fist";

        // The shot, as fractions of FireTime: the pull begins, the shot leaves halfway through it (the barrel kicks back
        // past its stop, bounces and settles), the pull ends; he lets go, the handle is reeled home (slow to start, fast
        // to finish) and his hand relaxes.
        private const float PullFrom = 0.06f, Shot = 0.27f, Pulled = 0.48f;
        private const float Kicked = Shot + 0.025f, Bounced = Shot + 0.11f, Settled = Shot + 0.2f;
        private const float Release = 0.62f, Reeling = 0.665f, Home = 0.725f, Relaxed = 0.8f;

        // How many steady keys lay the pull's smooth curve; how far his hand drops and drifts forward as it relaxes; where
        // the reeled-in handle swings round his hip, as a share of where it was let go along each axis.
        private const int Steps = 12;
        private static readonly Vector3 Slack = new Vector3(0.08f, 0.03f, -0.05f), Round = new Vector3(0.35f, 0.85f, 0.1f);

        /// <summary>A cannon's shot: its timing, the barrel's kick, the T-handle and its cord, and the fist that pulls it.</summary>
        public static void Add(KwArmDef arm, KwArmMover gun, KwBreech breech)
        {
            (arm.FireTime, arm.FireDelay) = (FireTime, FireTime * Shot);
            Kick(gun, breech.Recoil);
            KwArmMover tee = arm.Mover("pull", KwJoint.Pitch, breech.Tee, "block").Part(breech.Metal, breech.TeeHandle);
            KwArmMover fist = arm.Mover(Fist, KwJoint.Pitch, breech.Tee);
            Draw(tee, fist, gun, breech);
            ReelIn(tee, gun, breech);
            Vector3 slack = breech.Pull + Slack;
            fist.Fire.Key(Release, breech.Pull).Key(Relaxed, slack);
            fist.Reload.Key(0f, slack).Key(0.16f, slack).Key(0.5f);
            KwArmPoint door = new KwArmPoint(KwJoint.Pitch, breech.CordExit, "block"), stem = new KwArmPoint(KwJoint.Pitch, breech.StemEnd, "pull");
            arm.Cord("pullcord", KwLook.Linen, 0.006f, door, stem);
        }

        // The barrel: still through the first half of the pull, then a hard kick back past its stop, a bounce and a
        // settle, held back there (run in) until the reload runs it out.
        private static void Kick(KwArmMover gun, float back)
        {
            Vector3 run = new Vector3(-back, 0f, 0f);
            gun.Fire.Key(0f).Key(Shot).Key(Kicked, run * 1.08f, eased: false).Key(Bounced, run * 0.96f).Key(Settled, run);
        }

        // The pull, in steady steps along its curve: the fist where the pull has drawn it, and the handle with it (less
        // the barrel's kick, as it rides on the door), its stem turned along the cord.
        private static void Draw(KwArmMover tee, KwArmMover fist, KwArmMover gun, KwBreech breech)
        {
            tee.Fire.Key(0f);
            fist.Fire.Key(0f);
            foreach (float at in Moments())
            {
                Vector3 drawn = Drawn(breech, at), kick = gun.Fire.Sample(at).Move;
                tee.Fire.Key(at, drawn - kick, Along(breech, drawn, kick), eased: false);
                fist.Fire.Key(at, drawn, eased: false);
            }
        }

        // The pull's steps, and the moments the barrel's kick turns (so the handle keeps to the pull as the door jumps back).
        private static IEnumerable<float> Moments()
        {
            for (int i = 0; i <= Steps; i++)
                yield return Mathf.Lerp(PullFrom, Pulled, (float)i / Steps);
            yield return Kicked;
            yield return Bounced;
            yield return Settled;
        }

        // Where the pull has drawn the handle at a moment, from where it sits: along a smooth curve through its midway
        // point (at the shot) to its full pull, slow to start and slow to stop.
        private static Vector3 Drawn(KwBreech breech, float at)
        {
            float s = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(PullFrom, Pulled, at));
            Vector3 bend = breech.Midway * 2f - breech.Pull * 0.5f;
            return bend * (2f * s * (1f - s)) + breech.Pull * (s * s);
        }

        // The handle's turn that lays its stem along the cord, toward where the cord leaves the door (kicked back with the barrel).
        private static Vector3 Along(KwBreech breech, Vector3 drawn, Vector3 kick)
        {
            Vector3 cord = breech.CordExit + kick - (breech.Tee + drawn);
            float flat = new Vector2(cord.x, cord.y).magnitude;
            return new Vector3(0f, Mathf.Atan2(-cord.z, flat) * Mathf.Rad2Deg, Mathf.Atan2(cord.y, cord.x) * Mathf.Rad2Deg);
        }

        // Let go: the handle reeled home into the boss on the barrel (now kicked back), slow to start and fast to finish,
        // swinging forward round the gunner's hip before it comes in, its stem along the cord until it is in.
        private static void ReelIn(KwArmMover tee, KwArmMover gun, KwBreech breech)
        {
            Vector3 run = gun.Fire.Sample(Release).Move, held = breech.Pull - run, round = Vector3.Scale(held, Round), near = round * 0.1f;
            tee.Fire.Key(Release, held, Along(breech, breech.Pull, run), eased: false).Key(Reeling, round, Along(breech, round + run, run), eased: false)
                .Key(Home - 0.01f, near, Along(breech, near + run, run), eased: false).Key(Home, eased: false);
        }
    }
}
