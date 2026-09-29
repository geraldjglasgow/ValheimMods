using System.Globalization;
using PackPanel.Core;

namespace PackPanel.Backpacks
{
    /// <summary>
    /// The backpacks' $packpanel_backpack_ words: each pack's name and its description, which says what it gives (so it is
    /// written again when the backpacks file changes), and the message when there is no room to take a pack off.
    /// </summary>
    public static class BackpackWords
    {
        public static string NoRoom { get; private set; }

        public static void Register()
        {
            foreach (BackpackKind kind in BackpackCatalog.All)
                Language.Add(kind.Token.Substring(1), kind.Name);
            NoRoom = Language.Add("packpanel_backpack_noroom", "No room in your inventory to take the backpack off");
            DescribeAll();
        }

        public static void DescribeAll()
        {
            foreach (BackpackKind kind in BackpackCatalog.All)
                Language.Add(kind.DescriptionToken.Substring(1), Describe(kind));
        }

        private static string Describe(BackpackKind kind)
        {
            BackpackStats stats = kind.Stats;
            string gives = $"+{stats.Slots} inventory slots";
            if (stats.Carry > 0f)
                gives += $" and +{stats.Carry.ToString("0", CultureInfo.InvariantCulture)} carry weight";
            string portal = stats.Portal ? " What its slots hold may go through portals, if the server allows it." : "";
            return $"{kind.Look} Put it in your Backpack slot to wear it: {gives} while it is on your back. Taking it off drops what does not fit.{portal}";
        }
    }
}
