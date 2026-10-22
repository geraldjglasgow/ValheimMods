using TMPro;
using Translations;
using UnityEngine;
using UnityEngine.UI;

using Object = UnityEngine.Object;

namespace EliteEquipment.Casters
{
    /// <summary>
    /// The spell bar: while a wand is drawn, the wand's two spells under the hotbar, drawn with the game's own hotbar slot
    /// (<c>HotkeyBar.m_elementPrefab</c>: frame, icon, the key label), each labelled with its button (RMB, RMB+LMB) and its
    /// eitr cost where the hotbar shows a stack. Made under the hotbar on the local machine the first time a wand is drawn;
    /// refreshed only when the wand, its loadout, the known spells or the language change.
    /// </summary>
    public static class SpellBar
    {
        private const float Below = -78f;

        private static RectTransform bar;
        private static readonly GameObject[] slots = new GameObject[Loadout.Slots];
        private static ItemDrop.ItemData shownWand;
        private static string shownLoadout;
        private static int shownKnown = -1;
        private static int shownWords = -1;

        public static void Tick()
        {
            Player player = Player.m_localPlayer;
            ItemDrop.ItemData wand = player != null && !player.IsDead() && CastersSettings.On ? CasterGearWorn.Wand(player) : null;
            if (wand == null || !Ensure())
            {
                if (bar != null && bar.gameObject.activeSelf)
                    bar.gameObject.SetActive(false);
                shownWand = null;
                return;
            }
            if (!bar.gameObject.activeSelf)
                bar.gameObject.SetActive(true);
            if (Changed(player, wand))
                Refresh(player, wand);
        }

        private static bool Changed(Player player, ItemDrop.ItemData wand)
        {
            wand.m_customData.TryGetValue(Loadout.SlotsKey, out string loadout);
            int known = player.m_customData.TryGetValue(KnownSpells.Key, out string text) ? text.Length : 0;
            if (wand == shownWand && loadout == shownLoadout && known == shownKnown && Lang.Version == shownWords)
                return false;
            (shownWand, shownLoadout, shownKnown, shownWords) = (wand, loadout, known, Lang.Version);
            return true;
        }

        private static void Refresh(Player player, ItemDrop.ItemData wand)
        {
            for (int i = 0; i < Loadout.Slots; i++)
            {
                if (slots[i] == null)
                    continue;
                Show(slots[i], Loadout.In(player, wand, i), player);
                Label(slots[i].transform.Find("binding")?.GetComponent<TMP_Text>(), i);
            }
        }

        private static void Show(GameObject slot, Spell spell, Player player)
        {
            Image icon = slot.transform.Find("icon")?.GetComponent<Image>();
            if (icon != null)
            {
                icon.gameObject.SetActive(spell != null);
                icon.sprite = spell != null ? CasterBundles.Sprite(CasterBundles.Spells, spell.Icon) : null;
            }
            TMP_Text amount = slot.transform.Find("amount")?.GetComponent<TMP_Text>();
            if (amount != null)
            {
                amount.gameObject.SetActive(spell != null);
                amount.text = spell != null ? Mathf.CeilToInt(SpellCost.Of(player, spell)).ToString() : "";
            }
        }

        private static bool Ensure()
        {
            if (bar != null)
                return true;
            HotkeyBar hotbar = Hud.instance != null ? Hud.instance.GetComponentInChildren<HotkeyBar>(true) : null;
            if (hotbar == null || hotbar.m_elementPrefab == null)
                return false;
            bar = new GameObject("EE_SpellBar", typeof(RectTransform)).GetComponent<RectTransform>();
            bar.SetParent(hotbar.transform, false);
            bar.localPosition = new Vector3(0f, Below, 0f);
            for (int i = 0; i < Loadout.Slots; i++)
                slots[i] = Slot(hotbar, i);
            return true;
        }

        private static GameObject Slot(HotkeyBar hotbar, int index)
        {
            GameObject slot = Object.Instantiate(hotbar.m_elementPrefab, bar, false);
            slot.name = "EE_Spell" + index;
            slot.transform.localPosition = new Vector3(index * hotbar.m_elementSpace, 0f, 0f);
            Button button = slot.GetComponent<Button>();
            if (button != null)
                button.interactable = false;
            foreach (string part in new[] { "durability", "equiped", "queued", "selected" })
                slot.transform.Find(part)?.gameObject.SetActive(false);
            Label(slot.transform.Find("binding")?.GetComponent<TMP_Text>(), index);
            return slot;
        }

        /// <summary>The slot's button where the hotbar shows its number, shrunk to fit.</summary>
        public static void Label(TMP_Text binding, int slot)
        {
            if (binding == null)
                return;
            binding.text = CasterWords.KeyOf(slot);
            binding.enableAutoSizing = true;
            binding.fontSizeMin = 8f;
            binding.textWrappingMode = TextWrappingModes.NoWrap;
        }
    }
}
