using UnityEngine;

namespace GrindstoneSkills
{
    /// <summary>
    /// The breeding lore lines of a tamed animal that breeds (<c>Procreation</c>): "Pregnant, due in 40 s" or "Love 2 of
    /// 3", and the herd's room: how many of its kind (young included) stand within the game's crowding range against
    /// how many may before breeding stops, with the keeper's Herd Size bonus. Pace and room come from
    /// <see cref="BreedingPace"/> for the best keeper near it now, so the lines match what the breeding check does.
    /// </summary>
    public static class BreedingLore
    {
        public static string Lines(Tameable tameable)
        {
            Procreation procreation = tameable.GetComponent<Procreation>();
            if (procreation == null || !tameable.IsTamed() || tameable.m_character == null)
                return "";
            ZDO zdo = tameable.m_nview.GetZDO();
            float level = Keeper.BestLevel(tameable.transform.position);
            return LoreText.Add(PregnancyLine(procreation, zdo, level), HerdLine(procreation, zdo, level));
        }

        private static string PregnancyLine(Procreation procreation, ZDO zdo, float level)
        {
            long pregnant = zdo.GetLong(ZDOVars.s_pregnant);
            if (pregnant == 0L)
                return $"Love {zdo.GetInt(ZDOVars.s_lovePoints)} of {procreation.m_requiredLovePoints}";
            double left = procreation.m_pregnancyDuration / BreedingPace.SpeedFactor(procreation.m_nview, level) - Herd.SecondsSince(pregnant);
            return left > 0.0 ? $"Pregnant, due in {LoreText.Duration(left)}" : "Pregnant, due any moment";
        }

        private static string HerdLine(Procreation procreation, ZDO zdo, float level)
        {
            Vector3 position = procreation.transform.position;
            float range = procreation.m_totalCheckRange;
            int count = Count(ZNetScene.instance.GetPrefab(zdo.GetPrefab()), position, range)
                + Count(procreation.m_offspring, position, range);
            int room = procreation.m_maxCreatures + (HusbandrySkill.Active ? BreedingPace.ExtraRoom(level) : 0);
            return count >= room ? $"Herd full ({count} of {room} within {range:0} m)" : $"Herd {count} of {room}";
        }

        private static int Count(GameObject prefab, Vector3 position, float range)
        {
            GameObject named = prefab != null ? ZNetScene.instance.GetPrefab(Utils.GetPrefabName(prefab)) : null;
            return named != null ? SpawnSystem.GetNrOfInstances(named, position, range) : 0;
        }
    }
}
