using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using UnityEngine;

namespace OpenKeep.Blueprints
{
    /// <summary>
    /// <c>openkeep blueprint list | save &lt;name&gt; [radius] [all] [replace] | undo</c>; "all" (everyone's pieces) is for
    /// admins. Refused while blueprints are switched off. The hammer's Blueprints tab picks the blueprint to place; save
    /// writes into the folder that tab shows.
    /// </summary>
    public static class BlueprintCommands
    {
        private static readonly string[] Help =
        {
            "openkeep blueprint list               the blueprints and folders in BepInEx/config/OpenKeep.Blueprints",
            "openkeep blueprint save <name> [radius] [all] [replace]   saves your pieces around the crosshair (radius 15 m) with their ground",
            "                                      into the folder the Blueprints tab shows; stand in front of the build: the side facing you",
            "                                      becomes its front",
            "openkeep blueprint undo               takes down your last construction site of this session: the site, its pieces, its ground",
        };

        public static void Run(Terminal.ConsoleEventArgs args)
        {
            string verb = args.Length > 2 ? args[2].ToLowerInvariant() : "help";
            if (!BlueprintSettings.Enabled)
                args.Context.AddString("OpenKeep: blueprints are switched off (section " + BlueprintSettings.Section + " of the cfg).");
            else if (verb == "list")
                List(args);
            else if (verb == "save" && args.Length > 3)
                Save(args, args[3]);
            else if (verb == "undo")
                BuildUndo.Run(args);
            else
                foreach (string line in Help)
                    args.Context.AddString(line);
        }

        /// <summary>Every folder and every blueprint as paths from the top folder, and the folder the tab shows.</summary>
        private static void List(Terminal.ConsoleEventArgs args)
        {
            BlueprintLibrary.Rescan();
            List<string> folders = BlueprintLibrary.Everything(folders: true);
            List<string> names = BlueprintLibrary.Everything(folders: false);
            args.Context.AddString($"OpenKeep: {names.Count} blueprints and {folders.Count} folders in {BlueprintLibrary.Folder}; " +
                "the Blueprints tab shows " + BlueprintEntries.Shown(BlueprintLibrary.CurrentFolder) + ".");
            if (folders.Count > 0)
                args.Context.AddString("  folders: " + string.Join(", ", folders));
            if (names.Count > 0)
                args.Context.AddString("  blueprints: " + string.Join(", ", names));
        }

        private static void Save(Terminal.ConsoleEventArgs args, string name)
        {
            Player player = Player.m_localPlayer;
            string refusal = SaveRefusal(player, name, HasWord(args, "replace"), HasWord(args, "all"));
            if (refusal != null)
            {
                args.Context.AddString("OpenKeep: " + refusal + ".");
                return;
            }
            float radius = Mathf.Clamp(Number(args, 4, BlueprintRules.SaveRadius), 1f, BlueprintRules.SaveMaxRadius);
            Blueprint bp = BlueprintCapture.Capture(player, Aimed(player), CameraYaw(), radius, HasWord(args, "all"), out string error);
            if (bp == null)
            {
                args.Context.AddString("OpenKeep: " + error + ".");
                return;
            }
            string path = BlueprintLibrary.Join(BlueprintLibrary.CurrentFolder, name);
            bp.Name = name;
            Directory.CreateDirectory(BlueprintLibrary.FullPath(BlueprintLibrary.CurrentFolder));
            BlueprintWriter.Write(bp, BlueprintLibrary.PathOf(path));
            BlueprintLibrary.Rescan();
            args.Context.AddString($"OpenKeep: saved {bp.Pieces.Count} pieces as {path}" + (bp.HasWater ? " (with water)" : "") + ".");
        }

        private static string SaveRefusal(Player player, string name, bool replace, bool everyone)
        {
            if (player == null)
                return "join a world first";
            if (name.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0 || name.Any(char.IsWhiteSpace))
                return $"'{name}' cannot be a file name (no spaces or special characters)";
            if (everyone && ZNet.instance != null && !ZNet.instance.LocalPlayerIsAdminOrHost())
                return "only admins may save other players' pieces";
            string path = BlueprintLibrary.Join(BlueprintLibrary.CurrentFolder, name);
            return File.Exists(BlueprintLibrary.PathOf(path)) && !replace ? $"a blueprint named '{path}' exists: add 'replace' to overwrite it" : null;
        }

        /// <summary>The ground or piece under the crosshair, or the player's feet.</summary>
        private static Vector3 Aimed(Player player)
        {
            GameCamera camera = GameCamera.instance;
            int mask = LayerMask.GetMask("terrain", "piece", "Default", "static_solid");
            if (camera != null && Physics.Raycast(camera.transform.position, camera.transform.forward, out RaycastHit hit, BlueprintRules.AimRange, mask))
                return hit.point;
            return player.transform.position;
        }

        /// <summary>The camera's yaw to a quarter turn: the side of the build facing the player becomes its front.</summary>
        private static float CameraYaw()
        {
            GameCamera camera = GameCamera.instance;
            return camera != null ? Mathf.Round(camera.transform.eulerAngles.y / 90f) * 90f : 0f;
        }

        private static bool HasWord(Terminal.ConsoleEventArgs args, string word)
        {
            for (int i = 3; i < args.Length; i++)
            {
                if (string.Equals(args[i], word, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            return false;
        }

        private static float Number(Terminal.ConsoleEventArgs args, int index, float fallback)
        {
            return args.Length > index && float.TryParse(args[index], NumberStyles.Float, CultureInfo.InvariantCulture, out float value) ? value : fallback;
        }
    }
}
