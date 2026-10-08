using System.Collections.Generic;
using PlateColumn;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GrindstoneSkills
{
    /// <summary>
    /// The info pane beside the skills list (built by <see cref="PaneBuilder"/>): one skill's page at a time - its icon,
    /// name, a level line (level, a bonus from food or meads, progress to the next level) and the page text from
    /// <see cref="SkillPages"/> - with that skill's entry in the list tinted. The skill shown last is shown again the
    /// next time the window opens, else the first in the list. The page is written when shown, with the numbers of that
    /// moment. Local only; nothing is sent.
    /// </summary>
    internal sealed class BookPane : MonoBehaviour
    {
        private const string BonusColour = "#9BD46A";
        private static readonly Color Marked = new Color(1f, 0.8f, 0.4f, 1f);
        private static Skills.SkillType remembered = Skills.SkillType.None;

        public SkillsDialog Dialog;
        public Image Icon;
        public TMP_Text Title;
        public TMP_Text Level;
        public TMP_Text Body;
        public ScrollRect Scroll;
        public LinkTips Tips;

        private readonly List<Skills.Skill> skills = new List<Skills.Skill>();
        private Player player;
        private int shown = -1;

        /// <summary>Takes the skills in the order the window just listed them and shows the remembered one.</summary>
        public void Open(Player owner)
        {
            player = owner;
            skills.Clear();
            skills.AddRange(owner.GetSkills().GetSkillList());
            SkillOrder.Arrange(skills);
            shown = -1;
            int index = skills.FindIndex(skill => skill.m_info.m_skill == remembered);
            ShowAt(index >= 0 ? index : 0);
        }

        /// <summary>Shows the skill of a clicked entry.</summary>
        public void ShowEntry(GameObject entry) => ShowAt(Dialog.m_elements.IndexOf(entry));

        /// <summary>Follows the entry the window selected (a gamepad moves the selection).</summary>
        public void Follow(int index)
        {
            if (index != shown)
                ShowAt(index);
        }

        private void ShowAt(int index)
        {
            if (player == null || index < 0 || index >= skills.Count)
                return;
            shown = index;
            remembered = skills[index].m_info.m_skill;
            Write(skills[index]);
            Mark(index);
        }

        private void Write(Skills.Skill skill)
        {
            Skills.SkillDef info = skill.m_info;
            SkillPage page = SkillPages.For(player, info);
            Icon.sprite = info.m_icon;
            Icon.enabled = info.m_icon != null;
            Title.text = Localization.instance.Localize("$skill_" + info.m_skill.ToString().ToLower());
            Level.text = LevelLine(skill, page.Level);
            Body.text = Localization.instance.Localize(PageText.Write(page, Tips));
            Scroll.StopMovement();
            Scroll.content.anchoredPosition = Vector2.zero;
        }

        private static string LevelLine(Skills.Skill skill, float level)
        {
            int own = Mathf.FloorToInt(skill.m_level);
            string line = $"Level {own}";
            int bonus = Mathf.FloorToInt(level) - own;
            if (bonus != 0)
                line += $" <color={BonusColour}>{bonus:+0;-0}</color>";
            if (skill.m_level >= CustomSkill.MaxLevel)
                return line + "  ·  highest level";
            return line + $"  ·  {skill.GetLevelPercentage() * 100f:0}% to level {own + 1}";
        }

        private void Mark(int index)
        {
            Image template = Background(Dialog.m_elementPrefab);
            Color normal = template != null ? template.color : Color.white;
            for (int i = 0; i < Dialog.m_elements.Count; i++)
            {
                Image background = Background(Dialog.m_elements[i]);
                if (background != null)
                    background.color = i == index ? Marked : normal;
            }
        }

        private static Image Background(GameObject entry)
        {
            Transform background = entry != null ? Utils.FindChild(entry.transform, "bkg") : null;
            return background != null ? background.GetComponent<Image>() : null;
        }
    }
}
