using System.Collections.Generic;

namespace Broadside
{
    /// <summary>
    /// The railing tool on a cabin's roof: its click on a cabin (<see cref="KwCabinRoofs"/>) opens a window on which of the
    /// roof's edges get a rail (fore, aft, port, starboard, each on or off), the posts' spacing (<see cref="KwRailField"/>)
    /// and the material (<see cref="KwBiomeField"/>, the tier it is priced in; it wears the plank wood, <see cref="KwRailLooks"/>);
    /// the preview shows the railing on the roof see-through as they change (<see cref="KwPreview"/>); OK builds it, a railing part of its own
    /// (<c>KW_gen_rail</c> with a <see cref="KwRoofRail"/> text) on the roof's top in the cabin's heading, free, with a gap
    /// wherever a ladder already comes up at the roof's edge (<see cref="KwLadderGaps"/>); Cancel builds nothing. A roof
    /// railed already is not railed twice: a click on it edits its railing (<see cref="KwRailEdit.Over"/>), as a click on the
    /// railing does. Runs on the
    /// clicking player's game; the part is an ordinary networked piece whose text travels in its ZDO, so blueprints and
    /// launched ships carry it.
    /// </summary>
    public static class KwRoofRailTool
    {
        private static KwRoofSite? site;
        private static KwRoofRail? rail;
        private static string shownKey = "";
        private static KwBiomeTier tier = KwBiomeTiers.Meadows;

        /// <summary>The railing tool's click taken on a cabin's roof; false for any other click (the tool's two clicks as before).</summary>
        public static bool Click(Player player, string tool)
        {
            if (tool != KwBiomeCost.Rail)
                return false;
            Piece? piece = KwTools.Aim(player)?.Piece;
            KwRoofSite? found = piece != null ? KwRoofSite.Of(piece) : null;
            if (found == null)
                return false;
            // A roof railed already is edited (KwEdit) unless a first click waits: then it says so.
            if (found.Railed() != null)
                Say(player, "This roof has its railing already: click the waiting rib again to cancel it, then click the roof to change its railing");
            else
                Open(player, found);
            return true;
        }

        private static void Open(Player player, KwRoofSite found)
        {
            site = found;
            List<KwField> fields = KwRailField.SideRows(KwRailField.LastSides);
            fields.Add(KwRailField.Spacing(KwRailField.LastSpacing));
            fields.Add(KwBiomeField.Field(KwPart.Deck));
            KwForm.Open("New roof railing", fields, Changed, values => Done(player, values), true);
        }

        // The railing the window would build now, and its length on the note line.
        private static void Changed(KwFormValues values)
        {
            if (site == null)
                return;
            tier = KwBiomeField.Shown(values);
            rail = site.Rail(values.Number(KwRailField.Posts), KwRailField.Sides(values));
            shownKey = "roofrail|" + rail + "|" + tier.Id;
            int gaps = rail.Gaps.Count;
            string ladders = gaps == 0 ? "" : gaps == 1 ? ", a gap for the ladder there" : $", gaps for the {gaps} ladders there";
            KwForm.Say(rail.On.Length == 0 ? "Turn at least one edge on." : $"{rail.Length:0.0} m of railing{ladders}.");
        }

        private static void Done(Player player, KwFormValues? values)
        {
            KwRoofSite? at = site;
            site = null;
            KwPreview.Hide();
            if (values == null || at == null || player == null)
                return;
            KwRailField.Keep(values);
            tier = KwBiomeField.Picked(values, KwPart.Deck);
            KwRoofRail made = at.Rail(values.Number(KwRailField.Posts), KwRailField.Sides(values));
            if (made.On.Length == 0)
                Say(player, "No edge was on: no railing built");
            else if (at.Cabin == null)
                Say(player, "That cabin is gone");
            else
                KwTools.Build(player, KwBiomeCost.Rail, at.Origin, at.Turn, made.ToString(), tier);
        }

        /// <summary>The roof railing the open window would build, see-through on the roof; false while that window is not open.</summary>
        public static bool Preview(string? tool)
        {
            KwRoofSite? at = site;
            if (tool != KwBiomeCost.Rail || at == null || !KwForm.IsOpen)
                return false;
            KwRoofRail? shown = rail;
            if (shown == null || shown.On.Length == 0)
                KwPreview.Hide();
            else
                KwPreview.Show(shownKey, at.Origin, at.Turn, _ => KwMesh.Build(KwRoofRailShape.Def(shown)), KwRailLooks.Look);
            return true;
        }

        private static void Say(Player player, string text) => player.Message(MessageHud.MessageType.Center, text);
    }
}
