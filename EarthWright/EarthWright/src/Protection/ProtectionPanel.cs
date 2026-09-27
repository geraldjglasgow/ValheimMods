using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using BepInEx.Configuration;
using EarthWright.Core;
using UnityEngine;

namespace EarthWright.Protection
{
    /// <summary>
    /// The admin section of the EarthWright panel (drawn by the Preview module for admins only): the terrain lock, who
    /// may use terrain tools, the zone mode, and the zone list with a remove button per zone and a row to add one at the
    /// admin's position. Zone buttons send the same requests as the console commands; the server checks and answers.
    /// The setting buttons change the synced settings, which Charter sends to the server when the server binds its
    /// configuration (admins may amend it); without that binding they would only change this machine, so they are
    /// hidden then, except on the server itself.
    /// </summary>
    public static class ProtectionPanel
    {
        private const int Order = 900;
        private const float ButtonWidth = 130f;

        private static string newName = "";
        private static string newRadius = "30";
        private static string newPlayer = "";

        public static void Register()
        {
            PanelSections.Add(Order, PanelWords.Title, () => Safe.Run("EarthWright protection panel", Draw), adminOnly: true);
        }

        private static void Draw()
        {
            DrawSetting(PanelWords.Lock, TerrainLock.Locked ? PanelWords.On : PanelWords.Off,
                TerrainLock.Locked ? PanelWords.UnlockButton : PanelWords.LockButton, () => ProtectionSettings.LockTerrain.Value = !TerrainLock.Locked);
            DrawSetting(PanelWords.Tools, ProtectionSettings.ToolsAllowed.Value.ToString(), PanelWords.Next, () => Cycle(ProtectionSettings.ToolsAllowed));
            DrawSetting(ZoneWords.Mode, ProtectionSettings.Zones.Value.ToString(), PanelWords.Next, () => Cycle(ProtectionSettings.Zones));
            DrawZones();
            DrawAddRow();
        }

        private static void DrawSetting(string label, string value, string button, Action change)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(L(label) + ": " + L(value));
            if (CanChangeSettings && GUILayout.Button(L(button), GUILayout.Width(ButtonWidth)))
                change();
            GUILayout.EndHorizontal();
        }

        private static void DrawZones()
        {
            List<AdminZone> zones = ZoneBook.Current.ToList();
            GUILayout.Label(L(PanelWords.Zones) + " (" + zones.Count + "/" + AdminZone.MaxZones + ")");
            if (zones.Count == 0)
                GUILayout.Label("  " + L(ZoneWords.NoZones));
            foreach (AdminZone zone in zones)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label("  " + zone.Describe());
                if (GUILayout.Button(L(PanelWords.Remove), GUILayout.Width(ButtonWidth)))
                    ZoneRpc.SendRemove(zone.Name);
                GUILayout.EndHorizontal();
            }
        }

        private static void DrawAddRow()
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(L(PanelWords.Name));
            newName = GUILayout.TextField(newName, AdminZone.MaxNameLength, GUILayout.Width(110f));
            GUILayout.Label(L(PanelWords.Radius));
            newRadius = GUILayout.TextField(newRadius, 5, GUILayout.Width(45f));
            GUILayout.EndHorizontal();
            // Two rows: one row is wider than the panel.
            GUILayout.BeginHorizontal();
            GUILayout.Label(L(PanelWords.Player));
            newPlayer = GUILayout.TextField(newPlayer, 40, GUILayout.Width(110f));
            if (GUILayout.Button(L(PanelWords.AddHere), GUILayout.Width(ButtonWidth)))
                AddHere();
            GUILayout.EndHorizontal();
        }

        private static void AddHere()
        {
            Player player = Player.m_localPlayer;
            bool radiusOk = float.TryParse(newRadius, NumberStyles.Float, CultureInfo.InvariantCulture, out float radius);
            if (player == null || !radiusOk || !AdminZone.ValidName(newName.Trim()))
            {
                Messages.Center(player == null ? ZoneWords.NoPlayer : !radiusOk ? ZoneWords.Usage : ZoneWords.BadName);
                return;
            }
            Vector3 at = player.transform.position;
            ZoneRpc.SendAdd(new AdminZone { Name = newName.Trim(), X = at.x, Z = at.z, Radius = AdminZone.ClampRadius(radius), Player = newPlayer.Trim() });
            newName = "";
            newPlayer = "";
        }

        /// <summary>Setting changes reach the server: this is the server, or the server binds its configuration.</summary>
        private static bool CanChangeSettings => Side.IsServer || (Plugin.Synced != null && Plugin.Synced.Sync.IsBound);

        private static void Cycle<T>(ConfigEntry<T> entry) where T : struct, Enum
        {
            T[] values = (T[])Enum.GetValues(typeof(T));
            int index = Array.IndexOf(values, entry.Value);
            entry.Value = values[(index + 1) % values.Length];
        }

        private static string L(string text) => Language.Localize(text);
    }
}
