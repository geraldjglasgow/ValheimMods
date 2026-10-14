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
    /// Builds Hel's arena on demand (features/arena.md section 1, "Loaded only for those who go there"): one host per
    /// world on every peer, made by a postfix on <c>ZNetScene.Awake</c> in that world's scene, so it goes when the world is
    /// left. While the <c>Arena</c> setting is on, the arena is built where it is wanted and removed a minute after it
    /// stops being wanted:
    /// <list type="bullet">
    /// <item>On a client, a host or in single player: wanted while the local player is in the arena's area
    /// (<see cref="ArenaHall.Near"/>; tested every frame while it is not built, so a player who logs in there has a floor
    /// at once) or when a way in asks for it (<see cref="Ensure"/>, before the jump). The whole arena, look and all.</item>
    /// <item>On a dedicated server: wanted while any connected peer's position is in the arena's area (tested once a
    /// second), since the server may own creatures there. Colliders, doors and the river's kill only: no look.</item>
    /// </list>
    /// A player who never goes there never has it built, and its bundles are never read. The setting turning off removes
    /// it at once; a change arrives as the setting's SettingChanged (the server's value through Charter, or the .cfg's
    /// hot reload), applied on the host's next frame, on the main thread.
    /// </summary>
    internal sealed class HallHost : MonoBehaviour
    {
        private const float Linger = 60f;
        private const float Every = 1f;

        private static HallHost? _current;

        private GameObject? _hall;
        private bool _dirty = true;
        private float _wantedAt = float.NegativeInfinity;
        private float _next;

        public static void Attach(ZNetScene scene)
        {
            var host = new GameObject("ECR_HelArena");
            SceneManager.MoveGameObjectToScene(host, scene.gameObject.scene);
            host.transform.SetPositionAndRotation(ArenaHall.Origin, Quaternion.identity);
            host.AddComponent<HallHost>();
            HallWords.Add(Localization.instance);
        }

        /// <summary>Builds the arena now when the setting is on (a way in, before its jump); true when it stands.</summary>
        public static bool Ensure()
        {
            HallHost? host = _current;
            if (host == null || !ArenaSettings.Enabled)
            {
                return false;
            }
            host._wantedAt = Time.time;
            if (host._hall == null)
            {
                SafeCall.Run("Hel's arena", static h => h.Build(), host);
            }
            return ArenaHall.Built;
        }

        private void Awake()
        {
            _current = this;
            if (ArenaSettings.Arena != null)
            {
                ArenaSettings.Arena.SettingChanged += OnChanged;
            }
        }

        private void OnDestroy()
        {
            if (_current == this)
            {
                _current = null;
            }
            if (ArenaSettings.Arena != null)
            {
                ArenaSettings.Arena.SettingChanged -= OnChanged;
            }
            Tear();
        }

        private void OnChanged(object sender, EventArgs args) => _dirty = true;

        private void Update()
        {
            if (_dirty)
            {
                _dirty = false;
                if (!ArenaSettings.Enabled && _hall != null)
                {
                    SafeCall.Run("Hel's arena", static h => h.Remove("the Arena setting is off"), this);
                }
            }
            if (!ArenaSettings.Enabled || ((_hall != null || HallBuilder.NoScreen) && Time.time < _next))
            {
                return;
            }
            _next = Time.time + Every;
            Step();
        }

        // Built when wanted; removed once it has not been wanted for Linger seconds. The server's one-time move of the
        // old hall's graves comes first (a reference test once it is done).
        private void Step()
        {
            SafeCall.Run("Hel's arena old graves", static () => HallLegacyGraves.MoveOnce());
            bool wanted = Wanted();
            if (wanted)
            {
                _wantedAt = Time.time;
            }
            if (_hall == null && wanted)
            {
                SafeCall.Run("Hel's arena", static h => h.Build(), this);
            }
            else if (_hall != null && Time.time - _wantedAt > Linger)
            {
                SafeCall.Run("Hel's arena", static h => h.Remove("nobody has been there for a minute"), this);
            }
        }

        private static bool Wanted() => HallBuilder.NoScreen ? AnyPeerNear() : LocalNear();

        private static bool LocalNear()
        {
            Player? player = Player.m_localPlayer;
            return player != null && ArenaHall.Near(player.transform.position);
        }

        private static bool AnyPeerNear()
        {
            ZNet? net = ZNet.instance;
            if (net == null)
            {
                return false;
            }
            foreach (ZNetPeer peer in net.GetPeers())
            {
                if (peer.IsReady() && ArenaHall.Near(peer.m_refPos))
                {
                    return true;
                }
            }
            return false;
        }

        private void Build()
        {
            _hall = HallBuilder.Build(transform);
            ArenaHall.Built = _hall != null;
            if (_hall != null)
            {
                string what = HallBuilder.NoScreen ? "colliders only" : "colliders and look";
                Log.Info($"Hel's arena is built at {ArenaHall.Origin} ({what}).");
            }
        }

        private void Remove(string why)
        {
            Tear();
            Log.Info($"Hel's arena is taken down: {why}.");
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

    /// <summary>Makes the arena's host as each world's scene wakes (<see cref="HallHost"/>). Never throws out of
    /// ZNetScene.Awake: a throw there leaves the world loading for ever.</summary>
    [HarmonyPatch(typeof(ZNetScene), nameof(ZNetScene.Awake))]
    internal static class HallScenePatch
    {
        [HarmonyPostfix]
        private static void Postfix(ZNetScene __instance) =>
            SafeCall.Run("Hel's hall host", static scene => HallHost.Attach(scene), __instance);
    }
}
