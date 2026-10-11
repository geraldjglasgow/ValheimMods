using EliteCreaturesPack.Custom.Definitions;

namespace EliteCreaturesPack.Custom.Nature
{
    /// <summary>
    /// A definition's speeds on the shell's <see cref="Character"/>. `speed scale` multiplies every speed the creature
    /// has as it stands (walking, its normal pace, running, crouching, swimming, flying slow and fast, and every turning
    /// speed), so in a chain the scales of each pass multiply. Then the single speeds a definition names are set outright:
    /// `speed` is <c>m_speed</c>, `walk speed` <c>m_walkSpeed</c>, `run speed` <c>m_runSpeed</c>, `turn speed` both
    /// <c>m_turnSpeed</c> and <c>m_runTurnSpeed</c>, `swim speed` <c>m_swimSpeed</c>, `fly speed` <c>m_flySlowSpeed</c>,
    /// `fly fast speed` <c>m_flyFastSpeed</c>. The animations play at their own pace.
    /// </summary>
    internal static class Speeds
    {
        public static void Apply(CharacterBlock block, Character character)
        {
            if (block.SpeedScale != null)
            {
                Scale(character, block.SpeedScale.Value);
            }
            Assign.Set(ref character.m_walkSpeed, block.WalkSpeed);
            Assign.Set(ref character.m_speed, block.Speed);
            Assign.Set(ref character.m_runSpeed, block.RunSpeed);
            Assign.Set(ref character.m_turnSpeed, block.TurnSpeed);
            Assign.Set(ref character.m_runTurnSpeed, block.TurnSpeed);
            Assign.Set(ref character.m_swimSpeed, block.SwimSpeed);
            Assign.Set(ref character.m_flySlowSpeed, block.FlySpeed);
            Assign.Set(ref character.m_flyFastSpeed, block.FlyFastSpeed);
        }

        private static void Scale(Character character, float scale)
        {
            character.m_walkSpeed *= scale;
            character.m_speed *= scale;
            character.m_runSpeed *= scale;
            character.m_crouchSpeed *= scale;
            character.m_turnSpeed *= scale;
            character.m_runTurnSpeed *= scale;
            character.m_swimSpeed *= scale;
            character.m_swimTurnSpeed *= scale;
            character.m_flySlowSpeed *= scale;
            character.m_flyFastSpeed *= scale;
            character.m_flyTurnSpeed *= scale;
        }
    }
}
