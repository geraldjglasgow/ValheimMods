using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// Doors and windows on a launched ship where it floats (the user, 2026-10-26: "I might want to add in a porthole window
    /// to put in another cannon"; "let's just do it with the ladders, windows, doors and railings"): the Doorway and Window
    /// tools work on its planking and cabins as on the dock, through the same code (<see cref="KwDoorwayTool.Clicked"/>,
    /// <see cref="KwWindowTool.Clicked"/>, <see cref="KwOpeningHover"/>, the rows, the edit windows), aimed in the ship's
    /// frame (<see cref="KwSeaOpeningAim"/>) and placed with what its blueprint says (<see cref="KwSeaOpeningShip"/>); the
    /// ghost rides the ship with the part it stands in for hidden (<see cref="KwOpeningGhost"/>). An OK or a click is sent
    /// to the ship's owner as one sea edit (<see cref="KwSeaOpeningSend"/>): the cost of the frames, covers and doors cut, or
    /// given back for those filled in. The owner (<see cref="KwSeaOpeningOwner"/>) keeps the openings' states with them and
    /// drops the holes in the wood they take; every peer then draws only the parts changed again: their looks
    /// (<see cref="KwSeaOpeningLooks"/>, each planking part and cabin drawn on its own for this), their boxes
    /// (<see cref="KwSeaOpeningBoxes"/>) and the ship's covers and doors, numbered anew (<see cref="KwShipOpenings.Replan"/>),
    /// with the flooding through the open ones low in the hull. The hammer's remove fills one in (<see cref="KwSeaOpeningPick"/>).
    /// </summary>
    public sealed class KwSeaOpenings : KwSeaKind
    {
        public const string Kind = "opening";

        public override string Name => Kind;

        public override bool Owns(KwBpPiece piece) => KwSeaOpeningPart.Takes(piece);

        public override bool Serves(string tool) => tool == KwDoorwayTool.Tool || tool == KwWindowTool.Tool;

        public override bool DrawsOwn(KwBpPiece piece) => KwSeaOpeningPart.Takes(piece);

        public override void Build(KwSeaShip ship) => KwSeaOpeningLooks.Build(ship);

        public override bool Click(Player player, string tool, KwSeaAim aim)
        {
            if (aim.Hit && !aim.Within(player, aim.Point) && !KwForm.IsOpen)
            {
                KwOpeningSession.Say(player, "Too far away");
                return true;
            }
            KwOpeningSight sight = KwSeaOpeningAim.Of(KwSeaOpeningShip.Of(aim.Ship), aim);
            if (tool == KwDoorwayTool.Tool)
                KwDoorwayTool.Clicked(player, sight);
            else
                KwWindowTool.Clicked(player, sight);
            return true;
        }

        public override bool Hover(Player player, string tool, KwSeaAim aim)
        {
            KwSeaOpeningShip ship = KwSeaOpeningShip.Of(aim.Ship);
            KwOpeningHover.Frame(player, tool, () => KwSeaOpeningAim.Of(ship, aim));
            return true;
        }

        // The ghost goes with the hover, unless a window of these tools is open (its plan's ghost stays while it is).
        public override void Unhover()
        {
            if (KwForm.IsOpen)
                return;
            KwOpeningGhost.Hide();
            KwOpeningReads.Use(null);
        }

        public override KwSeaPick? Pick(KwSeaAim aim) => KwSeaOpeningPick.Of(this, KwSeaOpeningShip.Of(aim.Ship), aim);

        public override KwSeaEdit? Removal(KwSeaShip ship, KwSeaPick pick) => KwSeaOpeningPick.Removal(KwSeaOpeningShip.Of(ship), pick.Index, pick.Part);

        public override string? Check(KwSeaShip ship, KwSeaEdit edit) => KwSeaOpeningOwner.Check(ship.Blueprint, edit);

        // Cutting a door or window keeps the part's wood: only the holes it takes go (the owner's, below).
        public override bool NewWood(KwSeaChange change) => false;

        public override void Owner(KwSeaShip ship, KwSeaChanges mine, ZDO zdo) => KwSeaOpeningOwner.Owner(ship, mine, zdo);

        public override void Redraw(KwSeaShip ship, KwSeaChanges mine)
        {
            List<(int Index, KwBpPiece? Piece)> parts = mine.All.ConvertAll(c => (c.Index, c.New));
            double looks = KwSeaOpeningLooks.Redraw(ship, parts);
            System.Diagnostics.Stopwatch watch = System.Diagnostics.Stopwatch.StartNew();
            KwBodyMass kept = KwBodyMass.Of(ship.Rigid);
            int boxes = 0;
            foreach ((int index, KwBpPiece? piece) in parts)
                boxes += KwSeaOpeningBoxes.Redraw(ship, index, piece).Made;
            double boxed = watch.Elapsed.TotalMilliseconds;
            KwShipOpenings? openings = ship.GetComponent<KwShipOpenings>();
            if (openings != null)
                openings.Replan(ship.Blueprint, ship.Hull, mine.Indices(), ship.Revision);
            kept.Put(ship.Rigid);
            double hung = watch.Elapsed.TotalMilliseconds - boxed;
            if (looks + watch.Elapsed.TotalMilliseconds > 4.0)
                Log.Info($"Broadside: doors and windows of {parts.Count} part(s) drawn again in {looks:0.0} ms, {boxes} boxes in {boxed:0.0} ms, covers in {hung:0.0} ms");
        }
    }
}
