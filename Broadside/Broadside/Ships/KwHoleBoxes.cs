using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// A launched ship's boxes opened where its holes are (<see cref="KwHullHoles"/>), on every peer, a dedicated server
    /// included, so shots, thrown things and small objects pass through a hole as through an opening. The ship's boxes
    /// (<see cref="KwShipColliders"/>, <see cref="KwShipHouses"/>) are known as built; the boxes a hole reaches and its gaps
    /// in them (<see cref="KwHoleGaps"/>) are worked out once for each hole and size, and when the holes change only a box
    /// whose holes changed is made again: switched off and stood in for by boxes round its gaps (named as it is, on its
    /// layer), or switched on again once its holes are mended. The body's mass and turning are kept as they were
    /// (<see cref="KwBodyMass"/>), so a hole never changes how the ship floats; the water it lets in does
    /// (<see cref="KwShipFlood"/>).
    /// </summary>
    public sealed class KwHoleBoxes : MonoBehaviour
    {
        private readonly List<(BoxCollider Box, KwHoleSlab Slab)> boxes = new List<(BoxCollider Box, KwHoleSlab Slab)>();
        private readonly Dictionary<int, (string Key, List<GameObject> Pieces)> cut = new Dictionary<int, (string Key, List<GameObject> Pieces)>();
        private Dictionary<string, List<(int Box, List<Rect> Gaps)>> reached = new Dictionary<string, List<(int Box, List<Rect> Gaps)>>();
        private Transform? holder;
        private KwHullHoles? holes;
        private int done = -1;

        public static void Add(Transform ship, KwHullHoles holes)
        {
            Transform? holder = ship.Find(KwShipPrefab.Colliders);
            if (holder == null)
                return;
            KwHoleBoxes boxes = ship.gameObject.AddComponent<KwHoleBoxes>();
            (boxes.holder, boxes.holes) = (holder, holes);
            foreach (Transform child in holder)
            {
                if (child.TryGetComponent(out BoxCollider box))
                    boxes.boxes.Add((box, Slab(child, box)));
            }
        }

        /// <summary>
        /// Some of the ship's boxes made again (a part's, when a door or window is cut at sea, <see cref="KwSeaOpeningBoxes"/>):
        /// the ones going forgotten (their stand-ins let go), the ones made reached by the holes there are, every box looked at
        /// again on the next frame. Call before the old boxes are destroyed.
        /// </summary>
        public static void Rebox(Transform ship, List<BoxCollider> gone, List<BoxCollider> made)
        {
            KwHoleBoxes? boxes = ship.GetComponent<KwHoleBoxes>();
            if (boxes != null && boxes.holder != null)
                boxes.Swap(gone, made);
        }

        private void Swap(List<BoxCollider> gone, List<BoxCollider> made)
        {
            HashSet<BoxCollider> going = new HashSet<BoxCollider>(gone);
            for (int i = 0; i < boxes.Count; i++)
            {
                if (boxes[i].Box != null && going.Contains(boxes[i].Box))
                {
                    Clear(i);
                    boxes[i] = (null!, boxes[i].Slab);
                }
            }
            int first = boxes.Count;
            foreach (BoxCollider box in made)
                boxes.Add((box, Slab(box.transform, box)));
            Matrix4x4 into = holder!.worldToLocalMatrix * transform.localToWorldMatrix;
            foreach (KwHullHole h in holes != null ? holes.Holes : new List<KwHullHole>())
            {
                if (reached.TryGetValue(h.Key, out List<(int Box, List<Rect> Gaps)> reach))
                    reach.AddRange(Reach(KwHoleShape.Of(h).Moved(into.MultiplyPoint3x4, into.MultiplyVector), first));
            }
            done = -1;
        }

        // A box in the colliders' frame.
        private static KwHoleSlab Slab(Transform at, BoxCollider box)
        {
            Quaternion turn = at.localRotation;
            Vector3 centre = at.localPosition + turn * Vector3.Scale(box.center, at.localScale);
            return new KwHoleSlab(centre, turn * Vector3.right, turn * Vector3.up, turn * Vector3.forward, Vector3.Scale(box.size, at.localScale) / 2f);
        }

        private void Update()
        {
            if (holes == null || holder == null || holes.Version == done)
                return;
            done = holes.Version;
            Matrix4x4 into = holder.worldToLocalMatrix * transform.localToWorldMatrix;
            Dictionary<string, List<(int Box, List<Rect> Gaps)>> now = new Dictionary<string, List<(int Box, List<Rect> Gaps)>>();
            foreach (KwHullHole h in holes.Holes)
                now[h.Key] = reached.TryGetValue(h.Key, out var known) ? known : Reach(KwHoleShape.Of(h).Moved(into.MultiplyPoint3x4, into.MultiplyVector));
            reached = now;
            Rigidbody? body = GetComponent<Rigidbody>();
            KwBodyMass kept = KwBodyMass.Of(body);
            Dictionary<int, (StringBuilder Key, List<Rect> Gaps)> gaps = Gaps();
            bool changed = false;
            for (int i = 0; i < boxes.Count; i++)
                changed |= Box(i, gaps.TryGetValue(i, out var mine) ? mine : (new StringBuilder(), new List<Rect>()));
            if (changed)
                kept.Put(body);
        }

        // The boxes a hole reaches and the gaps it opens in each (worked out once a hole and size), from a box on; boxes
        // made again since (null) left out.
        private List<(int Box, List<Rect> Gaps)> Reach(KwHoleShape shape, int from = 0)
        {
            List<(int Box, List<Rect> Gaps)> reach = new List<(int Box, List<Rect> Gaps)>();
            for (int i = from; i < boxes.Count; i++)
            {
                if (boxes[i].Box == null)
                    continue;
                List<Rect> gaps = KwHoleGaps.Of(boxes[i].Slab, shape);
                if (gaps.Count > 0)
                    reach.Add((i, gaps));
            }
            return reach;
        }

        // Every reached box's gaps from all the holes, and which holes they are.
        private Dictionary<int, (StringBuilder Key, List<Rect> Gaps)> Gaps()
        {
            Dictionary<int, (StringBuilder Key, List<Rect> Gaps)> all = new Dictionary<int, (StringBuilder Key, List<Rect> Gaps)>();
            foreach (KeyValuePair<string, List<(int Box, List<Rect> Gaps)>> hole in reached)
            {
                foreach ((int i, List<Rect> gaps) in hole.Value)
                {
                    if (!all.TryGetValue(i, out var box))
                        all[i] = box = (new StringBuilder(), new List<Rect>());
                    box.Key.Append(hole.Key).Append(';');
                    box.Gaps.AddRange(gaps);
                }
            }
            return all;
        }

        // One box made again when the holes reaching it changed (true).
        private bool Box(int i, (StringBuilder Key, List<Rect> Gaps) mine)
        {
            string now = mine.Key.ToString(), was = cut.TryGetValue(i, out var before) ? before.Key : "";
            if (now == was)
                return false;
            Clear(i);
            if (mine.Gaps.Count > 0)
                Split(i, mine.Gaps, now);
            return true;
        }

        // A box's stand-ins gone and the box itself on again.
        private void Clear(int i)
        {
            if (cut.TryGetValue(i, out var was))
            {
                foreach (GameObject piece in was.Pieces)
                {
                    piece.SetActive(false);
                    Destroy(piece);
                }
                cut.Remove(i);
            }
            if (boxes[i].Box != null)
                boxes[i].Box.enabled = true;
        }

        // A box off, and boxes round its gaps in its place.
        private void Split(int i, List<Rect> gaps, string key)
        {
            (BoxCollider box, KwHoleSlab slab) = boxes[i];
            if (box == null)
                return;
            box.enabled = false;
            List<GameObject> pieces = new List<GameObject>();
            foreach (Rect r in KwHoleGaps.Left(slab, gaps))
                pieces.Add(Piece(box, slab, r));
            cut[i] = (key, pieces);
        }

        // A stand-in for part of a box: its rectangle of the box's face, as thick as the box, turned as it is.
        private GameObject Piece(BoxCollider box, KwHoleSlab slab, Rect r)
        {
            GameObject piece = new GameObject(box.name) { layer = box.gameObject.layer };
            piece.transform.SetParent(holder, false);
            piece.transform.SetLocalPositionAndRotation(slab.Centre + slab.Axis(slab.A) * r.center.x + slab.Axis(slab.B) * r.center.y, box.transform.localRotation);
            Vector3 size = slab.Half * 2f;
            size[slab.A] = r.width;
            size[slab.B] = r.height;
            BoxCollider made = piece.AddComponent<BoxCollider>();
            (made.size, made.sharedMaterial) = (size, box.sharedMaterial);
            return piece;
        }
    }
}
