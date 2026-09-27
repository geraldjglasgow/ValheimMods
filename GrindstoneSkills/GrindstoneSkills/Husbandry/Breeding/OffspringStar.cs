using BepInEx.Bootstrap;
using UnityEngine;

namespace GrindstoneSkills
{
    /// <summary>
    /// Better offspring's extra star, on the parent's owner, once its roll is won for a birth (<see cref="BirthRolls"/>).
    /// <list type="bullet">
    /// <item><b>Without Elite Creatures Reborn</b> the game passes the parent's level (at least the kind's minimum) to the
    /// newborn (Character.SetLevel) or to a laid egg (ItemDrop.SetQuality; the egg's quality becomes the chick's level
    /// when it hatches). <see cref="OffspringLevel"/> raises that value by one while the game's birth code runs, never
    /// above Max Offspring Level: a parent already at the cap gets no star.</item>
    /// <item><b>With Elite Creatures Reborn</b> the game level stays 1 and ECR decides stars itself. The parent's ZDO
    /// carries <see cref="Keys.StarUp"/> = 1 from this mod's Procreate prefix (which runs before ECR's) until its
    /// finalizer sets it back to 0, so it never reaches a later birth. ECR reads it at birth and leaves it at 1 when it
    /// adds the star, or writes 0 when it declines (the newborn is already at ECR's cap).</item>
    /// </list>
    /// "Strong offspring!" floats only when the star was added (<see cref="Added"/>).
    /// </summary>
    public static class OffspringStar
    {
        public const string EcrGuid = "gglasgow.elitecreaturesreborn";

        public enum Route
        {
            /// <summary>No star: the roll was lost, or the parent is already at Max Offspring Level.</summary>
            None,
            /// <summary>The game's own level, raised inside the birth scope (<see cref="OffspringLevel"/>).</summary>
            Level,
            /// <summary>The <see cref="Keys.StarUp"/> key on the parent, for Elite Creatures Reborn.</summary>
            EcrKey,
        }

        private static readonly int StarUpHash = Keys.StarUp.GetStableHashCode();
        private static bool? ecrInstalled;

        /// <summary>Elite Creatures Reborn is loaded on this machine; checked once, when the first star is won.</summary>
        public static bool EcrInstalled
        {
            get
            {
                if (!ecrInstalled.HasValue)
                    ecrInstalled = Chainloader.PluginInfos.ContainsKey(EcrGuid);
                return ecrInstalled.Value;
            }
        }

        /// <summary>Sets the star up for the birth about to happen, before the game's code runs.</summary>
        public static Route Begin(Procreation parent)
        {
            if (EcrInstalled)
            {
                parent.m_nview.GetZDO().Set(StarUpHash, 1);
                return Route.EcrKey;
            }
            int passed = PassedLevel(parent);
            int raised = Mathf.Min(passed + 1, HusbandryBreedingSettings.MaxOffspringLevel.Value);
            if (raised <= passed)
                return Route.None;
            OffspringLevel.Open(passed, raised);
            return Route.Level;
        }

        /// <summary>After the game's birth code: the newborn (or egg) got the star.</summary>
        public static bool Added(Procreation parent, Route route)
        {
            if (route == Route.Level)
                return OffspringLevel.Raised;
            ZNetView nview = parent.m_nview;
            return route == Route.EcrKey && nview != null && nview.IsValid() && nview.GetZDO().GetInt(StarUpHash) == 1;
        }

        /// <summary>In the finalizer: closes the birth scope and takes the key off the parent.</summary>
        public static void End(Procreation parent, Route route)
        {
            OffspringLevel.Close();
            ZNetView nview = parent.m_nview;
            if (route == Route.EcrKey && nview != null && nview.IsValid())
                nview.GetZDO().Set(StarUpHash, 0);
        }

        /// <summary>The level the game passes on, as Procreate computes it: the parent's own, at least the kind's minimum.</summary>
        private static int PassedLevel(Procreation parent)
        {
            Character character = parent.m_character;
            int level = character != null ? character.GetLevel() : parent.m_minOffspringLevel;
            return Mathf.Max(parent.m_minOffspringLevel, level);
        }
    }
}
