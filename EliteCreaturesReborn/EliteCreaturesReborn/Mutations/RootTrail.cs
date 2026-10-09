namespace EliteCreaturesReborn.Mutations
{
    /// <summary>
    /// Binding's trail: patches of tangled roots where it walks, smaller and further apart than the other trails, so a
    /// player can step between them, each lasting 10 seconds. A player stepping into one is held fast for a second
    /// (<see cref="RootFooting"/>). The shared ground-trail mechanism (<see cref="GroundTrailField"/>) does the rest.
    /// </summary>
    public sealed class RootTrail : GroundTrailField
    {
        private protected override TrailKind Kind => TrailKind.Roots;
    }
}
