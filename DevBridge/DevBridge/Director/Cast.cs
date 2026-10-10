using System;
using System.Collections.Generic;
using System.Linq;
using DevBridge.Server;
using UnityEngine;
using Object = UnityEngine.Object;

namespace DevBridge.Director
{
    /// <summary>One named actor: a creature (or any object) a shot spawned or adopted, and what the director tells it.</summary>
    internal sealed class Actor
    {
        internal readonly string Name;
        internal readonly GameObject Go;
        internal readonly Character Character;
        internal readonly BaseAI Ai;
        internal readonly bool Spawned;
        private readonly Dictionary<string, Transform> bones = new Dictionary<string, Transform>(StringComparer.OrdinalIgnoreCase);

        /// <summary>Under the director: its own AI does not run; it stands, faces Face, or walks to MoveTo.</summary>
        internal bool Held;
        internal Spot MoveTo;
        internal Spot Face;
        internal bool Run;
        internal float Arrive = 0.6f;
        /// <summary>Walks MoveTo in a straight line, not by the game's paths (which fail on a built floor).</summary>
        internal bool Straight;

        internal Actor(string name, GameObject go, bool spawned)
        {
            Name = name;
            Go = go;
            Spawned = spawned;
            Character = go.GetComponent<Character>();
            Ai = go.GetComponent<BaseAI>();
        }

        internal bool Alive => Go && (!Character || !Character.IsDead());

        private Transform ghost;

        /// <summary>
        /// Where the actor is, or, once it is gone (a projectile that hit, a creature removed), a still marker at the
        /// place and turn it last had, so camera keys on it hold rather than fail.
        /// </summary>
        internal void Forget()
        {
            if (ghost) Object.Destroy(ghost.gameObject);
        }

        internal Transform Root()
        {
            if (!ghost) ghost = new GameObject("DirectorGhost_" + Name).transform;
            if (!Go) return ghost;
            ghost.SetPositionAndRotation(Go.transform.position, Go.transform.rotation);
            return Go.transform;
        }

        /// <summary>
        /// A child transform by name (any depth), or by a path of names ("tentacle_4/kt_11": the part under the first),
        /// "eye" for the character's eye; null for the root.
        /// </summary>
        internal Transform Bone(string name)
        {
            Transform root = Root();
            if (!Go || string.IsNullOrEmpty(name)) return root;
            if (name.Equals("eye", StringComparison.OrdinalIgnoreCase) && Character && Character.m_eye) return Character.m_eye;
            if (bones.TryGetValue(name, out Transform found) && found) return found;
            found = Find(Go.transform, name) ?? throw new BridgeException($"actor {Name} has no part named {name}");
            bones[name] = found;
            return found;
        }

        private static Transform Find(Transform root, string path)
        {
            Transform at = root;
            foreach (string part in path.Split('/'))
                at = at ? at.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name.Equals(part, StringComparison.OrdinalIgnoreCase)) : null;
            return at;
        }

        internal Dictionary<string, object> Describe() => new Dictionary<string, object>
        {
            ["name"] = Name,
            ["prefab"] = Go ? Utils.GetPrefabName(Go) : null,
            ["alive"] = Alive,
            ["held"] = Held,
            ["position"] = Go ? Fmt.V3(Go.transform.position) : null,
            ["health"] = Character ? $"{Character.GetHealth():0}/{Character.GetMaxHealth():0}" : null,
        };
    }

    /// <summary>The named actors of the shots: spawned, adopted from the world, and "player" for the local player.</summary>
    internal static class Cast
    {
        private static readonly Dictionary<string, Actor> Actors = new Dictionary<string, Actor>(StringComparer.OrdinalIgnoreCase);
        private static readonly Dictionary<BaseAI, Actor> ByAi = new Dictionary<BaseAI, Actor>();

        /// <summary>The frame the held actors' move and face spots are read in.</summary>
        internal static ShotFrame Frame = ShotFrame.World;

        internal static Actor Spawn(string prefabName, string name, Vector3 at, Quaternion rotation, int level)
        {
            GameObject prefab = ZNetScene.instance ? ZNetScene.instance.GetPrefab(prefabName) : null;
            if (!prefab) throw new BridgeException($"no prefab {prefabName}");
            GameObject go = Object.Instantiate(prefab, at, rotation);
            Character character = go.GetComponent<Character>();
            if (character && level > 1) character.SetLevel(level);
            return Add(new Actor(name, go, true));
        }

        /// <summary>The nearest live object of a prefab within radius of a point, named for the shot.</summary>
        internal static Actor Adopt(string prefabName, string name, Vector3 near, float radius, bool newest = false)
        {
            GameObject best = null;
            float bestScore = float.MaxValue;
            foreach (ZNetView view in ZNetScene.instance.m_instances.Values)
            {
                if (!view || Utils.GetPrefabName(view.gameObject) != prefabName || IsActor(view.gameObject)) continue;
                float distance = Vector3.Distance(view.transform.position, near);
                if (distance > radius) continue;
                float score = newest ? -(view.GetZDO()?.m_uid.ID ?? 0) : distance;
                if (score < bestScore) (best, bestScore) = (view.gameObject, score);
            }
            if (!best) throw new BridgeException($"no {prefabName} within {radius} m");
            return Add(new Actor(name, best, false));
        }

        internal static Actor Register(Actor actor) => Add(actor);

        private static Actor Add(Actor actor)
        {
            if (actor.Name.Equals("player", StringComparison.OrdinalIgnoreCase)) throw new BridgeException("\"player\" is the local player's name");
            Remove(actor.Name, true);
            Actors[actor.Name] = actor;
            if (actor.Ai) ByAi[actor.Ai] = actor;
            return actor;
        }

        internal static Actor Get(string name)
        {
            if (name.Equals("player", StringComparison.OrdinalIgnoreCase)) return PlayerActor();
            return Actors.TryGetValue(name, out Actor actor) ? actor : throw new BridgeException($"no actor {name}; cast: {string.Join(", ", Actors.Keys)}");
        }

        private static Actor PlayerActor()
        {
            Player player = Player.m_localPlayer ? Player.m_localPlayer : throw new BridgeException("no local player");
            if (!Actors.TryGetValue("player", out Actor actor) || actor.Go != player.gameObject) Actors["player"] = actor = new Actor("player", player.gameObject, false);
            return actor;
        }

        internal static Transform Anchor(string name, string bone) => Get(name).Bone(bone);

        internal static bool IsActor(GameObject go) => Actors.Values.Any(a => a.Go == go);

        internal static Actor For(BaseAI ai) => ai && ByAi.TryGetValue(ai, out Actor actor) ? actor : null;

        /// <summary>Forgets an actor; one the director spawned is also removed from the world when destroy is set.</summary>
        internal static void Remove(string name, bool destroy)
        {
            if (!Actors.TryGetValue(name, out Actor actor)) return;
            Actors.Remove(name);
            if (actor.Ai) ByAi.Remove(actor.Ai);
            actor.Forget();
            if (destroy && actor.Spawned && actor.Go) Destroy(actor.Go);
        }

        internal static void Clear()
        {
            foreach (string name in Actors.Keys.ToList()) Remove(name, true);
        }

        internal static void Destroy(GameObject go)
        {
            ZNetView view = go.GetComponent<ZNetView>();
            if (view && view.IsValid() && ZNetScene.instance) ZNetScene.instance.Destroy(go);
            else Object.Destroy(go);
        }

        internal static List<Dictionary<string, object>> Describe() =>
            Actors.Values.Where(a => a.Name != "player").Select(a => a.Describe()).ToList();
    }
}
