using System.Collections.Generic;
using System.Linq;
using EliteCreaturesPack.Custom.Definitions;
using UnityEngine;

namespace EliteCreaturesPack.Custom.Export
{
    /// <summary>
    /// `senses:`, `movement:`, `behaviour:`, `taming:` and `sounds:` of an export, read from the creature's AI (BaseAI,
    /// and MonsterAI for what only a monster's mind has), its Tameable and Procreation, as the character step applies
    /// them (Custom/Nature). An animal's mind (AnimalAI) has no MonsterAI values, so those keys are notes. Eating to heal
    /// and muted sounds are Elite Creatures Pack's own: a custom creature's come from its chain, every other creature has
    /// none.
    /// </summary>
    internal static class MindExport
    {
        private const float Far = 10000f;
        private const string AnimalMind = "it has an animal's mind (AnimalAI), which has no such setting";

        public static void WriteSenses(ExportWriter writer, ExportSource source)
        {
            BaseAI? ai = source.Ai;
            if (ai == null)
            {
                NoMind(writer, "senses");
                return;
            }
            writer.Open("senses");
            writer.Number("sight range", ai.m_viewRange, 0f, 1000f);
            writer.Number("sight angle", ai.m_viewAngle, 0f, 360f);
            writer.Number("hearing range", ai.m_hearRange, 0f, Far);
            if (source.Monster != null)
            {
                writer.Number("alert range", source.Monster.m_alertRange, 0f, Far);
            }
            else
            {
                writer.Note("alert range: - " + AnimalMind);
            }
            writer.Close();
        }

        public static void WriteMovement(ExportWriter writer, ExportSource source)
        {
            BaseAI? ai = source.Ai;
            if (ai == null)
            {
                NoMind(writer, "movement");
                return;
            }
            writer.Open("movement");
            writer.Number("wander every", ai.m_randomMoveInterval, 0f, 600f);
            writer.Number("wander range", ai.m_randomMoveRange, 0f, 1000f);
            writer.Number("circle every", ai.m_randomCircleInterval, 0f, 600f);
            WriteFlight(writer, ai, source.Character);
            writer.Close();
        }

        /// <summary>
        /// The flyer's values where the character step uses them: take-off and landing on a creature that takes off and
        /// lands by itself (<c>m_randomFly</c>), the height on one that flies. On any other the step would warn that they do
        /// nothing, so they are notes.
        /// </summary>
        private static void WriteFlight(ExportWriter writer, BaseAI ai, Character character)
        {
            if (ai.m_randomFly)
            {
                writer.Number("take off chance", ai.m_chanceToTakeoff, 0f, 1f);
                writer.Number("land chance", ai.m_chanceToLand, 0f, 1f);
                writer.Number("time on ground", ai.m_groundDuration, 0f, 3600f);
                writer.Number("time in air", ai.m_airDuration, 0f, 3600f);
            }
            else
            {
                writer.Note("take off chance, land chance, time on ground, time in air: - only for a creature that takes off "
                    + "and lands by itself");
            }
            if (ai.m_randomFly || character.m_flying)
            {
                WriteFlyHeight(writer, ai.m_flyAltitudeMin, ai.m_flyAltitudeMax);
            }
            else
            {
                writer.Note("fly height: - only for a creature that flies");
            }
        }

        public static void WriteBehaviour(ExportWriter writer, ExportSource source)
        {
            BaseAI? ai = source.Ai;
            if (ai == null)
            {
                NoMind(writer, "behaviour");
                return;
            }
            writer.Open("behaviour");
            writer.Switch("avoid fire", ai.m_avoidFire);
            writer.Switch("fear fire", ai.m_afraidOfFire);
            writer.Switch("avoid water", ai.m_avoidWater);
            WriteMonster(writer, source.Monster);
            if (source.Monster != null)
            {
                WriteEatHeal(writer, source); // only a monster's mind eats: on an animal the line would only be warned about
            }
            writer.Close();
        }

