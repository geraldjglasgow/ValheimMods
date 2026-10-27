using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// Where a rope from any rigged piece (a bowsprit's jibs' sheets) ties on to its ship, by the masts' own rules
    /// (<see cref="KwRigTies"/>): to the top of the ship's side its aim is on, at the aim's place along the ship (its
    /// railing's top rail or its gunwale, or the outer edge of the roof the piece stands on), clear of the decks, cabins,
    /// railings, sails, yards and masts, worked out from the ship's own pieces (<see cref="KwRigSource"/>) the same on every
    /// peer, never by the physics. The rope's fitting (a cleat, or an eyebolt) is drawn under the piece where it ends. Kit
    /// axes as <see cref="KwSailLayout"/>. Where nothing is drawn (a dedicated server), the aim itself.
    /// </summary>
    public static class KwSailAnchor
    {
        public static Vector3 On(Transform piece, Vector3 from, Vector3 aim)
        {
            string name = $"tie_{Key(from)}_{Key(aim)}";
            KwRigTie? tie = KwRigState.Of(piece, null).Side(name, from, aim, KwTieKind.Cleat);
            if (tie == null)
                return aim;
            KwTieLook.Draw(piece, new[] { tie }, name);
            return tie.End;
        }

        // A point to the centimetre, naming its rope's tie and fitting (the same rope made again finds them).
        private static string Key(Vector3 p) => $"{Mathf.RoundToInt(p.x * 100f)}_{Mathf.RoundToInt(p.y * 100f)}_{Mathf.RoundToInt(p.z * 100f)}";
    }
}
