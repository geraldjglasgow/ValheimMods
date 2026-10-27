using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// The Cabin tool (the user's request, 2026-10-25: "rename the castle tool to cabin tool ... allow selecting the deck
    /// sections to build cabins on"): click a deck section, then another along the same deck (or the same one again); the
    /// cabin covers those two and every section between them. A built cabin's roof counts as a section: click it twice to
    /// stand a cabin on the cabin. After the first click a see-through cabin shows over the hovered range; after the second
    /// a window (<see cref="KwCabinForm"/>) takes its height, how far each side is drawn in and its material, its note says
    /// what it does, and <see cref="KwPreview"/> shows it as the values change, fitted to the hull (<see cref="KwCabinFit"/>:
    /// an end drawn in where the hull narrows, the note saying so); values no cabin can be built from keep the last cabin
    /// that could on show, and OK waits for values that can, the note saying why. OK builds it there, free: a networked
    /// generated part (<see cref="KwGenerated"/>) whose whole shape travels in its ZDO (<see cref="KwCabinSpec"/>), and the
    /// railing along the hull where a side stands at the hull taken away in the same undo step (<see cref="KwCabinRails"/>).
    /// Its front is the bow end when the ship tells (<see cref="KwCabinBow"/>), else the end the player faces. The window
    /// opens on the sizes used last. Local to the clicking player until built.
    /// </summary>
    public static class KwCabinTool
    {
        public const string Tool = KwHouses.Cabin;

        private static KwClick? pending;
        private static KwCabinSite? site;
        private static KwCabinRange? range;
        private static KwCabinSpec? spec, good;
        private static KwCabinSizes sizes = new KwCabinSizes(2f, 0f, 0f, 0f, 0f);
        private static KwBiomeTier tier = KwBiomeTiers.Meadows;
        private static string why = "", shown = "", front = "";
        private static Piece? hovered;
        private static (string Key, KwCabinRange At, KwCabinSpec Cabin)? hover;

        /// <summary>The first section clicked, while it waits for the second and still stands.</summary>
        public static KwClick? Pending => pending != null && pending.Value.Piece != null ? pending : null;

        public static void Click(Player player)
        {
            if (KwForm.IsOpen)
                return;
            KwClick? aim = KwCabinPick.Section(player);
            KwClick? first = Pending;
            if (aim == null)
            {
                Say(player, first == null ? "Click a deck section, or a cabin's roof to build on it" : "Click the cabin's last deck section along the deck, or the first again");
                return;
            }
            (pending, hovered, hover) = (null, null, null);
            if (first == null)
                First(player, aim.Value);
            else
                Second(player, first.Value, aim.Value);
        }

        // The first section: the keel line's decks and hull read now, for the hover and the second click.
        private static void First(Player player, KwClick aim)
        {
            string no = "";
            site = KwCabinPick.IsCabin(aim.Piece) ? null : KwCabinHull.Site(aim.Piece, out no);
            if (site == null && !KwCabinPick.IsCabin(aim.Piece))
            {
                Say(player, no);
                return;
            }
            pending = aim;
            Say(player, site == null ? "Cabin on this cabin's roof: click the roof again" : "Cabin from this deck section: now click its last section along the deck, or this one again");
        }

        // The second section: the range they choose, and the window on it.
        private static void Second(Player player, KwClick first, KwClick last)
        {
            KwCabinRange? chosen = Choose(first.Piece, last.Piece, out string no);
            if (chosen == null)
            {
                Say(player, no);
                return;
            }
            (range, good) = (chosen, null);
            front = site == null ? "Front is the front of the cabin under it" : site.Bow != 0f ? "Front is the bow end" : "Front is the end you faced";
            KwForm.Open("New cabin", KwCabinForm.Fields(sizes, chosen.Length, KwCabinForm.Wide(chosen.Stations, chosen.Floor), KwBiomeField.Last(KwPart.Hull)),
                Changed, values => Done(player, values), true, Accepts);
        }

        // OK only with values a cabin can be built from; else the window stays open, its note saying why.
        private static bool Accepts(KwFormValues values)
        {
            Changed(values);
            return spec != null || range == null;
        }

        /// <summary>The range two clicks choose: a roof clicked twice, or deck sections on the first one's keel line.</summary>
        private static KwCabinRange? Choose(Piece first, Piece last, out string why)
        {
            if (KwCabinPick.IsCabin(first))
            {
                why = "A cabin on a cabin covers that cabin's roof: click the same roof twice";
                return first == last ? KwCabinHull.OnRoof(first, out why) : null;
            }
            why = "Click a deck section on the same deck as the first";
            if (site == null || KwCabinPick.IsCabin(last))
                return null;
            return KwCabinHull.Range(site, first, last, Way(site), out why);
        }

        // The cabin's front: the bow when the ship tells, else the way the player looks along the line.
        private static float Way(KwCabinSite on) =>
            on.Bow != 0f ? on.Bow : Vector3.Dot(GameCamera.instance.transform.forward, on.Forward) >= 0f ? 1f : -1f;

        // The window's values as the cabin they plan, fitted to the hull: remembered for next time, its note said, shown. Values
        // no cabin can be built from leave the last one that could on show, and the note says why.
        private static void Changed(KwFormValues values)
        {
            sizes = KwCabinForm.Read(values);
            tier = KwBiomeField.Picked(values, KwPart.Hull);
            KwCabinSpec? planned = range?.Spec(sizes);
            why = planned == null ? "Nothing chosen to build on" : planned.Unbuilt ?? "";
            spec = why.Length == 0 ? planned : null;
            good = spec ?? good;
            shown = good != null ? Tool + "|" + tier.Id + "|" + good.ToString().GetStableHashCode() + "|" + range!.Position.ToString("F2") : "";
            int rails = spec != null ? KwCabinRails.Under(range!.Position, range.Direction, spec).Count : 0;
            KwForm.Say(spec != null ? KwCabinForm.Note(spec, front, rails) : why + (good != null ? ". The cabin shown is the last that fits." : "."));
            Preview();
        }

        /// <summary>
        /// The tool's preview: the cabin the open window would build (the last one that could, while its values cannot); while
        /// a first section waits, the cabin the hovered section would make with the sizes used last; nothing otherwise.
        /// </summary>
        public static void Preview()
        {
            if (KwForm.IsOpen)
            {
                if (good != null && range != null)
                    Show(shown, range, good);
                else
                    KwPreview.Hide();
                return;
            }
            Hover();
        }

        private static void Show(string key, KwCabinRange at, KwCabinSpec cabin)
        {
            string text = cabin.ToString();
            KwBiomeTier look = tier;
            KwLookPreview.Show(key, at.Position, Quaternion.LookRotation(at.Direction), () => KwHouses.Looks(Tool, text, look));
        }

        // The cabin from the waiting section to the one hovered, as the planking tool shows its part before the second click;
        // planned again only when another section is hovered.
        private static void Hover()
        {
            KwClick? first = Pending;
            Player? player = Player.m_localPlayer;
            KwClick? aim = first != null && player != null ? KwCabinPick.Section(player) : null;
            if (aim == null)
            {
                (hovered, hover) = (null, null);
                KwPreview.Hide();
                return;
            }
            if (aim.Value.Piece != hovered)
                (hovered, hover) = (aim.Value.Piece, Hovered(first!.Value.Piece, aim.Value.Piece));
            if (hover == null)
                KwPreview.Hide();
            else
                Show(hover.Value.Key, hover.Value.At, hover.Value.Cabin);
        }

        // The cabin from one section to another with the sizes used last; null where it would not build.
        private static (string Key, KwCabinRange At, KwCabinSpec Cabin)? Hovered(Piece first, Piece last)
        {
            KwCabinRange? at = Choose(first, last, out _);
            KwCabinSpec? cabin = at?.Spec(sizes);
            if (at == null || cabin == null || KwCabinOutline.Refused(cabin) != null)
                return null;
            return ("hover|" + Tool + "|" + tier.Id + "|" + cabin.ToString().GetStableHashCode() + "|" + at.Position.ToString("F2"), at, cabin);
        }

        private static void Done(Player player, KwFormValues? values)
        {
            if (values != null && range != null)
                Changed(values);
            (KwCabinRange? at, KwCabinSpec? built) = (values != null ? range : null, spec);
            (range, spec, good, site, shown) = (null, null, null, null, "");
            KwPreview.Hide();
            if (player == null || values == null || at == null)
                return;
            if (built == null)
                Say(player, why);
            else
                Build(player, at, built);
        }

        // The cabin spawned with a seed of its own for its roof's boards, the railing it stands in taken away in the same undo step.
        private static void Build(Player player, KwCabinRange at, KwCabinSpec built)
        {
            string saved = built.Seeded(Random.Range(1, int.MaxValue)).ToString();
            KwTools.Spawn(player, Tool, at.Position, Quaternion.LookRotation(at.Direction), saved, tier);
            int rails = KwCabinRails.Take(player, at.Position, at.Direction, built);
            string taken = rails > 0 ? $"\nThe railing taken from its sides at the hull ({rails} part{(rails == 1 ? "" : "s")} changed)" : "";
            Say(player, KwTools.Named(Tool, tier) + " built" + taken);
        }

        private static void Say(Player player, string text) => player.Message(MessageHud.MessageType.Center, text);

        /// <summary>The tool's placeholder: a cabin on a strip of deck, its roof over a planked wall, a post at each corner.</summary>
        public static void Mark(MeshBuilder b)
        {
            Solid.Box(b, new Vector3(-0.4f, -0.6f, 0f), new Vector3(0.8f, 0.6f, 0.06f));
            Solid.Box(b, new Vector3(-0.05f, -0.6f, 0.75f), new Vector3(0.8f, 0.6f, 0.82f));
            Solid.Box(b, new Vector3(0f, -0.55f, 0.06f), new Vector3(0.05f, 0.55f, 0.75f));
            foreach (float y in new[] { -0.55f, 0.55f })
                Solid.Box(b, new Vector3(-0.03f, y - 0.05f, 0.06f), new Vector3(0.08f, y + 0.05f, 0.75f));
        }
    }
}
