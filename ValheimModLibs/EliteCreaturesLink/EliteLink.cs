using System;

namespace EliteCreaturesLink
{
    /// <summary>
    /// Elite Creatures Reborn's API, its shape (api.md section 1): whether it is there and which version. The endpoints
    /// are in <see cref="EliteTraits"/> (mutation and aspect names, and a prefab's mutations, aspects, portal attacks
    /// and summons). Every wrapper does nothing and answers false, null or an empty array while Elite Creatures Reborn is
    /// absent, older than API version 1, or lacks the endpoint.
    /// <para>
    /// Register only once Elite Creatures Reborn has loaded: give the plugin
    /// <c>[BepInDependency(EliteLink.Guid, BepInDependency.DependencyFlags.SoftDependency)]</c>. Registrations are code:
    /// every peer runs the same mods, so register the same things on every peer.
    /// </para>
    /// </summary>
    public static class EliteLink
    {
        /// <summary>Elite Creatures Reborn's plugin GUID, for <c>BepInDependency</c>.</summary>
        public const string Guid = ApiBinding.Guid;

        private static readonly Endpoint<Func<string?>> pluginVersion = new Endpoint<Func<string?>>("GetPluginVersion");
        private static readonly Endpoint<Func<string, bool>> hasEndpoint = new Endpoint<Func<string, bool>>("HasEndpoint");
        private static readonly Endpoint<Func<string[]?>> endpointNames = new Endpoint<Func<string[]?>>("GetEndpointNames");

        /// <summary>Elite Creatures Reborn is loaded with API version 1 or later.</summary>
        public static bool Present => ApiBinding.Api != null;

        /// <summary>Elite Creatures Reborn's API version (0 when absent).</summary>
        public static int ApiVersion => ApiBinding.Version;

        /// <summary>Elite Creatures Reborn's plugin version, or null.</summary>
        public static string? PluginVersion => Safe.Call(pluginVersion.Call, null);

        /// <summary>Whether Elite Creatures Reborn has an endpoint of this name (a newer one than this library knows, too).</summary>
        public static bool HasEndpoint(string name) => Safe.Call(hasEndpoint.Call, name, false);

        /// <summary>Every endpoint name Elite Creatures Reborn has; empty when absent.</summary>
        public static string[] EndpointNames => Safe.Call(endpointNames.Call, null) ?? Array.Empty<string>();
    }
}
