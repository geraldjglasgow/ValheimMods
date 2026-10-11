namespace EliteCreaturesReborn.Mutations
{
    /// <summary>
    /// Flamebound's trail: patches of burning ground where it walks, drawn as flames (<see cref="PatchFlames"/>), each
    /// lasting 7 seconds. A player standing in one catches fire (<see cref="BurnFooting"/>). The shared ground-trail
    /// mechanism (<see cref="GroundTrailField"/>) does the rest.
    /// </summary>
    public sealed class FireTrail : GroundTrailField
    {
        private protected override TrailKind Kind => TrailKind.Fire;
    }
}
