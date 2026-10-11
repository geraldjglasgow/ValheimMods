using System;
using System.Collections.Generic;
using System.Linq;
using EliteCreaturesLink;
using EliteCreaturesPack.Custom.Definitions;
using UnityEngine;

namespace EliteCreaturesPack.Custom.Export
{
    /// <summary>
    /// `effects:`, `look:`, `texture` and `elite:` of an export. Effects are the names in the creature's effect
    /// lists (Character <c>m_hitEffects</c>, <c>m_deathEffects</c>, BaseAI <c>m_alertedEffects</c>, <c>m_idleSound</c>),
    /// as the look step (Custom/Look) replaces them; a list whose effects are placed in ways a name cannot say (attached,
    /// at a bone, scaled or turned with the body, one variant) is a note, so copying the export never changes how they
    /// play. Size is relative to the base, so 1. Tints, overlay, texture and Elite Creatures Reborn's lines come from a
    /// custom creature's chain; any other creature has none.
    /// </summary>
    internal static class LookExport
    {
        public static void WriteEffects(ExportWriter writer, ExportSource source)
        {
            writer.Open("effects");
            WriteList(writer, "hit", source.Character.m_hitEffects, source, definition => definition.Effects?.Hit);
            WriteList(writer, "death", source.Character.m_deathEffects, source, definition => definition.Effects?.Death);
            WriteList(writer, "alert", source.Ai?.m_alertedEffects, source, definition => definition.Effects?.Alert);
            WriteList(writer, "idle", source.Ai?.m_idleSound, source, definition => definition.Effects?.Idle);
            writer.Close();
        }

        public static void WriteLook(ExportWriter writer, ExportSource source)
        {
            writer.Open("look");
            writer.Note("size is relative to the base: 1 keeps its size");
            writer.Key("size", ExportValues.Number(1f));
            WriteColour(writer, "body tint", source.LastValue(definition => definition.Look?.BodyTint), "none - its own colours");
            WriteColour(writer, "item tint", source.LastValue(definition => definition.Look?.ItemTint), "none - its items' own colours");
            OverlayKind? overlay = source.LastValue(definition => definition.Look?.Overlay);
            if (overlay != null)
            {
                writer.Key("overlay", ExportValues.Word(overlay.Value));
            }
            else
            {
                writer.Note("overlay: none (smoke or flame over the whole body)");
            }
            WriteColour(writer, "overlay colour", source.LastValue(definition => definition.Look?.OverlayColour), "none");
            writer.Close();
        }

        public static void WriteTexture(ExportWriter writer, ExportSource source) =>
            writer.Text("texture", source.Last(definition => definition.Texture),
                "none - the base's own (an image file in the config folder replaces it)");

        public static void WriteElite(ExportWriter writer, ExportSource source)
        {
            writer.Open("elite");
            WriteEliteChoices(writer);
            writer.Key("mutations", ExportValues.Names(source.LastList(definition => definition.Elite?.Mutations)));
            writer.Key("aspect", ExportValues.Names(source.LastList(definition => definition.Elite?.Aspects)));
            writer.Key("portal attacks", ExportValues.Names(source.LastList(definition => definition.Elite?.PortalAttacks)));
            WriteSummon(writer, source.Last(definition =>
                definition.Elite != null && definition.Elite.Summon.Count > 0 ? definition.Elite.Summon : null));
            writer.Close();
        }

        // A custom creature's list is the one its definitions gave (an effect that spawns a creature is not an effect
        // prefab); any other creature's is the names in its effect list, a copy made for a custom creature by its original.
        private static void WriteList(ExportWriter writer, string key, EffectList? list, ExportSource source,
            Func<CreatureDefinition, List<string>?> read)
        {
            List<string>? defined = source.Last(read);
            if (defined != null)
            {
                writer.Key(key, ExportValues.Names(defined));
                return;
            }
            if (list == null)
            {
                writer.Note($"{key}: - it has no AI, which plays these");
                return;
            }
            EffectList.EffectData[] played = (list.m_effectPrefabs ?? new EffectList.EffectData[0])
                .Where(effect => effect != null && effect.m_enabled && effect.m_prefab != null).ToArray();
            List<string> named = played.Select(effect => source.OriginOf(effect.m_prefab) ?? effect.m_prefab.name).ToList();
            string names = ExportValues.Names(named);
            string? unsayable = Unsayable(played, named);
            if (unsayable == null)
            {
                writer.Key(key, names);
                return;
            }
            writer.Note($"{key}: {names} - {unsayable}, so it is left as it is");
        }

        private static string? Unsayable(EffectList.EffectData[] played, List<string> named)
        {
            if (played.Any(Placed))
            {
                return "some play attached, at a bone, scaled or turned with the body, or as one variant only, which a list of "
                    + "names cannot say";
            }
            string? unknown = named.FirstOrDefault(name => ZNetScene.instance.GetPrefab(name) == null);
            return unknown == null ? null : $"{unknown} is not a prefab the game knows by name";
        }

        private static bool Placed(EffectList.EffectData effect) =>
            effect.m_variant >= 0 || effect.m_attach || effect.m_follow || !string.IsNullOrEmpty(effect.m_childTransform)
            || effect.m_inheritParentRotation || effect.m_inheritParentScale || effect.m_multiplyParentVisualScale
            || effect.m_randomRotation || effect.m_scale;

        private static void WriteColour(ExportWriter writer, string key, Color? colour, string none)
        {
            if (colour != null)
            {
                writer.Key(key, ExportValues.Colour(colour.Value));
                return;
            }
            writer.Note($"{key}: {none}");
        }

        private static void WriteEliteChoices(ExportWriter writer)
        {
            if (!EliteLink.Present)
            {
                writer.Note("Elite Creatures Reborn is not installed here: these lines do nothing without it");
                return;
            }
            writer.Note("Elite Creatures Reborn's mutations: " + string.Join(", ", EliteTraits.MutationNames));
            writer.Note("its aspects (on a boss): " + string.Join(", ", EliteTraits.AspectNames));
        }

        private static void WriteSummon(ExportWriter writer, List<SummonEntry>? summon)
        {
            if (summon == null)
            {
                writer.Key("summon", "[]");
                return;
            }
            writer.Open("summon");
            foreach (SummonEntry entry in summon)
            {
                writer.Entry(entry.Stars == null ? ExportValues.Name(entry.Creature)
                    : $"{{ creature: {ExportValues.Name(entry.Creature)}, stars: {entry.Stars.Value} }}");
            }
            writer.Close();
        }
    }
}
