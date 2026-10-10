using System;
using System.Linq;
using HarmonyLib;
using DevBridge.Server;
using Newtonsoft.Json.Linq;
using UnityEngine;
using Object = UnityEngine.Object;

namespace DevBridge.Director
{
    /// <summary>
    /// Extras: more Vikings for a scene, made from the game's own Player prefab with its input controller taken off, so
    /// they look, move, block and swing like players but answer only to the director (the "act" cue). God mode keeps them
    /// standing (health never below 1) while hits still stagger and push them.
    /// {"extra": "Bjorn", "at": [x,y,z], "face": spot, "look": {"model": 0, "hair": "Hair5", "beard": "Beard3",
    ///  "skin": [r,g,b], "hairColor": [r,g,b]}, "equip": ["ArmorIronChest", "SwordIron", ...]}
    /// </summary>
    internal static class Extras
    {
        internal static Actor Spawn(JObject s, ShotFrame frame)
        {
            string name = s.Value<string>("extra");
            Vector3 at = Spot.Parse(s["at"], "extra at")?.Resolve(frame) ?? throw new BridgeException("extra needs at=");
            GameObject prefab = ZNetScene.instance.GetPrefab("Player") ?? throw new BridgeException("no Player prefab");
            GameObject go = Object.Instantiate(prefab, at, Cues.Facing(s, frame, at));
            PlayerController controller = go.GetComponent<PlayerController>();
            if (controller) Object.DestroyImmediate(controller);
            Player player = go.GetComponent<Player>();
            player.SetGodMode(true);
            player.m_lookYaw = Quaternion.Euler(0f, go.transform.eulerAngles.y, 0f);
            Dress(player, s["look"] as JObject);
            go.AddComponent<ExtraPilot>();
            if (s["equip"] is JArray gear) Dresser.Put(player, gear.Select(item => item.Value<string>()));
            return Cast.Register(new Actor(name, go, true));
        }

        private static void Dress(Player player, JObject look)
        {
            VisEquipment vis = player.GetComponent<VisEquipment>();
            if (!vis || look == null) return;
            vis.SetModel(look.Value<int?>("model") ?? 0);
            if (look["hair"] != null) vis.SetHairItem(look.Value<string>("hair").GetStableHashCode());
            if (look["beard"] != null) vis.SetBeardItem(look.Value<string>("beard").GetStableHashCode());
            if (look["skin"] is JArray skin) vis.SetSkinColor(Vector(skin));
            if (look["hairColor"] is JArray hair) vis.SetHairColor(Vector(hair));
        }

        private static Vector3 Vector(JArray c) => new Vector3(c[0].Value<float>(), c[1].Value<float>(), c[2].Value<float>());

        internal static void Equip(Humanoid who, string prefab)
        {
            Inventory inventory = who.GetInventory();
            ItemDrop.ItemData item = inventory.GetAllItems().FirstOrDefault(i => i.m_dropPrefab && i.m_dropPrefab.name == prefab)
                ?? inventory.AddItem(prefab, 1, 4, 0, 0L, "", false)
                ?? throw new BridgeException($"could not add {prefab} (no such item, or the inventory is full)");
            if (!who.IsItemEquiped(item)) who.EquipItem(item, false);
        }

        /// <summary>The pilot of an actor that is a human: the local player's (which takes over its controls) or an extra's.</summary>
        internal static Pilot PilotOf(Actor actor)
        {
            if (actor.Go == (Player.m_localPlayer ? Player.m_localPlayer.gameObject : null))
            {
                Puppet.Set(new JObject());
                return Puppet.Local;
            }
            ExtraPilot pilot = actor.Go.GetComponent<ExtraPilot>();
            return pilot ? pilot.Pilot : throw new BridgeException($"actor {actor.Name} is not a human the director drives");
        }
    }

    /// <summary>On an extra: its controls each physics step and its look each frame, from its pilot.</summary>
    internal sealed class ExtraPilot : MonoBehaviour
    {
        internal readonly Pilot Pilot = new Pilot();
        private Player player;

        private void Awake() => player = GetComponent<Player>();

