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
    }
}
