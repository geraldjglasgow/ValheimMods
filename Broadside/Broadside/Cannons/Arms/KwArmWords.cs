using System.Collections.Generic;
using System.Linq;
using MannedWeapons;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// A ship cannon's words: its hover (whether it is loaded, empty or reloading, who mans it or hauls it, why it cannot be
    /// taken from where the player stands, the captain's broadside key aboard a ship, what it fires) and its haul line
    /// (<see cref="KwArmTow"/>; hauled with the game's alternate use, <see cref="AltUse"/>), the messages as it is taken up
    /// and why it cannot be, why it cannot be built somewhere, the gunner's hint as he takes it, the messages when he
    /// carries nothing it fires or its load is for the other trigger, the broadside's count (and its reloads), the helm key
    /// list's broadside and shot arcs lines, the reload lines (<see cref="KwReloadRows"/>), and its ammunition's names as
    /// the game names the items. Plain English, as the rest of Broadside; the item names go through the game's
    /// localization.
    /// </summary>
    public static class KwArmWords
    {
        private const string Use = "[<color=yellow><b>$KEY_Use</b></color>]", Grey = "<color=#888888>{0}</color>";

        public static string Hint(KwArmDef def)
        {
            string alternate = def.Alternate != null ? ", [<color=yellow><b>$KEY_SecondaryAttack</b></color>] fires " + Fires(def.Alternate) : "";
            string arc = $" It is levered round {def.YawLimit:0} degrees each way on its trucks: let go and haul it round ({AltUse}) to turn it further.";
            return $"Aim with the mouse. [<color=yellow><b>$KEY_Attack</b></color>] fires {Fires(def.Primary)}{alternate}. {Use} or jump lets go.{arc}";
        }

        /// <summary>
        /// The cannon's hover: loaded or not; let go, for its gunner; else man it (or who has it, or why not from here;
        /// nothing when he is too far), haul it (or let go of it, or why not from here: <see cref="KwArmTow.HaulLine"/>), the
        /// broadside key aboard a ship, what it fires.
        /// </summary>
        public static string Hover(KwArm arm, in WeaponState s, bool mine, bool takeable, string? barred)
        {
            if (arm.Frame == null)
                return "";
            string head = $"{arm.Def.Label} ({State(s)})";
            if (mine)
                return $"{head}\n{Use} Let go";
            string trail = arm.Tow?.HaulLine() ?? "";
            string helm = arm.Aboard ? "\n" + string.Format(Grey, $"At the helm, [{KwBroadside.KeyName}] fires the loaded cannons on the side you look toward") : "";
            return $"{head}{ManLine(arm, takeable, barred)}{trail}{helm}\nFires {Fires(arm.Def.Primary)}";
        }

        /// <summary>
        /// The game's alternate use as its own hovers name it: the alternate place key with the use key (Shift+E), or a
        /// gamepad's alternate keys on the newer gamepad layouts.
        /// </summary>
        public static string AltUse => ZInput.IsNonClassicFunctionality() && ZInput.IsGamepadActive()
            ? "[<color=yellow><b>$KEY_AltKeys + $KEY_Use</b></color>]" : "[<color=yellow><b>$KEY_AltPlace + $KEY_Use</b></color>]";

        /// <summary>
        /// The haul line on its hover: let go, for its hauler; haul it; or (greyed) why not from here (`barred`:
        /// null he may; empty, too far or busy: no line).
        /// </summary>
        public static string HaulLine(bool mine, string? barred)
        {
            if (mine)
                return $"\n{AltUse} Let go";
            if (barred != null)
                return barred.Length == 0 ? "" : "\n" + string.Format(Grey, barred.TrimEnd('.'));
            return $"\n{AltUse} Haul it (slowly)";
        }

        /// <summary>
        /// The captain's broadside, on his top line: the side he looked toward, how many of its cannons fired (manned ones
        /// too), how many of those nobody mans reload from his pack and how many are short (of what), and why the rest did
        /// not fire ("Port broadside: 4 of 6 cannons fired, 3 reloading, 1 short of Iron cannonball (1 still reloading, 1 not
        /// loaded).").
        /// </summary>
        public static string Broadside(in KwVolley v)
        {
            string side = v.Port ? "Port" : "Starboard";
            if (v.Aboard == 0)
                return $"{side} broadside: no cannons on the {(v.Port ? "port" : "starboard")} side.";
            string reload = v.Reloads > 0 ? $", {v.Reloads} reloading" : "";
            string shortOf = v.Short > 0 ? $", {v.Short} short of {(v.ShortOf != null ? Display(v.ShortOf) : "a load")}" : "";
            return $"{side} broadside: {v.Fired} of {v.Aboard} {(v.Aboard == 1 ? "cannon" : "cannons")} fired{reload}{shortOf}{Unfired(v)}.";
        }

        // Why the cannons that did not fire did not: "(2 still reloading, 1 not loaded)", or nothing.
        private static string Unfired(in KwVolley v)
        {
            List<string> why = new List<string>();
            if (v.Reloading > 0)
                why.Add($"{v.Reloading} still reloading");
            if (v.Empty > 0)
                why.Add($"{v.Empty} not loaded");
            return why.Count > 0 ? $" ({string.Join(", ", why)})" : "";
        }

        /// <summary>The helm key list's broadside line (beside its key cap, <see cref="KwHelmRows"/>).</summary>
        public const string BroadsideRow = "Fire the broadside you look toward (loads from your pack)";

        /// <summary>The helm key list's shot arcs line, on or off (<see cref="KwShotArcs"/>).</summary>
        public static string ArcsRow(bool on) => on ? "Shot arcs: on" : "Shot arcs: off";

        /// <summary>A reload line's label (<see cref="KwReloadRows"/>): "Port guns" or "Starboard guns".</summary>
        public static string SideLabel(bool port) => port ? "Port guns" : "Starboard guns";

        /// <summary>A reload line's tooltip on the ship panel.</summary>
        public static string SideTip(bool port) =>
            $"The cannons on the {(port ? "port" : "starboard")} side, manned ones too: how many are loaded of them all, the seconds " +
            $"until those reloading are all loaded, and how many are empty with nothing to reload them. At the helm, [{KwBroadside.KeyName}] " +
            "fires the loaded cannons on the side you look toward.";

        /// <summary>
        /// One side's guns on a reload line, coloured as the ship panel's own lines are: all loaded, "3/3 ready" in the
        /// game's orange (the panel's Ready for an ability); else how many are loaded of all, the seconds until the last
        /// reload under way is done in grey (as an ability's cooldown), and the empty ones in the panel's red (as a ship
        /// nearly broken): "1/3, 4.2 s, 1 empty".
        /// </summary>
        public static string Side(in KwSideCount c)
        {
            if (c.Ready == c.All)
                return $"<color={ReadyColour}>{c.All}/{c.All} ready</color>";
            string text = $"{c.Ready}/{c.All}";
            if (c.Reloading > 0)
                text += $", <color={WaitColour}>{c.Tenths / 10f:0.0} s</color>";
            return c.Empty > 0 ? text + $", <color={EmptyColour}>{c.Empty} empty</color>" : text;
        }

        // The ship panel's own colours (ShipConfig's): the game's highlight orange, the cooldown grey, the warning red.
        private const string ReadyColour = "#FFB65C", WaitColour = "#B8B8B8", EmptyColour = "#FF8A80";

        /// <summary>The message for a player who tries to man a cannon someone (perhaps he himself) is hauling.</summary>
        public static string Hauled(KwArm arm, Player player) => arm.Tower == player.GetZDOID() ? "Let go of it first." : "Someone is hauling it.";

        /// <summary>A cannon whose rear the player cannot be brought to from where he is: swimming, too far up, something in the way.</summary>
        public const string Reach = "You cannot reach its rear from here.";

        /// <summary>The hauler's hint as he takes a cannon up.</summary>
        public static string Hitched => $"Hauling the cannon: walk to roll it on its trucks, slowly; walk off to a side to swing it round. {AltUse} lets go.";

        /// <summary>Why a ship cannon is not built on a Broadside dock's design parts (the ship is built whole from its blueprint first).</summary>
        public const string Dock = "Build cannons on the ship once it is launched";

        /// <summary>Why a ship cannon is not built anywhere but a launched Broadside ship's deck.</summary>
        public const string Afloat = "Ship cannons are built on a launched ship's deck";

        /// <summary>Why a cannon riding a launched ship cannot be hauled from where the player stands: not on its deck.</summary>
        public const string OffDeck = "Stand on its deck to haul it.";

        // A line: man it, or (greyed) someone mans it, someone hauls it, or why not from here; none when he is too far or
        // hauls it himself.
        private static string ManLine(KwArm arm, bool takeable, string? barred)
        {
            if (!takeable)
                return "\n" + string.Format(Grey, "Someone mans it");
            if (!arm.Tower.IsNone())
                return arm.Tower == Player.m_localPlayer?.GetZDOID() ? "" : "\n" + string.Format(Grey, "Someone is hauling it");
            if (barred != null)
                return barred.Length == 0 ? "" : "\n" + string.Format(Grey, barred.TrimEnd('.'));
            return $"\n{Use} Man the {arm.Def.Label.ToLowerInvariant()}";
        }

        /// <summary>The message for a gunner whose weapon holds the other trigger's load (a shot of other ammunition).</summary>
        public static string LoadedFor(KwArmDef def, int loaded)
        {
            string button = loaded == 1 ? "$KEY_SecondaryAttack" : "$KEY_Attack";
            return $"Loaded with {Fires(KwArmTrigger.Of(def, loaded))}: [<color=yellow><b>{button}</b></color>] fires it.";
        }

        /// <summary>The message for a gunner who carries none of a shot's ammunition.</summary>
        public static string OutOf(KwShot shot) =>
            $"Out of ammunition: it takes {shot.AmmoPerShot} {string.Join(" or ", shot.Ammo.Select(Display))} a shot.";

        /// <summary>The message for a gunner who carries too little of a shot's charge (`prefab`, its powder).</summary>
        public static string OutOfCharge(string prefab, KwShot shot) =>
            $"Out of {Display(prefab)}: it takes {shot.Charge.FirstOrDefault(c => c.Item == prefab).Amount} a shot.";

        /// <summary>An item prefab's name as the game keeps it in inventories ("$item_wood", "Black powder"); null for none.</summary>
        public static string? SharedName(string prefab)
        {
            GameObject? item = ObjectDB.instance != null ? ObjectDB.instance.GetItemPrefab(prefab) : null;
            ItemDrop? drop = item != null ? item.GetComponent<ItemDrop>() : null;
            return drop != null ? drop.m_itemData.m_shared.m_name : null;
        }

        /// <summary>An item prefab's name in the player's language.</summary>
        public static string Display(string prefab)
        {
            string? name = SharedName(prefab);
            return name != null && Localization.instance != null ? Localization.instance.Localize(name) : prefab;
        }

        private static string Fires(KwShot shot) => string.Join(" or ", shot.Ammo.Select(Display));

        private static string State(in WeaponState s) => s.Loaded ? "Loaded" : s.Busy ? "Reloading" : "Empty";
    }
}
