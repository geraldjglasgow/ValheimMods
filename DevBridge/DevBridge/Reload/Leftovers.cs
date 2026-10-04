using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using BepInEx;
using BepInEx.Bootstrap;
using UnityEngine;

namespace DevBridge.Reload
{
    /// <summary>
    /// What a teardown cannot take back and only reports: the game's tables still holding the old copy's prefabs and
    /// status effects, status effects of its types running on characters, and its ScriptableObjects. They go with the
    /// world (ZNetScene and ObjectDB are rebuilt when a world loads) or with the session.
    /// </summary>
    internal static class Leftovers
    {
        private static readonly Regex ReloadedSuffix = new Regex(@"-reload-\d+$");

        internal static void Run(Assembly old, HashSet<GameObject> prefabParts, Report report)
        {
            if (ZNetScene.instance)
            {
                int prefabs = ZNetScene.instance.m_prefabs.Count(prefabParts.Contains);
                if (prefabs > 0) report.Leave($"{prefabs} ZNetScene prefabs carry the old copy's components (until the world loads again)");
            }
            if (ObjectDB.instance)
            {
                int items = ObjectDB.instance.m_items.Count(prefabParts.Contains);
                int effects = ObjectDB.instance.m_StatusEffects.Count(e => e && e.GetType().Assembly == old);
                if (items + effects > 0) report.Leave($"ObjectDB holds {items} items and {effects} status effects of the old copy (until the world loads again)");
            }
            int running = Character.GetAllCharacters().Where(c => c && c.GetSEMan() != null)
                .Sum(c => c.GetSEMan().GetStatusEffects().Count(e => e && e.GetType().Assembly == old));
            if (running > 0) report.Leave($"{running} status effects of the old copy's types run on characters until they end");
            List<string> assets = Resources.FindObjectsOfTypeAll<ScriptableObject>().Where(o => o && o.GetType().Assembly == old).Select(o => o.GetType().Name).ToList();
            if (assets.Count > 0) report.Leave($"{assets.Count} ScriptableObjects of the old copy's types stay loaded: {Report.Tally(assets)}");
        }

        /// <summary>
        /// Other plugins that depend on the reloaded ones or were compiled against their assembly, which always binds to
        /// the first copy's name (without -reload-N).
        /// </summary>
        internal static void Dependents(ReloadTarget target, Report report)
        {
            HashSet<string> guids = new HashSet<string>(target.Plugins.Select(p => p.Metadata.GUID));
            string name = ReloadedSuffix.Replace(target.Old.GetName().Name, "");
            IEnumerable<PluginInfo> others = Chainloader.PluginInfos.Values.Where(p => !ReferenceEquals(p.Instance, null) && p.Instance.GetType().Assembly != target.Old);
            foreach (PluginInfo other in others)
            {
                bool declared = other.Dependencies.Any(d => guids.Contains(d.DependencyGUID));
                bool compiled = other.Instance.GetType().Assembly.GetReferencedAssemblies().Any(r => r.Name == name);
                if (compiled) report.Leave($"{other.Metadata.Name} was compiled against {name} and keeps calling the old copy");
                else if (declared) report.Leave($"{other.Metadata.Name} depends on it: what it took from the old copy at its start (entries, objects) stays");
            }
        }
    }
}
