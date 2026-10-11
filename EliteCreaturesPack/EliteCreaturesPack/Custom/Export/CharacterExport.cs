using System;

namespace EliteCreaturesPack.Custom.Export
{
    /// <summary>
    /// `character:` and `progress:` of an export, read from the creature's Character (and its AI's spawn and death
    /// messages), each key as the character step applies it (Custom/Nature): <c>m_name</c>, <c>m_health</c>, the speeds,
    /// <c>m_faction</c>, <c>m_boss</c>, <c>m_bossEvent</c>, <c>m_damageModifiers</c>, the <c>m_tolerate...</c> switches
    /// turned round, <c>m_staggerWhenBlocked</c>, <c>m_defeatSetGlobalKey</c>, <c>BaseAI.m_spawnMessage</c> and
    /// <c>m_deathMessage</c>.
    /// </summary>
    internal static class CharacterExport
    {
        private const float MaxSpeed = 100f;

        public static void Write(ExportWriter writer, ExportSource source)
        {
            writer.Open("character");
            WriteWho(writer, source.Character);
            WriteBody(writer, source.Character);
            writer.Close();
        }

        /// <summary>Who it is: display name, health, faction.</summary>
        public static void WriteWho(ExportWriter writer, Character character)
        {
            writer.Text("display name", character.m_name, "none - it has no name of its own");
            writer.Number("health", character.m_health, 1f, 1000000f);
            WriteFaction(writer, character.m_faction);
        }

        /// <summary>The rest of `character:`: speeds, boss, boss fight, resistances, what hurts it, staggering.</summary>
        public static void WriteBody(ExportWriter writer, Character character)
        {
            WriteSpeeds(writer, character);
            writer.Switch("boss", character.m_boss);
            writer.Text("boss fight", character.m_bossEvent, "none - no boss event's weather and music play for it");
            DamageExport.WriteResistances(writer, character.m_damageModifiers);
            writer.Switch("hurt by water", !character.m_tolerateWater);
            writer.Switch("hurt by fire", !character.m_tolerateFire);
            writer.Switch("hurt by smoke", !character.m_tolerateSmoke);
            writer.Switch("staggers when blocked", character.m_staggerWhenBlocked);
        }

        public static void WriteProgress(ExportWriter writer, ExportSource source)
        {
            writer.Open("progress");
            writer.Text("key on death", source.Character.m_defeatSetGlobalKey, "none - its death sets no world key");
            writer.Text("spawn message", source.Ai?.m_spawnMessage, "none");
            writer.Text("death message", source.Ai?.m_deathMessage, "none");
            writer.Close();
        }

        private static void WriteSpeeds(ExportWriter writer, Character character)
        {
            writer.Note("speed scale multiplies every speed below; 1 keeps them as they are");
            writer.Number("speed scale", 1f, 0.05f, 20f);
            writer.Number("walk speed", character.m_walkSpeed, 0f, MaxSpeed);
            writer.Number("speed", character.m_speed, 0f, MaxSpeed);
            writer.Number("run speed", character.m_runSpeed, 0f, MaxSpeed);
            WriteTurnSpeed(writer, character);
            writer.Number("swim speed", character.m_swimSpeed, 0f, MaxSpeed);
            writer.Number("fly speed", character.m_flySlowSpeed, 0f, MaxSpeed);
            writer.Number("fly fast speed", character.m_flyFastSpeed, 0f, MaxSpeed);
        }

        // `turn speed` sets the walking and the running turn alike, so two different ones cannot be written as one.
        private static void WriteTurnSpeed(ExportWriter writer, Character character)
        {
            if (character.m_turnSpeed == character.m_runTurnSpeed)
            {
                writer.Number("turn speed", character.m_turnSpeed, 0f, 10000f);
                return;
            }
            writer.Note($"turn speed: {ExportValues.Number(character.m_turnSpeed)} walking and "
                + $"{ExportValues.Number(character.m_runTurnSpeed)} running - `turn speed` sets both to one number, so "
                + "it is left as it is");
        }

        private static void WriteFaction(ExportWriter writer, Character.Faction faction)
        {
            if (Enum.IsDefined(typeof(Character.Faction), faction))
            {
                writer.Key("faction", ExportValues.Word(faction));
                return;
            }
            writer.Note($"faction: {(int)faction} - a faction added by another mod, which a definition cannot name");
        }
    }
}
