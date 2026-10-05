using OpenKeep.BuildCamera;

namespace OpenKeep.Blueprints
{
    /// <summary>
    /// With an entry of the hammer's Blueprints tab selected the build camera (section 11) comes out by itself, so a
    /// blueprint is placed, a ground fix aimed and a ghost building planned from the air: it flies anywhere within
    /// <see cref="CameraArea.PlayerRange"/> m of the player instead of only near a crafting station (the camera's own
    /// switch and entry needs still count). The camera's Toggle Key puts it back, and it then stays back until another
    /// piece is picked or the hammer is put away, which also brings a camera this class brought out back.
    /// </summary>
    public static class BlueprintCamera
    {
        private static bool broughtOut;
        private static bool declined;

        /// <summary>An entry of the Blueprints tab is selected in build mode with blueprints on (the camera's area is then round the player).</summary>
        public static bool EntryOut => BlueprintMenu.InHand(Player.m_localPlayer);

        public static void Tick()
        {
            Player player = Player.m_localPlayer;
            if (!EntryOut)
            {
                Leave();
                return;
            }
            if (CameraState.Active)
                return;
            if (broughtOut)
            {
                broughtOut = false;
                declined = true;
            }
            if (!declined && CanBringOut(player))
                BringOut(player);
        }

        private static bool CanBringOut(Player player)
        {
            return CameraSettings.Enabled.Value && CameraToggle.CanUse(player) && !Hud.IsPieceSelectionVisible()
                && CameraNeeds.Met(player, CameraSettings.EntryNeedsResting, CameraSettings.EntryMinComfort);
        }

        private static void BringOut(Player player)
        {
            CameraState.Enter(player, GameCamera.instance.transform);
            CameraState.Position = CameraArea.Clamp(GameCamera.instance.transform.position);
            broughtOut = true;
        }

        /// <summary>The entry went away (another piece, the hammer put away): a camera brought out here goes back, and the next entry brings it out again.</summary>
        private static void Leave()
        {
            if (broughtOut && CameraState.Active)
                CameraState.Exit();
            broughtOut = false;
            declined = false;
        }
    }
}
