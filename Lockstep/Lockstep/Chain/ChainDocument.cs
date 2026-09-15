using System.Collections.Generic;
using System.Linq;
using YamlConfig;

namespace Lockstep
{
    /// <summary>
    /// The chain YAML: every top level key is a stage name with <c>order</c>, <c>key</c> and <c>boss</c>.
    /// </summary>
    public sealed class ChainDocument : YamlModel
    {
        private readonly List<Stage> stages = new List<Stage>();

        protected override void Read(YamlNode root)
        {
            foreach (KeyValuePair<string, YamlNode> entry in root.Entries)
                ReadStage(entry.Key, entry.Value);
        }

        private void ReadStage(string name, YamlNode node)
        {
            node.Get("order").TryInt(out int order);
            if (!node.Get("key").TryString(out string key) || string.IsNullOrEmpty(key))
                node.Error("missing 'key', the global key the game sets when the boss dies");
            if (!node.Get("boss").TryString(out string boss) || string.IsNullOrEmpty(boss))
                node.Error("missing 'boss', the prefab name of the boss the altar spawns");
            stages.Add(new Stage { Name = name, Key = key, BossPrefab = boss, Order = order });
        }

        protected override void Verify()
        {
            foreach (IGrouping<int, Stage> group in stages.GroupBy(s => s.Order).Where(g => g.Count() > 1))
                Warnings.Add($"stages {string.Join(", ", group.Select(s => s.Name))} share order {group.Key}; their relative order is undefined");
            if (stages.Count == 0)
                Errors.Add("the chain contains no stages");
        }

        public List<Stage> Process() => stages;
    }
}
