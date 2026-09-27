using System.Collections.Generic;
using UnityEngine;

namespace GrindstoneSkills
{
    /// <summary>
    /// Finds the Echo's target on the miner's own client: the nearest loaded ore deposit within a radius.
    /// <list type="bullet">
    /// <item><b>Search:</b> one walk over the objects this client has loaded (ZNetScene.m_instances, one entry per
    /// networked object), with a squared-distance check on each ZDO's position. Only objects nearer than the best so far
    /// are classified (<see cref="Rock.Find"/>, a cached lookup per prefab), and prefabs found to be no rock are
    /// remembered by prefab hash, so later scans pass them with one set lookup. Nothing per frame: the Echo scans at most
    /// once per swing.</item>
    /// <item><b>A target:</b> an ore deposit (<see cref="Rock.IsOre"/>) that still exists here, without a Beacon (buried
    /// silver and beacon mud piles stay the Wishbone's job), with at least one intact chunk when it breaks chunk by
    /// chunk, and not one of the rocks the swing just hit.</item>
    /// <item><b>One deposit:</b> an intact deposit and its fractured form are the same deposit (same kind, same spot),
    /// so a fractured form the swing's own hit just spawned is skipped with the intact deposit it replaced.</item>
    /// </list>
    /// </summary>
    internal static class EchoScan
    {
        /// <summary>How close two rocks of one kind stand when they are one deposit: a fractured form spawns at the intact deposit's exact spot.</summary>
        private const float SameSpot = 1f;

        private static readonly HashSet<int> NotRocks = new HashSet<int>();

        /// <summary>The nearest target within <paramref name="radius"/> of <paramref name="from"/>, skipping <paramref name="hit"/>; null when there is none.</summary>
        public static Rock Nearest(Vector3 from, float radius, IReadOnlyList<Rock> hit)
        {
            ZNetScene scene = ZNetScene.instance;
            if (scene == null)
                return null;
            Rock best = null;
            float bestSqr = radius * radius;
            foreach (KeyValuePair<ZDO, ZNetView> entry in scene.m_instances)
            {
                float sqr = (entry.Key.GetPosition() - from).sqrMagnitude;
                if (sqr > bestSqr)
                    continue;
                Rock rock = Classify(entry.Key, entry.Value);
                if (rock != null && IsTarget(rock, hit))
                {
                    best = rock;
                    bestSqr = sqr;
                }
            }
            return best;
        }

        /// <summary>The rock a loaded object is; null for anything else, remembered per prefab.</summary>
        private static Rock Classify(ZDO zdo, ZNetView view)
        {
            int prefab = zdo.GetPrefab();
            if (view == null || NotRocks.Contains(prefab))
                return null;
            Rock rock = Rock.Find(view.gameObject);
            if (rock == null)
                NotRocks.Add(prefab);
            return rock;
        }

        private static bool IsTarget(Rock rock, IReadOnlyList<Rock> hit) =>
            rock.IsOre && !rock.HasBeacon && !JustHit(rock, hit) && rock.IsValid && HasIntact(rock);

        /// <summary>A rock that breaks chunk by chunk has a chunk left; a single piece or an intact deposit is whole while it exists.</summary>
        private static bool HasIntact(Rock rock) => !rock.HasChunks || RockChunks.Left(rock) > 0;

        private static bool JustHit(Rock rock, IReadOnlyList<Rock> hit)
        {
            for (int i = 0; i < hit.Count; i++)
            {
                if (hit[i] != null && SameDeposit(rock, hit[i]))
                    return true;
            }
            return false;
        }

        /// <summary>
        /// The same rock, or an intact deposit and its fractured form: same kind, standing on the same spot. A rock the
        /// swing destroyed this frame (an intact deposit replaced by its fractured form) still has its transform.
        /// </summary>
        private static bool SameDeposit(Rock rock, Rock other)
        {
            if (ReferenceEquals(rock.Target, other.Target))
                return true;
            if (other.Target == null || rock.Kind != other.Kind)
                return false;
            return (rock.Position - other.Position).sqrMagnitude < SameSpot * SameSpot;
        }
    }
}
