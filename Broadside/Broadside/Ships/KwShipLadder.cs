using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// The ladder over a launched ship's side, to climb aboard from the water (the karve's own ladder and its use, moved
    /// and remade), where its blueprint puts it (<see cref="KwShipLadderFind"/>): on a stretch of the hull's side with no
    /// cabin standing at the hull, near the widest section, port before starboard; else up to a doorway in a side, else up
    /// a raised side to the lowest cabin roof's edge. It hangs straight down from its top to a little under the waterline,
    /// clear of the hull at its widest between those heights, with the hatch ladders' rungs (<see cref="KwLadder"/>) in their
    /// wood, hooked over the edge (over a railing's cap where there is one) and held off the planking by pairs of timbers
    /// from the hull's side to its rails, every 1.3 m or so down to the water. It is used from anywhere along it (the game
    /// measures from the ladder's top); climbing puts the player on the floor the plan found: a deck inside the rail, inside
    /// the doorway, or on the roof, facing inboard. The climb lifts the player over a railing, as the game's own ladder
    /// does: the railing keeps its look. Placed on every peer, a dedicated server too (the climb's place is the ladder's);
    /// only drawn where there is a screen.
    /// </summary>
    public static class KwShipLadder
    {
        private const float Rail = 0.22f, Beam = 0.045f, Every = 1.3f;

        public static void Place(Transform ship, KwBlueprint blueprint, KwBpHull hull)
        {
            Transform? ladder = ship.Find("interactive/ladder"), target = ship.Find("interactive/ladder_TargetPoint");
            if (ladder == null)
                return;
            KwShipLadderPlan plan = KwShipLadderFind.For(blueprint, hull);
            Transform? holder = ship.Find(KwShipPrefab.Fittings);
            Transform frame = holder != null ? holder : ship;
            ladder.SetPositionAndRotation(frame.TransformPoint(plan.TopAt), frame.rotation * Quaternion.LookRotation(plan.Outboard, Vector3.up));
            ladder.localScale = Vector3.one;
            Ladder? use = ladder.GetComponent<Ladder>();
            if (use != null)
                use.m_useDistance = plan.Top - plan.Foot + 1.5f;
            if (target != null)
                target.SetPositionAndRotation(frame.TransformPoint(plan.TargetAt), frame.rotation * Quaternion.LookRotation(-plan.Outboard, Vector3.up));
            Box(ladder, plan.Top - plan.Foot);
            if (!KwShipBody.Drawn)
                return;
            Dress(ladder, KwLadder.Mesh(plan.Top - plan.Foot));
            Supports(ladder, plan);
        }

        // The heights of the supports: one near the top, then evenly down to just over the water, about every 1.3 m.
        private static List<float> Heights(KwShipLadderPlan plan)
        {
            float high = plan.Top - 0.2f, low = Mathf.Max(plan.Foot, plan.Water + 0.15f);
            List<float> heights = new List<float> { high };
            int gaps = high - low > 0.3f ? Mathf.Max(1, Mathf.CeilToInt((high - low) / Every)) : 0;
            for (int i = 1; i <= gaps; i++)
                heights.Add(Mathf.Lerp(high, low, i / (float)gaps));
            return heights;
        }

        // Pairs of timbers from the hull's side to the rails at each support's height, and the rails' tops bent over the
        // edge and down inside it, so the ladder hangs on the ship (the ladder's kit: x out, y across, z up from its top).
        private static void Supports(Transform ladder, KwShipLadderPlan plan)
        {
            MeshBuilder b = new MeshBuilder(Vector3.zero);
            foreach (float h in Heights(plan))
            {
                float hull = plan.HullAt(h), reach = plan.Out - hull + 0.04f, down = h - plan.Top;
                if (hull <= 0f)
                    continue;
                foreach (float y in new[] { -Rail, Rail })
                    Solid.Box(b, new Vector3(-reach, y - Beam, down - Beam), new Vector3(0.1f, y + Beam, down + Beam));
            }
            float over = plan.Out - plan.HullAt(plan.Top) + 0.14f;
            foreach (float y in new[] { -Rail, Rail })
            {
                Solid.Box(b, new Vector3(-over, y - 0.03f, 0.04f), new Vector3(0.1f, y + 0.03f, 0.1f));
                Solid.Box(b, new Vector3(-over, y - 0.03f, -0.18f), new Vector3(-over + 0.06f, y + 0.03f, 0.1f));
            }
            Transform? old = ladder.Find("supports");
            if (old != null)
                Object.Destroy(old.gameObject);
            GameObject supports = new GameObject("supports") { layer = ladder.gameObject.layer };
            supports.transform.SetParent(ladder, false);
            supports.AddComponent<MeshFilter>().sharedMesh = b.ToMesh("kw_ladder_supports");
            supports.AddComponent<MeshRenderer>().sharedMaterial = ladder.GetComponent<MeshRenderer>()?.sharedMaterial;
        }

        // The ladder's mesh and its wood.
        private static void Dress(Transform ladder, Mesh mesh)
        {
            MeshFilter? filter = ladder.GetComponent<MeshFilter>();
            if (filter != null)
                filter.sharedMesh = mesh;
            MeshRenderer? look = ladder.GetComponent<MeshRenderer>();
            Material? wood = ZNetScene.instance?.GetPrefab(KwToolPieces.PartPrefix + "ladder")?.GetComponentInChildren<MeshRenderer>()?.sharedMaterial;
            if (look != null && wood != null)
                look.sharedMaterial = wood;
        }

        // Its box (what the player aims at to climb), round its rails from the foot to over the top (KwLadder's shape:
        // 0.25 m each side, its rails 0.03 to 0.1 m out, 0.1 m over its top), made bigger a little.
        private static void Box(Transform ladder, float length)
        {
            BoxCollider? box = ladder.GetComponent<BoxCollider>();
            if (box == null)
                return;
            box.center = new Vector3(0f, (0.1f - length) / 2f, 0.065f);
            box.size = new Vector3(0.6f, length + 0.1f, 0.17f);
        }
    }
}
