using System;
using System.Reflection;
using BepInEx;
using BepInEx.Bootstrap;
using UnityEngine;

namespace Hearthhold
{
    /// <summary>
    /// GrindstoneSkills' public API (<c>GrindstoneSkills.Api.StarsApi</c> and <c>SkillsApi</c>, version 1 or later), found by
    /// reflection: the plugin by its GUID in the chainloader, then the types in its assembly; nothing of GrindstoneSkills is
    /// referenced. GrindstoneSkills owns item stars (quality, stacking apart, recipe counting, the bronze, silver and gold
    /// display) and the levels of its own skills; Hearthhold decides when an item gets stars and what they do. Bound once,
    /// on the first call. Without a usable API every star reads 0, nothing becomes a star item and every level is 0, so
    /// the game stays plain; the reason is logged once.
    /// </summary>
    public static class GrindstoneLink
    {
        public const string Guid = "com.GrindstoneSkills";
        private const string StarsType = "GrindstoneSkills.Api.StarsApi";
        private const string SkillsType = "GrindstoneSkills.Api.SkillsApi";
        private const int Required = 1;

        private static bool bound;
        private static bool ready;
        private static Action<GameObject> addStarItem;
        private static Func<ItemDrop.ItemData, bool> isStarItem;
        private static Func<ItemDrop.ItemData, int> getStars;
        private static Action<ItemDrop.ItemData, int> setStars;
        private static Func<int, string> starText;
        private static Func<string, float> localLevel;
        private static Func<Player, string, float> levelOf;

        public static bool Ready => Bind();

        public static void AddStarItem(GameObject prefab)
        {
            if (prefab != null && Bind())
                addStarItem(prefab);
        }

        public static bool IsStarItem(ItemDrop.ItemData item) => item != null && Bind() && isStarItem(item);

        public static int GetStars(ItemDrop.ItemData item) => item != null && item.m_quality > 1 && Bind() ? getStars(item) : 0;

        public static void SetStars(ItemDrop.ItemData item, int stars)
        {
            if (item != null && Bind())
                setStars(item, stars);
        }

        public static string StarText(int stars) => stars > 0 && Bind() ? starText(stars) : "";

        /// <summary>The local player's level in a skill by panel name ("Cooking", "Foraging"...), 0 to 100.</summary>
        public static float LocalLevel(string skill) => Bind() ? localLevel(skill) : 0f;

        /// <summary>Any player's level in GrindstoneSkills' own skills (Foraging, Husbandry); the game's only for the local player.</summary>
        public static float LevelOf(Player player, string skill) => player != null && Bind() ? levelOf(player, skill) : 0f;

        private static bool Bind()
        {
            if (bound)
                return ready;
            bound = true;
            Assembly assembly = FindAssembly();
            Type stars = Api(assembly, StarsType);
            Type skills = Api(assembly, SkillsType);
            if (stars != null && skills != null)
                BindAll(stars, skills);
            if (!ready)
                Hearthhold.Log.LogError("GrindstoneSkills 0.15.0 or later is needed: without its API nothing gets stars.");
            return ready;
        }

        private static void BindAll(Type stars, Type skills)
        {
            addStarItem = Create<Action<GameObject>>(stars, "AddStarItem");
            isStarItem = Create<Func<ItemDrop.ItemData, bool>>(stars, "IsStarItem");
            getStars = Create<Func<ItemDrop.ItemData, int>>(stars, "GetStars");
            setStars = Create<Action<ItemDrop.ItemData, int>>(stars, "SetStars");
            starText = Create<Func<int, string>>(stars, "GetStarText");
            localLevel = Create<Func<string, float>>(skills, "GetLocalLevel");
            levelOf = Create<Func<Player, string, float>>(skills, "GetLevel");
            ready = addStarItem != null && isStarItem != null && getStars != null && setStars != null && starText != null
                && localLevel != null && levelOf != null;
        }

        private static Assembly FindAssembly() =>
            Chainloader.PluginInfos.TryGetValue(Guid, out PluginInfo info) && info.Instance != null ? info.Instance.GetType().Assembly : null;

        /// <summary>An API type of the installed GrindstoneSkills whose version is at least <see cref="Required"/>, else null.</summary>
        private static Type Api(Assembly assembly, string name)
        {
            Type api = assembly?.GetType(name);
            Func<int> version = api != null ? Create<Func<int>>(api, "GetApiVersion") : null;
            return version != null && version() >= Required ? api : null;
        }

        /// <summary>A typed delegate to a public static method of that name and shape, or null.</summary>
        private static T Create<T>(Type api, string name) where T : Delegate
        {
            MethodInfo invoke = typeof(T).GetMethod("Invoke");
            Type[] parameters = Array.ConvertAll(invoke.GetParameters(), p => p.ParameterType);
            MethodInfo method = api.GetMethod(name, BindingFlags.Public | BindingFlags.Static, null, parameters, null);
            return method != null && method.ReturnType == invoke.ReturnType ? (T)Delegate.CreateDelegate(typeof(T), method) : null;
        }
    }
}
