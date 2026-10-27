using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// What a ship needs to know about some of its blueprint's pieces without the pieces being there: a keel's length and
    /// thickness (from its saved sizes), a sail's saved spec, a kit prefab's visible parts copied as plain drawing objects
    /// (no behaviour; box colliders kept, mesh colliders not), and how the rudder's post is sized.
    /// </summary>
    public static class KwShipParts
    {
        private static readonly int SpanHash = KwGenerated.Key.GetStableHashCode();

        public static string Saved(KwBpPiece piece) => KwBpData.Text(piece.Data, SpanHash) ?? "";

        /// <summary>A keel piece's length and thickness; false for any other piece, or a keel whose sizes do not read.</summary>
        public static bool Keel(KwBpPiece piece, out float length, out KwKeelBody body)
        {
            (length, body) = (0f, KwKeel.Standard);
            if (piece.Prefab != KwKeel.MadeName || !KwKeelSpec.TryParse(Saved(piece), out KwKeelSpec spec))
                return false;
            (length, body) = (spec.Length, spec.Body);
            return true;
        }

        /// <summary>A kit prefab's drawn parts (its children with renderers) copied under a holder placed as the piece is.</summary>
        public static Transform Copy(Transform holder, KwBpPiece piece, string name)
        {
            Transform made = new GameObject(name) { layer = holder.gameObject.layer }.transform;
            made.SetParent(holder, false);
            made.SetLocalPositionAndRotation(piece.Position, piece.Rotation);
            GameObject? prefab = ZNetScene.instance.GetPrefab(piece.Prefab);
            if (prefab == null)
                return made;
            foreach (Transform child in prefab.transform)
            {
                if (child.CompareTag("snappoint") || child.GetComponentInChildren<Renderer>(true) == null)
                    continue;
                GameObject copy = Object.Instantiate(child.gameObject, made, false);
                copy.name = child.name;
                Plain(copy);
            }
            return made;
        }

        // Drawing and simple shapes left: behaviours off a copy, and mesh colliders, which a moving ship cannot carry.
        private static void Plain(GameObject copy)
        {
            foreach (MeshCollider collider in copy.GetComponentsInChildren<MeshCollider>(true))
                Object.DestroyImmediate(collider);
            foreach (MonoBehaviour behaviour in copy.GetComponentsInChildren<MonoBehaviour>(true))
                Object.DestroyImmediate(behaviour);
            copy.SetActive(true);
        }

        /// <summary>
        /// The post a rudder hangs on, from the blueprint's keels: the one whose rudder point is where the rudder is, and
        /// the drop to the keel bottom of it and the keels in line with it (<see cref="KwRudderDrop"/>), as on the dock; and
        /// the ship's design waterline on it (<see cref="KwRudderWater"/>).
        /// </summary>
        public static KwRudderFit Fit(KwBlueprint blueprint, KwBpPiece rudder)
        {
            foreach (KwBpPiece piece in blueprint.Pieces)
            {
                KwDef? def = KeelDef(piece);
                if (def == null)
                    continue;
                Vector3? point = Snap(def, piece, "Rudder"), top = Snap(def, piece, "Post top");
                if (point.HasValue && top.HasValue && (point.Value - rudder.Position).sqrMagnitude < 0.04f && top.Value.y - point.Value.y > 0.3f)
                {
                    Keel(piece, out _, out KwKeelBody body);
                    float drop = KwRudderDrop.Of(point.Value.y, Bottom(blueprint, piece, point.Value.y));
                    return new KwRudderFit(top.Value.y - point.Value.y, body.PostHalf, body.PostDepth, drop, KwRudderWater.Over(blueprint, point.Value.y));
                }
            }
            return KwRudderFit.Standard.WithWater(KwRudderWater.Over(blueprint, rudder.Position.y));
        }

        // The lowest of a height and the feet of a keel and of the blueprint's keels in line with it.
        private static float Bottom(KwBlueprint blueprint, KwBpPiece keel, float bottom)
        {
            Vector3 along = keel.Rotation * Vector3.forward;
            foreach (KwBpPiece piece in blueprint.Pieces)
            {
                KwDef? def = KeelDef(piece);
                if (def != null && (piece == keel || KwRudderDrop.InLine(keel.Position, along, piece.Position, piece.Rotation * Vector3.forward)))
                    bottom = Lowest(def, piece, bottom);
            }
            return bottom;
        }

        private static float Lowest(KwDef def, KwBpPiece piece, float bottom)
        {
            Vector3 pivot = new Vector3(def.Pivot.x, 0f, def.Pivot.y);
            foreach ((string Name, Vector3 Kit) snap in def.Snaps)
            {
                if (snap.Name == KwRudderDrop.Foot)
                    bottom = Mathf.Min(bottom, (piece.Position + piece.Rotation * KwMesh.ToUnity(snap.Kit - pivot)).y);
            }
            return bottom;
        }

        private static KwDef? KeelDef(KwBpPiece piece) =>
            piece.Prefab == KwKeel.MadeName && KwKeelSpec.TryParse(Saved(piece), out KwKeelSpec spec) ? KwKeel.Custom(spec) : null;

        // A snap point of a keel's definition placed as the keel piece is, as KwSnaps sets it on the piece.
        private static Vector3? Snap(KwDef def, KwBpPiece piece, string name)
        {
            Vector3 pivot = new Vector3(def.Pivot.x, 0f, def.Pivot.y);
            foreach ((string Name, Vector3 Kit) snap in def.Snaps)
            {
                if (snap.Name == name)
                    return piece.Position + piece.Rotation * KwMesh.ToUnity(snap.Kit - pivot);
            }
            return null;
        }
    }
}
