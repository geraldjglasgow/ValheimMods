using OpenKeep.Core;
using UnityEngine;

namespace OpenKeep.Blueprints
{
    /// <summary>
    /// The keys while a blueprint is selected in the hammer's Blueprints tab, fixed (<see cref="BlueprintRules"/>; the
    /// tab picks the blueprint): the arrows turn it a quarter, Alt + arrows a degree (repeating while held), Home turns
    /// it back to face you, PageUp and PageDown move its floor (Alt for big steps), End puts the floor back on the
    /// ground and Backspace lets go of a pinned blueprint. Nothing fires while text is typed. Alt, not Ctrl or Shift:
    /// those move the build camera down and up (the user's rule).
    /// </summary>
    public static class BlueprintKeys
    {
        public static void Handle()
        {
            Player player = Player.m_localPlayer;
            if (player == null || Hud.instance == null || !player.TakeInput() || Keys.TextInputActive)
                return;
            if (Alt && !Input.GetKey(KeyCode.Tab))
            {
                BlueprintSession.Lift(Step(BlueprintRules.UpKey) - Step(BlueprintRules.DownKey));
                BlueprintSession.Nudge(FineTurn.Step(BlueprintRules.TurnRightKey, BlueprintRules.TurnLeftKey));
                return;
            }
            if (Input.GetKeyDown(BlueprintRules.TurnRightKey))
                BlueprintSession.Turn(1);
            if (Input.GetKeyDown(BlueprintRules.TurnLeftKey))
                BlueprintSession.Turn(-1);
            if (Input.GetKeyDown(BlueprintRules.FaceKey))
                BlueprintSession.ResetTurn();
            BlueprintSession.Lift(Step(BlueprintRules.UpKey) - Step(BlueprintRules.DownKey));
            if (Input.GetKeyDown(BlueprintRules.GroundKey))
                BlueprintSession.ResetLift();
            if (Input.GetKeyDown(BlueprintRules.ReleaseKey))
                BlueprintSession.Release(quiet: false);
        }

        /// <summary>The height step of a press this frame: the small step, the big one with Alt, or 0.</summary>
        private static float Step(KeyCode key)
        {
            if (!Input.GetKeyDown(key))
                return 0f;
            return Alt ? BlueprintRules.HeightFastStep : BlueprintRules.HeightStep;
        }

        public static bool Ctrl => Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);

        /// <summary>Alt is held: small turns, big floor steps, Alt + Z in Fix ground.</summary>
        public static bool Alt => Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt);
    }

    /// <summary>
    /// A small turn per press of one of two keys, repeating while it is held (after <see cref="BlueprintRules.FineRepeatDelay"/>,
    /// every <see cref="BlueprintRules.FineRepeat"/> s): degrees this frame, positive for the first key.
    /// </summary>
    public static class FineTurn
    {
        private static KeyCode held;
        private static float nextRepeat;

        public static float Step(KeyCode right, KeyCode left)
        {
            foreach ((KeyCode key, float sign) in new[] { (right, 1f), (left, -1f) })
            {
                if (Input.GetKeyDown(key))
                {
                    held = key;
                    nextRepeat = Time.time + BlueprintRules.FineRepeatDelay;
                    return sign * BlueprintRules.FineTurnDegrees;
                }
                if (key == held && Input.GetKey(key) && Time.time >= nextRepeat)
                {
                    nextRepeat = Time.time + BlueprintRules.FineRepeat;
                    return sign * BlueprintRules.FineTurnDegrees;
                }
            }
            return 0f;
        }
    }
}
