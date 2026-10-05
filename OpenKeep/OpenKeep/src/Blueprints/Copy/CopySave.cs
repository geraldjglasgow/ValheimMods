using System;
using System.IO;
using OpenKeep.Core;
using UnityEngine;

namespace OpenKeep.Blueprints.Copy
{
    /// <summary>
    /// Enter with a selection: asks for a name in the game's text box (<see cref="NamePrompt"/>, offering "Building N",
    /// the first number not taken in the folder the menu shows), then makes a blueprint of exactly the selected pieces
    /// (<see cref="BlueprintCapture.Of"/>, its front the side that faced the camera when Enter was pressed) and saves it
    /// there (<see cref="BlueprintLibrary.SaveNew"/>). A refused name says why and keeps the selection; a saved blueprint
    /// clears it. The frame the text box closes in takes no keys, so the Enter that confirmed the name saves nothing again.
    /// </summary>
    public static class CopySave
    {
        private const int NameLimit = 40;

        private static float yaw;
        private static bool asking;
        private static int closedFrame = -1;

        /// <summary>The name box is open, or closed this frame (its Enter is not the tool's, nor the chat's).</summary>
        public static bool Busy => asking || closedFrame == Time.frameCount;

        public static void Ask()
        {
            yaw = BlueprintCapture.FacingYaw();
            asking = NamePrompt.Ask(CopyWords.NameTopic, DefaultName(), NameLimit, Save);
        }

        /// <summary>Per frame, whatever the tool shows: notices the name box closing (confirmed, Esc, or gone with the HUD).</summary>
        public static void Tick()
        {
            if (!asking || BoxOpen)
                return;
            asking = false;
            closedFrame = Time.frameCount;
        }

        /// <summary>The game's text box is up now (its panel, not the flag it sets once a frame).</summary>
        private static bool BoxOpen => TextInput.instance != null && TextInput.instance.m_panel != null && TextInput.instance.m_panel.activeSelf;

        private static void Save(string name)
        {
            Blueprint bp = BlueprintCapture.Of(CopySelection.Pieces, yaw);
            if (bp == null)
            {
                CopySelection.Clear();
                Messages.Center(CopyWords.Gone);
                return;
            }
            string error = Write(bp, name);
            if (error != null)
            {
                Messages.Center(BlueprintWords.Format(CopyWords.Refused, error));
                return;
            }
            Messages.Center(BlueprintWords.Format(CopyWords.Saved, name, bp.Pieces.Count));
            CopySelection.Clear();
        }

        /// <summary>Saves the blueprint under the name; the reason when it was refused or could not be written, else null.</summary>
        private static string Write(Blueprint bp, string name)
        {
            try
            {
                return BlueprintLibrary.SaveNew(bp, name, out string error) ? null : error;
            }
            catch (Exception e)
            {
                Plugin.Log.LogWarning($"OpenKeep: cannot save blueprint {name}: {e.Message}");
                return e.Message;
            }
        }

        /// <summary>"Building 1", "Building 2", ...: the first one with no file in the folder the menu shows.</summary>
        private static string DefaultName()
        {
            string folder = Path.Combine(BlueprintLibrary.Folder, BlueprintLibrary.CurrentFolder ?? "");
            for (int n = 1; ; n++)
            {
                string name = BlueprintWords.Format(CopyWords.DefaultName, n);
                if (!File.Exists(Path.Combine(folder, name + ".json")))
                    return name;
            }
        }
    }
}
