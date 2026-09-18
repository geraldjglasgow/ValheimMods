namespace Wayfare.Core
{
    /// <summary>Every localisation token Wayfare defines. Touch <see cref="Touch"/> once, early, so the static
    /// initializers register every word with <see cref="Language"/> before the language is first set up.</summary>
    public static class Words
    {
        public static readonly string ModePublic = Language.Add("wf_mode_public", "Public");
        public static readonly string ModePrivate = Language.Add("wf_mode_private", "Private");
        public static readonly string ModeAdmin = Language.Add("wf_mode_admin", "Admin");

        public static readonly string HoverCycle = Language.Add("wf_hover_cycle", "Cycle access mode: {0}");

        public static readonly string ModeSet = Language.Add("wf_mode_set", "Portal access set to {0}");
        public static readonly string NotOwner = Language.Add("wf_notowner", "You don't own this portal");

        public static readonly string DeniedPrivate = Language.Add("wf_denied_private", "Only the portal's owner may target it");
        public static readonly string DeniedAdmin = Language.Add("wf_denied_admin", "Only a server admin may target this portal");
        public static readonly string DeniedBlocked = Language.Add("wf_denied_blocked", "Portal travel is blocked here");
        public static readonly string DeniedGeneric = Language.Add("wf_denied_generic", "You may not target that portal");

        public static readonly string Favourited = Language.Add("wf_favourited", "Added to favourites");
        public static readonly string Unfavourited = Language.Add("wf_unfavourited", "Removed from favourites");

        public static readonly string TargetingHint = Language.Add("wf_targeting_hint", "Click a portal to travel there");

        public static void Touch()
        {
            // Referencing any field above is enough to run the static initializers.
            _ = ModePublic;
        }
    }
}
