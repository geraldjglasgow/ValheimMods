using System;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace EliteEquipment.Casters
{
    /// <summary>
    /// Makes the caster items once per process, on every peer (dedicated server included), the first time the scene or the
    /// item database wakes with the game's club in its list, and the wand's bolt, the Rootbound Wand's roots
    /// (<see cref="WandRoots"/>), the Frostsilver Wand's ice shards (<see cref="IceShards"/>), the Greywood Wand's rock
    /// (<see cref="WandRock"/>), the Ashwood Wand's firebolt (<see cref="Firebolt"/>) and Elder's Volley's seeds
    /// (<see cref="VolleySeeds"/>) and Moder's Breath's breath (<see cref="BreathLook"/>) the first time the scene
    /// does (the shards and the root they copy are scene prefabs, not items); then adds them to every scene's and database's list before the game
    /// indexes it (<c>ZNetScene.Awake</c>, <c>ObjectDB.Awake</c>, <c>ObjectDB.CopyOtherDB</c>), with the casters' status
    /// effects (<see cref="CasterStatus"/>). They exist whether the switch is on or not, so items already made always load.
    /// A piece that cannot be made is logged and left out; nothing ever throws out of an Awake (a throw there stops the
    /// world loading). After the scene wakes, the bolt gets its look and the bosses their page drops (<see cref="BossPages"/>).
    /// </summary>
    public static class CasterItems
    {
        private const string Probe = "Club";

        private static bool itemsBuilt;
        private static bool boltBuilt;
        private static GameObject bolt;
        private static readonly List<GameObject> spellItems = new List<GameObject>();

        [HarmonyPatch(typeof(ZNetScene), nameof(ZNetScene.Awake))]
        private static class ScenePatch
        {
            [HarmonyPrefix]
            [HarmonyPriority(Priority.High)]
            private static void Prefix(ZNetScene __instance) => Prepare(__instance.m_prefabs, scene: true);

            [HarmonyPostfix]
            private static void Postfix(ZNetScene __instance) => Safe("scene", () =>
            {
                WandBolt.Dress(bolt);
                BossPages.Refresh(__instance);
            });
        }

        [HarmonyPatch(typeof(ObjectDB), nameof(ObjectDB.Awake))]
        private static class DatabasePatch
        {
            [HarmonyPrefix]
            [HarmonyPriority(Priority.High)]
            private static void Prefix(ObjectDB __instance)
            {
                Prepare(__instance.m_items, scene: false);
                Safe("status effects", () => CasterStatus.Prepare(__instance.m_StatusEffects));
            }
        }

        [HarmonyPatch(typeof(ObjectDB), nameof(ObjectDB.CopyOtherDB))]
        private static class CopyPatch
        {
            [HarmonyPrefix]
            [HarmonyPriority(Priority.High)]
            private static void Prefix(ObjectDB other)
            {
                Prepare(other.m_items, scene: false);
                Safe("status effects", () => CasterStatus.Prepare(other.m_StatusEffects));
            }
        }

        /// <summary>The spell pages made (to drop, to recognise in the inventory).</summary>
        public static IReadOnlyList<GameObject> MadeSpellItems => spellItems;

        private static void Prepare(List<GameObject> prefabs, bool scene) => Safe("items", () =>
        {
            if (prefabs == null)
                return;
            CasterBoots.Register();
            BuildItems(prefabs);
            if (scene)
                BuildBolt(prefabs);
            foreach (GameObject prefab in Made(scene))
            {
                if (!prefabs.Contains(prefab))
                    prefabs.Add(prefab);
            }
        });

        private static void BuildItems(List<GameObject> prefabs)
        {
            // The main menu's database wakes with an empty list before it copies the real one in: wait for the club.
            if (itemsBuilt || Find(prefabs, Probe) == null)
                return;
            itemsBuilt = true;
            foreach (GearPiece wand in CasterGear.Wands)
                Make(wand, () => WandItem.Build(wand, Need(prefabs, wand.Base), Need(prefabs, "StaffFireball")));
            foreach (GearPiece book in CasterGear.Books)
                Make(book, () => BookItem.Build(book, Need(prefabs, book.Base)));
            foreach (GearPiece piece in CasterGear.Robes)
                Make(piece, () => RobeItems.Build(piece, Need(prefabs, piece.Base), Need(prefabs, "HelmetMage")));
            foreach (FoodPiece food in CasterFood.All)
                Safe(food.Prefab, () => food.Item = FoodItems.Build(food, Need(prefabs, food.Base)));
            foreach (Spell spell in Spells.All)
                Safe(spell.Item, () => spellItems.Add(SpellItems.Build(spell, Need(prefabs, "Raspberry"))));
            ArmWands();
        }

        private static void ArmWands()
        {
            foreach (GearPiece wand in CasterGear.Wands)
                WandItem.Arm(wand.Item, Fired(wand));
        }

        /// <summary>
        /// What a wand's left mouse fires: the Rootbound's roots, the Frostsilver's ice shards, the Greywood's rock, the
        /// Ashwood's firebolt, else the
        /// bolt (also when that wand's own could not be made).
        /// </summary>
        private static GameObject Fired(GearPiece wand)
        {
            if (WandRoots.Raises(wand) && WandRoots.Cast != null)
                return WandRoots.Cast;
            if (WandRock.Fires(wand) && WandRock.Rock != null)
                return WandRock.Rock;
            if (Firebolt.Fires(wand) && Firebolt.Bolt != null)
                return Firebolt.Bolt;
            return IceShards.Fires(wand) && IceShards.Shard != null ? IceShards.Shard : bolt;
        }

        private static void BuildBolt(List<GameObject> prefabs)
        {
            if (boltBuilt || Find(prefabs, WandBolt.GameName) == null)
                return;
            boltBuilt = true;
            Safe(WandBolt.Prefab, () => bolt = WandBolt.Build(Need(prefabs, WandBolt.GameName)));
            Safe(WandRoots.RootPrefab, () => WandRoots.Build(Need(prefabs, WandRoots.GameRoot), Need(prefabs, WandRoots.GameElder)));
            Safe(IceShards.Prefab, () => IceShards.Build(Need(prefabs, IceShards.GameShard)));
            Safe(VolleySeeds.Prefab, () => VolleySeeds.Build(Need(prefabs, VolleySeeds.GameSeed)));
            Safe(BreathLook.Prefab, () => BreathLook.Build(Need(prefabs, BreathLook.GameBreath)));
            Safe(WandRock.Prefab, () => WandRock.Build(Need(prefabs, WandRock.GameStone), Find(prefabs, WandRock.GameChips),
                Find(prefabs, WandRock.GameCrack)));
            Safe(Firebolt.Prefab, () => Firebolt.Build(Need(prefabs, Firebolt.GameBolt), Find(prefabs, Firebolt.GameSpark),
                Find(prefabs, Firebolt.GameBang)));
            ArmWands();
        }

        private static IEnumerable<GameObject> Made(bool scene)
        {
            foreach (GearPiece piece in CasterGear.All)
            {
                if (piece.Item != null)
                    yield return piece.Item;
            }
            foreach (GameObject item in spellItems)
                yield return item;
            foreach (FoodPiece food in CasterFood.All)
            {
                if (food.Item != null)
                    yield return food.Item;
            }
            if (!scene)
                yield break;
            foreach (GameObject shot in Shots())
            {
                if (shot != null)
                    yield return shot;
            }
        }

        /// <summary>The scene prefabs the wands and spells fire or spawn (those made).</summary>
        private static GameObject[] Shots() => new[]
        {
            bolt, WandRoots.Root, WandRoots.Rise, IceShards.Shard, WandRock.Rock, Firebolt.Bolt, VolleySeeds.Seed,
            BreathLook.Breath,
        };

        private static void Make(GearPiece piece, Func<GameObject> build) => Safe(piece.Prefab, () => piece.Item = build());

        private static GameObject Need(List<GameObject> prefabs, string name) =>
            Find(prefabs, name) ?? throw new InvalidOperationException($"the game has no {name}");

        private static GameObject Find(List<GameObject> prefabs, string name) => prefabs.Find(p => p != null && p.name == name);

        private static void Safe(string what, Action action)
        {
            try
            {
                action();
            }
            catch (Exception e)
            {
                Plugin.Log.LogError($"EliteEquipment: casters, {what} could not be made: {e}");
            }
        }
    }
}
