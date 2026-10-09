using UnityEngine;

namespace EliteCreaturesPack.Ballista
{
    /// <summary>
    /// The holder's machine, which owns the ballista's ZDO while holding it: the aim follows the view (whose turn is held
    /// to the ballista's pace), within 45 degrees either side of where it was placed (90 in all), 20 down and 25 up; the reload runs by itself whenever the holder carries a Bone Missile (pull the string to the latch, then lay
    /// a missile, which is taken from the inventory as the hand goes for it); attack shoots a laid missile
    /// (<see cref="BallistaShot"/>). The holder's feet are walked to the spot behind the ballista the timeline asks for
    /// (aiming, or stepped in to reach the string or the groove), with the game's own movement, so turning the ballista
    /// or reaching in makes the holder shuffle along on every screen.
    /// </summary>
    public static class BallistaOperator
    {
        public const float YawLimit = 45f, PitchDown = -20f, PitchUp = 25f;
        /// <summary>The fastest the view and the ballista turn: about what the holder's jog round its back can keep up with.</summary>
        public const float YawRate = 240f;

        private const float CatchUp = 720f, PitchRate = 200f, Reach = 150f, WriteStep = 0.1f;
        private const float Arrive = 0.06f, Restart = 0.15f, Slow = 0.25f, Walking = 0.5f, TurnRate = 720f, GiveUp = 0.6f;
        private static int aimMask;

        public static void Tick(BallistaControl c, Player p)
        {
            if (c.Parts == null || !c.Net.IsOwner())
            {
                return;   // ownership is on its way from the old owner
            }
            Reload(c, p, c.State);
        }

        /// <summary>
        /// The holder's aim, each drawn frame: the yaw follows the view's own (which <see cref="BallistaLookLimit"/> holds
        /// to the ballista's turn and pace, so it stops at the bound and as the mouse stops), the pitch lays the groove on
        /// what the crosshair is on.
        /// </summary>
        public static void Aim(BallistaControl c, Player p, float dt)
        {
            if (c.Parts == null || !c.Net.IsValid())
            {
                return;
            }
            float yaw = Mathf.Clamp(Mathf.DeltaAngle(c.transform.eulerAngles.y, p.m_lookYaw.eulerAngles.y), -YawLimit, YawLimit);
            c.Yaw = Mathf.MoveTowards(c.Yaw, yaw, CatchUp * dt);
            c.Pitch = Mathf.MoveTowards(c.Pitch, Elevation(c), PitchRate * dt);
            if (c.Net.IsOwner())
            {
                Write(c.Net.GetZDO(), BallistaKeys.Yaw, c.Yaw);
                Write(c.Net.GetZDO(), BallistaKeys.Pitch, c.Pitch);
            }
        }

        private static void Write(ZDO zdo, int key, float value)
        {
            if (Mathf.Abs(zdo.GetFloat(key) - value) > WriteStep)
            {
                zdo.Set(key, value);
            }
        }

        /// <summary>The pitch that lays the groove on what the crosshair is on, within the ballista's tilt.</summary>
        private static float Elevation(BallistaControl c)
        {
            if (GameCamera.instance == null)
            {
                return c.Pitch;
            }
            Transform camera = GameCamera.instance.transform;
            Vector3 pivot = c.Parts!.Yaw.position;
            Vector3 from = camera.position + camera.forward * Mathf.Max(0f, Vector3.Dot(pivot - camera.position, camera.forward) + 1f);
            aimMask = aimMask != 0 ? aimMask : LayerMask.GetMask("Default", "static_solid", "Default_small", "piece", "terrain", "vehicle", "character", "character_net", "character_ghost");
            Vector3 target = Physics.Raycast(from, camera.forward, out RaycastHit hit, Reach, aimMask) ? hit.point : from + camera.forward * Reach;
            Vector3 run = target - pivot;
            float pitch = Mathf.Atan2(run.y, new Vector2(run.x, run.z).magnitude) * Mathf.Rad2Deg;
            return Mathf.Clamp(pitch, PitchDown, PitchUp);
        }

        /// <summary>Finishes an action that ran its time; else starts the next one when the holder has a missile.</summary>
        private static void Reload(BallistaControl c, Player p, in BallistaState s)
        {
            if (s.Busy)
            {
                return;
            }
            if (s.Act != Act.None)
            {
                Set(c, s.Settled, Act.None);
                return;
            }
            ItemDrop.ItemData? missile = s.Spring == Spring.Loaded || s.ShotAge < BallistaTimeline.Settle ? null : Missile(p);
            if (missile == null)
            {
                return;
            }
            if (s.Spring == Spring.Drawn)
            {
                p.GetInventory().RemoveItem(missile, 1);
            }
            Set(c, s.Spring, s.Spring == Spring.Slack ? Act.Pulling : Act.Loading);
            c.Net.GetZDO().Set(BallistaKeys.ActAt, BallistaKeys.Now());
        }

