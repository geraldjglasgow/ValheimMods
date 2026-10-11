using EliteCreaturesPack.Custom.Build;
using EliteCreaturesPack.Custom.Definitions;

namespace EliteCreaturesPack.Custom.Nature
{
    /// <summary>
    /// `progress:` on the shell. `key on death` is the game's own boss key, <c>Character.m_defeatSetGlobalKey</c>: the
    /// creature's owner, wherever it is, sends it to the server as it dies (<c>ZoneSystem.SetGlobalKey</c>, a routed RPC),
    /// which adds it to the world and sends the keys to everyone; the owner's player also gets it as a player key, as for
    /// the game's bosses. The game reads what follows a space in a key as its value, so the key is one word. The spawn and
    /// death messages are <see cref="CreatureMessages"/>, shown only to players nearby; the base's own messages, which the
    /// game sends to every player in the world (<c>BaseAI.m_spawnMessage</c>, <c>m_deathMessage</c>), give way to them.
    /// </summary>
    internal static class ProgressFields
    {
        private const string KeyField = "progress.key on death";

        public static void Apply(CreatureBuild build, Character character)
        {
            ProgressBlock? progress = build.Definition.Progress;
            if (progress == null)
            {
                return;
            }
            if (progress.KeyOnDeath != null)
            {
                ApplyKey(build, character, progress.KeyOnDeath);
            }
            if (progress.SpawnMessage != null || progress.DeathMessage != null)
            {
                ApplyMessages(build, progress);
            }
        }

        private static void ApplyKey(CreatureBuild build, Character character, string key)
        {
            if (key.IndexOfAny(new[] { ' ', '\t' }) >= 0)
            {
                build.Report.Fail($"'{key}' is not one word: the game reads what follows a space in a world key as its value", KeyField);
                return;
            }
            character.m_defeatSetGlobalKey = key;
        }

        private static void ApplyMessages(CreatureBuild build, ProgressBlock progress)
        {
            CreatureMessages messages = Assign.Ensure<CreatureMessages>(build.Shell);
            BaseAI ai = build.Shell.GetComponent<BaseAI>();
            if (progress.SpawnMessage != null)
            {
                messages.Spawn = progress.SpawnMessage;
                if (ai != null)
                {
                    ai.m_spawnMessage = "";
                }
            }
            if (progress.DeathMessage != null)
            {
                messages.Death = progress.DeathMessage;
                if (ai != null)
                {
                    ai.m_deathMessage = "";
                }
            }
        }
    }
}
