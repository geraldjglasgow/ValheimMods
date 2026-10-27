using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// A launched ship's ladders (the ladder tool's: under a hatch, down from a deck's or a cabin roof's open edge), each on
    /// its own under the ship's fittings so a sea edit redraws only the ladders it changed (<see cref="KwSeaLadders"/>): the
    /// unseen box E climbs it by (<see cref="KwLadderClimb"/>, as long as it was made, its blueprint piece kept on it), on
    /// every peer, a dedicated server too; and where there is a screen its look as the box's child, the ladder's mesh in the
    /// plank wood (left out of the ship's combined look, <see cref="KwSeaKind.DrawsOwn"/>).
    /// </summary>
    public static class KwSeaLadderLook
    {
        public const string Name = "ladder_below";

        /// <summary>A ladder's box and look made under the ship's fittings, placed as its piece; null for a text that does not read.</summary>
        public static Transform? Make(Transform holder, KwBpPiece piece)
        {
            if (!KwLadder.TryLength(KwShipParts.Saved(piece), out float length))
                return null;
            GameObject climb = new GameObject(Name) { layer = LayerMask.NameToLayer("piece_nonsolid") };
            climb.transform.SetParent(holder, false);
            climb.transform.SetLocalPositionAndRotation(piece.Position, piece.Rotation);
            BoxCollider box = climb.AddComponent<BoxCollider>();
            box.center = new Vector3(0f, (0.1f - length) / 2f, 0.07f);
            box.size = new Vector3(0.6f, length + 0.1f, 0.3f);
            KwLadderClimb use = climb.AddComponent<KwLadderClimb>();
            (use.FixedLength, use.Piece) = (length, piece);
            if (KwShipBody.Drawn)
                Look(climb.transform, holder.gameObject.layer, length);
            return climb.transform;
        }

        /// <summary>The ladders a sea edit changed made again: the old ones' boxes and looks gone, the new ones made and dressed as the ship's.</summary>
        public static void Redraw(KwSeaShip ship, KwSeaChanges mine)
        {
            Transform holder = ship.Fittings;
            foreach (KwSeaChange change in mine.All)
            {
                if (change.Old != null)
                    Drop(holder, change.Old);
                Transform? made = change.New != null ? Make(holder, change.New) : null;
                if (made != null && KwShipBody.Drawn)
                    KwShipMaterials.Apply(made);
            }
        }

        /// <summary>The look of a ladder piece on a ship (its renderers), for hiding while its window is open.</summary>
        public static Renderer[] Renderers(KwSeaShip ship, KwBpPiece piece)
        {
            KwLadderClimb? climb = Of(ship.Fittings, piece);
            return climb != null ? climb.GetComponentsInChildren<Renderer>(true) : new Renderer[0];
        }

        /// <summary>The climb box made for a piece; null for none.</summary>
        public static KwLadderClimb? Of(Transform holder, KwBpPiece piece)
        {
            foreach (KwLadderClimb climb in holder.GetComponentsInChildren<KwLadderClimb>(true))
            {
                if (ReferenceEquals(climb.Piece, piece))
                    return climb;
            }
            return null;
        }

        // A piece's box and look gone.
        private static void Drop(Transform holder, KwBpPiece piece)
        {
            KwLadderClimb? climb = Of(holder, piece);
            if (climb != null)
                Object.Destroy(climb.gameObject);
        }

        // The ladder's mesh in the plank wood, as the ship's look drew it (KwBpMeshSet), casting no shadow as that look casts
        // none (KwShipShadow), its mesh going with it.
        private static void Look(Transform climb, int layer, float length)
        {
            GameObject look = new GameObject("look") { layer = layer };
            look.transform.SetParent(climb, false);
            look.AddComponent<MeshFilter>().sharedMesh = KwLadder.Mesh(length);
            MeshRenderer renderer = look.AddComponent<MeshRenderer>();
            (renderer.sharedMaterial, renderer.shadowCastingMode) = (KwPrefabs.Planks, UnityEngine.Rendering.ShadowCastingMode.Off);
            look.AddComponent<KwMeshOwner>();
        }
    }
}
