using System.Collections.Generic;
using UnityEngine;

namespace GrindstoneSkills
{
    /// <summary>
    /// One creature kind's produce: the item prefabs it can give while alive, each weighted by its drop chance in the
    /// creature's CharacterDrop (a wolf's pelt at chance 1 comes twice as often as a fang at 0.5). Built once per kind by
    /// <see cref="ProduceTables"/>; read-only afterwards.
    /// </summary>
    public sealed class ProduceTable
    {
        private readonly List<GameObject> items = new List<GameObject>();
        private readonly List<float> weights = new List<float>();
        private float total;

        public bool IsEmpty => items.Count == 0;

        /// <summary>The item prefab names, for the log.</summary>
        public IEnumerable<string> Names
        {
            get
            {
                foreach (GameObject item in items)
                    yield return item.name;
            }
        }

        public void Add(GameObject item, float weight)
        {
            if (item == null || weight <= 0f || items.Contains(item))
                return;
            items.Add(item);
            weights.Add(weight);
            total += weight;
        }

        /// <summary>One item prefab, picked by weight; null when the table is empty.</summary>
        public GameObject Pick()
        {
            if (IsEmpty)
                return null;
            float roll = Random.Range(0f, total);
            for (int i = 0; i < items.Count; i++)
            {
                roll -= weights[i];
                if (roll < 0f)
                    return items[i];
            }
            return items[items.Count - 1];
        }
    }
}
