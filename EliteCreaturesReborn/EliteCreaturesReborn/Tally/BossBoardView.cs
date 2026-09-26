using System.Collections.Generic;
using System.Linq;
using System.Text;
using EliteCreaturesReborn.Config;
using TMPro;
using UnityEngine;

namespace EliteCreaturesReborn.Tally
{
    /// <summary>
    /// The boss damage board on screen: a block of text at the top centre, made as a copy of the game's own centre
    /// message so it wears the game's font and outline. It shows the boss's name and every player who hurt it, most
    /// damage first, for the configured number of seconds, then hides. One board at a time: a new boss replaces it, and
    /// a second message for the fight already on screen (a Twin falling with its partner) is ignored.
    /// </summary>
    internal sealed class BossBoardView : MonoBehaviour
    {
        private static BossBoardView? _instance;

        private TMP_Text _text = null!;
        private float _hideAt;
        private string _fight = "";

        public static void Show(string fight, string bossName, List<DamageTally.Entry> entries)
        {
            if (!Configuration.ShowBossBoard.Value)
            {
                return;
            }
            BossBoardView? view = Ensure();
            if (view == null || (view._fight == fight && view.gameObject.activeSelf))
            {
                return;
            }
            view._fight = fight;
            view._text.text = Compose(bossName, entries);
            view._text.CrossFadeAlpha(1f, 0f, true);
            view._hideAt = Time.time + Mathf.Max(1f, Configuration.BossBoardSeconds.Value);
            view.gameObject.SetActive(true);
        }

        private void Update()
        {
            if (Time.time >= _hideAt)
            {
                gameObject.SetActive(false);
            }
        }

        // Built on first use and again after the HUD it lives on is rebuilt (a logout and a new world).
        private static BossBoardView? Ensure()
        {
            if (_instance != null)
            {
                return _instance;
            }
            MessageHud hud = MessageHud.instance;
            TMP_Text? source = hud != null ? hud.m_messageCenterText : null;
            if (source == null || source.canvas == null)
            {
                return null;
            }
            GameObject copy = Instantiate(source.gameObject, source.canvas.transform);
            copy.name = "ecr_boss_board";
            _instance = copy.AddComponent<BossBoardView>();
            _instance._text = copy.GetComponent<TMP_Text>();
            Place((RectTransform)copy.transform, _instance._text);
            return _instance;
        }

        // Below where the boss's health bar was, so a second boss's bar is not covered.
        private static void Place(RectTransform rect, TMP_Text text)
        {
            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0f, -110f);
            rect.sizeDelta = new Vector2(900f, 600f);
            text.alignment = TextAlignmentOptions.Top;
            text.enableAutoSizing = false;
            text.fontSize = 24f;
            text.richText = true;
        }

        private static string Compose(string bossName, List<DamageTally.Entry> entries)
        {
            string boss = Localization.instance != null ? Localization.instance.Localize(bossName) : bossName;
            StringBuilder text = new StringBuilder();
            text.Append("<b>").Append(boss).Append(" defeated</b>");
            foreach (DamageTally.Entry entry in entries.OrderByDescending(e => e.Damage))
            {
                text.Append("\n<noparse>").Append(entry.Name).Append("</noparse>   ")
                    .Append(Mathf.RoundToInt(entry.Damage).ToString("N0"));
            }
            return text.ToString();
        }
    }
}
