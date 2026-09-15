using System;
using System.Collections.Generic;
using System.Linq;

namespace Lockstep
{
    /// <summary>One boss in the progression chain.</summary>
    public sealed class Stage
    {
        /// <summary>Display name, the top level key in the chain YAML.</summary>
        public string Name;

        /// <summary>Global key the game sets when the boss dies.</summary>
        public string Key;

        /// <summary>Prefab name of the boss the altar spawns.</summary>
        public string BossPrefab;

        /// <summary>Position in the chain; the lowest order is always open.</summary>
        public int Order;
    }

    /// <summary>The ordered chain of stages, vanilla by default and replaced by the chain YAML when it applies.</summary>
    public static class Chain
    {
        public static IReadOnlyList<Stage> Stages { get; private set; } = Vanilla();

        /// <summary>Raised after the chain was replaced, so the server can republish.</summary>
        public static event Action Changed;

        public static List<Stage> Vanilla() => new List<Stage>
        {
            new Stage { Name = "Eikthyr", Key = "defeated_eikthyr", BossPrefab = "Eikthyr", Order = 1 },
            new Stage { Name = "TheElder", Key = "defeated_gdking", BossPrefab = "gd_king", Order = 2 },
            new Stage { Name = "Bonemass", Key = "defeated_bonemass", BossPrefab = "Bonemass", Order = 3 },
            new Stage { Name = "Moder", Key = "defeated_dragon", BossPrefab = "Dragon", Order = 4 },
            new Stage { Name = "Yagluth", Key = "defeated_goblinking", BossPrefab = "GoblinKing", Order = 5 },
            new Stage { Name = "TheQueen", Key = "defeated_queen", BossPrefab = "SeekerQueen", Order = 6 },
            new Stage { Name = "Fader", Key = "defeated_fader", BossPrefab = "Fader", Order = 7 },
        };

        public static void Set(IEnumerable<Stage> stages)
        {
            Stages = stages.OrderBy(s => s.Order).ToList();
            Lockstep.Log.LogInfo($"Progression chain: {string.Join(" > ", Stages.Select(s => s.Name))}");
            Changed?.Invoke();
        }

        public static Stage ByBoss(string prefabName) =>
            Stages.FirstOrDefault(s => string.Equals(s.BossPrefab, prefabName, StringComparison.OrdinalIgnoreCase));

        public static Stage ByKey(string key) =>
            Stages.FirstOrDefault(s => string.Equals(s.Key, key, StringComparison.OrdinalIgnoreCase));

        /// <summary>Finds a stage by display name, key or boss prefab, for console commands.</summary>
        public static Stage Find(string nameOrKey) =>
            Stages.FirstOrDefault(s => string.Equals(s.Name, nameOrKey, StringComparison.OrdinalIgnoreCase))
            ?? ByKey(nameOrKey) ?? ByBoss(nameOrKey);

        /// <summary>The stage before this one, or null for the first stage.</summary>
        public static Stage Previous(Stage stage)
        {
            int index = Stages.ToList().IndexOf(stage);
            return index > 0 ? Stages[index - 1] : null;
        }
    }
}
