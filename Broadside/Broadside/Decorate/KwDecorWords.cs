using System.Collections.Generic;
using System.Text;

namespace Broadside
{
    /// <summary>Every word the auto-decorate tool shows (English in the code, as the rest of Broadside's).</summary>
    public static class KwDecorWords
    {
        public const string Name = "Auto-decorate";
        public const string UndoKey = "Ctrl + Z";

        public const string Description =
            "Dresses a shipyard with the dockside decorations in scenes that belong together: a carpentry yard, steam bending, a ship slip, rope works, a sail loft, a fishing corner, a ship gear store, quay furniture along the docks.\n"
            + "Aim at the ground, a dock or the water and click to read the area; click again inside it to place. Alt + wheel or [ ]: the area's size, then how busy. N: another layout. Esc: let it go. Ctrl + Z: take the last back.\n"
            + "Keeps docks' lanes and ends, doors, ladders, stations and paths clear; nothing in water but what is made for it.";

        public const string NoAim = "Aim at the ground, a dock or the water within reach";
        public const string Placing = "Still placing the last decorations";
        public const string NothingFits = "Nothing fits here: no open ground, dock edge or shore clear of lanes, paths and doors";
        public const string NoKinds = "No dockside decorations are loaded";

        private static readonly string[] Densities = { "A few touches", "A light yard", "A working yard", "A busy yard", "A bustling yard" };

        /// <summary>The lines while aiming.</summary>
        public static string Aiming(float half) =>
            $"<b>{Name}</b>: an area {half * 2f:0} m across\n"
            + $"Click reads it · Alt + wheel or [ ]: its size ({KwDecorSession.MinHalf * 2f:0} to {KwDecorSession.MaxHalf * 2f:0} m) · the wheel zooms as usual";

        /// <summary>The lines while a locked area is read (its share done) or its layout worked out (a share under 0).</summary>
        public static string Working(float share) =>
            (share >= 0f ? $"<b>{Name}</b>: reading the area... {share * 100f:0}%" : $"<b>{Name}</b>: laying out the yard...") + "\nEsc lets it go";

        /// <summary>The lines once the layout shows: how busy, the scenes and pieces, the cost, the keys.</summary>
        public static string Ready(Player player, List<KwDecorPlan> shown, KwDecorLayout layout)
        {
            List<Piece.Requirement> sum = KwDecorCost.Sum(shown);
            string? station = KwDecorCost.MissingStation(player, shown);
            string pay = station != null ? $"<color=#ff7a6a>Needs a {Localize(station)} in range</color>"
                : KwDecorCost.CanPay(player, sum) ? "Click places them" : "<color=#ff7a6a>Not enough for all</color>: Shift + click places what you can pay for";
            return $"<b>{Name}</b>: {Busy(layout, shown.Count)}: {List(layout.Names(shown))}, {Pieces(shown.Count)}\n"
                + $"Cost: {KwDecorCost.Text(player, sum)}\n{pay} · Alt + wheel or [ ]: more or fewer · N: another layout · Esc lets it go";
        }

        // "A busy yard (4 of 5)", and whether turning it up would add anything.
        private static string Busy(KwDecorLayout layout, int count)
        {
            int density = KwDecorSession.Density;
            string more = density < KwDecorLayout.Densities && layout.Shown(KwDecorLayout.Densities).Count == count ? ", nothing more fits" : "";
            return $"{Densities[density - 1]} ({density} of {KwDecorLayout.Densities}{more})";
        }

        private static string Pieces(int count) => count == 1 ? "1 piece" : $"{count} pieces";

        // "a, b and c".
        private static string List(List<string> names)
        {
            StringBuilder text = new StringBuilder();
            for (int n = 0; n < names.Count; n++)
                text.Append(n == 0 ? "" : n == names.Count - 1 ? " and " : ", ").Append(names[n]);
            return text.ToString();
        }

        public static string Localize(string text) => Localization.instance != null ? Localization.instance.Localize(text) : text;
    }
}
