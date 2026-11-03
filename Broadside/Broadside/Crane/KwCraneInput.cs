using HarmonyLib;

namespace Broadside
{
    /// <summary>
    /// The crane's keys, the game's own bindings: while the local player works a crane (its doodad controller), Jump held
    /// raises the head and Crouch held lowers it, Attack picks up a crate and Block sets it down. The game would stop a
    /// doodad on a jump or an attack and crouch the player; this prefix on <c>Player.SetControls</c> takes those keys for
    /// the crane and passes them on as nothing. Movement and Run reach the crane through the game's own
    /// <c>ApplyControlls</c>; Use (E) leaves, as at a ship's helm.
    /// </summary>
    [HarmonyPatch(typeof(Player), nameof(Player.SetControls))]
    public static class KwCraneInput
    {
        private static bool pick, drop;

        /// <summary>Raise (1), lower (-1) or hold (0) the head, as the keys are held now.</summary>
        public static float Hoist { get; private set; }

        /// <summary>Whether Run is held (the game passes Run on only while walking, so it is read here for the hoist too).</summary>
        public static bool Run { get; private set; }

        [HarmonyPrefix]
        private static void Prefix(Player __instance, ref bool attack, ref bool attackHold, ref bool block, ref bool blockHold,
            ref bool jump, ref bool crouch, ref bool dodge)
        {
            if (__instance != Player.m_localPlayer || !(__instance.GetDoodadController() is KwCrane))
            {
                (Hoist, Run) = (0f, false);
                return;
            }
            pick |= attack;
            drop |= block;
            bool keys = __instance.TakeInput() && !Hud.InRadial();
            bool up = keys && (ZInput.GetButton("Jump") || ZInput.GetButton("JoyJump"));
            bool down = keys && (ZInput.GetButton("Crouch") || ZInput.GetButton("JoyCrouch"));
            Hoist = (up ? 1f : 0f) - (down ? 1f : 0f);
            Run = keys && (ZInput.GetButton("Run") || ZInput.GetButton("JoyRun"));
            attack = attackHold = block = blockHold = jump = crouch = dodge = false;
        }

        /// <summary>Whether Attack was pressed since the last call.</summary>
        public static bool TakePick() => Take(ref pick);

        /// <summary>Whether Block was pressed since the last call.</summary>
        public static bool TakeDrop() => Take(ref drop);

        public static void Clear() => (pick, drop, Hoist, Run) = (false, false, 0f, false);

        private static bool Take(ref bool pressed)
        {
            bool was = pressed;
            pressed = false;
            return was;
        }
    }
}
