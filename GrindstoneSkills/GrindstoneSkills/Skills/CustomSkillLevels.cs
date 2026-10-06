using System.Collections.Generic;
using PatchGuard;

namespace GrindstoneSkills
{
    /// <summary>
    /// Publishes the local player's level in each of the mod's own skills to their own ZDO (the skill's
    /// <see cref="CustomSkill.LevelKey"/>), checked once a second and written only when it changed, so other machines
    /// can read it (<see cref="CustomSkill.Of"/>): a ship's owner reads the helmsman's Sailing, an ally's client reads a
    /// blocker's Defense. Skills live on the player's own client in the game too, so this trusts the client no less than
    /// the game does.
    /// </summary>
    public static class CustomSkillLevels
    {
        private const float Interval = 1f;

        private static readonly Dictionary<Skills.SkillType, float> published = new Dictionary<Skills.SkillType, float>();
        private static float timer;
        private static Player publishedFor;

        /// <summary>Every frame for the local player (<see cref="LocalPlayerTick"/>).</summary>
        public static void Tick(Player player, float dt)
        {
            timer += dt;
            if (timer < Interval)
                return;
            timer = 0f;
            Guard.Run("custom skill levels", static p => Publish(p), player);
        }

        private static void Publish(Player player)
        {
            ZNetView nview = player.m_nview;
            if (nview == null || !nview.IsValid() || !nview.IsOwner())
                return;
            if (publishedFor != player)
                published.Clear();
            publishedFor = player;
            foreach (CustomSkill skill in CustomSkills.All)
            {
                float level = player.GetSkillLevel(skill.Type);
                if (published.TryGetValue(skill.Type, out float last) && last == level)
                    continue;
                nview.GetZDO().Set(skill.LevelKey, level);
                published[skill.Type] = level;
            }
        }
    }
}
