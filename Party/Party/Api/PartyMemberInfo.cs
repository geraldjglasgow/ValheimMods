namespace Party.Api
{
    /// <summary>One party member, as the public API reports it.</summary>
    public sealed class PartyMemberInfo
    {
        public long Id { get; set; }
        public string Name { get; set; } = "";
        public bool Online { get; set; }
        public bool IsLeader { get; set; }
    }
}
