using UnityEngine;
using UnityEngine.Rendering;

namespace EliteCreaturesPack.Ballista
{
    /// <summary>
    /// Draws a Bone Ballista on every peer that draws (never a dedicated server), from its ZDO and the timeline alone,
    /// sending nothing: the turn (the holder's own aim at once, everyone else's eased toward the last one sent), the
    /// spine arms bending back as the string is drawn and ringing forward after a shot, the string's two halves from
    /// each arm's tip to the string's centre, the missile laid in the groove, the sounds (the shot, the string taken,
    /// the latch, the missile laid), the kick of a shot, and the holder's arms and lean (<see cref="BallistaHands"/>).
    /// </summary>
    public sealed class BallistaLook : MonoBehaviour
    {
        /// <summary>The game's effects (from <see cref="BallistaPrefabs"/>): the ballista's shot and laid missile, the crossbow's draw and latch.</summary>
        public static EffectList? Fire, Draw, Latch, Laid;

        /// <summary>Degrees each vertebra turns at full draw; the left arm turns the other way (each arm bends back).</summary>
        private const float Bend = 4.5f, Kick = 3f, KickTime = 0.08f, Follow = 12f;

        private BallistaControl control = null!;
        private BallistaHands? hands;
        private float yaw, pitch, lastActTime;
        private int shots = -1;
        private Act lastAct;

        private void Awake()
        {
            control = GetComponent<BallistaControl>();
            enabled = SystemInfo.graphicsDeviceType != GraphicsDeviceType.Null;
        }

        /// <summary>A placement ghost (no ZDO) is posed at rest once, its string strung, and left still.</summary>
        private void Start()
        {
            if (control.Net != null && control.Net.GetZDO() != null)
            {
                return;
            }
            if (control.Parts != null)
            {
                Bow(control.Parts, 0f);
                Strings(control.Parts, Centre(control.Parts, 0f));
            }
            enabled = false;
        }

        private void LateUpdate()
        {
            BallistaParts? parts = control.Parts;
            if (parts == null || !control.Net.IsValid())
            {
                return;
            }
            BallistaState s = control.State;
            Turn(parts, s);
            float draw = BallistaTimeline.Draw(s);
            Bow(parts, draw);
            Vector3 centre = Centre(parts, draw);
            Strings(parts, centre);
            parts.Laid?.SetActive(BallistaTimeline.MissileLaid(s));
            Cues(parts, s);
            (hands ??= new BallistaHands()).Pose(parts, s, centre, Time.deltaTime);
        }

        /// <summary>The holder's own aim, worked out here each frame; anyone else's eased toward the ZDO's; a shot kicks the nose up.</summary>
        private void Turn(BallistaParts parts, in BallistaState s)
        {
            if (Player.m_localPlayer != null && control.HeldBy(Player.m_localPlayer))
            {
                BallistaOperator.Aim(control, Player.m_localPlayer, Time.deltaTime);
                (yaw, pitch) = (control.Yaw, control.Pitch);
            }
            else
            {
                float ease = 1f - Mathf.Exp(-Follow * Time.deltaTime);
                (yaw, pitch) = (Mathf.LerpAngle(yaw, s.Yaw, ease), Mathf.LerpAngle(pitch, s.Pitch, ease));
            }
            float kick = s.ShotAge < 1f ? Kick * Mathf.Exp(-s.ShotAge / KickTime) : 0f;
            parts.Yaw.localRotation = Quaternion.Euler(0f, yaw, 0f);
            parts.Pitch.localRotation = Quaternion.Euler(-(pitch + kick), 0f, 0f);
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
        private static Vector3 Centre(BallistaParts parts, float draw)
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
        private void Cues(BallistaParts parts, in BallistaState s)
        {
            if (shots >= 0 && s.Shots > shots)
            {
                Fire?.Create(parts.Muzzle.position, parts.Muzzle.rotation);
            }
            bool fresh = s.Act != lastAct || s.ActTime < lastActTime - 0.05f;
            if (shots >= 0 && s.Act == Act.Pulling)
            {
                Play(fresh && s.ActTime < 0.5f, Draw, parts.NockRest);
                Play(Crossed(s, fresh, BallistaTimeline.DrawEnd), Latch, parts.NockDrawn);
            }
            if (shots >= 0 && s.Act == Act.Loading)
            {
                Play(Crossed(s, fresh, BallistaTimeline.Lay), Laid, parts.Seat);
            }
            (shots, lastAct, lastActTime) = (s.Shots, s.Act, s.ActTime);
        }

        private bool Crossed(in BallistaState s, bool fresh, float moment) => s.ActTime >= moment && s.ActTime < moment + 0.5f && (fresh || lastActTime < moment);

        private static void Play(bool now, EffectList? effect, Transform at)
        {
            if (now)
            {
                effect?.Create(at.position, at.rotation);
            }
        }
    }
}
