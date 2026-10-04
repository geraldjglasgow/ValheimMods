using System;
using System.Collections.Generic;
using UnityEngine;
using Wayfare.Core;

namespace Wayfare.SeaGates
{
    /// <summary>One ticker for the sea gate modules that work every frame. Each module's tick is guarded on its own: an
    /// exception is logged once per module and the others keep running.</summary>
    public static class SeaGateDriver
    {
        private static readonly HashSet<string> failed = new HashSet<string>();
        private static GameObject driver;

        public static void EnsureRunning()
        {
            if (driver != null)
                return;
            driver = new GameObject("Wayfare.SeaGates") { hideFlags = HideFlags.HideAndDontSave };
            UnityEngine.Object.DontDestroyOnLoad(driver);
            driver.AddComponent<Ticker>();
        }

        private static bool On => WayfareConfig.Enabled.Value && WayfareConfig.SeaGatesEnabled.Value;

        private static bool surfacesShown;

        /// <summary>The ship jump runs whatever the settings say: it starts new jumps only while sea gates are on, but a
        /// jump under way must reach its release, or a frozen ship would stay frozen. Switching sea gates off clears the
        /// surfaces once.</summary>
        private static void Tick()
        {
            if (ZNet.instance == null)
                return;
            Run("ship jump", ShipJump.Tick);
            if (On)
                Run("pairing", SeaGatePairing.Tick);
            if (!On || Player.m_localPlayer == null)
            {
                if (surfacesShown)
                    Run("surface", SeaGateSurface.Clear);
                surfacesShown = false;
                return;
            }
            surfacesShown = true;
            Run("surface", SeaGateSurface.Tick);
            Run("cargo", CargoCheck.Tick);
        }

        private static void Run(string name, Action tick)
        {
            try
            {
                tick();
            }
            catch (Exception e)
            {
                if (failed.Add(name))
                    Plugin.Log.LogError($"Sea gate {name} failed (logged once): {e}");
            }
        }

        private sealed class Ticker : MonoBehaviour
        {
            private void Update() => Tick();
        }
    }
}
