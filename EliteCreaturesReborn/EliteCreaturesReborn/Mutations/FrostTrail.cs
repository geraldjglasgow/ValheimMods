namespace EliteCreaturesReborn.Mutations
{
    /// <summary>
    /// Frostbound's trail: patches of pale, glossy ice where it walks, each lasting `trail life` seconds. A player on
    /// one keeps only `grip` percent of their footing, so they slide, turn and stop slowly, and moves `slow` percent
    /// slower (<see cref="IceFooting"/>, the "Slick ice" status). The shared ground-trail mechanism
    /// (<see cref="GroundTrailField"/>) does the rest; the aura is <see cref="FrostAura"/>'s.
    /// </summary>
    public sealed class FrostTrail : GroundTrailField
    {
        private protected override TrailKind Kind => TrailKind.Frost;
    }
}