        public static void WriteTaming(ExportWriter writer, ExportSource source)
        {
            Tameable? tameable = source.Prefab.GetComponent<Tameable>();
            writer.Open("taming");
            writer.Switch("tameable", tameable != null);
            bool bornTame = source.LastValue(definition => definition.Taming?.BornTame) ?? (tameable != null && tameable.m_startsTamed);
            writer.Switch("born tame", bornTame);
            if (tameable != null)
            {
                writer.Switch("follows commands", tameable.m_commandable);
            }
            else
            {
                writer.Note("follows commands: - only for a creature that can be tamed or is born tame");
            }
            writer.Switch("breeds", source.Prefab.GetComponent<Procreation>() != null);
            writer.Close();
        }

        /// <summary>`sounds: mute:` every sound any definition of the chain muted (a muted sound stays muted down the chain).</summary>
        public static void WriteSounds(ExportWriter writer, ExportSource source)
        {
            IEnumerable<SoundKind> muted = source.Chain
                .Where(definition => definition.Sounds != null)
                .SelectMany(definition => definition.Sounds!.Mute)
                .Distinct()
                .OrderBy(kind => kind);
            writer.Open("sounds");
            writer.Key("mute", ExportValues.Names(muted.Select(kind => ExportValues.Word(kind))));
            writer.Close();
        }

        /// <summary>What only a monster's mind has: fleeing, hunting, buildings, chasing, attack pace, circling, sleep, eating.</summary>
        private static void WriteMonster(ExportWriter writer, MonsterAI? monster)
        {
            if (monster == null)
            {
                writer.Note("flee, hunt players, attack buildings, chasing, attack pace, circling, sleep and eating: - " + AnimalMind);
                return;
            }
            WriteFlee(writer, monster);
            WriteFight(writer, monster);
            WriteRest(writer, monster);
        }

        private static void WriteFlee(ExportWriter writer, MonsterAI monster)
        {
            writer.Number("flee at health", monster.m_fleeIfLowHealth, 0f, 1f);
            writer.Switch("flee when unreachable", monster.m_fleeIfHurtWhenTargetCantBeReached);
        }

        private static void WriteFight(ExportWriter writer, MonsterAI monster)
        {
            writer.Switch("hunt players", monster.m_enableHuntPlayer);
            writer.Switch("attack buildings", monster.m_attackPlayerObjects);
            writer.Number("chase distance", monster.m_maxChaseDistance, 0f, Far);
            writer.Number("time between attacks", monster.m_minAttackInterval, 0f, 600f);
            writer.Switch("circle before charging", monster.m_circulateWhileCharging);
            writer.Number("circle target every", monster.m_circleTargetInterval, 0f, 600f);
            writer.Number("circle target for", monster.m_circleTargetDuration, 0f, 600f);
            writer.Number("circle target distance", monster.m_circleTargetDistance, 0f, 100f);
        }

        private static void WriteRest(ExportWriter writer, MonsterAI monster)
        {
            writer.Switch("starts asleep", monster.m_sleeping);
            writer.Switch("wakes on noise", monster.m_noiseWakeup);
            writer.Number("wake distance", monster.m_wakeupRange, 0f, 1000f);
            IEnumerable<string> eats = (monster.m_consumeItems ?? new List<ItemDrop>())
                .Where(item => item != null)
                .Select(item => item.gameObject.name);
            writer.Key("eats", ExportValues.Names(eats));
            writer.Number("eat search range", monster.m_consumeSearchRange, 0f, 100f);
        }

        // The game's creatures heal nothing from what they eat: the heal is Elite Creatures Pack's own, kept by the chain.
        private static void WriteEatHeal(ExportWriter writer, ExportSource source)
        {
            float? heal = source.LastValue(definition => definition.Behaviour?.EatHeal);
            if (heal == null)
            {
                writer.Note("eat heal: health one eaten item heals; 0, as for every creature of the game");
            }
            writer.Number("eat heal", heal ?? 0f, 0f, 1000000f);
        }

        private static void WriteFlyHeight(ExportWriter writer, float low, float high)
        {
            if (ExportValues.Within(low, 0f, 1000f) && ExportValues.Within(high, 0f, 1000f) && low <= high)
            {
                writer.Key("fly height", ExportValues.Range(low, high));
                return;
            }
            writer.Note($"fly height: {ExportValues.Number(low)} to {ExportValues.Number(high)} - outside the 0 to 1,000 a "
                + "definition takes, so it is left as it is");
        }

        private static void NoMind(ExportWriter writer, string block) =>
            writer.Note($"{block}: - it has no AI, so there is nothing to set");
    }
}
