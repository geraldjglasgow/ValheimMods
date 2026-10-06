using TMPro;

namespace PackPanel.Core
{
    /// <summary>
    /// Whole numbers as text, each made once: cell counts are written every frame the inventory is drawn, and a new string
    /// per write is garbage the game later stops to collect. Text the same as shown is not redrawn either.
    /// </summary>
    public static class NumberText
    {
        private static readonly string[] made = new string[1000];

        public static string Of(int number)
        {
            if (number < 0 || number >= made.Length)
                return number.ToString();
            return made[number] ?? (made[number] = number.ToString());
        }

        /// <summary>
        /// A cell's count alone ("3"), shown only above one, written only when it changes: the game's own writes to a ring
        /// or tackle cell's count go to a stand-in meanwhile (<see cref="Panels.GridHold"/>).
        /// </summary>
        public static void ShowCount(TMP_Text amount, ItemDrop.ItemData item)
        {
            bool show = item != null && item.m_stack > 1;
            if (amount.enabled != show)
                amount.enabled = show;
            if (!show)
                return;
            string text = Of(item.m_stack);
            if (amount.text != text)
                amount.text = text;
        }
    }
}
