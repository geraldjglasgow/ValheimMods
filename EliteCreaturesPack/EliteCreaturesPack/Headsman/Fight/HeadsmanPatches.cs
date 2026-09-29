using System.Collections.Generic;
using EliteCreaturesPack.Core;
using HarmonyLib;

namespace EliteCreaturesPack.Headsman
{
    /// <summary>
    /// The game's AI steps aside, on the creature's owner: for the Executioner while its rear strike turns it (else it
    /// would turn it back towards its target, <see cref="HeadsmanBrain"/>), and for a skeleton it raises while its bones
    /// are still forming (<see cref="HeadsmanRising"/>).
    /// </summary>
    [HarmonyPatch(typeof(MonsterAI), nameof(MonsterAI.UpdateAI))]
    public static class HeadsmanAiPatch
    {
        /// <summary>The AIs that may have to wait (the Executioners and the skeletons they raise), each with its question.</summary>
        private static readonly Dictionary<MonsterAI, System.Func<bool>> waiting = new Dictionary<MonsterAI, System.Func<bool>>();

        public static void Watch(MonsterAI? ai, System.Func<bool> waits)
        {
            if (ai != null)
            {
                waiting[ai] = waits;
            }
        }

        public static void Forget(MonsterAI? ai)
        {
            if (ai != null)
            {
                waiting.Remove(ai);
            }
        }

        private static bool Prefix(MonsterAI __instance, ref bool __result)
        {
            if (!waiting.TryGetValue(__instance, out System.Func<bool> waits) || !waits())
            {
                return true;
            }
            __instance.StopMoving();
            __result = true;
            return false;
        }
    }

    /// <summary>
    /// The rear strike only when two or more foes are near (its target is behind, which its attack's inverted angle
    /// check already asks): the user's "several targets and one behind".
    /// </summary>
    [HarmonyPatch(typeof(BaseAI), nameof(BaseAI.CanUseAttack))]
    public static class HeadsmanRear
    {
        private const float Near = 8f;
        private static readonly List<Character> around = new List<Character>();

        private static void Postfix(BaseAI __instance, ItemDrop.ItemData item, ref bool __result)
        {
            if (__result && item?.m_shared.m_name == HeadsmanAttacks.RearName)
            {
                bool several = true;
                SafeCall.Run("BaseAI.CanUseAttack rear strike", () => several = Foes(__instance.GetComponent<Character>()) >= 2);
                __result = several;
            }
        }

        private static int Foes(Character? boss)
        {
            if (boss == null)
            {
                return 0;
            }
            around.Clear();
            Character.GetCharactersInRange(boss.transform.position, Near, around);
            return around.FindAll(c => c != boss && !c.IsDead() && BaseAI.IsEnemy(boss, c)).Count;
        }
    }
}
