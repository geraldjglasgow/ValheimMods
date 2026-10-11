using EliteCreaturesPack.Custom.Definitions;
using YamlConfig;

namespace EliteCreaturesPack.Custom.Files
{
    /// <summary>Reads `effects:` (<see cref="EffectsBlock"/>) and `look:` (<see cref="LookBlock"/>).</summary>
    internal static class LookReader
    {
        public static EffectsBlock? ReadEffects(YamlNode entry, FieldReader fields)
        {
            YamlNode? block = fields.Block(entry, "effects");
            if (block == null)
            {
                return null;
            }
            return new EffectsBlock
            {
                Hit = fields.Names(block, "hit"),
                Death = fields.Names(block, "death"),
                Alert = fields.Names(block, "alert"),
                Idle = fields.Names(block, "idle"),
            };
        }

        public static LookBlock? ReadLook(YamlNode entry, FieldReader fields)
        {
            YamlNode? block = fields.Block(entry, "look");
            if (block == null)
            {
                return null;
            }
            return new LookBlock
            {
                Size = fields.Scale(block, "size", 0.05f, 20f),
                BodyTint = fields.Colour(block, "body tint"),
                ItemTint = fields.Colour(block, "item tint"),
                Overlay = fields.Word<OverlayKind>(block, "overlay"),
                OverlayColour = fields.Colour(block, "overlay colour"),
            };
        }
    }
}
