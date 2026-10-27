using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// A new railing on a launched ship: over the planking part whose top edge the railing tool clicked (the railing the
    /// dock's tool lays between the same ribs: <see cref="KwSeaRailSite.Text"/>), in the dock's window (post spacing, sides
    /// by port and starboard, material); or on a cabin's roof with none (<see cref="KwSeaRoofSite"/>), in the dock's roof
    /// railing window (its edges on or off, post spacing, material), with a gap at each ladder coming up there. While the
    /// window is open the railing shows solid on the ship; OK asks the owner to add it (<see cref="KwSeaAsk"/>), its cost
    /// the railing's. The hover before a click shows it see-through (<see cref="Hover"/>).
    /// </summary>
    public static class KwSeaRailAdd
    {
        private static readonly string Prefab = KwToolPieces.PartPrefix + KwBiomeCost.Rail;
        private static string hoverKey = "";
        private static int hoverVersion = -1;
        private static KwBpPiece? hovered;

        /// <summary>The window for a new railing over a planking part's top edge.</summary>
        public static void Hull(Player player, KwSeaShip ship, KwSeaSheer sheer)
        {
            KwBpPiece plank = ship.Blueprint.Pieces[sheer.Index];
            List<KwField> fields = new List<KwField> { KwRailField.Spacing(KwRailField.LastSpacing), KwRailField.SidesRow(0), KwBiomeField.Field(KwPart.Deck) };
            KwBpPiece? Planned(KwFormValues form) => HullPiece(plank, form);
            Open(player, ship, "New railing", fields, Planned, sheer.Point);
        }

        /// <summary>The window for a new railing on a cabin's roof.</summary>
        public static void Roof(Player player, KwSeaShip ship, KwSeaRoofSite roof, Vector3 point)
        {
            List<KwField> fields = KwRailField.SideRows(KwRailField.LastSides);
            fields.Add(KwRailField.Spacing(KwRailField.LastSpacing));
            fields.Add(KwBiomeField.Field(KwPart.Deck));
            KwBpPiece? Planned(KwFormValues form) => RoofPiece(ship, roof, form);
            Open(player, ship, "New roof railing", fields, Planned, point);
        }

        // The window: the planned railing shown solid as it changes; OK asks for it.
        private static void Open(Player player, KwSeaShip ship, string title, List<KwField> fields, System.Func<KwFormValues, KwBpPiece?> planned, Vector3 point)
        {
            string? why = KwSeaRules.Why(player, ship, ship.World(point));
            if (why != null)
            {
                player.Message(MessageHud.MessageType.Center, why);
                return;
            }
            KwSeaWindow.Hold(true);
            KwForm.Open(title, fields, form => Show(ship, planned(form)), form => Done(player, ship, form, planned, point), true);
        }

        private static void Show(KwSeaShip ship, KwBpPiece? piece)
        {
            KwForm.Say(piece == null ? "Turn at least one edge on." : "");
            if (piece == null)
                KwSeaGhost.Clear();
            else
                KwSeaGhost.Show(ship, "add|" + KwSeaPiece.Text(piece) + "|" + KwBiomeTiers.Of(piece.Data).Id, new[] { piece }, true);
        }

        private static void Done(Player player, KwSeaShip ship, KwFormValues? form, System.Func<KwFormValues, KwBpPiece?> planned, Vector3 point)
        {
            KwSeaWindow.Hold(false);
            KwBpPiece? piece = form != null ? planned(form) : null;
            if (form == null || piece == null)
                return;
            if (form.Has(KwRoofRail.Sides[0]))
                KwRailField.Keep(form);
            KwBiomeField.Picked(form, KwPart.Deck);
            KwSeaAsk.Send(player, ship, new KwSeaEdit("rail", ship.Revision, "Railing built", new List<KwSeaPart> { KwSeaPart.Add(piece) }), point);
        }

        // A hull railing over the planking part as the window has it: its spacing, its sides by port and starboard, its tier.
        private static KwBpPiece? HullPiece(KwBpPiece plank, KwFormValues form)
        {
            string name = KwRailBow.Names[Mathf.Clamp(form.ChoiceIndex(KwRailField.HullSides), 0, 2)];
            string? text = KwSeaRailSite.Text(KwSeaPiece.Text(plank), form.Number(KwRailField.Posts), KwRailBow.Letters(name, KwSeaRailSite.Runs(plank)));
            return text != null ? KwSeaPiece.Make(Prefab, plank.Position, plank.Rotation, text, KwBiomeField.Shown(form)) : null;
        }

        // A roof railing as the window has it; null with no edge on.
        private static KwBpPiece? RoofPiece(KwSeaShip ship, KwSeaRoofSite roof, KwFormValues form)
        {
            KwRoofRail rail = roof.Rail(ship.Blueprint, form.Number(KwRailField.Posts), KwRailField.Sides(form));
            return rail.On.Length > 0 ? KwSeaPiece.Make(Prefab, roof.Origin, roof.Turn, rail.ToString(), KwBiomeField.Shown(form)) : null;
        }

        /// <summary>
        /// The railing tool's hover on a launched ship: the new railing a click would build, see-through (the last spacing,
        /// both sides or the last edges, the last material); nothing over a railing or where one stands. True: shown or
        /// nothing to show here (the dock's preview stays off).
        /// </summary>
        public static bool Hover(KwSeaAim aim, bool onRailing)
        {
            KwBpPiece? piece = onRailing ? null : Planned(aim);
            if (piece == null)
                KwSeaGhost.Clear();
            else
                KwSeaGhost.Show(aim.Ship, "hover|" + KwSeaPiece.Text(piece), new[] { piece }, false);
            return true;
        }

        // What a click would add here, worked out again only when the aimed part, the spacing or the ship changed.
        private static KwBpPiece? Planned(KwSeaAim aim)
        {
            KwSeaShip ship = aim.Ship;
            KwSeaRoofSite? roof = KwSeaRoofSite.At(ship.Blueprint, aim.Point);
            KwSeaSheer? sheer = roof == null ? KwSeaRailSite.Sheer(ship.Blueprint, ship.Keels, aim.Point, KwSeaRails.Reach) : null;
            string key = $"{ship.GetInstanceID()}|{ship.Version}|{roof?.Cabin ?? -1}|{sheer?.Index ?? -1}|{KwRailField.LastSpacing}|{KwRailField.LastSides}";
            if (key == hoverKey && hoverVersion == ship.Version)
                return hovered;
            (hoverKey, hoverVersion, hovered) = (key, ship.Version, null);
            KwBiomeTier tier = KwBiomeField.Last(KwPart.Deck);
            if (roof != null && roof.Railed(ship.Blueprint) < 0)
                hovered = KwSeaPiece.Make(Prefab, roof.Origin, roof.Turn, roof.Rail(ship.Blueprint, KwRailField.LastSpacing, KwRailField.LastSides).ToString(), tier);
            else if (sheer != null && KwSeaRailSite.Over(ship.Blueprint, sheer.Value.Index) < 0)
                hovered = KwSeaPiece.Make(Prefab, ship.Blueprint.Pieces[sheer.Value.Index].Position, ship.Blueprint.Pieces[sheer.Value.Index].Rotation,
                    KwSeaRailSite.Text(KwSeaPiece.Text(ship.Blueprint.Pieces[sheer.Value.Index]), KwRailField.LastSpacing, KwRailText.Both) ?? "", tier);
            return hovered;
        }
    }
}
