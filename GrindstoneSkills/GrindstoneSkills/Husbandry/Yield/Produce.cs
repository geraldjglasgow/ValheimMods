using HarmonyLib;
using UnityEngine;

namespace GrindstoneSkills
{
    /// <summary>
    /// Produce: a fed tamed animal now and then drops one of its own materials without being killed
    /// (<see cref="ProduceTables"/>), on the creature's ZDO owner. The game calls Tameable.TamingUpdate every 3 s on
    /// every machine that has the creature; a postfix acts only on the owner, for a tamed creature that is not hungry
    /// and whose kind produces something, while Husbandry is on and "Produce Chance At 100" is above 0. The creature's
    /// ZDO keeps the world time of its last roll (<see cref="Keys.ProduceLast"/>): the first tick only starts the clock
    /// (so taming an animal never pays out at once), and once "Produce Interval" has passed, a tick with a keeper in
    /// range restarts it and rolls the best keeper's share (<see cref="Keeper"/>, within Keeper Range) of the chance. A
    /// due roll waits while no one with a level is in range, so an area loading while its keeper is still 60 m away
    /// does not waste it; it never stacks up more than one roll. A clock that lies ahead of world time (a world moved
    /// back) restarts. A success drops one item beside the animal, the way the game spawns drops: a networked instance
    /// owned by this machine, so every client sees it, with the world's resource rate applied to its stack as beehives
    /// do.
    /// </summary>
    public static class Produce
    {
        private const float Gap = 0.3f;
        private const float DropHeight = 0.5f;

        private static readonly int LastHash = Keys.ProduceLast.GetStableHashCode();

        [HarmonyPatch(typeof(Tameable), nameof(Tameable.TamingUpdate))]
        private static class Tick
        {
            [HarmonyPostfix]
            private static void Postfix(Tameable __instance)
            {
                if (HusbandrySkill.Active && HusbandryYieldSettings.ProduceChance.Value > 0f && IsFedOwnedTame(__instance))
                    HookGuard.Run("produce", () => Advance(__instance));
            }
        }

        private static bool IsFedOwnedTame(Tameable tameable) =>
            tameable.m_nview != null && tameable.m_nview.IsValid() && tameable.m_nview.IsOwner()
            && tameable.m_character != null && tameable.m_character.IsTamed() && !tameable.IsHungry();

        private static void Advance(Tameable tameable)
        {
            ProduceTable table = ProduceTables.For(tameable.m_character);
            if (table.IsEmpty)
                return;
            ZDO zdo = tameable.m_nview.GetZDO();
            double since = SinceLast(zdo);
            if (since < 0.0)
            {
                zdo.Set(LastHash, Herd.Now.Ticks);
                return;
            }
            if (since < HusbandryYieldSettings.ProduceInterval.Value)
                return;
            float chance = Keeper.Share(HusbandryYieldSettings.ProduceChance.Value, tameable.transform.position);
            if (chance <= 0f)
                return;
            zdo.Set(LastHash, Herd.Now.Ticks);
            if (Random.value < chance)
                Spawn(table.Pick(), tameable.m_character);
        }

        /// <summary>Seconds since the last roll; below 0 when the clock is not started or lies ahead of world time.</summary>
        private static double SinceLast(ZDO zdo)
        {
            long last = zdo.GetLong(LastHash);
            return last == 0L ? -1.0 : Herd.SecondsSince(last);
        }

        private static void Spawn(GameObject prefab, Character animal)
        {
            if (prefab == null)
                return;
            Vector2 side = Random.insideUnitCircle.normalized * (animal.GetRadius() + Gap);
            Vector3 position = animal.transform.position + new Vector3(side.x, DropHeight, side.y);
            GameObject spawned = Object.Instantiate(prefab, position, Quaternion.Euler(0f, Random.Range(0f, 360f), 0f));
            ItemDrop item = spawned.GetComponent<ItemDrop>();
            if (item == null)
                return;
            ItemDrop.OnCreateNew(item);
            item.SetStack(Game.instance.ScaleDrops(item.m_itemData, 1));
        }
    }
}
