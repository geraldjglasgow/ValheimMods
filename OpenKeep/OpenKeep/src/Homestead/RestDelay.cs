using UnityEngine;

namespace OpenKeep.Homestead
{
    /// <summary>
    /// The seconds of resting before Rested under section 8, read at use time. Only the game's own Resting status effect
    /// (the one <c>Player.UpdateEnvStatusEffects</c> adds) follows the setting; any other <c>SE_Cozy</c> keeps its delay.
    /// </summary>
    public static class RestDelay
    {
        private static float loggedSeconds = -1f;

        /// <summary>True for the game's Resting effect: the SEMan's copy keeps the asset's name, so its hash matches.</summary>
        public static bool IsResting(StatusEffect effect)
        {
            return effect.NameHash() == SEMan.s_statusEffectResting;
        }

        /// <summary>The wait the setting asks for, never below 0.</summary>
        public static float Seconds()
        {
            float seconds = Mathf.Max(0f, RestSettings.RestedDelay.Value);
            LogChange(seconds);
            return seconds;
        }

        /// <summary>The delay of the Resting asset in the item database (never changed here), or the value read from the assets.</summary>
        private static float GameSeconds()
        {
            SE_Cozy resting = ObjectDB.instance != null ? ObjectDB.instance.GetStatusEffect(SEMan.s_statusEffectResting) as SE_Cozy : null;
            return resting != null ? resting.m_delay : RestSettings.GameDelay;
        }

        /// <summary>One log line on this machine whenever the wait it applies changes (a setting, the server's value).</summary>
        private static void LogChange(float seconds)
        {
            if (Mathf.Approximately(seconds, loggedSeconds))
                return;
            loggedSeconds = seconds;
            Plugin.Log.LogInfo($"OpenKeep: Rested comes after {seconds:0.#} s of resting (the game's own delay is {GameSeconds():0.#} s)");
        }
    }
}
