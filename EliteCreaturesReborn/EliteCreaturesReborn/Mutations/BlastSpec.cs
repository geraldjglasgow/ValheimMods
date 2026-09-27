namespace EliteCreaturesReborn.Mutations
{
    /// <summary>
    /// What one Bloated blast is: its damage before the star scaling, its radius, the dead creature's stars, and the
    /// vanilla prefabs it draws and plays. Read from the dead creature's rules on its owner and carried unchanged in both
    /// Bloated broadcasts, so every client draws and hears the same blast.
    /// </summary>
    public readonly struct BlastSpec
    {
        public readonly float Damage;
        public readonly float Radius;
        public readonly int Stars;
        public readonly string Effect;
        public readonly string Sound;

        public BlastSpec(float damage, float radius, int stars, string effect, string sound)
        {
            Damage = damage;
            Radius = radius;
            Stars = stars;
            Effect = effect ?? "";
            Sound = sound ?? "";
        }

        public void Write(ZPackage pkg)
        {
            pkg.Write(Damage);
            pkg.Write(Radius);
            pkg.Write(Stars);
            pkg.Write(Effect);
            pkg.Write(Sound);
        }

        public static BlastSpec Read(ZPackage pkg) =>
            new BlastSpec(pkg.ReadSingle(), pkg.ReadSingle(), pkg.ReadInt(), pkg.ReadString(), pkg.ReadString());
    }
}
