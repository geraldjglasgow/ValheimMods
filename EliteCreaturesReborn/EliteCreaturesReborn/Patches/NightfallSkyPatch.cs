using System;
using EliteCreaturesReborn.Aspects;
using HarmonyLib;
using PatchGuard;

namespace EliteCreaturesReborn.Patches
{
    /// <summary>
    /// The one place the game draws its weather and time of day, once per environment step on every machine with a
    /// screen, after it has decided them and before it judges whether it is day. While a Nightfall fight holds this
    /// client, the weather and the four light weights about to be drawn are blended toward the storm at midnight
    /// (<see cref="NightfallLook"/>), and the game's smoothed time of day is lent the blended time for this one call -
    /// it sets the sun's and the moon's angle - and given back by the finalizer whatever happens. So the game's day and
    /// night, its weather and everything that reads them (spawns, sleeping, the day count, rain on buildings) never
    /// change; only what is drawn does. With no Nightfall boss loaded and no night left to lift, it is one bool check.
    /// It runs inside the game's environment step, so a failure is reported and never rethrown.
    /// </summary>
    [HarmonyPatch(typeof(EnvMan), "SetEnv")]
    public static class NightfallSkyPatch
    {
        /// <summary>The game's own time of day while a blended one is lent; negative when nothing is lent.</summary>
        private static float _real = -1f;

        private static void Prefix(EnvMan __instance, ref EnvSetup env, ref float dayInt, ref float nightInt,
            ref float morningInt, ref float eveningInt)
        {
            if (NightfallBlend.Idle)
            {
                return;
            }
            try
            {
                float shown = NightfallBlend.Step(__instance);
                if (shown > 0f)
                {
                    float fraction = NightfallLook.Apply(__instance, shown, ref env, ref dayInt, ref nightInt,
                        ref morningInt, ref eveningInt);
                    _real = __instance.m_smoothDayFraction;
                    __instance.m_smoothDayFraction = fraction;
                }
            }
            catch (Exception e)
            {
                Guard.Report(e, "EnvMan.SetEnv nightfall");
            }
        }

        private static Exception? Finalizer(EnvMan __instance, Exception? __exception)
        {
            if (_real >= 0f)
            {
                __instance.m_smoothDayFraction = _real;
                _real = -1f;
            }
            return __exception;
        }
    }

    /// <summary>
    /// The local player's weather check for Wet, Cold and Freezing, once per physics step. While Nightfall's storm is
    /// drawn here the check reads the storm's flags on top of the real weather (<see cref="NightfallChill"/>), lent for
    /// this call alone and given back by the finalizer whatever happens. Any other step is a single bool check; a
    /// failure is reported, never rethrown.
    /// </summary>
    [HarmonyPatch(typeof(Player), "UpdateEnvStatusEffects")]
    public static class NightfallChillPatch
    {
        private static void Prefix(Player __instance)
        {
            if (!NightfallBlend.Storming || __instance != Player.m_localPlayer)
            {
                return;
            }
            try
            {
                NightfallChill.Begin();
            }
            catch (Exception e)
            {
                Guard.Report(e, "Player.UpdateEnvStatusEffects nightfall");
            }
        }

        private static Exception? Finalizer(Exception? __exception)
        {
            NightfallChill.End();
            return __exception;
        }
    }
}
