using System;
using EliteCreaturesReborn.Config;
using EliteCreaturesReborn.Patches;
using EliteCreaturesReborn.Util;
using HarmonyLib;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace EliteCreaturesReborn.Arena.Hall
{
    /// <summary>
    /// Keeps Hel's hall standing while the <c>Arena</c> setting is on (features/arena.md section 1, "Always there, on
    /// every peer"). One host per world on every peer, the dedicated server included: made by a postfix on
    /// <c>ZNetScene.Awake</c>, which runs on every peer as a world loads, in that scene, so it goes when the world is left.
    /// It builds the hall on its first frame (every Awake of the world's scene has run by then: the scene's prefabs and
    /// the environments are ready; a player's jump in comes seconds later), removes it when the setting turns off, and
    /// builds it again when it turns on. A change arrives as the setting's SettingChanged (the server's value through
    /// Charter, or the .cfg's hot reload) and is applied on the host's next frame, on the main thread.
    /// </summary>
    internal sealed class HallHost : MonoBehaviour
    {
        private GameObject? _hall;
        private bool _dirty = true;

        public static void Attach(ZNetScene scene)
        {
            var host = new GameObject("ECR_HelHall");
            SceneManager.MoveGameObjectToScene(host, scene.gameObject.scene);
            host.transform.SetPositionAndRotation(ArenaHall.Origin, Quaternion.identity);
            host.AddComponent<HallHost>();
            HallWords.Add(Localization.instance);
        }

        private void Awake()
        {
            if (ArenaSettings.Arena != null)
            {
                ArenaSettings.Arena.SettingChanged += OnChanged;
            }
        }

        private void OnDestroy()
        {
            if (ArenaSettings.Arena != null)
            {
                ArenaSettings.Arena.SettingChanged -= OnChanged;
            }
            Tear();
        }

        private void OnChanged(object sender, EventArgs args) => _dirty = true;

        private void Update()
        {
            if (!_dirty)
            {
                return;
            }
            _dirty = false;
            bool wanted = ArenaSettings.Enabled;
            if (wanted != (_hall != null))
            {
                SafeCall.Run("Hel's hall", wanted ? (Action)Build : Remove);
            }
        }

        private void Remove()
        {
            Tear();
            Log.Info("Hel's hall is gone: the Arena setting is off.");
        }

        private void Build()
        {
            _hall = HallBuilder.Build(transform);
            ArenaHall.Built = _hall != null;
            if (_hall != null)
            {
                string what = HallBuilder.NoScreen ? "colliders only" : "colliders and look";
                Log.Info($"Hel's hall stands at {ArenaHall.Origin} ({what}).");
            }
        }

        private void Tear()
        {
            ArenaHall.Built = false;
            HallPoints.Clear();
            if (_hall == null)
            {
                return;
            }
            HallEnvironment.Release();
            Destroy(_hall);
            _hall = null;
        }
    }

    /// <summary>Makes the hall's host as each world's scene wakes (<see cref="HallHost"/>). Never throws out of
    /// ZNetScene.Awake: a throw there leaves the world loading for ever.</summary>
    [HarmonyPatch(typeof(ZNetScene), nameof(ZNetScene.Awake))]
    internal static class HallScenePatch
    {
        [HarmonyPostfix]
        private static void Postfix(ZNetScene __instance) =>
            SafeCall.Run("Hel's hall host", static scene => HallHost.Attach(scene), __instance);
    }
}
