using EliteCreaturesReborn.Config;
using EliteCreaturesReborn.Runtime;
using EliteCreaturesReborn.Util;
using PlateColumn;
using UnityEngine;

namespace EliteCreaturesReborn.Display
{
    /// <summary>
    /// The world tier under the minimap: a small box in the PlateColumn library's HUD row (shared with our other mods;
    /// OpenKeep puts the weight beside it) showing the globe and the tier alone ("3") as the inventory's plate does
    /// (<see cref="TierPlate"/>, whose tooltip names the number of tiers; this box takes no pointer), so the tier is in view without opening the inventory. Made the first time there is a
    /// tier to show, written every frame the HUD updates but only when the tier changes, hidden while world tiers are off
    /// or by the player's display setting, and never made without PackPanel (the user's call, 2026-09-29: Elite Creatures
    /// Reborn adds no UI of its own; <see cref="Column.Active"/>). Read from the world's boss keys, which the server already sends every player;
    /// nothing is sent.
    /// </summary>
    internal static class TierHud
    {
        private const string Id = "elitecreaturesreborn_world_tier";
        private const int Rank = Column.WeightRank + 100;
        private const string GlobeResource = "EliteCreaturesReborn.assets.globe.png";

        /// <summary>Seconds between tries while the box cannot be made (no minimap yet, the inventory's plates missing).</summary>
        private const float RetrySeconds = 1f;

        private static Plate? _box;
        private static Sprite? _globe;
        private static bool _globeRead;
        private static float _nextTry;
        private static int _shownTier = -1;
        private static int _shownCeiling = -1;

        public static void Refresh()
        {
            bool show = Configuration.ShowWorldTierOnHud.Value && Player.m_localPlayer != null && WorldTier.Ceiling() > 0 && Column.Active;
            Plate? box = Current(show);
            if (box == null)
            {
                return;
            }
            if (box.Rect.gameObject.activeSelf != show)
            {
                box.Rect.gameObject.SetActive(show);
            }
            if (show)
            {
                Write(box);
            }
        }

        /// <summary>The box already made while it lives, else a new one when there is a tier to show; null otherwise.</summary>
        private static Plate? Current(bool show)
        {
            if (_box != null && _box.Rect)
            {
                return _box;
            }
            InventoryGui? gui = InventoryGui.instance;
            if (!show || gui == null || Time.time < _nextTry || Globe() == null)
            {
                return null;
            }
            _nextTry = Time.time + RetrySeconds;
            _box = HudRow.Add(gui, new PlateSpec(Id, Rank, _globe, withText: true, "", ""));
            _shownTier = _shownCeiling = -1;
            return _box;
        }

        /// <summary>The globe icon, read once; a missing image is logged once and leaves the box out.</summary>
        private static Sprite? Globe()
        {
            if (_globe == null && !_globeRead)
            {
                _globeRead = true;
                _globe = EmbeddedSprite.Load(typeof(TierHud).Assembly, GlobeResource, "ecr_globe_hud");
                if (_globe == null)
                {
                    Log.Warn($"could not read the embedded image {GlobeResource}; no world tier under the minimap");
                }
            }
            return _globe;
        }

        private static void Write(Plate box)
        {
            int tier = WorldTier.Current();
            int ceiling = WorldTier.Ceiling();
            if (box.Text == null || (tier == _shownTier && ceiling == _shownCeiling))
            {
                return;
            }
            box.Text.text = tier.ToString();
            _shownTier = tier;
            _shownCeiling = ceiling;
        }
    }
}
