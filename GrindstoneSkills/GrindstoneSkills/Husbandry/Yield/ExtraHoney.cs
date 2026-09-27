using HarmonyLib;
using UnityEngine;

namespace GrindstoneSkills
{
    /// <summary>
    /// Extra honey and honey experience, on the harvester's own client (where their skill lives). Beehive.Interact runs
    /// there: when the player is not holding the key, the ward allows it and the hive has honey, it calls Extract, which
    /// sends RPC_Extract to the hive's owner; the owner spawns one honey per level (its stack scaled by the world's
    /// resource rate) and empties the hive. A prefix opens a harvest for the local player's interaction; a prefix on
    /// Extract, which the game calls only after its own checks, notes the honey count the game is taking; a postfix
    /// then raises Husbandry by "Honey Experience" per honey and rolls, per honey, the player's share of "Extra Honey
    /// At 100" for one more. Extra honey is spawned here as RPC_Extract spawns it: a networked instance at the hive's
    /// spawn point, stacked above the game's own, so every client sees it. Settings that change how fast a hive fills
    /// (OpenKeep's) only change the count the game takes, so the two add up.
    /// </summary>
    public static class ExtraHoney
    {
        private const float Spread = 0.5f;
        private const float StackStep = 0.25f;

        private static bool harvesting;
        private static int harvested;

        [HarmonyPatch(typeof(Beehive), nameof(Beehive.Interact))]
        private static class Harvest
        {
            [HarmonyPrefix]
            private static void Prefix(Humanoid character, bool repeat)
            {
                harvested = 0;
                harvesting = !repeat && HusbandrySkill.Active && character != null && character == Player.m_localPlayer;
            }

            [HarmonyPostfix]
            private static void Postfix(Beehive __instance, Humanoid character)
            {
                int count = harvesting ? harvested : 0;
                if (count > 0 && character is Player player)
                    HookGuard.Run("extra honey", () => Reward(__instance, player, count));
            }

            [HarmonyFinalizer]
            private static void Finalizer()
            {
                harvesting = false;
                harvested = 0;
            }
        }

        [HarmonyPatch(typeof(Beehive), nameof(Beehive.Extract))]
        private static class Extracting
        {
            [HarmonyPrefix]
            private static void Prefix(Beehive __instance)
            {
                if (harvesting)
                    harvested = __instance.GetHoneyLevel();
            }
        }

        private static void Reward(Beehive hive, Player player, int count)
        {
            HusbandryXp.Raise(player, Mathf.Max(0f, HusbandryExperienceSettings.Honey.Value) * count);
            float share = HusbandrySkill.Share(HusbandryYieldSettings.ExtraHoney.Value, HusbandrySkill.Of(player));
            int extra = 0;
            for (int i = 0; i < count; i++)
            {
                if (Random.value < share)
                    extra++;
            }
            for (int i = 0; i < extra; i++)
                Spawn(hive, count + i);
        }

        private static void Spawn(Beehive hive, int index)
        {
            if (hive.m_honeyItem == null || hive.m_spawnPoint == null)
                return;
            Vector2 circle = Random.insideUnitCircle * Spread;
            Vector3 position = hive.m_spawnPoint.position + new Vector3(circle.x, StackStep * index, circle.y);
            ItemDrop honey = Object.Instantiate(hive.m_honeyItem, position, Quaternion.identity);
            if (honey != null)
                honey.SetStack(Game.instance.ScaleDrops(hive.m_honeyItem.m_itemData, 1));
        }
    }
}
