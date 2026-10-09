namespace EliteCreaturesReborn.Mutations
{
    /// <summary>
    /// Flamebound's trail: patches of burning ground where it walks, glowing embers with flames over them
    /// (<see cref="PatchFlames"/>), each lasting 10 seconds like Frostbound's ice and Mudbound's mud. A player standing in
    /// one catches fire (<see cref="BurnFooting"/>). The shared ground-trail mechanism (<see cref="GroundTrailField"/>)
    /// does the rest.
    /// </summary>
    public sealed class FireTrail : GroundTrailField
    {
        private protected override TrailKind Kind => TrailKind.Fire;
    }
}
