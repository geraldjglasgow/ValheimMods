using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;

namespace EarthWright.Core
{
    /// <summary>
    /// Ordered callbacks for the moments the game's databases are ready. Lower order runs first. Every callback runs
    /// again whenever the database is rebuilt (the main menu and the world each have their own ObjectDB). A callback that
    /// throws is logged and skipped, never passed on: the game's own database wake-up must not break.
    /// <list type="bullet">
    /// <item><see cref="OnObjectDb"/>: after <c>ObjectDB.Awake</c> and <c>ObjectDB.CopyOtherDB</c>, once the items exist.
    /// Menu pieces register at order 100, tools (the shovel) at 200, anything that reads tool tables at 300 or later.</item>
    /// <item><see cref="OnScene"/>: after <c>ZNetScene.Awake</c>, when every networked prefab is registered.</item>
    /// </list>
    /// </summary>
    public static class GameReady
    {
        private sealed class Entry
        {
            public int Order;
            public string Name;
            public Action<ObjectDB> Run;
        }

        private static readonly List<Entry> objectDb = new List<Entry>();
        private static readonly List<(int Order, string Name, Action<ZNetScene> Run)> scene = new List<(int, string, Action<ZNetScene>)>();

        public static void OnObjectDb(int order, string name, Action<ObjectDB> run) => objectDb.Add(new Entry { Order = order, Name = name, Run = run });

        public static void OnScene(int order, string name, Action<ZNetScene> run) => scene.Add((order, name, run));

        internal static void RaiseObjectDb(ObjectDB db)
        {
            if (db == null || db.m_items == null || db.m_items.Count == 0)
                return;
            foreach (Entry entry in objectDb.OrderBy(e => e.Order))
                Safe.Run(entry.Name, () => entry.Run(db));
        }

        internal static void RaiseScene(ZNetScene zNetScene)
        {
            foreach (var entry in scene.OrderBy(e => e.Order))
                Safe.Run(entry.Name, () => entry.Run(zNetScene));
        }
    }

    [HarmonyPatch(typeof(ObjectDB), nameof(ObjectDB.Awake))]
    public static class ObjectDbAwakePatch
    {
        [HarmonyPostfix]
        [HarmonyPriority(Priority.Low)]
        public static void Postfix(ObjectDB __instance) => GameReady.RaiseObjectDb(__instance);
    }

    [HarmonyPatch(typeof(ObjectDB), nameof(ObjectDB.CopyOtherDB))]
    public static class ObjectDbCopyPatch
    {
        [HarmonyPostfix]
        [HarmonyPriority(Priority.Low)]
        public static void Postfix(ObjectDB __instance) => GameReady.RaiseObjectDb(__instance);
    }

    [HarmonyPatch(typeof(ZNetScene), nameof(ZNetScene.Awake))]
    public static class ZNetSceneAwakePatch
    {
        [HarmonyPostfix]
        [HarmonyPriority(Priority.Low)]
        public static void Postfix(ZNetScene __instance) => GameReady.RaiseScene(__instance);
    }
}