        private void FixedUpdate()
        {
            if (player && player.m_nview.IsValid() && player.m_nview.IsOwner()) Pilot.Control(player);
        }

        private void LateUpdate()
        {
            if (player) Pilot.Aim(player);
        }
    }

    /// <summary>
    /// The game runs a player's own physics update only for the local player (it destroys any other player this machine
    /// owns as an "old local player"), and mods hook it for the local player's inventory and slots. An extra runs only the
    /// parts a fighter needs, as itself: queued actions, attack input, crouch and dodge, with stamina kept full.
    /// </summary>
    [HarmonyPatch(typeof(Player), "FixedUpdate")]
    internal static class ExtraFixedUpdatePatch
    {
        internal static bool IsExtra(Player player) => player && player != Player.m_localPlayer && player.GetComponent<ExtraPilot>();

        private static bool Prefix(Player __instance)
        {
            if (!IsExtra(__instance)) return true;
            if (__instance.m_nview.GetZDO() == null || !__instance.m_nview.IsOwner() || __instance.IsDead()) return false;
            float dt = Time.fixedDeltaTime;
            __instance.m_stamina = __instance.GetMaxStamina();
            __instance.UpdateActionQueue(dt);
            __instance.PlayerAttackInput(dt);
            __instance.UpdateCrouch(dt);
            __instance.UpdateDodge(dt);
            return false;
        }
    }

    [HarmonyPatch(typeof(Player), "Update")]
    internal static class ExtraUpdatePatch
    {
        private static bool Prefix(Player __instance) => !ExtraFixedUpdatePatch.IsExtra(__instance);
    }

    [HarmonyPatch(typeof(Player), "LateUpdate")]
    internal static class ExtraLateUpdatePatch
    {
        private static bool Prefix(Player __instance)
        {
            if (!ExtraFixedUpdatePatch.IsExtra(__instance)) return true;
            if (__instance.m_nview.IsValid()) __instance.UpdateEmote();
            return false;
        }
    }

    /// <summary>
    /// Gear kept on: a character just spawned, falling, swimming or in an attack refuses an item, and the sea or a mod can
    /// take one off (another mod's equipment watcher strips an extra mid-fight), so every physics step it puts back,
    /// silently, whatever of its gear is not on (each item on its own, so one item's trouble cannot stop the rest), for as
    /// long as it lives: a stripped piece is back before the next frame. An item still off after six seconds is reported once.
    /// </summary>
    internal sealed class Dresser : MonoBehaviour
    {
        private readonly System.Collections.Generic.List<string> gear = new System.Collections.Generic.List<string>();
        private Humanoid who;
        private float age, next;
        private bool reported;

        internal static void Put(Humanoid who, System.Collections.Generic.IEnumerable<string> items)
        {
            Dresser dresser = who.GetComponent<Dresser>() ?? who.gameObject.AddComponent<Dresser>();
            (dresser.who, dresser.age, dresser.next, dresser.reported) = (who, 0f, 0f, false);
            dresser.gear.Clear();
            dresser.gear.AddRange(items);
        }

        /// <summary>At the end of a shot: nobody's gear is kept on any more (the player's would fight the next shot's).</summary>
        internal static void StopAll()
        {
            foreach (Dresser dresser in FindObjectsByType<Dresser>(FindObjectsSortMode.None)) Destroy(dresser);
        }

        private void FixedUpdate()
        {
            if (!who) { Destroy(this); return; }
            age += Time.fixedDeltaTime;
            if (age < next) return;
            next = age;
            var off = gear.Where(item => !TryOn(item)).ToList();
            if (off.Count == 0 || age < 6f || reported) return;
            reported = true;
            ShotRunner.Report($"{who.name}: will not put on {string.Join(", ", off)}");
        }

        private bool TryOn(string prefab)
        {
            try
            {
                if (Worn(prefab)) return true;
                Extras.Equip(who, prefab);
                return Worn(prefab);
            }
            catch (Exception)
            {
                return false;
            }
        }

        private bool Worn(string prefab) =>
            who.GetInventory().GetAllItems().Any(item => item.m_dropPrefab && item.m_dropPrefab.name == prefab && who.IsItemEquiped(item));
    }
}
