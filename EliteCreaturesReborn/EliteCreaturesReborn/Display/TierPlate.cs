using EliteCreaturesReborn.Config;
using EliteCreaturesReborn.Runtime;
using EliteCreaturesReborn.Util;
using PlateColumn;
using UnityEngine;

namespace EliteCreaturesReborn.Display
{
    /// <summary>
    /// The world tier on the inventory screen: a plate at the bottom of the player panel's stat column, under the
    /// weight, showing a globe and the tier alone ("3"), with a tooltip naming how many tiers there are and saying what
    /// the tier does. The column (the PlateColumn library,
    /// shared with our other mods) moves the game's plates up to make room, spaces them evenly, and closes the gap again
    /// when the plate is hidden - with world tiers off, or by the player's display setting. The plate is made the first
    /// time the inventory is open with a tier to show. Read from the world's boss keys, which the server already sends
    /// every player; nothing is sent.
    /// </summary>
    internal static class TierPlate
    {
        private const string Id = "elitecreaturesreborn_world_tier";
        private const int Rank = Column.WeightRank + 100;
        private const string Topic = "World tier";
        private const string Tip = "The tier rises the first time each boss is defeated, and each tier makes stars and "
            + "mutations more common in every biome.";
        private const string GlobeResource = "EliteCreaturesReborn.assets.globe.png";

        private static Plate? _plate;
        private static Sprite? _globe;
        private static bool _globeRead;
        private static InventoryGui? _failedOn;
        private static int _shownTier = -1;
        private static int _shownCeiling = -1;

        /// <summary>Each frame the inventory is open, after the game has written the weight.</summary>
        public static void Refresh(InventoryGui gui)
        {
            bool show = Configuration.ShowWorldTier.Value && WorldTier.Ceiling() > 0;
            Plate? plate = Current(gui, show);
            if (plate == null)
            {
                return;
            }
            GameObject go = plate.Rect.gameObject;
            if (go.activeSelf != show)
            {
                go.SetActive(show);
                Column.Arrange(gui); // close or open the gap
            }
            if (show)
            {
                Write(gui);
            }
        }

        /// <summary>
        /// The plate to update: the one already made while the panel it is on lives, else a new one when there is a tier
        /// to show; null when there is nothing to update.
        /// </summary>
        private static Plate? Current(InventoryGui gui, bool show)
        {
            if (_plate != null && _plate.Rect)
            {
                return _plate;
            }
            return show && Create(gui) ? _plate : null;
        }

        /// <summary>Adds the plate to this panel's column; tried once per panel, so a failure is not retried every frame.</summary>
        private static bool Create(InventoryGui gui)
        {
            if (_failedOn == gui || Globe() == null)
            {
                return false;
            }
            _plate = Column.Add(gui, Spec(WorldTier.Ceiling()));
            _shownTier = _shownCeiling = -1;
            if (_plate?.Text == null)
            {
                _failedOn = gui;
                _plate = null;
                Log.Warn("the world tier plate could not be added: the inventory's armor or weight plate is missing");
                return false;
            }
            return true;
        }

        /// <summary>The globe icon, read once; a missing image is logged once and leaves the plate out.</summary>
        private static Sprite? Globe()
        {
            if (_globe == null && !_globeRead)
            {
                _globeRead = true;
                _globe = EmbeddedSprite.Load(typeof(TierPlate).Assembly, GlobeResource, "ecr_globe");
                if (_globe == null)
                {
                    Log.Warn($"could not read the embedded image {GlobeResource}; no world tier plate");
                }
            }
            return _globe;
        }

        /// <summary>The plate's spec; its tooltip names the number of tiers, which the rule file sets.</summary>
        private static PlateSpec Spec(int ceiling)
        {
            string total = $"There are {ceiling} world tiers in total. ";
            return new PlateSpec(Id, Rank, _globe, withText: true, Topic, total + Tip);
        }

        /// <summary>The tier when it changes; when the number of tiers changes, adding the plate again rewrites its tooltip.</summary>
        private static void Write(InventoryGui gui)
        {
            int tier = WorldTier.Current();
            int ceiling = WorldTier.Ceiling();
            if (_plate?.Text == null || (tier == _shownTier && ceiling == _shownCeiling))
            {
                return;
            }
            if (_shownCeiling >= 0 && ceiling != _shownCeiling)
            {
                Column.Add(gui, Spec(ceiling));
            }
            _plate.Text.text = tier.ToString();
            _shownTier = tier;
            _shownCeiling = ceiling;
        }
    }
}
