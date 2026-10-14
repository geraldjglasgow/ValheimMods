using UnityEngine;

namespace EliteCreaturesReborn.Creatures.BatFiend
{
    /// <summary>
    /// What the Bat-fiend's attacks look like, every look a local copy of one of the game's own effects
    /// (<see cref="BatFiendFxParts"/>), sized to the hit shape it shows and recoloured to Hel's cold blue-green where it
    /// is the creature's own power. <c>size</c> is the body's size against one star's (1 for a plain
    /// Bat-fiend, more with stars). Nothing here is networked and nothing deals damage: <see cref="BatFiendEffects"/>
    /// calls these on every machine with a screen from the animation that machine already plays.
    /// <list type="bullet">
    /// <item>Dust and grit (the Lox's ground slam, its dust ring and stones; the Troll's for a heavy landing) where it
    /// pushes off and lands: the pounce's take-off and landing, the bite's lunge, the gravewind's downbeat.</item>
    /// <item>Wing Guillotine: a crescent of cut cold air and frost glints along the wing's sweep (the Morgen's swipe).</item>
    /// <item>Gravewind Blast: a gust of grave-cold mist and frost rolling out in its fan (Moder's cold breath, one beat).</item>
    /// <item>Shattering Screech: the air rippling round its jaws, then wavefronts racing down its narrow beam (the Fader's
    /// roar distortion, Eikthyr's forward shockwave).</item>
    /// <item>Bloodletting Bite, when it heals: a dark red wisp drawn back from its jaws into its body (the blood crossbow's
    /// smoke).</item>
    /// </list>
    /// </summary>
    internal static class BatFiendFx
    {
        private const string DustRing = "vfx_lox_groundslam", HeavyDust = "vfx_troll_groundslam", Crescent = "fx_morgen_swipe",
            Breath = "vfx_dragon_coldbreath", Roar = "fx_Fader_Roar", Shockwave = "fx_eikthyr_forwardshockwave", Blood = "vfx_crossbow_blood_fire";

        // Hel's palette (#2a978b glow) lightened for mist, frost and sound; the wing's mist is lit, so it is drawn paler.
        private static readonly Color MistLow = new Color(0.33f, 0.62f, 0.60f, 0.25f), MistHigh = new Color(0.58f, 0.92f, 0.88f, 0.29f);
        private static readonly Color SprayLow = new Color(0.60f, 0.92f, 0.88f, 0.25f), SprayHigh = new Color(0.92f, 1f, 0.98f, 0.29f);
        private static readonly Color CutAir = new Color(0.45f, 0.66f, 0.64f, 0.28f), FrostGlint = new Color(0.72f, 0.94f, 0.92f, 0.9f);
        private static readonly Color SoundWave = new Color(0.50f, 0.95f, 0.88f, 0.42f), HelLight = new Color(0.30f, 0.85f, 0.78f);
        private static readonly Color BloodLow = new Color(0.30f, 0.03f, 0.05f, 0.40f), BloodHigh = new Color(0.45f, 0.08f, 0.10f, 0.45f);

        /// <summary>The pounce's take-off: a ring of dust from its feet.</summary>
        public static void Kick(Vector3 ground, float size) => BatFiendFxParts.Burst(DustRing, ground, 0.2f * size);

        /// <summary>The pounce's landing on its claws: dust, a puff thrown up and grit.</summary>
        public static void Land(Vector3 ground, float size) => BatFiendFxParts.Burst(HeavyDust, ground, 0.25f * size);

        /// <summary>The bite's lunge: a small kick from the hind feet, then the hands planting.</summary>
        public static void Step(Vector3 ground, float size) => BatFiendFxParts.Burst(DustRing, ground, 0.15f * size);

        /// <summary>The gravewind's downbeat: the wings drive the dust out from under it.</summary>
        public static void Downdraft(Vector3 ground, float size) => BatFiendFxParts.Burst(DustRing, ground, 0.2f * size);

