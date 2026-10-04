using System;
using System.Collections.Generic;
using DevBridge.Eval;
using DevBridge.Server;
using DevBridge.Stage;
using UnityEngine;

namespace DevBridge.Tune
{
    /// <summary>Turns prefab=, item= and field= into a target: a component of a prefab, or an item's shared data.</summary>
    internal static class TargetFinder
    {
        // field= is read from the item's shared data, so these leading parts of a full path are dropped
        private static readonly string[] SharedPrefixes = { "m_itemData.m_shared", "m_shared", "shared" };

        internal static TuneTarget Find(string prefab, string item, string field)
        {
            if (item != null) return Item(prefab == null ? null : GamePrefabs.Require(prefab), item, field);
            if (prefab != null) return Component(GamePrefabs.Require(prefab), field);
            throw new BridgeException("give prefab=<name>&field=<Component>.<member>, or item=<name>&field=<member> (with prefab= for a creature's attack)");
        }

        private static TuneTarget Item(GameObject creature, string name, string field)
        {
            GameObject item = creature ? ItemPrefabs.OfCreature(creature, name) : ItemPrefabs.Named(name);
            List<object> templates = ItemPrefabs.Templates(item);
            if (templates.Count == 0) throw new BridgeException($"{item.name} has no ItemDrop with shared data");
            return new TuneTarget
            {
                Identity = "item " + item.name,
                Label = creature ? creature.name + " / " + item.name : item.name,
                Variable = "shared",
                Path = TunePath.Parse(Unshared(field)),
                Templates = templates,
                Live = () => LiveItems.Copies(item),
            };
        }

        private static TuneTarget Component(GameObject prefab, string field)
        {
            if (field == null) throw new BridgeException("give field=<Component>.<member>, e.g. Character.m_runSpeed (field=Character&members=1 lists them)");
            int dot = field.IndexOf('.');
            Type type = PartType(prefab, dot < 0 ? field : field.Substring(0, dot));
            object template = LiveParts.Part(prefab, type) ?? throw new BridgeException($"{prefab.name} has no {type.Name}; it has {LiveParts.Names(prefab)}");
            return new TuneTarget
            {
                Identity = $"prefab {prefab.name} {type.Name}",
                Label = prefab.name + " / " + type.Name,
                Variable = char.ToLowerInvariant(type.Name[0]) + type.Name.Substring(1),
                Path = TunePath.Parse(dot < 0 ? "" : field.Substring(dot + 1)),
                Templates = new List<object> { template },
                Live = () => LiveParts.Instances(prefab, type),
            };
        }

        private static Type PartType(GameObject prefab, string name)
        {
            Type type = TypeIndex.Find(name.Trim());
            if (type != null && (type == typeof(GameObject) || typeof(Component).IsAssignableFrom(type))) return type;
            throw new BridgeException($"field= starts with the component, e.g. Character.m_runSpeed; {prefab.name} has {LiveParts.Names(prefab)}");
        }

        private static string Unshared(string field)
        {
            field = (field ?? "").Trim();
            foreach (string prefix in SharedPrefixes)
            {
                if (string.Equals(field, prefix, StringComparison.OrdinalIgnoreCase)) return "";
                if (field.StartsWith(prefix + ".", StringComparison.OrdinalIgnoreCase)) return field.Substring(prefix.Length + 1);
            }
            return field;
        }
    }
}
