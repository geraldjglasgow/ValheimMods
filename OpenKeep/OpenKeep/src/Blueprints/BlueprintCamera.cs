using OpenKeep.BuildCamera;

namespace OpenKeep.Blueprints
{
    /// <summary>
    /// With an entry of the hammer's Blueprints tab selected the build camera (section 11) flies anywhere within
    /// <see cref="CameraArea.PlayerRange"/> m of the player instead of only near a crafting station, so a blueprint is
    /// placed, a ground fix aimed and a ghost building planned from the air. It comes out and goes back with the camera's
    /// own Toggle Key, as in normal building (the user's rule); its switch and entry needs still count.
    /// </summary>
    public static class BlueprintCamera
    {
        /// <summary>An entry of the Blueprints tab is selected in build mode with blueprints on (the camera's area is then round the player).</summary>
        public static bool EntryOut => BlueprintMenu.InHand(Player.m_localPlayer);
    }
}
