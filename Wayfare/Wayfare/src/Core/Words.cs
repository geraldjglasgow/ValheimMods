namespace Wayfare.Core
{
    /// <summary>The keys of the portal words, whose English is in <c>translations/English.yml</c> and whose text comes
    /// from <see cref="Translations.Lang"/> where it is shown. Keys, never text: a refusal travels as its key (the
    /// server's to the asking client as a <see cref="Translations.LangWire"/> message), so each player reads it in their
    /// own language. The sea gate words are in <see cref="SeaGates.SeaGateWords"/>.</summary>
    public static class Words
    {
        public const string ModePublic = "wf_mode_public";
        public const string ModePrivate = "wf_mode_private";
        public const string ModeAdmin = "wf_mode_admin";

        public const string HoverCycle = "wf_hover_cycle";
        public const string NotOwner = "wf_notowner";

        public const string DeniedPrivate = "wf_denied_private";
        public const string DeniedAdmin = "wf_denied_admin";
        public const string DeniedBlocked = "wf_denied_blocked";
        public const string DeniedGeneric = "wf_denied_generic";

        public const string Favourited = "wf_favourited";
        public const string Unfavourited = "wf_unfavourited";
        public const string UnnamedPortal = "wf_unnamed_portal";

        public const string YouAreHere = "wf_you_are_here";

        public const string PickerTopic = "wf_picker_topic";
        public const string PickerGo = "wf_picker_go";
        public const string PickerNone = "wf_picker_none";
        public const string PickerUnnamed = "wf_picker_unnamed";
        public const string DistanceMetres = "wf_distance_m";
        public const string DistanceKilometres = "wf_distance_km";
    }
}
