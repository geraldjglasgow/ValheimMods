using System.Collections.Generic;
using System.Linq;
using BepInEx.Bootstrap;
using DevBridge.Server;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DevBridge.Routes
{
    /// <summary>/status: where the game is and what is open, in one call.</summary>
    internal static class StatusRoute
    {
        internal static void Register(Router router) => router.Add("/status",
            "/status                game state (starting/menu/loading/ingame/server), world, network role, player, open screens, time, plugins",
            request => request.Json(Build()));

        private static Dictionary<string, object> Build() => new Dictionary<string, object>
        {
            ["bridge"] = DevBridgePlugin.PluginVersion,
            ["port"] = DevBridgePlugin.Instance.Port,
            ["state"] = State(),
            ["scene"] = SceneManager.GetActiveScene().name,
            ["network"] = Network(),
            ["player"] = PlayerInfo(),
            ["screens"] = Screens(),
            ["time"] = TimeInfo(),
            ["display"] = Display(),
            ["plugins"] = Chainloader.PluginInfos.Values.Select(p => p.Metadata.Name + " " + p.Metadata.Version).OrderBy(n => n).ToList(),
        };

        internal static string State()
        {
            if (Player.m_localPlayer) return "ingame";
            if (Game.instance) return ZNet.instance && ZNet.instance.IsDedicated() ? "server" : "loading";
            return FejdStartup.instance ? "menu" : "starting";
        }

        private static object Network()
        {
            ZNet net = ZNet.instance;
            if (!net) return null;
            return new Dictionary<string, object>
            {
                ["world"] = ZNet.World?.m_name,
                ["role"] = Role(net),
                ["server"] = net.IsServer(),
                ["dedicated"] = net.IsDedicated(),
                ["peers"] = net.GetPeers().Count,
            };
        }

        private static object PlayerInfo()
        {
            Player player = Player.m_localPlayer;
            if (!player) return null;
            GameObject hover = player.GetHoverObject();
            return new Dictionary<string, object>
            {
                ["name"] = player.GetPlayerName(),
                ["position"] = Fmt.V3(player.transform.position),
                ["biome"] = player.GetCurrentBiome().ToString(),
                ["health"] = $"{player.GetHealth():0.#}/{player.GetMaxHealth():0.#}",
                ["stamina"] = $"{player.GetStamina():0.#}/{player.GetMaxStamina():0.#}",
                ["dead"] = player.IsDead(),
                ["hover"] = hover ? Utils.GetPrefabName(hover) : null,
                ["god"] = player.InGodMode(),
                ["ghost"] = player.InGhostMode(),
                ["devcommands"] = Terminal.m_cheat,
            };
        }

        private static string Role(ZNet net)
        {
            if (net.IsDedicated()) return "dedicated server";
            if (!net.IsServer()) return "client";
            return ZNet.IsSinglePlayer ? "single player" : "host";
        }

        private static List<string> Screens()
        {
            var open = new List<string>();
            if (InventoryGui.IsVisible()) open.Add("inventory");
            if (Menu.IsVisible()) open.Add("menu");
            if (global::Console.IsVisible()) open.Add("console");
            if (TextInput.IsVisible()) open.Add("textinput");
            if (StoreGui.IsVisible()) open.Add("trader");
            if (Hud.IsPieceSelectionVisible()) open.Add("build");
            if (Minimap.instance && Minimap.instance.m_mode == Minimap.MapMode.Large) open.Add("map");
            if (Hud.instance && Hud.instance.m_loadingScreen.gameObject.activeInHierarchy) open.Add("loadingscreen");
            return open;
        }

        private static object TimeInfo()
        {
            EnvMan env = EnvMan.instance;
            return new Dictionary<string, object>
            {
                ["day"] = env ? env.GetDay() : (int?)null,
                ["dayFraction"] = env ? Fmt.R(env.GetDayFraction()) : (float?)null,
                ["timeScale"] = Time.timeScale,
                ["fps"] = Mathf.RoundToInt(1f / Mathf.Max(0.0001f, Time.smoothDeltaTime)),
                ["frame"] = Time.frameCount,
            };
        }

        private static object Display() => new Dictionary<string, object>
        {
            ["screen"] = new[] { Screen.width, Screen.height },
            ["focused"] = Application.isFocused,
            ["cursor"] = Cursor.lockState.ToString(),
            ["cursorVisible"] = Cursor.visible,
        };
    }
}
