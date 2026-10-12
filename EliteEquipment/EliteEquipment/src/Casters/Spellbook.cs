using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace EliteEquipment.Casters
{
    /// <summary>
    /// The Spellbook (features/casters.md, "How spells are chosen", step 2): anywhere, with the inventory open and a wand
    /// in hand, the Spellbook button opens it beside the inventory. Click one of the wand's two slots, then a known spell
    /// to hold it ready there (a spell already in another slot moves); clicking the picked slot again empties it. The
    /// loadout is the wand's own (<see cref="Loadout"/>). The window closes with the inventory, and when the wand leaves
    /// the hand or the switch goes off. Refreshed only when the wand, its loadout, the picked slot or the known spells change.
    /// </summary>
    public static class Spellbook
    {
        private static bool open;
        private static int editing;
        private static bool dirty = true;
        private static ItemDrop.ItemData shownWand;
        private static string shownLoadout;
        private static int shownKnown = -1;

        public static void Tick()
        {
            Player player = Player.m_localPlayer;
            ItemDrop.ItemData wand = CastersSettings.On && InventoryGui.IsVisible() ? CasterGearWorn.Wand(player) : null;
            if (wand == null)
            {
                Hide();
                return;
            }
            if (!SpellbookUi.Ensure(Toggle, PickSlot))
                return;
            if (!SpellbookUi.OpenButton.gameObject.activeSelf)
                SpellbookUi.OpenButton.gameObject.SetActive(true);
            if (open && Changed(player, wand))
                Refresh(player, wand);
        }

        private static void Hide()
        {
            open = false;
            if (SpellbookUi.Window != null && SpellbookUi.Window.gameObject.activeSelf)
                SpellbookUi.Window.gameObject.SetActive(false);
            if (SpellbookUi.OpenButton != null && SpellbookUi.OpenButton.gameObject.activeSelf)
                SpellbookUi.OpenButton.gameObject.SetActive(false);
        }

        private static void Toggle()
        {
            open = !open;
            SpellbookUi.Window.gameObject.SetActive(open);
            dirty = true;
        }

        private static void PickSlot(int slot)
        {
            ItemDrop.ItemData wand = CasterGearWorn.Wand(Player.m_localPlayer);
            if (wand != null && slot == editing && Loadout.Read(wand)[slot].Length > 0)
                Loadout.Set(wand, slot, null);
            editing = slot;
            dirty = true;
        }

        private static void PickSpell(Spell spell)
        {
            ItemDrop.ItemData wand = CasterGearWorn.Wand(Player.m_localPlayer);
            if (wand != null)
                Loadout.Set(wand, editing, spell);
            dirty = true;
        }

        private static bool Changed(Player player, ItemDrop.ItemData wand)
        {
            wand.m_customData.TryGetValue(Loadout.SlotsKey, out string loadout);
            int known = player.m_customData.TryGetValue(KnownSpells.Key, out string text) ? text.Length : 0;
            if (!dirty && wand == shownWand && loadout == shownLoadout && known == shownKnown)
                return false;
            if (known != shownKnown)
                SpellbookUi.ClearRows();
            (dirty, shownWand, shownLoadout, shownKnown) = (false, wand, loadout, known);
            return true;
        }

        private static void Refresh(Player player, ItemDrop.ItemData wand)
        {
            for (int i = 0; i < Loadout.Slots; i++)
                ShowSlot(SpellbookUi.Slots[i], Loadout.In(player, wand, i), i == editing);
            var known = KnownSpells.Of(player);
            Spell picked = Loadout.In(player, wand, editing);
            for (int i = 0; i < known.Count; i++)
            {
                GameObject row = i < SpellbookUi.Rows.Count ? SpellbookUi.Rows[i] : SpellbookUi.Row(i, known[i], PickSpell);
                ShowRow(row, known[i], known[i] == picked, player);
            }
            SpellbookUi.Detail.text = Core.Language.Localize(picked != null ? picked.DescriptionToken : CasterWords.Hint);
        }

        private static void ShowSlot(GameObject slot, Spell spell, bool picked)
        {
            Image icon = slot.transform.Find("icon")?.GetComponent<Image>();
            if (icon != null)
            {
                icon.gameObject.SetActive(spell != null);
                icon.sprite = spell != null ? CasterBundles.Sprite(CasterBundles.Spells, spell.Icon) : null;
            }
            slot.transform.Find("selected")?.gameObject.SetActive(picked);
        }

        private static void ShowRow(GameObject row, Spell spell, bool inPicked, Player player)
        {
            Image icon = row.transform.Find("icon")?.GetComponent<Image>();
            if (icon != null)
                icon.sprite = CasterBundles.Sprite(CasterBundles.Spells, spell.Icon);
            TMP_Text name = row.transform.Find("name")?.GetComponent<TMP_Text>();
            if (name != null)
            {
                string cost = Mathf.CeilToInt(SpellCost.Of(player, spell)) + " " + Core.Language.Localize(CasterWords.EitrBonus).ToLowerInvariant();
                name.text = Core.Language.Localize(spell.Token) + "  <color=#9a9a9a>" + cost + "</color>";
                name.color = inPicked ? new Color(1f, 0.83f, 0.4f) : Color.white;
            }
        }
    }
}
