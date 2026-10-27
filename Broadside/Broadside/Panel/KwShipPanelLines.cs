using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// What the ship panel's lines say (<see cref="KwShipPanelView"/>), drawn four times a second: the speed over the water
    /// (<see cref="KwSpeedometer"/>), the top speed now at the sail set and the wind (<see cref="KwShipTopSpeed"/>), the explore radius (the game's,
    /// times GrindstoneSkills' Sailing factor), the ship's health from its wear, other code's lines
    /// (<see cref="KwShipPanel"/>) and the Sailing abilities. Each line compares the numbers it shows, rounded as shown,
    /// and formats its text only when they moved.
    /// </summary>
    internal static class KwShipPanelLines
    {
        private const float VanillaExploreRadius = 100f;

        public static void Refresh(KwShipPanelView view, Ship ship, KwSeaShip sea, WearNTear? wear, float metresPerSecond)
        {
            int tenths = Mathf.RoundToInt(metresPerSecond * 36f);   // 0.1 km/h, as shown
            if (view.Speed.Changed(tenths, 0))
                view.Speed.Set("Speed", KwPanelLook.N(tenths / 10f, 1) + " km/h", "Speed", "How fast the ship moves over the water, in kilometres per hour.");
            Multiplier(view.Multiplier, ship, sea);
            Explore(view.Explore);
            Health(view.Health, wear);
            long key = Others(view);
            int count = Abilities(view);
            view.Layout(key * 64 + count, count);
        }

        // The top speed now, at the sail set and the wind as it blows (KwShipTopSpeed), with every setting's in its tooltip.
        private static void Multiplier(KwPanelRow row, Ship ship, KwSeaShip sea)
        {
            float skill = KwGrindstoneLink.Present ? KwGrindstoneLink.SpeedFactor(ship) : 1f;
            Ship.Speed setting = ship.GetSpeedSetting();
            float now = KwShipTopSpeed.At(ship, setting, skill), full = KwShipTopSpeed.At(ship, Ship.Speed.Full, skill);
            float halfSail = KwShipTopSpeed.At(ship, Ship.Speed.Half, skill), rowed = KwShipTopSpeed.At(ship, Ship.Speed.Slow, skill);
            int nowShown = Mathf.RoundToInt(now * 3.6f), fullShown = Mathf.RoundToInt(full * 3.6f), halfShown = Mathf.RoundToInt(halfSail * 3.6f);
            if (!row.Changed(nowShown * 1000 + (int)setting, fullShown * 1000 + halfShown))
                return;
            const string NL = "\n";
            string tip = "How fast the ship gets going with the wind as it blows now, steering straight on this heading:"
                + NL + "Full sail: " + Kmh(full) + NL + "Half sail: " + Kmh(halfSail) + NL + "Rowing: " + Kmh(rowed)
                + NL + "Turning, holed sails and a fallen mast slow it. Square sails want the wind behind; fore-and-aft sails sail closer to it."
                + NL + "Against the game's karve's best: x" + KwPanelLook.N(KwShipPace.Build(sea, false), 2)
                + (KwGrindstoneLink.Present ? NL + "Your Sailing skill: x" + KwPanelLook.N(skill, 2) : "");
            row.Set("Top speed", setting == Ship.Speed.Stop ? "sails furled" : Kmh(now), "Top speed", tip);
        }

        private static string Kmh(float metresPerSecond) => KwPanelLook.N(metresPerSecond * 3.6f, 0) + " km/h";

        private static void Explore(KwPanelRow row)
        {
            bool skilled = KwGrindstoneLink.Present;
            float factor = KwGrindstoneLink.ExploreFactor();
            float radius = Minimap.instance != null ? Minimap.instance.m_exploreRadius : VanillaExploreRadius;
            if (!row.Changed(Mathf.RoundToInt(radius * factor), Mathf.RoundToInt(factor * 100f) * 2 + (skilled ? 1 : 0)))
                return;
            string account = "How far around you the map uncovers as you sail. The game's own: " + KwPanelLook.N(VanillaExploreRadius, 0) + " m.";
            if (skilled)
                account += "\nYour Sailing skill: x" + KwPanelLook.N(factor, 2);
            row.Set("Explore radius", KwPanelLook.N(radius * factor, 0) + " m", "Explore radius", account);
        }

        // The ship's health now and at full, from its wear (its health kept in its ZDO, as the game keeps every piece's).
        private static void Health(KwPanelRow row, WearNTear? wear)
        {
            ZDO? zdo = wear != null && wear.m_nview != null && wear.m_nview.IsValid() ? wear.m_nview.GetZDO() : null;
            if (wear == null || zdo == null)
                return;
            float max = wear.m_health, now = Mathf.Clamp(zdo.GetFloat(ZDOVars.s_health, max), 0f, max);
            if (!row.Changed(Mathf.CeilToInt(now), Mathf.RoundToInt(max)))
                return;
            string text = Mathf.CeilToInt(now) + " / " + Mathf.RoundToInt(max);
            float share = max > 0f ? now / max : 1f;
            text = share < 0.25f ? KwPanelLook.InRed(text) : share < 0.6f ? KwPanelLook.InOrange(text) : text;
            row.Set("Ship health", text, "Ship health",
                "How much more damage the ship takes before it breaks apart. Holes below the waterline can sink it sooner. The hammer mends it near a workbench.");
        }

        /// <summary>Other code's lines drawn; returns how many there are and which show, one bit a line after the count, for the layout.</summary>
        private static long Others(KwShipPanelView view)
        {
            long shown = KwShipPanel.Lines.Count;
            foreach (KwPanelLine line in KwShipPanel.Lines)
            {
                shown = shown * 2 + (line.Shown ? 1 : 0);
                if (!line.Shown)
                    continue;
                KwPanelRow row = view.Other(line.Index);
                row.Set(line.Label, line.Value, line.Topic, line.Tip);
                row.SetBar(line.Bar);
            }
            return shown;
        }

        /// <summary>The abilities GrindstoneSkills says the player has unlocked, drawn; returns how many.</summary>
        private static int Abilities(KwShipPanelView view)
        {
            string[] ids = KwGrindstoneLink.Abilities();
            for (int i = 0; i < ids.Length; i++)
                Ability(view.Ability(i), ids[i]);
            view.HideAbilitiesFrom(ids.Length);
            return ids.Length;
        }

        private static void Ability(KwPanelRow row, string id)
        {
            string name = KwGrindstoneLink.AbilityName(id);
            string key = KwGrindstoneLink.AbilityKey(id);
            float left = KwGrindstoneLink.AbilityCooldown(id);
            float length = KwGrindstoneLink.AbilityCooldownLength(id);
            row.SetBar(left > 0f && length > 0f ? 1f - left / length : 1f);
            if (!row.Changed(Mathf.Max(0, Mathf.CeilToInt(left)), 0, name, key))
                return;
            string label = key.Length > 0 ? "[<color=yellow>" + key + "</color>] <color=#FFFFFF>" + name + "</color>" : name;
            row.Set(label, Cooldown(left), name, KwGrindstoneLink.AbilityDescription(id));
        }

        /// <summary>"Ready" in the game's orange, else the time left in grey: "42 s", "2:05".</summary>
        private static string Cooldown(float seconds)
        {
            int left = Mathf.CeilToInt(seconds);
            if (left <= 0)
                return KwPanelLook.InOrange("Ready");
            string time = left < 60 ? left + " s" : left / 60 + ":" + (left % 60).ToString("00");
            return "<color=" + KwPanelLook.Grey + ">" + time + "</color>";
        }
    }
}
