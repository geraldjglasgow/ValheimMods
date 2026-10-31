using System;
using HarmonyLib;
using UnityEngine;

namespace HighSeatPlus.Crowd
{
    /// <summary>
    /// The crowd's clock: right after the game's own per-frame updates (MonoUpdaters.Update: equipment visuals, foot steps
    /// and the rest), before Unity runs the animators and before any LateUpdate, so a stepped animator is posed when the
    /// game and other mods read or bend its bones afterwards, as it would be on its own. Every <see cref="DecideSeconds"/>
    /// it decides who is drawn simpler (<see cref="CrowdDecision"/>), then every frame it steps the animators of those
    /// drawn simpler (<see cref="SteppedAnimator"/>). It runs only in a game on a PC that draws, never on a dedicated
    /// server. With the setting off (it hot reloads) everyone is put back in full and nothing else runs. Should anything
    /// here throw, everyone is put back in full, the error is logged once and the crowd stays off for the session; the
    /// game's own update is never interrupted.
    /// </summary>
    [HarmonyPatch(typeof(MonoUpdaters), nameof(MonoUpdaters.Update))]
    internal static class CrowdTick
    {
        /// <summary>Seconds between two decisions: four a second.</summary>
        private const float DecideSeconds = 0.25f;

        private static float _nextDecision;
        private static bool _broken;

        private static void Postfix()
        {
            if (_broken)
            {
                return;
            }
            try
            {
                Tick();
            }
            catch (Exception e)
            {
                Fail(e);
            }
        }

        private static void Tick()
        {
            if (!Settings.SimplerFarPlayers.Value || !Drawing())
            {
                CrowdList.RestoreAll();
                return;
            }
            float now = Time.time;
            if (now >= _nextDecision)
            {
                _nextDecision = now + DecideSeconds;
                CrowdDecision.Decide(now);
            }
            CrowdList.StepAll(Time.deltaTime);
        }

        private static bool Drawing() => ZNet.instance != null && Game.instance != null && !ZNet.instance.IsDedicated();

        private static void Fail(Exception e)
        {
            _broken = true;
            HighSeatPlus.Log.LogWarning($"Simpler far players stopped for this session after an error, everyone is drawn in full: {e}");
            try
            {
                CrowdList.RestoreAll();
            }
            catch (Exception)
            {
                // Half-gone objects: what could be put back is back.
            }
        }
    }
}
