using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// The looks' working parts on a launched ship, one blueprint piece at a time (<see cref="KwShipFittings"/>): a
    /// railing's shield hooks (<see cref="KwShipShields"/>) and a name board's name (<see cref="KwShipNameBoard"/>). Their
    /// state is the ship's ZDO's, so they are made on every peer, a dedicated server too, which may own the ship and
    /// answer their calls. A piece of any other kind is left alone. The painted bands are in the ship's look.
    /// </summary>
    public static class KwShipDress
    {
        private const string Rail = KwToolPieces.PartPrefix + "rail";

        public static void Fit(Transform ship, Transform holder, KwBpPiece piece)
        {
            if (piece.Prefab == Rail)
                KwShipBody.Step("Broadside ship shield hooks", () => KwShipShields.Of(ship).Rail(holder, piece));
            else if (piece.Prefab == KwNameBoard.Prefab)
                KwShipBody.Step("Broadside ship name board", () => KwShipNameBoard.Make(ship, holder, piece));
        }
    }
}
