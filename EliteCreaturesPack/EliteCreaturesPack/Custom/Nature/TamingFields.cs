using EliteCreaturesPack.Custom.Build;
using EliteCreaturesPack.Custom.Definitions;

namespace EliteCreaturesPack.Custom.Nature
{
    /// <summary>
    /// `taming:` on the shell, through the game's own <see cref="Tameable"/> and <see cref="Procreation"/>.
    /// <list type="bullet">
    /// <item>`tameable: true` gives a base without taming a Tameable with the game's <see cref="Teacher"/>'s values (fed
    /// time, taming time, effects, follows commands). The game tames by feeding: the creature must eat (`behaviour: eats`)
    /// and have a monster's mind. `tameable: false` takes taming off: the Tameable, its breeding (which cannot run without
    /// it) and a saddle (hidden, so its riding parts never wake).</item>
    /// <item>`born tame` is the game's <c>Tameable.m_startsTamed</c>, as the game's summoned creatures have it: as each one
    /// wakes, the owner sets the game's tamed flag in its ZDO, so it is tame on every peer and in every save. A creature
    /// born tame needs a Tameable (a tame creature without one fills the log), so it gets one, even after
    /// `tameable: false`.</item>
    /// <item>`follows commands` is <c>Tameable.m_commandable</c>.</item>
    /// <item>`breeds`: <see cref="Breeding"/>.</item>
    /// </list>
    /// </summary>
    internal static class TamingFields
    {
        /// <summary>The game creature whose taming and breeding a creature without its own learns: the game's fighting tame.</summary>
        public const string Teacher = "Wolf";

        public static void Apply(CreatureBuild build)
        {
            TamingBlock? taming = build.Definition.Taming;
            if (taming == null)
            {
                return;
            }
            if (taming.Tameable != null)
            {
                SetTameable(build, taming.Tameable.Value);
            }
            if (taming.BornTame != null)
            {
                SetBornTame(build, taming.BornTame.Value);
            }
            if (taming.FollowsCommands != null)
            {
                SetCommandable(build, taming.FollowsCommands.Value);
            }
            if (taming.Breeds != null)
            {
                Breeding.Apply(build, taming.Breeds.Value);
            }
        }

        /// <summary>The shell's Tameable, given one with the teacher's values when it has none.</summary>
        public static Tameable Ensure(CreatureBuild build)
        {
            Tameable tameable = build.Shell.GetComponent<Tameable>();
            if (tameable != null)
            {
                return tameable;
            }
            tameable = build.Shell.AddComponent<Tameable>();
            if (!Lessons.Learn(build, tameable, Teacher))
            {
                build.Report.Warn($"the game has no {Teacher} to learn taming from; the game's defaults are used", "taming");
            }
            tameable.m_saddle = null;
            tameable.m_saddleItem = null;
            tameable.m_startsTamed = false;
            return tameable;
        }

        private static void SetTameable(CreatureBuild build, bool tameable)
        {
            if (tameable)
            {
                Ensure(build);
                return;
            }
            Tameable own = build.Shell.GetComponent<Tameable>();
            if (own != null && own.m_saddle != null)
            {
                own.m_saddle.gameObject.SetActive(false);
            }
            Assign.Remove<Procreation>(build.Shell);
            Assign.Remove<Tameable>(build.Shell);
        }

        private static void SetBornTame(CreatureBuild build, bool bornTame)
        {
            if (bornTame)
            {
                Ensure(build).m_startsTamed = true;
                return;
            }
            Tameable tameable = build.Shell.GetComponent<Tameable>();
            if (tameable != null)
            {
                tameable.m_startsTamed = false;
            }
        }

        private static void SetCommandable(CreatureBuild build, bool commandable)
        {
            Tameable tameable = build.Shell.GetComponent<Tameable>();
            if (tameable == null)
            {
                build.Report.Warn("it can be neither tamed nor born tame, so follows commands does nothing", "taming.follows commands");
                return;
            }
            tameable.m_commandable = commandable;
        }
    }
}