        /// <summary>
        /// Wing Guillotine: a crescent of cold air 2.4 m out over the wing's 100 degree arc (the swipe's arc runs from its
        /// left round to the front, so <paramref name="facing"/> turns it onto the hit's arc), with frost glints falling off it.
        /// </summary>
        public static void Swipe(Transform maker, Vector3 at, Quaternion facing, float size)
        {
            GameObject? clone = BatFiendFxParts.Free(Crescent, maker, at, facing, 0.7f * size);
            if (clone == null)
            {
                return;
            }
            BatFiendFxParts.Arc(clone, 100f);
            BatFiendFxParts.Speed(clone, Crescent, 1.5f, 3.5f);   // glints flung a little past the reach, not metres
            BatFiendFxParts.Speed(clone, "clouds", 0.3f, 0.8f);   // the cut air hangs on the arc
            BatFiendFxParts.Tint(clone, Crescent, FrostGlint, FrostGlint);
            BatFiendFxParts.Tint(clone, "clouds", CutAir, CutAir);
        }

        /// <summary>
        /// Gravewind Blast: one beat of grave-cold mist, frost flakes and spray, a 30 degree fan about a metre up that
        /// rolls out to 6 m and settles, lit cold blue-green; the spray leaves a few frost marks where it strikes the ground.
        /// </summary>
        public static void Gust(Transform maker, Vector3 at, Quaternion facing, float size)
        {
            GameObject? clone = BatFiendFxParts.Free(Breath, maker, at, facing, 0.2f * size);
            if (clone == null)
            {
                return;
            }
            BatFiendFxParts.Shorten(clone, 0.3f);
            BatFiendFxParts.Widen(clone, 12f);
            BatFiendFxParts.Speed(clone, "clouds", 1.5f, 6f);
            BatFiendFxParts.Speed(clone, "pixel flakes", 2f, 7f);
            BatFiendFxParts.Speed(clone, "spray", 4f, 6.5f);
            BatFiendFxParts.Tint(clone, "clouds", MistLow, MistHigh);
            BatFiendFxParts.Tint(clone, "spray", SprayLow, SprayHigh);
            BatFiendFxParts.Glow(clone, HelLight);
        }

        /// <summary>Shattering Screech's onset: the air round its jaws ripples (distortion only, none of the Fader's spores).</summary>
        public static void Ripple(Transform maker, Vector3 mouth, float size)
        {
            GameObject? clone = BatFiendFxParts.Free(Roar, maker, mouth, Quaternion.identity, 0.25f * size);
            if (clone != null)
            {
                BatFiendFxParts.Off(clone, "Spores");
            }
        }

        /// <summary>
        /// A wavefront of the screech racing 8 m down its narrow beam in half a second, 1.2 m across at the jaws and 2.4 m
        /// at the end (Eikthyr's forward shockwave without its lightning, light and sound).
        /// </summary>
        public static void Wave(Transform maker, Vector3 from, Quaternion facing, float size)
        {
            GameObject? clone = BatFiendFxParts.Free(Shockwave, maker, from, facing, 0.4f * size);
            if (clone == null)
            {
                return;
            }
            BatFiendFxParts.Off(clone, "lightning", "lightning (1)");
            BatFiendFxParts.Dark(clone);
            BatFiendFxParts.Recentre(clone, "shockwave_plane");
            BatFiendFxParts.Speed(clone, "shockwave_plane", 16f, 16f);
            BatFiendFxParts.Tint(clone, "shockwave_plane", SoundWave, SoundWave);
        }

        /// <summary>
        /// The bite healing it: a dark red wisp drawn from its jaws back into its body, held to the Bat-fiend so the wisp
        /// goes with it as it pulls away (the crossbow's blood smoke and its faint ring; none of its flying drops).
        /// </summary>
        public static void Drain(Transform maker, Vector3 mouth, Quaternion inward, float size)
        {
            GameObject? clone = BatFiendFxParts.Held(Blood, maker, mouth, inward, 0.4f * size);
            if (clone == null)
            {
                return;
            }
            BatFiendFxParts.Off(clone, "spikes");
            BatFiendFxParts.Speed(clone, "smoke", 0.3f * size, 1.4f * size);
            BatFiendFxParts.Tint(clone, "smoke", BloodLow, BloodHigh);
        }
    }
}
