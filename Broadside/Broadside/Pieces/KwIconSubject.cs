using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace Broadside
{
    /// <summary>
    /// What a hammer icon shows: a copy of a piece's drawn parts set out on the icon stage, on the icon layer. A part is
    /// every mesh renderer under the piece that is switched on, on a child that is switched on all the way up to the piece
    /// (a hidden part draws nothing on a placed piece either), placed, turned and scaled as on the piece, in its own
    /// materials; a piece or a saved ship's whole hull alike. Also the box round the parts and the points their outline is
    /// framed by: every vertex of a mesh Unity can read (a ship's thousands too), the corners of its box otherwise.
    /// </summary>
    public sealed class KwIconSubject : IDisposable
    {
        private readonly GameObject holder;
        private readonly List<Vector3> points = new List<Vector3>();

        private KwIconSubject(Vector3 stage)
        {
            holder = new GameObject("KwIconSubject");
            holder.transform.position = stage;
        }

        /// <summary>The box round the parts, in the piece's own frame (the stage's).</summary>
        public Bounds Box { get; private set; }

        /// <summary>The copy, or null when nothing under the piece draws.</summary>
        public static KwIconSubject? Make(Transform piece, Vector3 stage, int layer)
        {
            KwIconSubject subject = new KwIconSubject(stage);
            foreach (MeshRenderer renderer in piece.GetComponentsInChildren<MeshRenderer>(true))
            {
                MeshFilter? filter = renderer.GetComponent<MeshFilter>();
                if (filter != null && filter.sharedMesh != null && Drawn(renderer, piece))
                    subject.Add(piece, filter, renderer, layer);
            }
            if (subject.points.Count > 0)
                return subject;
            subject.Dispose();
            return null;
        }

        // Switched on, drawing more than a shadow, on children switched on up to the piece (the piece's own switch, and
        // the inactive bench its prefab waits on, do not count).
        private static bool Drawn(MeshRenderer renderer, Transform piece)
        {
            if (!renderer.enabled || renderer.forceRenderingOff || renderer.shadowCastingMode == ShadowCastingMode.ShadowsOnly)
                return false;
            for (Transform at = renderer.transform; at != piece && at != null; at = at.parent)
            {
                if (!at.gameObject.activeSelf)
                    return false;
            }
            return true;
        }

        private void Add(Transform piece, MeshFilter filter, MeshRenderer renderer, int layer)
        {
            Matrix4x4 place = piece.worldToLocalMatrix * filter.transform.localToWorldMatrix;
            GameObject part = new GameObject("KwIconPart") { layer = layer };
            part.transform.SetParent(holder.transform, false);
            part.transform.localPosition = place.GetColumn(3);
            part.transform.localRotation = place.rotation;
            part.transform.localScale = place.lossyScale;
            part.AddComponent<MeshFilter>().sharedMesh = filter.sharedMesh;
            MeshRenderer copy = part.AddComponent<MeshRenderer>();
            copy.sharedMaterials = renderer.sharedMaterials;
            copy.shadowCastingMode = ShadowCastingMode.Off;
            copy.receiveShadows = false;
            copy.lightProbeUsage = LightProbeUsage.Off;
            AddPoints(filter.sharedMesh, place);
        }

        private void AddPoints(Mesh mesh, Matrix4x4 place)
        {
            int before = points.Count;
            if (mesh.isReadable && mesh.vertexCount > 0)
            {
                foreach (Vector3 vertex in mesh.vertices)
                    points.Add(place.MultiplyPoint3x4(vertex));
            }
            else
            {
                Bounds box = mesh.bounds;
                for (int i = 0; i < 8; i++)
                    points.Add(place.MultiplyPoint3x4(new Vector3((i & 1) == 0 ? box.min.x : box.max.x, (i & 2) == 0 ? box.min.y : box.max.y, (i & 4) == 0 ? box.min.z : box.max.z)));
            }
            Bounds grown = before == 0 ? new Bounds(points[0], Vector3.zero) : Box;
            for (int i = before; i < points.Count; i++)
                grown.Encapsulate(points[i]);
            Box = grown;
        }

        /// <summary>The parts' outline in a view turned so: its rectangle across and up the picture, from the box's middle.</summary>
        public Rect Seen(Quaternion view)
        {
            Quaternion back = Quaternion.Inverse(view);
            Vector2 min = new Vector2(float.MaxValue, float.MaxValue), max = new Vector2(float.MinValue, float.MinValue);
            foreach (Vector3 point in points)
            {
                Vector3 p = back * (point - Box.center);
                min = Vector2.Min(min, p);
                max = Vector2.Max(max, p);
            }
            return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
        }

        public void Dispose() => Object.DestroyImmediate(holder);
    }
}
