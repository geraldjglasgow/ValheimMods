using System.Collections.Generic;
using UnityEngine;

namespace HighSeat
{
    /// <summary>
    /// The lone set: what the server makes of a sector round a single player when "Creatures only where shared" is on.
    /// The ground itself is always built there (<see cref="ZoneKeeping"/>). On top of it, only what the server's own work
    /// there reads: the spawners it runs; the players and creatures and fish they count (a nest counts its creatures
    /// round it, a wild spawn and a trigger spawner the same kind near their spot - all by instance; the creatures stay
    /// with their player, so the server's copies only follow their owner's updates); the terrain
    /// compilers (the ground's shape); the locations and dungeons (a spawn point reads its location, a dungeon nest its
    /// floor); anything marking a player base or a no-monster area (wild spawns keep out of them); and anything the
    /// server owns itself (what it has just made). Buildings, trees, rocks, pickables, items, ships and carts are left to
    /// the players: nothing the server runs there reads them, save the game's blocked test for a spawn point, which
    /// <see cref="SpawnPoints"/> answers from the footprint kept here: how far an object's solid colliders reach from its
    /// position across the ground (the colliders the game's ZoneSystem.IsBlocked ray can hit), decided once per prefab.
    /// A prefab this server does not know is left out (the game would delete its ZDO trying to make it).
    /// </summary>
    internal static class LoneLoad
    {
        private const float MinFootprint = 0.25f;
        private const EffectArea.Type Marks = EffectArea.Type.PlayerBase | EffectArea.Type.NoMonsters;

        private struct Decision
        {
            public bool Keep;
            public float Radius;
            public float Scale;
        }

        private static readonly Dictionary<int, Decision> Decisions = new Dictionary<int, Decision>();
        private static readonly List<Collider> Colliders = new List<Collider>();
        private static readonly List<EffectArea> Areas = new List<EffectArea>();
        private static int _blockMask;

        /// <summary>True when the lone set holds this object; otherwise also how far round its position it blocks a spawn
        /// point, in metres (0 when it blocks none).</summary>
        public static bool Keeps(ZDO zdo, out float footprint)
        {
            footprint = 0f;
            if (zdo.IsOwner())
            {
                return true;
            }
            Decision decision = Of(zdo.GetPrefab());
            if (decision.Keep)
            {
                return true;
            }
            if (decision.Radius > 0f)
            {
                Vector3 scale = zdo.GetVec3(ZDOVars.s_scaleHash, Vector3.zero);
                float factor = scale != Vector3.zero ? Mathf.Max(scale.x, scale.z) : zdo.GetFloat(ZDOVars.s_scaleScalarHash, decision.Scale);
                footprint = decision.Radius * factor;
            }
            return false;
        }

        private static Decision Of(int prefab)
        {
            if (!Decisions.TryGetValue(prefab, out Decision decision))
            {
                decision = Decide(prefab);
                Decisions[prefab] = decision;
            }
            return decision;
        }

        private static Decision Decide(int prefab)
        {
            GameObject go = ZNetScene.instance != null ? ZNetScene.instance.GetPrefab(prefab) : null;
            if (go == null)
            {
                return default;
            }
            if (Kept(go))
            {
                return new Decision { Keep = true };
            }
            Vector3 own = go.transform.localScale;
            return new Decision { Radius = Radius(go), Scale = Mathf.Max(own.x, own.z) };
        }

        private static bool Kept(GameObject go)
        {
            if (go.GetComponent<Player>() != null || go.GetComponent<BaseAI>() != null || go.GetComponent<Fish>() != null
                || go.CompareTag("spawned") || ServerRun.IsSpawner(go))
            {
                return true;
            }
            if (go.GetComponent<TerrainComp>() != null || go.GetComponent<TerrainModifier>() != null
                || go.GetComponent<LocationProxy>() != null || go.GetComponent<DungeonGenerator>() != null)
            {
                return true;
            }
            go.GetComponentsInChildren(true, Areas);
            bool marks = false;
            foreach (EffectArea area in Areas)
            {
                marks |= (area.m_type & Marks) != 0;
            }
            Areas.Clear();
            return marks;
        }

        // The farthest any solid collider of the prefab reaches from its pivot across the ground, in its own units.
        private static float Radius(GameObject go)
        {
            if (_blockMask == 0)
            {
                _blockMask = LayerMask.GetMask("Default", "static_solid", "Default_small", "piece");
            }
            go.GetComponentsInChildren(true, Colliders);
            Matrix4x4 toRoot = go.transform.worldToLocalMatrix;
            float best = 0f;
            foreach (Collider collider in Colliders)
            {
                Bounds box = LocalBox(collider);
                if (!collider.isTrigger && (_blockMask & (1 << collider.gameObject.layer)) != 0 && box.size != Vector3.zero)
                {
                    best = Mathf.Max(best, Reach(box, toRoot * collider.transform.localToWorldMatrix));
                }
            }
            Colliders.Clear();
            return best > 0f ? Mathf.Max(best, MinFootprint) : 0f;
        }

        // The farthest corner of a box, carried into the prefab's own space, from its pivot across the ground.
        private static float Reach(Bounds box, Matrix4x4 toRoot)
        {
            float best = 0f;
            for (int i = 0; i < 8; i++)
            {
                Vector3 sign = new Vector3((i & 1) == 0 ? -1f : 1f, (i & 2) == 0 ? -1f : 1f, (i & 4) == 0 ? -1f : 1f);
                Vector3 corner = toRoot.MultiplyPoint3x4(box.center + Vector3.Scale(box.extents, sign));
                best = Mathf.Max(best, corner.x * corner.x + corner.z * corner.z);
            }
            return Mathf.Sqrt(best);
        }

        // A collider's own box, from its shape (a prefab that is not in the scene has no world bounds yet).
        private static Bounds LocalBox(Collider collider)
        {
            switch (collider)
            {
                case BoxCollider box:
                    return new Bounds(box.center, box.size);
                case SphereCollider sphere:
                    return new Bounds(sphere.center, Vector3.one * (2f * sphere.radius));
                case CapsuleCollider capsule:
                    Vector3 size = Vector3.one * (2f * capsule.radius);
                    size[capsule.direction] = Mathf.Max(capsule.height, 2f * capsule.radius);
                    return new Bounds(capsule.center, size);
                case MeshCollider mesh when mesh.sharedMesh != null:
                    return mesh.sharedMesh.bounds;
                default:
                    return new Bounds(Vector3.zero, Vector3.zero);
            }
        }
    }
}
