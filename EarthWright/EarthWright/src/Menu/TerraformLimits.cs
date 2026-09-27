using EarthWright.Terrain;

namespace EarthWright.Menu
{
    /// <summary>
    /// The admin-only Terraform entry levels past the height limits when "Terraform Ignores Height Limits" is on. Its
    /// edits are already privileged (the server checks the sender is an admin); this adds the flag the owner honours
    /// only on such a server-approved edit, so a non-admin cannot use it to break the limits.
    /// </summary>
    public static class TerraformLimits
    {
        public const string EntryId = "ew_terraform";

        public static void Register() => EditEvents.Building += Amend;

        private static void Amend(TerrainEdit edit)
        {
            if (edit != null && edit.Source == EntryId && edit.Has(EditFlags.Privileged) && MenuSettings.TerraformIgnoresLimits.Value)
                edit.Flags |= EditFlags.IgnoreLimits;
        }
    }
}
