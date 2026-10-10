using DevBridge.Server;
using DevBridge.Ui;
using DevBridge.World;
using UnityEngine;

namespace DevBridge.Eval
{
    /// <summary>The $names an expression can start from.</summary>
    internal static class Variables
    {
        internal const string Known = "$player $last $hover $go(\"path\") $v3(x,y,z) $prefab(\"name\") $type(\"name\") $zdo(\"id\") $cast(\"actor\")";

        internal static object Get(string name, object[] args)
        {
            switch (name.ToLowerInvariant())
            {
                case "player": return Player.m_localPlayer;
                case "last": return Evaluator.Last;
                case "hover": return Player.m_localPlayer ? Player.m_localPlayer.GetHoverObject() : null;
                case "go": return ScenePaths.Require(Text(args, name));
                case "v3": return new Vector3(Number(args, 0, name), Number(args, 1, name), Number(args, 2, name));
                case "prefab": return Prefab(Text(args, name));
                case "type": return new StaticRef(TypeIndex.Find(Text(args, name)) ?? throw new BridgeException($"no type {args[0]}"));
                case "zdo": return ZdoLookup.ById(Text(args, name));
                case "cast": return Director.Cast.Get(Text(args, name)).Go;
                default: throw new BridgeException($"unknown variable ${name}; known: {Known}");
            }
        }

        private static GameObject Prefab(string prefab)
        {
            ZNetScene scene = ZNetScene.instance;
            GameObject found = scene ? scene.GetPrefab(prefab) : null;
            return found ? found : throw new BridgeException($"no prefab {prefab} (ZNetScene loads with the world)");
        }

        private static string Text(object[] args, string name) =>
            args != null && args.Length == 1 && args[0] is string text ? text : throw new BridgeException($"${name} takes one string: ${name}(\"...\")");

        private static float Number(object[] args, int index, string name) =>
            args != null && args.Length == 3 ? (float)Coerce.To(args[index], typeof(float)) : throw new BridgeException($"${name} takes three numbers");
    }
}
