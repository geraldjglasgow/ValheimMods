using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BepInEx;
using BundlePrefabs;
using EliteCreaturesPack.Core;
using UnityEngine;
using Object = UnityEngine.Object;

namespace EliteCreaturesPack.Custom.Humans
{
    /// <summary>
    /// The player's body made into a creature's. An inactive copy of the game's Player prefab keeps what makes the body:
    /// the rig, the animator with every player animation, the colliders, footsteps, the network sync of transform and
    /// animator, and VisEquipment, still flagged as a player's (<c>m_isPlayer</c>), which is what makes it draw the body
    /// model, hair, beard and the skin and hair colours, reading them from the ZDO on every peer as it does for players.
    /// The Player component gives way to a plain Humanoid carrying the same Character and Humanoid values, and the parts
    /// that assume a person at a keyboard go with it (<see cref="PlayerOnly"/>). With no Player component it is in none of
    /// the game's player lists (<c>Player.s_players</c>, the local player, ZNet's player list, which the map's pins come
    /// from) and leaves no tombstone: its death is the creature's (<see cref="HumanDeath"/>). Every world load makes its
    /// shells anew from the Player prefab as it is then, after every Awake, so the parts other mods put on that prefab are
    /// taken off too (<see cref="LeaveModParts"/>).
    /// </summary>
    internal static class HumanShell
    {
        public const string PlayerPrefab = "Player";

        /// <summary>
        /// The player-only parts: input (PlayerController), chat bubbles (Talker, whose RPC speaks for a player) and
        /// skill levels (Skills, which belong to a profile). The Player component goes once its values are copied.
        /// </summary>
        private static readonly Type[] PlayerOnly = { typeof(PlayerController), typeof(Talker), typeof(Skills) };

        /// <summary>
        /// A bare human's kit, so a definition that gives no gear still fights: the game's club and the rags. A human
        /// whose chain gives gear carries that instead (<see cref="Unkit"/>).
        /// </summary>
        private static readonly string[] Kit = { "Club", "ArmorRagsChest", "ArmorRagsLegs" };

        /// <summary>Mod parts already named in the log, so each is named once and not for every human of every world.</summary>
        private static readonly HashSet<string> toldParts = new HashSet<string>(StringComparer.Ordinal);

        /// <summary>An inactive copy of the Player prefab under the given name (the caller has checked the prefab exists).</summary>
        public static GameObject Copy(ZNetScene scene, string name) => PrefabBench.Copy(scene.GetPrefab(PlayerPrefab), name);

        /// <summary>Swaps the copy's Player for a Humanoid and makes it a saved, hostile creature; returns the Humanoid.</summary>
        public static Humanoid Convert(GameObject shell, ZNetScene scene)
        {
            LeaveModParts(shell);
            Humanoid human = Replace(shell);
            Persist(shell.GetComponent<ZNetView>());
            Hostile(human, scene);
            return human;
        }

        /// <summary>Takes the bare kit off the human's default items; anything else there stays.</summary>
        public static void Unkit(GameObject shell)
        {
            Humanoid? human = shell.GetComponent<Humanoid>();
            if (human != null && human.m_defaultItems != null)
            {
                human.m_defaultItems = human.m_defaultItems.Where(item => item != null && Array.IndexOf(Kit, item.name) < 0).ToArray();
            }
        }

        private static Humanoid Replace(GameObject shell)
        {
            Player player = shell.GetComponent<Player>();
            Humanoid human = shell.AddComponent<Humanoid>();
            HumanFieldCopy.Copy(typeof(Character), player, human);
            HumanFieldCopy.Copy(typeof(Humanoid), player, human);
            foreach (Type part in PlayerOnly)
            {
                Component? found = shell.GetComponent(part);
                if (found != null)
                {
                    Object.DestroyImmediate(found);
                }
            }
            Object.DestroyImmediate(player);
            Unplayer(shell.GetComponent<VisEquipment>());
            return human;
        }

        /// <summary>
        /// VisEquipment stays a player's (it draws the body, hair and colours) but answers to no Player component, and
        /// never picks a body model of its own as it wakes: the look's roll (<see cref="HumanAppearance"/>) decides it, and
        /// a pick on a later load would turn a woman into a man.
        /// </summary>
        private static void Unplayer(VisEquipment vis)
        {
            vis.m_playerComponent = null;
            vis.m_randomModelIndex = false;
        }

        /// <summary>
        /// A player's ZDO is not saved with the world (the profile keeps the player) and is sent before everything else;
        /// a creature's is saved and waits its turn, and is loaded near the players like any creature, never with the
        /// distant objects.
        /// </summary>
        private static void Persist(ZNetView nview)
        {
            nview.m_persistent = true;
            nview.m_type = ZDO.ObjectType.Default;
            nview.m_distant = false;
        }

        /// <summary>
        /// Takes off the parts other mods put on the player's prefab: each was written for a player and may take a human
        /// for one, or look for its Player every frame and fail. Only components of assemblies loaded from BepInEx's
        /// plugins folder go (never this mod's own); the game's and Unity's stay. Each is named once in the log.
        /// </summary>
        private static void LeaveModParts(GameObject shell)
        {
            foreach (MonoBehaviour part in shell.GetComponentsInChildren<MonoBehaviour>(true))
            {
                if (part == null || !FromMod(part.GetType().Assembly))
                {
                    continue;
                }
                string name = part.GetType().FullName;
                Object.DestroyImmediate(part);
                if (toldParts.Add(name))
                {
                    Log.Info($"Humans: '{name}', which another mod put on the player's prefab, is left off humans.");
                }
            }
        }

        private static bool FromMod(Assembly assembly)
        {
            if (assembly == typeof(HumanShell).Assembly || assembly.IsDynamic)
            {
                return false;
            }
            string location = assembly.Location;
            return !string.IsNullOrEmpty(location) && location.StartsWith(Paths.PluginPath, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Faction Boss: in the game's rules (BaseAI.IsEnemy) that faction is the enemy of players and what they bring
        /// (tames, summons) and of nothing else, and every monster faction leaves it alone. So a human hunts players without
        /// brawling with the greydwarfs or raid creatures around it; a definition can give it another faction. The player
        /// ignores the damage type meant for creatures only (NonPlayer); a human takes it like any creature.
        /// </summary>
        private static void Hostile(Humanoid human, ZNetScene scene)
        {
            human.m_faction = Character.Faction.Boss;
            human.m_damageModifiers.m_nonPlayer = HitData.DamageModifier.Normal;
            human.m_defaultItems = Kit.Select(item => scene.GetPrefab(item)).Where(item => item != null).ToArray();
        }
    }
}
