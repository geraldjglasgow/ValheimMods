namespace EliteCreaturesReborn.Mutations
{
    /// <summary>
    /// Mudbound's trail: patches of thick, dark, wet mud where it walks, each lasting `trail life` seconds. A player in
    /// one moves `slow` percent slower, and for a moment after stepping out (the "Deep mud" status). The ground itself
    /// is never changed. The shared ground-trail mechanism (<see cref="GroundTrailField"/>) does the rest.
    /// </summary>
    public sealed class MudTrail : GroundTrailField
    {
        private protected override TrailKind Kind => TrailKind.Mud;
    }
}
