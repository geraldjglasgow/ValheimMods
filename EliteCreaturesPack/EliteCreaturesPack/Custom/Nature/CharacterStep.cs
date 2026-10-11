using EliteCreaturesPack.Custom.Build;

namespace EliteCreaturesPack.Custom.Nature
{
    /// <summary>
    /// Puts a creature's nature on its shell: who it is and how it behaves (features/custom-creatures.md, section 3:
    /// Character, Progress, Senses and movement, Behaviour, Taming, Sounds). Each block of <see cref="CreatureBuild.Definition"/>
    /// goes onto the game's own components of the shell, field by field, and only the values the definition sets: the
    /// rest keep what the shell has (the base's values, or what an earlier pass of the chain put there; a speed scale
    /// multiplies what is there, so scales stack). A human is a Humanoid with a MonsterAI and takes every line the same.
    /// <list type="bullet">
    /// <item><see cref="CharacterFields"/> (with <see cref="Speeds"/> and <see cref="BossFight"/>): the Character.</item>
    /// <item><see cref="ProgressFields"/>: the world key on death and the messages (<see cref="CreatureMessages"/>).</item>
    /// <item><see cref="SensesFields"/>: senses and movement; <see cref="BehaviourFields"/>: behaviour, eating
    /// (<see cref="EatHeal"/>).</item>
    /// <item><see cref="TamingFields"/> and <see cref="Breeding"/>: Tameable and Procreation.</item>
    /// <item><see cref="SoundMute"/>: muted sounds (before the look step, which may replace whole effect lists).</item>
    /// <item><see cref="NatureChecks"/>: on the finished creature, lines that can never do anything together.</item>
    /// </list>
    /// An unknown item (eats), a boss fight that is not one of the game's events, or a world key of more than one word
    /// leaves the creature out. The runtime parts decide on the creature's owner and keep their state in its ZDO.
    /// </summary>
    internal sealed class CharacterStep : ICreatureStep
    {
        public string Name => "character";

        public void Apply(CreatureBuild build)
        {
            Character character = build.Shell.GetComponent<Character>();
            if (character == null)
            {
                build.Report.Fail("its prefab has no Character");
                return;
            }
            CharacterFields.Apply(build, character);
            ProgressFields.Apply(build, character);
            SensesFields.Apply(build, character);
            BehaviourFields.Apply(build);
            TamingFields.Apply(build);
            SoundMute.Apply(build, character);
            NatureChecks.Leaf(build, character);
        }
    }
}
