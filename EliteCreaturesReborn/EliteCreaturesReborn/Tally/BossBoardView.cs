using System.Linq;
using System.Text;
using EliteCreaturesReborn.Config;
using TMPro;
using UnityEngine;

namespace EliteCreaturesReborn.Tally
{
    /// <summary>
    /// The boss damage board on screen: a block of left-aligned text at the far left of the screen, halfway down, out of
    /// the way of the boss bar, the hotbar and the health and food. It is made as a copy of the game's own centre message
    /// so it wears the game's font and outline. It shows the boss's name and every player who hurt it, most damage first,
    /// for the configured number of seconds, then hides. One board at a time: a new boss replaces it (a Twin's partner
    /// repeating the fight never reaches here, <see cref="BossBoard.Remember"/> drops it), and <c>damage</c> shows the
    /// latest again from the start, whether it is still up or has gone.
    /// </summary>
    internal sealed class BossBoardView : MonoBehaviour
    {
        /// <summary>
        /// From the screen's left edge to the text, in the HUD canvas's units: in line with the game's hotbar above it and
        /// its health panel below (47 and 49.5 in the game's own layout).
        /// </summary>
        private const float Margin = 48f;

        private static BossBoardView? _instance;

        private TMP_Text _text = null!;
        private float _hideAt;

        /// <summary>A board just received from a boss's death, unless this player turned the board off.</summary>
        public static void Show(BossBoard board)
        {
            if (Configuration.ShowBossBoard.Value)
            {
                Replay(board);
            }
        }

        /// <summary>
        /// Shows <paramref name="board"/> for the full time from now, whatever is on screen. Asked for by the player, so it
        /// shows even with the board turned off. False when there is no HUD to draw on.
        /// </summary>
        public static bool Replay(BossBoard board)
        {
            BossBoardView? view = Ensure();
            if (view == null)
            {
                return false;
            }
            view._text.text = Compose(board);
            view._text.CrossFadeAlpha(1f, 0f, true);
            view._hideAt = Time.time + Mathf.Max(1f, Configuration.BossBoardSeconds.Value);
            view.gameObject.SetActive(true);
            return true;
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

        // Pinned to the left edge at half height and grown from there, so a longer board spreads up and down evenly
        // around the screen's middle rather than down into the health and food.
        private static void Place(RectTransform rect, TMP_Text text)
        {
            rect.anchorMin = new Vector2(0f, 0.5f);
            rect.anchorMax = new Vector2(0f, 0.5f);
            rect.pivot = new Vector2(0f, 0.5f);
            rect.anchoredPosition = new Vector2(Margin, 0f);
            rect.sizeDelta = new Vector2(520f, 600f);
            text.alignment = TextAlignmentOptions.Left;
            text.enableAutoSizing = false;
            text.fontSize = 24f;
            text.richText = true;
        }

        private static string Compose(BossBoard board)
        {
            string boss = Localization.instance != null ? Localization.instance.Localize(board.BossName) : board.BossName;
            StringBuilder text = new StringBuilder();
            text.Append("<b>").Append(boss).Append(" defeated</b>");
            foreach (DamageTally.Entry entry in board.Entries.OrderByDescending(e => e.Damage))
            {
                text.Append("\n<noparse>").Append(entry.Name).Append("</noparse>   ")
                    .Append(Mathf.RoundToInt(entry.Damage).ToString("N0"));
            }
            return text.ToString();
        }
    }
}
