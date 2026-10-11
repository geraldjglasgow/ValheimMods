using UnityEngine;

namespace EliteCreaturesPack.Custom.Humans
{
    /// <summary>
    /// Rolls one person within a look's ranges and writes it where VisEquipment reads a player's look: the body model
    /// (0 male, 1 female, as the creator's toggles set it), the hair and beard items, the hair and skin colours. The
    /// colours are mapped as the game's character creator maps its sliders (the values of its PlayerCustomizaton in the
    /// start scene): skin from white to a 0.3 grey by one slider (0 lightest, 1 darkest, so a look's skin tone is that
    /// slider); hair from a pale blond to a red brown by one slider, times a level from 0.1 to 1 by another. The game's
    /// natural range is both hair sliders anywhere.
    /// <para>No hair or no beard (item 0) is also written as the game's "no hair" and "no beard" marks: VisEquipment,
    /// flagged as a player's, gives an owner whose ZDO has item 0 and no mark a random hair of its own as it wakes
    /// (SetupFacialHairNonPlayer), which on every later load would cover a bald or beardless roll.</para>
    /// </summary>
    internal static class HumanLookRoll
    {
        private const int Male = 0, Female = 1;
        private const float HairDarkest = 0.1f, HairLightest = 1f;
        private static readonly Color SkinLightest = Color.white, SkinDarkest = new Color(0.3f, 0.3f, 0.3f);
        private static readonly Color HairPale = new Color(1f, 0.931f, 0.706f), HairRed = new Color(1f, 0.488f, 0.279f);

        public static void Write(ZDO zdo, HumanAppearance look)
        {
            int model = Model(look.Gender);
            int hair = HumanHairs.Pick(look.Hair, HumanHairs.HairKind);
            int beard = model == Female && look.BeardlessWomen ? 0 : HumanHairs.Pick(look.Beard, HumanHairs.BeardKind);
            zdo.Set(ZDOVars.s_modelIndex, model);
            zdo.Set(ZDOVars.s_hairItem, hair);
            zdo.Set(ZDOVars.s_noHair, hair == 0);
            zdo.Set(ZDOVars.s_beardItem, beard);
            zdo.Set(ZDOVars.s_noBeard, beard == 0);
            zdo.Set(ZDOVars.s_hairColor, Utils.ColorToVec3(HairColour(look.HairColours)));
            zdo.Set(ZDOVars.s_skinColor, Utils.ColorToVec3(Skin(look.SkinTone)));
        }

        private static int Model(string gender)
        {
            switch ((gender ?? "").Trim().ToLowerInvariant())
            {
                case "male":
                    return Male;
                case "female":
                    return Female;
                default:
                    return Random.Range(Male, Female + 1);
            }
        }

        private static Color HairColour(Color[] chosen)
        {
            if (chosen.Length > 0)
            {
                return chosen[Random.Range(0, chosen.Length)];
            }
            return Color.Lerp(HairPale, HairRed, Random.value) * Mathf.Lerp(HairDarkest, HairLightest, Random.value);
        }

        private static Color Skin(Vector2 tone)
        {
            float lightest = Mathf.Clamp01(Mathf.Min(tone.x, tone.y)), darkest = Mathf.Clamp01(Mathf.Max(tone.x, tone.y));
            return Color.Lerp(SkinLightest, SkinDarkest, Random.Range(lightest, darkest));
        }
    }
}
