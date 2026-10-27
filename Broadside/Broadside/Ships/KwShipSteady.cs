using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// A steadier view on a Broadside ship. On a ship the game pins the camera to the player's body with no smoothing,
    /// so every heave of the hull on the waves (half a metre and more on a big ship) moved the whole view. On deck the
    /// camera's height now follows the player's a little behind (<see cref="Lag"/> seconds), so the horizon stays calm and
    /// the player rises and falls a little in the view instead; across, it follows exactly. Never lower than the floor
    /// the camera was placed over this frame (<see cref="KwShipCamera.Floor"/>: the deck or roof under it, the sea), so
    /// lagging behind a rising ship never takes it into the hold or under the water. Not below decks, where the view is
    /// the player's eyes (<see cref="KwShipEyes"/>) and the hold must stay put around him. Local only.
    /// </summary>
    public static class KwShipSteady
    {
        private const float Lag = 0.6f, Jump = 3f;
        private static float height, speed;
        private static bool holding;

        /// <summary>Called with the camera placed for this frame: steadies its height while the player is on our ship.</summary>
        public static void Hold(GameCamera game, Player player)
        {
            if (KwShipCamera.Aboard(player) == null)
            {
                holding = false;
                return;
            }
            Vector3 at = game.transform.position;
            if (!holding || Mathf.Abs(at.y - height) > Jump)
                (height, speed, holding) = (at.y, 0f, true);
            height = Mathf.SmoothDamp(height, at.y, ref speed, Lag, Mathf.Infinity, Time.deltaTime);
            float floor = KwShipCamera.Floor;
            if (height < floor)
                (height, speed) = (floor, Mathf.Max(speed, 0f));
            game.transform.position = new Vector3(at.x, height, at.z);
        }

        /// <summary>Lets go: the next steady view starts from where the camera is.</summary>
        public static void Reset() => holding = false;
    }
}