        /// <summary>Attack, held: shoot a laid missile; with nothing laid and nothing to lay, say so.</summary>
        public static void Trigger(BallistaControl c, Player p)
        {
            if (c.Parts == null || !c.Net.IsValid() || !c.Net.IsOwner())
            {
                return;
            }
            BallistaState s = c.State;
            if (s.Settled == Spring.Loaded && !s.Busy)
            {
                BallistaShot.Fire(c, p);
            }
            else if (!s.Busy && Missile(p) == null)
            {
                p.Message(MessageHud.MessageType.Center, "$msg_ecp_bal_nomissiles");
            }
        }

        /// <summary>The owner, as the holder lets go: an action under way is done at once, and nobody holds it.</summary>
        public static void LetGo(BallistaControl c)
        {
            BallistaState s = c.State;
            if (s.Act != Act.None)
            {
                Set(c, s.Act == Act.Pulling ? Spring.Drawn : Spring.Loaded, Act.None);
            }
            c.Net.GetZDO().Set(BallistaKeys.User, 0L);
        }

        /// <summary>
        /// The holder's walk to the spot the timeline asks for (the game's movement does the walking): at a jog while
        /// first walking up and to keep up with a big turn, easing in over the last 30 cm; then the facing, every step
        /// (the game turns a standing player only slowly, which left them sliding round after a big turn).
        /// </summary>
        public static void Steer(BallistaControl c, Player p)
        {
            if (c.Parts == null || !c.Net.IsValid())
            {
                return;
            }
            Vector3 spot = Vector3.Lerp(c.Parts.StandAim.position, c.Parts.StandLoad.position, BallistaTimeline.Close(c.State));
            Vector3 to = spot - p.transform.position;
            to.y = 0f;
            float distance = to.magnitude;
            p.m_moveDir = Settle(c, p, spot, distance) ? Vector3.zero : to / distance * Mathf.Min(1f, distance / Slow);
            Face(c, p, Time.fixedDeltaTime);
        }

        /// <summary>
        /// Whether the feet stay put: they stop a few centimetres from the spot and start again only once the spot has
        /// moved 15 cm from where they stopped (a small turn moves the hands, not the feet; no endless walking in place),
        /// and they give up on a spot they cannot reach (blocked) after a moment without moving.
        /// </summary>
        private static bool Settle(BallistaControl c, Player p, Vector3 spot, float distance)
        {
            if (c.Settled && (spot - c.RestAt).magnitude < Restart)
            {
                return true;
            }
            c.Stuck = p.GetVelocity().magnitude < 0.1f ? c.Stuck + Time.fixedDeltaTime : 0f;
            if (distance < Arrive || c.Stuck > GiveUp)
            {
                (c.Settled, c.Arrived, c.Stuck, c.RestAt) = (true, true, 0f, spot);
                return true;
            }
            c.Settled = false;
            return false;
        }

        /// <summary>
        /// The holder's facing, in place of the game's: the way they go while still walking up to the ballista (at a jog
        /// from up to 3 m, easing in over the last half metre), then the way it points, turned into fast. Returns the turn
        /// for the game's turning animation (radians a second).
        /// </summary>
        public static float Face(BallistaControl c, Player p, float dt)
        {
            Vector3 travel = p.m_moveDir, ahead = c.Parts != null ? c.Parts.Yaw.forward : c.transform.forward;
            Vector3 wanted = !c.Arrived && travel.magnitude > Walking ? travel : ahead;
            wanted.y = 0f;
            if (wanted.sqrMagnitude < 1e-6f)
            {
                return 0f;
            }
            Quaternion facing = Quaternion.LookRotation(wanted);
            float delta = Utils.GetYawDeltaAngle(p.transform.rotation, facing);
            p.transform.rotation = Quaternion.RotateTowards(p.transform.rotation, facing, TurnRate * dt);
            return Mathf.Abs(delta) > 1f ? TurnRate * Mathf.Sign(delta) * Mathf.Deg2Rad : 0f;
        }

        private static ItemDrop.ItemData? Missile(Player p) => p.GetInventory().GetAmmoItem(BallistaMissile.AmmoType);

        private static void Set(BallistaControl c, Spring spring, Act act)
        {
            ZDO zdo = c.Net.GetZDO();
            zdo.Set(BallistaKeys.Spring, (int)spring);
            zdo.Set(BallistaKeys.Act, (int)act);
        }
    }
}
