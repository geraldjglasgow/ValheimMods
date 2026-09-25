using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace EliteCrafting.Effects
{
    /// <summary>
    /// What other players see of a player's affixes, drawn by every client from the published stats
    /// (<see cref="PlayerStats"/>): Hearthlight's soft light on the player, and Mistbane's wider mist clearing around
    /// the player's own demister (the mist is drawn per client, so each client widens the demisters of the players it
    /// sees). Once a second, over the loaded players and demisters only; nothing on a headless server.
    /// </summary>
    internal static class PlayerVisuals
    {
        private const float Interval = 1f;
        private const string LightName = "ECF_Hearthlight";

        /// <summary>Judgement calls: a warm, shadowless light a little above the head, 6 m reach.</summary>
        private static readonly Color LightColor = new Color(1f, 0.82f, 0.55f);

        private static readonly List<Demister> Scaled = new List<Demister>();
        private static readonly List<float> BaseRange = new List<float>();
        private static float _next;
        private static bool? _headless;

        public static void Tick()
        {
            if (Time.time < _next || Headless)
            {
                return;
            }
            _next = Time.time + Interval;
            foreach (Player player in Player.GetAllPlayers())
            {
                UpdateLight(player, PlayerStats.Of(player, PlayerStats.Light) > 0f);
            }
            UpdateDemisters();
        }

        private static bool Headless => _headless ??= SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null;

        private static void UpdateLight(Player player, bool on)
        {
            Transform? existing = player.transform.Find(LightName);
            if (existing != null)
            {
                if (existing.gameObject.activeSelf != on)
                {
                    existing.gameObject.SetActive(on);
                }
                return;
            }
            if (on)
            {
                CreateLight(player.transform);
            }
        }

        private static void CreateLight(Transform parent)
        {
            GameObject holder = new GameObject(LightName);
            holder.transform.SetParent(parent, false);
            holder.transform.localPosition = new Vector3(0f, 2.2f, 0f);
            Light light = holder.AddComponent<Light>();
            light.type = LightType.Point;
            light.range = 6f;
            light.intensity = 1.1f;
            light.color = LightColor;
            light.shadows = LightShadows.None;
        }

        // Each demister under a player gets that player's published radius bonus over the range it had when first seen.
        private static void UpdateDemisters()
        {
            for (int i = Scaled.Count - 1; i >= 0; i--)
            {
                if (Scaled[i] == null)
                {
                    Scaled.RemoveAt(i);
                    BaseRange.RemoveAt(i);
                }
            }
            foreach (Demister demister in Demister.GetDemisters())
            {
                Player? owner = demister != null && demister.m_forceField != null ? demister.GetComponentInParent<Player>() : null;
                if (owner != null)
                {
                    Scale(demister!, PlayerStats.Of(owner, PlayerStats.Demist));
                }
            }
        }

        private static void Scale(Demister demister, float bonus)
        {
            int i = Scaled.IndexOf(demister);
            if (i < 0)
            {
                Scaled.Add(demister);
                BaseRange.Add(demister.m_forceField.endRange);
                i = Scaled.Count - 1;
            }
            float range = BaseRange[i] * (1f + bonus);
            if (demister.m_forceField.endRange != range)
            {
                demister.m_forceField.endRange = range;
            }
        }
    }
}
