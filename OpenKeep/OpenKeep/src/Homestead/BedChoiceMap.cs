using UnityEngine;

namespace OpenKeep.Homestead
{
    /// <summary>
    /// The large map during the choice of bed. The game's <c>Minimap.Update</c> closes the map while the local player
    /// is dead, so <see cref="BedChoiceMapPatch"/> runs this instead then: the game's own map update (drag, zoom, pins,
    /// the biome under the cursor), every bed icon twice the size and pulsing (<see cref="BedPinLook"/>), the countdown
    /// in the map's upper left corner (<see cref="BedChoiceLabel"/>), and the end of the choice on the map key or Escape
    /// one frame after the press, so the same press does not also open the game's menu. While open the bed icons show
    /// even when the player's map filter hides them, and the map zooms out (never in) until every bed fits around the
    /// death point.
    /// </summary>
    public static class BedChoiceMap
    {
        private const float FitMargin = 1.25f;

        /// <summary>Above 2 the game's <c>Minimap.IsOpen</c> is false for a hidden map, so the cursor and the menu are the game's again.</summary>
        private const int HiddenFrames = 3;

        private static Minimap owner;
        private static bool closeNextFrame;
        private static bool bedsWereHidden;

        public static void Opened(Minimap map, Vector3 deathPoint)
        {
            owner = map;
            closeNextFrame = false;
            int bed = (int)Minimap.PinType.Bed;
            bedsWereHidden = !map.m_visibleIconTypes[bed];
            map.m_visibleIconTypes[bed] = true;
            map.SetMapMode(Minimap.MapMode.Large);
            map.m_mapOffset = Vector3.zero;
            FitZoom(map, deathPoint);
        }

        public static void Update(Minimap map, Player player, float dt)
        {
            if (closeNextFrame)
            {
                BedChoice.Confirm();
                return;
            }
            map.SetMapMode(Minimap.MapMode.Large);
            map.m_hiddenFrames = 0;
            map.m_shownFrames++;
            bool input = TakesInput();
            closeNextFrame = input && map.m_shownFrames > 1 && CloseKey();
            map.UpdateMap(player, dt, input);
            map.UpdateDynamicPins(dt);
            BedPinLook.Enlarge(map);
            map.m_pinUpdateRequired = false;
            map.UpdatePins();
            map.UpdateBiome(player);
            BedChoiceLabel.Show(map, BedChoice.SecondsLeft);
            BedChoice.Tick();
        }

        public static void Closed()
        {
            BedChoiceLabel.Hide();
            BedPinLook.Restore(owner);
            if (owner == null)
                return;
            if (bedsWereHidden)
                owner.m_visibleIconTypes[(int)Minimap.PinType.Bed] = false;
            owner.SetMapMode(Minimap.MapMode.None);
            owner.m_hiddenFrames = HiddenFrames;
            owner = null;
        }

        /// <summary>The same conditions as the game's map input, less the ones that cannot hold while dead.</summary>
        private static bool TakesInput()
        {
            return (Chat.instance == null || !Chat.instance.HasFocus()) && !Console.IsVisible() && !TextInput.IsVisible()
                && !Menu.IsActive() && !Minimap.InTextInput();
        }

        private static bool CloseKey()
        {
            return ZInput.GetButtonDown("Map") || ZInput.GetKeyDown(KeyCode.Escape) || ZInput.GetButtonDown("JoyMap")
                || ZInput.GetButtonDown("JoyButtonB");
        }

        /// <summary>Zooms out, never in, until every bed lies inside the map's height around the death point.</summary>
        private static void FitZoom(Minimap map, Vector3 deathPoint)
        {
            float farthest = 0f;
            foreach (Vector3 bed in BedChoice.Beds)
                farthest = Mathf.Max(farthest, BedPoints.MapDistance(bed, deathPoint));
            float worldHeight = map.m_textureSize * map.m_pixelSize;
            map.LargeZoom = Mathf.Max(map.LargeZoom, 2f * farthest * FitMargin / worldHeight);
        }
    }
}
