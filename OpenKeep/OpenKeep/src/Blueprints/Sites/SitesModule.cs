using SyncedConfig;

namespace OpenKeep.Blueprints.Sites
{
    /// <summary>
    /// Entry point of the construction sites (SPEC-Blueprints.md, section 1): ghost buildings that stay until they are
    /// built or taken down. Binds "Build As Resources Come In", registers the words, the post's RPCs (through
    /// <see cref="SiteHooks.MarkerCreated"/>) and the per-frame ghosts and builder. The post's prefab
    /// (<see cref="SitePrefab"/>) and the routed RPCs (<see cref="SiteNetwork"/>) come in through their own patch classes.
    /// </summary>
    public static class SitesModule
    {
        public static void Initialize(SyncedConfiguration synced)
        {
            SiteSettings.Bind(synced);
            SiteWords.Register();
            SiteHooks.MarkerCreated += SiteDelivery.Register;
            SiteHooks.MarkerCreated += SiteTakeDown.Register;
            SiteHooks.OnUpdate("OpenKeep site ghosts", SiteGhost.TickAll);
            SiteHooks.OnUpdate("OpenKeep site builder", SiteBuilder.Tick);
        }
    }
}
