using System.Collections.Generic;
using System.Linq;
using EarthWright.Terrain;
using UnityEngine;

namespace EarthWright.History
{
    /// <summary>
    /// The recorded raw values of one heightmap's compiler: what each touched array index held before the change.
    /// Addressed by the heightmap's centre, since the compiler object itself comes and goes as the area loads.
    /// </summary>
    internal sealed class CompRecord
    {
        public Vector3 Position;

        public readonly Dictionary<int, RawVertex> Values = new Dictionary<int, RawVertex>();

        /// <summary>Indices recorded since the last prune, still to be checked against the ground after the edit.</summary>
        public readonly List<int> Fresh = new List<int>();

        public Vector2Int Key => RawAccess.KeyOf(Position);
    }

    /// <summary>
    /// One entry of the undo or redo list: the ground as it was before a group of edits (a dragged stroke, or edits
    /// sent close together), per heightmap. Within a step the first recorded value of an index wins, because a later
    /// snapshot may already be stale while earlier edits are still on their way to the compiler's owner.
    /// </summary>
    internal sealed class Step
    {
        /// <summary>What made the edits, in order ("mud_road_v2", "ramp", "command:reset" ...), without repeats.</summary>
        public readonly List<string> Sources = new List<string>();

        public readonly Dictionary<Vector2Int, CompRecord> Comps = new Dictionary<Vector2Int, CompRecord>();

        /// <summary>Privilege flags of the recorded edits, carried by the restore so admin edits can be taken back in full.</summary>
        public EditFlags Carry;

        public int Edits;

        /// <summary>Time.time of the first and the last recorded edit.</summary>
        public float Started;
        public float LastSend;

        /// <summary>The primary button press this step began in (<see cref="PressTracker"/>), or -1.</summary>
        public int Press = -1;

        public Step(IEnumerable<string> sources, EditFlags carry)
        {
            foreach (string source in sources)
                AddSource(source);
            Carry = carry;
            Started = LastSend = Time.time;
        }

        public void AddSource(string source)
        {
            if (!string.IsNullOrEmpty(source) && !Sources.Contains(source))
                Sources.Add(source);
        }

        /// <summary>The record of a heightmap, created on first use.</summary>
        public CompRecord For(Heightmap map)
        {
            Vector2Int key = RawAccess.KeyOf(map.transform.position);
            if (!Comps.TryGetValue(key, out CompRecord record))
            {
                record = new CompRecord { Position = map.transform.position };
                Comps[key] = record;
            }
            return record;
        }

        public void Add(CompRecord record) => Comps[record.Key] = record;

        public int Points => Comps.Values.Sum(c => c.Values.Count);

        public bool IsEmpty => Comps.Values.All(c => c.Values.Count == 0);

        /// <summary>A step with the same sources and flags, for the opposite list or a remainder.</summary>
        public Step Sibling() => new Step(Sources, Carry) { Edits = Edits };

        /// <summary>The sources as the player knows them: entry names where possible.</summary>
        public string Describe() => SourceNames.Describe(Sources);
    }
}
