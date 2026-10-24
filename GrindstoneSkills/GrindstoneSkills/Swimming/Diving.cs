using HarmonyLib;
using UnityEngine;

namespace GrindstoneSkills
{
    /// <summary>
    /// Diving, on the local player's own client, which simulates the player. The game keeps a swimmer at the surface:
    /// Character.UpdateSwimming (each physics step, on the owner only) pulls the body's vertical speed toward the float
    /// height, the water level less m_swimDepth. A postfix on it lets the local player leave that height:
    /// <list type="bullet">
    /// <item>Holding Crouch dives (straight down at the game's swim speed), holding Jump swims up; under water, swimming
    /// forward follows the camera up or down. With neither key nor forward movement the player drifts up slowly
    /// (<see cref="Drift"/>), or stays put on the bottom. Only Crouch starts a dive: looking down at the surface swims as
    /// in the game.</item>
    /// <item>The postfix writes the vertical speed after the game's, plus the frame's gravity, which Unity adds next.
    /// Horizontal swimming, stamina, swim experience and the swim animation stay the game's: the game counts the player
    /// as swimming at any depth below its float height.</item>
    /// <item>The dive ends when the player is back at the float height and not heading down, or stops swimming (out of
    /// the water). On the bottom the player stands; the game still counts them as swimming there.</item>
    /// </list>
    /// Breath (<see cref="Breath"/>), the camera under water (<see cref="DiveCamera"/>, <see cref="UnderwaterView"/>) and
    /// building in water (<see cref="WaterBuilder"/>) go with it. Off, the player floats as in the game.
    /// </summary>
    public static class Diving
    {
        /// <summary>Metres below the float height that still count as at the surface.</summary>
        private const float SurfaceMargin = 0.05f;

        /// <summary>Metres a second an idle diver rises, so a dive started by mistake ends by itself.</summary>
        private const float Drift = 0.4f;

        /// <summary>Seconds without a swimming step that end a dive.</summary>
        private const float StepGap = 0.25f;

        private static bool under;
        private static float lastStep = float.NegativeInfinity;

        /// <summary>Whether the local player is diving: below the float height by choice.</summary>
        public static bool Under => under && Time.time - lastStep < StepGap;

        [HarmonyPatch(typeof(Character), nameof(Character.UpdateSwimming))]
        private static class Swim
        {
            [HarmonyPostfix]
            private static void Postfix(Character __instance, float dt)
            {
                if (ReferenceEquals(__instance, Player.m_localPlayer) && GameSkillSettings.Swimming.Value)
                    HookGuard.Run("diving", static swim => Steer(swim.player, swim.dt), (player: Player.m_localPlayer, dt));
            }
        }

        private static void Steer(Player player, float dt)
        {
            if (Time.time - lastStep > StepGap)
                under = false;
            lastStep = Time.time;
            bool down = Held(player, "Crouch", "JoyCrouch");
            bool up = Held(player, "Jump", "JoyJump");
            if ((!under && !down) || player.IsDead())
                return;
            float vertical = Vertical(player, down, up);
            float floatHeight = player.GetLiquidLevel() - player.m_swimDepth;
            under = player.transform.position.y < floatHeight - SurfaceMargin || vertical < 0f;
            if (!under)
                return;
            Vector3 velocity = player.m_body.linearVelocity;
            velocity.y = vertical - Physics.gravity.y * dt;
            player.m_body.linearVelocity = velocity;
        }

        /// <summary>Metres a second up (positive) or down: the keys, else the camera while swimming forward, else the drift.</summary>
        private static float Vertical(Player player, bool down, bool up)
        {
            float speed = player.m_swimSpeed;
            if (down != up)
                return down ? -speed : speed;
            Vector3 look = player.m_lookDir;
            Vector3 flat = new Vector3(look.x, 0f, look.z).normalized;
            float forward = Vector3.Dot(player.m_moveDir, flat);
            if (forward > 0.1f)
                return look.y * speed * forward;
            return player.IsOnGround() ? 0f : Drift;
        }

        private static bool Held(Player player, string button, string joyButton) =>
            player.TakeInput() && !Hud.IsPieceSelectionVisible() && (ZInput.GetButton(button) || ZInput.GetButton(joyButton));
    }
}
