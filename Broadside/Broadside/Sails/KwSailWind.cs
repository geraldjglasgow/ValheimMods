using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// The air a sail stands in: where the wind blows to and how hard (the game's own wind, as its sails take it), which
    /// way the sail faces (level), and its speed.
    /// </summary>
    public readonly struct KwSailAir
    {
        public KwSailAir(Vector3 wind, float strength, Vector3 forward, Vector3 left, Vector3 speed)
        {
            (Wind, Strength, Speed) = (Level(wind), Mathf.Clamp01(strength), speed);
            (Forward, Left) = (Level(forward), Level(left));
        }

        public Vector3 Wind { get; }
        public float Strength { get; }

        /// <summary>The way a square sail bellies, or a sail along the ship's forward (from its boom's end to its mast).</summary>
        public Vector3 Forward { get; }

        /// <summary>The cloth's left: the way a sail along the ship bellies, and across a square sail.</summary>
        public Vector3 Left { get; }

        public Vector3 Speed { get; }

        /// <summary>The air at a sail's cloth now: the game's wind (none before there is one), the cloth's facing and its speed.</summary>
        public static KwSailAir Here(Transform cloth, Vector3 speed)
        {
            EnvMan? env = EnvMan.instance;
            return new KwSailAir(env != null ? env.GetWindDir() : Vector3.zero, env != null ? env.GetWindIntensity() : 0f, cloth.forward, -cloth.right, speed);
        }

        private static Vector3 Level(Vector3 v) => new Vector3(v.x, 0f, v.z).normalized;
    }

    /// <summary>
    /// How the wind fills one sail, eased from moment to moment: how full it is (signed: a sail along the ship bellies to
    /// either side, a square sail backs a little against its mast), how hard it luffs (flaps loose: with the wind from
    /// ahead or along the cloth, or the sail reefed to half), how windy it is, which way its billows run across it, and
    /// the clocks of its billows, breathing and flapping. A sail fills deeper the harder the game's wind blows and the more
    /// it comes from behind, and in a strong wind it blows out past its drawn fullness (<see cref="Gale"/>: up to 1.8 times
    /// as far from slack at the game's strongest wind, which bellies the cloth about 1.7 times as deep, growing smoothly
    /// from nothing extra in a light wind); a square sail is backed against its mast no further than before. It fills
    /// quickly as a gust comes and eases slowly as it goes; a ship running before the wind takes a little of it away. Each
    /// sail's clocks start at a point of their own, so sails side by side do not move together.
    /// </summary>
    public sealed class KwSailWind
    {
        private const float Tau = 2f * Mathf.PI;

        /// <summary>How much further than its drawn fullness the strongest wind blows a sail out (0.8: 1.8 times as far from slack).</summary>
        public const float Gale = 0.8f;

        private float billowA, billowB, breath, flapA, flapB;
        private int side = 1;
        private bool settled;

        /// <summary>A sail's wind, its clocks started from seed (0 to 1); the first step takes the wind as it is, unhurried.</summary>
        public KwSailWind(float seed)
        {
            (billowA, billowB, breath) = (seed * Tau, seed * 7.3f % Tau, seed * 3.1f % Tau);
            (flapA, flapB) = (seed * 5.7f % Tau, seed * 11.9f % Tau);
        }

        public float Fill { get; private set; }
        public float Luff { get; private set; }
        public float Windy { get; private set; }
        public float Way { get; private set; }

        /// <summary>One step of the wind on a sail at hoist f, dt seconds after the last.</summary>
        public void Feel(in KwSailAir air, bool square, float f, float dt)
        {
            float step = settled ? dt : 1000f;
            settled = true;
            (float drive, float edgeOn) = square ? Square(air) : Along(air);
            float eased = 1f - (1f - air.Strength) * (1f - air.Strength), strength = 0.3f + 0.7f * eased;
            float blown = drive > 0f || !square ? 1f + Gale * eased * eased : 1f;
            float target = drive * strength * blown * (1f - 0.25f * Mathf.Clamp01(Vector3.Dot(air.Speed, air.Wind) / 10f));
            bool filling = Mathf.Abs(target) > Mathf.Abs(Fill) && target * Fill >= 0f;
            Fill = Mathf.MoveTowards(Fill, target, step * (filling ? 0.9f : 0.4f));
            Windy = Mathf.MoveTowards(Windy, Mathf.Clamp01(air.Strength * 1.5f), step * 0.6f);
            float luff = edgeOn * (0.3f + 0.7f * Windy) * Mathf.Clamp01(2f * f) + 2.4f * f * (1f - f) * (0.4f + 0.6f * Windy);
            Luff = Mathf.MoveTowards(Luff, Mathf.Clamp01(luff), step * 1.2f);
            Way = Mathf.MoveTowards(Way, square ? Mathf.Clamp(Vector3.Dot(air.Wind, air.Left) * 2.5f, -1f, 1f) : 1f, step * 0.5f);
            Advance(dt);
        }

        // A square sail: full with the wind behind it, still drawing with it abeam, emptying and luffing as it comes ahead,
        // backed a little (it lies against the mast) with it right ahead, flapping still.
        private static (float Drive, float EdgeOn) Square(in KwSailAir air)
        {
            float c = Vector3.Dot(air.Wind, air.Forward);
            if (c >= 0f)
                return (0.6f + 0.4f * c, 0f);
            if (c > -0.45f)
                return (0.6f * (1f + c / 0.45f), -c / 0.45f);
            float back = (-c - 0.45f) / 0.55f;
            return (-0.2f * back, 1f - 0.4f * back);
        }

        // A sail along the ship: full to the side the wind pushes it (kept on that side until the wind is well over the
        // other way), less full running before it, empty and luffing head to wind.
        private (float Drive, float EdgeOn) Along(in KwSailAir air)
        {
            float across = Vector3.Dot(air.Wind, air.Left), along = Vector3.Dot(air.Wind, air.Forward);
            if (Mathf.Abs(across) > 0.12f)
                side = across > 0f ? 1 : -1;
            float drive = Mathf.Clamp01(Mathf.Abs(across) * 1.25f + 0.45f * Mathf.Max(0f, along));
            return (side * drive, 1f - Mathf.Clamp01(drive / 0.5f));
        }

        // The clocks: the billows a quarter of a beat a second or so, the flapping one to two beats, quicker the harder
        // the sail luffs.
        private void Advance(float dt)
        {
            float billow = Tau * (0.16f + 0.2f * Windy) * dt, flap = Tau * (0.8f + 1.4f * Windy) * (0.7f + 0.3f * Luff) * dt;
            billowA = (billowA + billow) % Tau;
            billowB = (billowB + billow * 1.43f) % Tau;
            breath = (breath + Tau * (0.09f + 0.05f * Windy) * dt) % Tau;
            flapA = (flapA + flap) % Tau;
            flapB = (flapB + flap * 1.55f) % Tau;
        }

        /// <summary>The cloth of a sail at hoist f this moment, leaning by swing (<see cref="KwSailSwing"/>); a sail set
        /// higher on its mast billows and flaps a little more (<see cref="KwSailLayout.Aloft"/>).</summary>
        public KwSailNow Now(KwSailLayout s, float f, Vector3 swing)
        {
            float size = Mathf.Max(s.Width, f * s.Height, 1f), aloft = s.Aloft;
            return new KwSailNow
            {
                Fill = Fill,
                Billow = aloft * size * 0.035f * (0.2f + 0.8f * Windy) * (1f - 0.3f * Mathf.Min(Mathf.Abs(Fill), 1f)),
                Breath = size * 0.012f * Windy * Mathf.Sin(breath),
                Flap = aloft * Mathf.Min(size * 0.065f, 0.9f) * Luff * (0.35f + 0.65f * Windy),
                K1 = Tau / (1.1f * size), K2 = Tau / (0.75f * size), Kf = Tau / (0.6f * size),
                Way = Way, Spill = s.Square ? 1f - Mathf.Abs(Way) : 0f,
                PhaseA = billowA, PhaseB = billowB, FlapA = flapA, FlapB = flapB,
                Normal = s.Square ? Vector3.forward : Vector3.left,
                Swing = swing,
            };
        }
    }
}
