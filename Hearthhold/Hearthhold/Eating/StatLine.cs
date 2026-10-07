using System.Globalization;
using System.Text;

namespace Hearthhold
{
    /// <summary>
    /// Writes the stats a food or mead gives, by name and then their raised values: "health and stamina (60 / 24)".
    /// Only stats above 0 are named; nothing is written when none is.
    /// </summary>
    public static class StatLine
    {
        public static void Values(StringBuilder text, float health, float stamina, float eitr, float factor)
        {
            int count = (health > 0f ? 1 : 0) + (stamina > 0f ? 1 : 0) + (eitr > 0f ? 1 : 0);
            if (count == 0)
                return;
            int written = 0;
            Name(text, health, "health", count, ref written);
            Name(text, stamina, "stamina", count, ref written);
            Name(text, eitr, "eitr", count, ref written);
            text.Append(" (");
            written = 0;
            Number(text, health * factor, ref written);
            Number(text, stamina * factor, ref written);
            Number(text, eitr * factor, ref written);
            text.Append(')');
        }

        private static void Name(StringBuilder text, float value, string name, int count, ref int written)
        {
            if (value <= 0f)
                return;
            if (written > 0)
                text.Append(written == count - 1 ? " and " : ", ");
            text.Append(name);
            written++;
        }

        private static void Number(StringBuilder text, float value, ref int written)
        {
            if (value <= 0f)
                return;
            if (written++ > 0)
                text.Append(" / ");
            text.Append(EatBonus.Whole(value).ToString("0", CultureInfo.InvariantCulture));
        }
    }
}
