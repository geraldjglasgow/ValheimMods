using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// A launched ship's hatch openings, from its blueprint: every hatch piece's opening (its size from the hatch's prefab)
    /// as the decks under it see it, so the ship's look and its deck colliders leave the same holes a placed deck cuts
    /// round a placed hatch (<see cref="KwDeckHoles"/>). All in the ship's frame, the same on every peer.
    /// </summary>
    public static class KwShipDeckHoles
    {
        /// <summary>The openings over a deck piece of the blueprint, in the deck's kit coordinates.</summary>
        public static List<Rect> For(KwBlueprint blueprint, KwBpPiece deck, KwDef def)
        {
            Quaternion back = Quaternion.Inverse(deck.Rotation);
            return KwDeckHoles.Of(p => back * (p - deck.Position), def, Hatches(blueprint));
        }

        // Each hatch's middle and its opening's four corners, in the ship's frame.
        private static List<(Vector3 Middle, Vector3[] Corners)> Hatches(KwBlueprint blueprint)
        {
            List<(Vector3, Vector3[])> hatches = new List<(Vector3, Vector3[])>();
            foreach (KwBpPiece piece in blueprint.Pieces)
            {
                KwHatchCover? kind = piece.Prefab.StartsWith(KwHatch.Prefix) ? ZNetScene.instance.GetPrefab(piece.Prefab)?.GetComponent<KwHatchCover>() : null;
                if (kind == null)
                    continue;
                float hx = kind.Opening.x / 2f, hy = kind.Opening.y / 2f;
                Vector3[] corners = new Vector3[4];
                Vector2[] kit = { new Vector2(-hx, -hy), new Vector2(hx, -hy), new Vector2(hx, hy), new Vector2(-hx, hy) };
                for (int i = 0; i < 4; i++)
                    corners[i] = piece.Position + piece.Rotation * KwMesh.ToUnity(new Vector3(kit[i].x, kit[i].y, 0f));
                hatches.Add((piece.Position, corners));
            }
            return hatches;
        }
    }
}
