using System.Collections.Generic;

namespace OpenKeep.Capacity
{
    /// <summary>
    /// The container prefabs of the scene: every ZNetScene prefab with a Container component on it or on a child
    /// (ships and carts keep their storage on a child), by prefab name. Found in the scene's one shared walk
    /// (<see cref="ScenePrefabs"/>), shared and read-only.
    /// </summary>
    public static class ContainerPrefabs
    {
        public static IReadOnlyDictionary<string, Container> All() => ScenePrefabs.Containers;
    }
}
