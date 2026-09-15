using UnityEngine;

namespace EliteCreaturesReborn.Rules
{
    /// <summary>
    /// The six per-star lines - growth, hp, attack, swing speed, speed, drops - each one entry per star count. A plain
    /// data holder: the parser fills the arrays, the scaling reads them. An index beyond an array's length reuses its
    /// last entry, so a creature above the tabulated ceiling keeps the top row's values.
    /// </summary>
    public sealed class StarPower
    {
        public float[] Growth = { 0f };
        public float[] Hp = { 1f };
        public float[] Attack = { 1f };
        public float[] SwingSpeed = { 1f };
        public float[] Speed = { 1f };
        public float[] Drops = { 1f };

        public float GrowthAt(int stars) => At(Growth, stars);
        public float HpAt(int stars) => At(Hp, stars);
        public float AttackAt(int stars) => At(Attack, stars);
        public float SwingSpeedAt(int stars) => At(SwingSpeed, stars);
        public float SpeedAt(int stars) => At(Speed, stars);
        public float DropsAt(int stars) => At(Drops, stars);

        private static float At(float[] line, int stars)
        {
            if (line == null || line.Length == 0)
            {
                return 1f;
            }
            int index = Mathf.Clamp(stars, 0, line.Length - 1);
            return line[index];
        }

        public StarPower Clone()
        {
            return new StarPower
            {
                Growth = (float[])Growth.Clone(), Hp = (float[])Hp.Clone(), Attack = (float[])Attack.Clone(),
                SwingSpeed = (float[])SwingSpeed.Clone(), Speed = (float[])Speed.Clone(), Drops = (float[])Drops.Clone(),
            };
        }
    }
}
