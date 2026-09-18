namespace HaloMenu.API
{
    /// <summary>
    /// Implemented by the HaloMenu plugin and handed to <see cref="HaloMenuAPI.Provide"/> in its Awake. Dependent
    /// mods never touch this type directly; it exists so <see cref="HaloMenuAPI"/> can route calls to the plugin
    /// without this assembly ever referencing it.
    /// </summary>
    public abstract class HaloMenuService
    {
        public abstract void Register(RingEntry entry);

        public abstract Ring CreateRing(string ringId);
    }
}
