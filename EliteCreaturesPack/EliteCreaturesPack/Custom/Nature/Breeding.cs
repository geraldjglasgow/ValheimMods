using System.Collections.Generic;
using EliteCreaturesPack.Custom.Build;
using EliteCreaturesPack.Custom.Saves;
using UnityEngine;

namespace EliteCreaturesPack.Custom.Nature
{
    /// <summary>
    /// `taming: breeds` through the game's <see cref="Procreation"/>, which runs on the owner of a tame, fed creature and
    /// needs its Tameable (without one it would fail every ten seconds): without taming the line is named and ignored.
    /// Its offspring is always this creature, never the base's kind:
    /// <list type="bullet">
    /// <item>a base that breeds keeps its numbers and its young: a creature young that grows up (<c>Growup</c>, the game's
    /// piglets and cubs) is copied as this creature's own <c>&lt;name&gt;_young</c>, registered on every peer, which grows
    /// into this creature (any other kinds it could grow into are dropped) and is marked like a custom creature, so the
    /// save keeps it if the definition goes. Any other offspring (an egg, say) gives way to this creature itself;</item>
    /// <item>a base that does not breed gets the game's <see cref="TamingFields.Teacher"/>'s breeding, giving birth to this
    /// creature itself (the teacher's young would wear the teacher's body);</item>
    /// <item>it breeds with its own kind (the base's separate partner and partnerless offspring are dropped).</item>
    /// </list>
    /// `breeds: false` takes breeding off.
    /// </summary>
    internal static class Breeding
    {
        private const string Field = "taming.breeds";

        public static void Apply(CreatureBuild build, bool breeds)
        {
            if (!breeds)
            {
                Assign.Remove<Procreation>(build.Shell);
                return;
            }
            if (build.Shell.GetComponent<Tameable>() == null)
            {
                build.Report.Warn("only a tame creature breeds, and it can be neither tamed nor born tame; ignored", Field);
                return;
            }
            Procreation procreation = build.Shell.GetComponent<Procreation>();
            if (procreation == null)
            {
                procreation = Learn(build);
            }
            procreation.m_seperatePartner = null;
            procreation.m_noPartnerOffspring = null;
            procreation.m_offspring = Offspring(build, procreation.m_offspring);
        }

        private static Procreation Learn(CreatureBuild build)
        {
            Procreation procreation = build.Shell.AddComponent<Procreation>();
            if (!Lessons.Learn(build, procreation, TamingFields.Teacher))
            {
                build.Report.Warn($"the game has no {TamingFields.Teacher} to learn breeding from; the game's defaults are used", Field);
            }
            procreation.m_offspring = build.Shell;
            return procreation;
        }

        /// <summary>This creature, or its young: the one made in an earlier pass, or a copy of the base's young.</summary>
        private static GameObject Offspring(CreatureBuild build, GameObject? offspring)
        {
            if (offspring == null || offspring == build.Shell)
            {
                return build.Shell;
            }
            Growup growup = offspring.GetComponent<Growup>();
            if (growup == null || offspring.GetComponent<Character>() == null)
            {
                return build.Shell;
            }
            return growup.m_grownPrefab == build.Shell ? offspring : Young(build, offspring);
        }

        private static GameObject Young(CreatureBuild build, GameObject source)
        {
            GameObject young = build.CopyPart(source, "young", networked: true);
            Growup growup = young.GetComponent<Growup>();
            growup.m_grownPrefab = build.Shell;
            growup.m_altGrownPrefabs = new List<Growup.GrownEntry>();
            Assign.Ensure<CustomTag>(young);
            return young;
        }
    }
}
