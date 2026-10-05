using System;
using System.Collections.Generic;
using UnityEngine;
using Wayfare.Core;

namespace Wayfare.SeaGates
{
    /// <summary>The ticker of the index and the picker: the server's pillar scan and its answers, the picker's session
    /// checks, the sea gate map icons, the crew's pointers and the picker's hint. Started by <see cref="SeaGateIndex.EnsureRegistered"/>, so
    /// it runs on a dedicated server too, where everything but the scan finds no map and does nothing. Each part is
    /// guarded on its own: an exception is logged once per part and the others keep running.</summary>
    internal static class SeaGateMapDriver
    {
        private static readonly HashSet<string> failed = new HashSet<string>();
        private static GameObject driver;

        internal static void EnsureRunning()
        {
            if (driver != null)
                return;
            driver = new GameObject("Wayfare.SeaGateMap") { hideFlags = HideFlags.HideAndDontSave };
            UnityEngine.Object.DontDestroyOnLoad(driver);
            driver.AddComponent<Ticker>();
        }

        private static bool Running => WayfareConfig.Enabled.Value && WayfareConfig.SeaGatesEnabled.Value && ZNet.instance != null;

        private static void Tick()
        {
            if (!Running)
            {
                Run("picker", SeaGatePicker.Close);
                Run("crew pointers", SeaGatePointers.Clear);
                Run("map icons", SeaGateMapIcons.Clear);
                return;
            }
            Run("index", SeaGateIndexServer.Tick);
            Run("picker", SeaGatePicker.Tick);
            Run("map icons", SeaGateMapIcons.Tick);
            Run("crew pointers", SeaGatePointers.Tick);
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
