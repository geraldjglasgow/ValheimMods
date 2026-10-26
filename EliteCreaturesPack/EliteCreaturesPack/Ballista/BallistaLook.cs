using MannedWeapons;
using UnityEngine;

namespace EliteCreaturesPack.Ballista
{
    /// <summary>
    /// The Bone Ballista's own look on every peer that draws (never a dedicated server), from its ZDO and the timeline
    /// alone, sending nothing, each frame after the library has turned it and before the holder's hands
    /// (<see cref="BallistaControl"/>): the spine arms bending back as the string is drawn and ringing forward after a shot,
    /// the string's two halves from each arm's tip to the string's centre, the missile laid in the groove, and the sounds
    /// (the shot, the string taken, the latch, the missile laid).
    /// </summary>
    public sealed class BallistaLook
    {
        /// <summary>The game's effects (from <see cref="BallistaPrefabs"/>): the ballista's shot and laid missile, the crossbow's draw and latch.</summary>
        public static EffectList? Fire, Draw, Latch, Laid;

        /// <summary>Degrees each vertebra turns at full draw; the left arm turns the other way (each arm bends back).</summary>
        private const float Bend = 4.5f;

        private float lastActTime;
        private int shots = -1;
        private Act lastAct;

        /// <summary>The string's centre this frame, where the holder's hands take it.</summary>
        public Vector3 Centre { get; private set; }

        /// <summary>A placement ghost's look: at rest, its string strung.</summary>
        public void Rest(BallistaParts parts)
        {
            Bow(parts, 0f);
            Strings(parts, StringCentre(parts, 0f));
        }

        public void Show(BallistaParts parts, in WeaponState s)
        {
            float draw = BallistaTimeline.Draw(s);
            Bow(parts, draw);
            Centre = StringCentre(parts, draw);
            Strings(parts, Centre);
            parts.Laid?.SetActive(BallistaTimeline.MissileLaid(s));
            Cues(parts, s);
        }

        /// <summary>Each vertebra a little turn about Y from its rest: the arms back as the string draws, forward as they ring.</summary>
        private static void Bow(BallistaParts parts, float draw)
        {
            for (int side = 0; side < 2; side++)
            {
                float turn = (side == 0 ? -Bend : Bend) * draw;
                Transform[] arm = parts.Arms[side];
                for (int i = 0; i < arm.Length; i++)
                {
                    arm[i].localRotation = parts.Rests[side][i] * Quaternion.Euler(0f, turn, 0f);
                }
            }
        }

        /// <summary>The string's centre: on the line between the tips when slack, drawn back toward the latch.</summary>
        private static Vector3 StringCentre(BallistaParts parts, float draw)
        {
            Vector3 slack = parts.Tips[0] != null && parts.Tips[1] != null ? (parts.Tips[0]!.position + parts.Tips[1]!.position) * 0.5f : parts.NockRest.position;
            return Vector3.Lerp(slack, parts.NockDrawn.position, Mathf.Clamp01(draw));
        }

        /// <summary>Each half of the string (a 1 m cord along its +Z) from its arm's tip to the centre.</summary>
        private static void Strings(BallistaParts parts, Vector3 centre)
        {
            for (int side = 0; side < 2; side++)
            {
                Transform? cord = parts.Strings[side], tip = parts.Tips[side];
                if (cord == null || tip == null)
                {
                    continue;
                }
                Vector3 run = centre - tip.position;
                cord.position = tip.position;
                cord.rotation = Quaternion.LookRotation(run, parts.Pitch.up);
                cord.localScale = new Vector3(1f, 1f, Mathf.Max(0.001f, run.magnitude));
            }
        }

        /// <summary>The sounds, each once, at its moment: a new shot, a pull begun, the latch caught, a missile laid.</summary>
        private void Cues(BallistaParts parts, in WeaponState s)
        {
            if (shots >= 0 && s.Shots > shots)
            {
                Fire?.Create(parts.Muzzle.position, parts.Muzzle.rotation);
            }
            Act act = BallistaTimeline.Doing(s);
            bool fresh = act != lastAct || s.StepTime < lastActTime - 0.05f;
            if (shots >= 0 && act == Act.Pulling)
            {
                Play(fresh && s.StepTime < 0.5f, Draw, parts.NockRest);
                Play(Crossed(s, fresh, BallistaTimeline.DrawEnd), Latch, parts.NockDrawn);
            }
            if (shots >= 0 && act == Act.Loading)
            {
                Play(Crossed(s, fresh, BallistaTimeline.Lay), Laid, parts.Seat);
            }
            (shots, lastAct, lastActTime) = (s.Shots, act, s.StepTime);
        }

        private bool Crossed(in WeaponState s, bool fresh, float moment) => s.StepTime >= moment && s.StepTime < moment + 0.5f && (fresh || lastActTime < moment);

        private static void Play(bool now, EffectList? effect, Transform at)
        {
            if (now)
            {
                effect?.Create(at.position, at.rotation);
            }
        }
    }
}
