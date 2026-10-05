using OpenKeep.Core;

namespace OpenKeep.Blueprints
{
    /// <summary>
    /// The folders of the Blueprints tab: the folder panel's rows and the breadcrumb's parts open a folder with the menu
    /// staying open (<see cref="Open"/>); the panel's New folder button asks for a name in the game's text box over the
    /// open menu (which ignores the keys meanwhile) and makes the folder in the folder shown (<see cref="AskNewFolder"/>).
    /// </summary>
    public static class BlueprintFolders
    {
        /// <summary>Shows another folder in the tab (the menu stays open).</summary>
        public static void Open(string folder)
        {
            if (!BlueprintLibrary.Open(folder))
                BlueprintLibrary.Rescan();
            Player.m_localPlayer?.PlayButtonSound();
            BlueprintMenu.Refresh();
        }

        /// <summary>Asks for a name and makes that folder inside the folder shown.</summary>
        public static void AskNewFolder()
        {
            string parent = BlueprintLibrary.CurrentFolder;
            NamePrompt.Ask(BlueprintWords.NewFolderTopic, "", BlueprintFiles.NameLimit, name => Made(parent, name));
        }

        private static void Made(string parent, string name)
        {
            if (BlueprintFiles.CreateFolder(parent, name, out string path, out string error))
                Messages.Center(BlueprintWords.Format(BlueprintWords.FolderMade, path));
            else
                Messages.Center(error);
            BlueprintLibrary.Rescan();
            BlueprintMenu.Refresh();
        }
    }
}
