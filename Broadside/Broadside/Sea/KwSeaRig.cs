using System.Collections.Generic;

namespace Broadside
{
    /// <summary>
    /// The rig edited on a launched ship where it floats (the user's request, 2026-10-26: "the sails, the post cap and that
    /// big long pole that goes out the front of the ship"): masts with their sails, tops and pennant (the sail tool), the
    /// bowsprit with its jibs (the bowsprit tool) and post caps (the Post cap tool; an old hammer cap is built again as the
    /// tool's), added, changed and removed with the dock's own windows (<see cref="KwSeaRigTools"/>,
    /// <see cref="KwSeaRigWindows"/>) and the hammer's remove aimed at one (<see cref="KwSeaRigPick"/>). The owner checks
    /// them (<see cref="KwSeaRigCheck"/>); every peer remakes only the changed pieces' rig and what hangs on it
    /// (<see cref="KwSeaRigRedraw"/>). A changed or removed piece is new wood: its marks go (the core's default).
    /// </summary>
    public sealed class KwSeaRig : KwSeaKind
    {
        public const string Kind = "rig";

        public override string Name => Kind;

        /// <summary>Whether a prefab is the rig's: a mast, a bowsprit, a post cap (the tool's or an old hammer cap).</summary>
        public static bool Is(string prefab) => prefab == KwSailTool.Made || prefab == KwBowsprit.Made || KwCaps.Is(prefab);

        public override bool Owns(KwBpPiece piece) => Is(piece.Prefab);

        public override bool Serves(string tool) => tool == KwSailTool.Tool || tool == KwBowspritTool.Tool || tool == KwCapTool.Tool;

        public override bool Click(Player player, string tool, KwSeaAim aim) => KwSeaRigTools.Click(player, tool, aim);

        public override bool Hover(Player player, string tool, KwSeaAim aim) => KwSeaRigTools.Hover(player, tool, aim);

        public override void Unhover() => KwSeaRigGhost.Unhover();

        public override KwSeaPick? Pick(KwSeaAim aim) => KwSeaRigPick.Of(this, aim);

        public override KwSeaEdit? Removal(KwSeaShip ship, KwSeaPick pick) => KwSeaRigPick.Removal(ship, pick.Index);

        public override string? Check(KwSeaShip ship, KwSeaEdit edit) => KwSeaRigCheck.Why(ship, edit);

        public override void Redraw(KwSeaShip ship, KwSeaChanges mine) => KwSeaRigRedraw.Redraw(ship, mine);

        public override void After(KwSeaShip ship, KwSeaChanges all) => KwSeaRigRedraw.After(ship, all);

        /// <summary>A fallen piece removed gives nothing back (<see cref="KwSeaRigCheck.Refund"/>).</summary>
        public override void Cost(KwSeaShip ship, KwSeaChanges mine, List<(string Item, int Amount)> delta) => KwSeaRigCheck.Refund(ship, mine, delta);
    }
}
