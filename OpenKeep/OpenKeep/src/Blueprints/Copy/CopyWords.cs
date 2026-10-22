namespace OpenKeep.Blueprints.Copy
{
    /// <summary>
    /// The keys of the Copy building tool's words (<c>ok_copy_*</c>; the entry's own name and description, ok_copy and
    /// ok_copy_desc, are in <see cref="BlueprintWords"/>). Their English is in <c>translations/English.blueprints.yml</c>;
    /// texts with values carry $1, $2 placeholders for <c>Lang.Format</c>.
    /// </summary>
    public static class CopyWords
    {
        public const string Title = "ok_copy_title";
        public const string TitleOne = "ok_copy_title_one";
        public const string TitleEmpty = "ok_copy_title_empty";
        public const string AimHint = "ok_copy_aim";
        public const string Aiming = "ok_copy_aiming";
        public const string Keys = "ok_copy_keys";

        public const string TakeBuilding = "ok_copy_take_building";
        public const string TakeRest = "ok_copy_take_rest";
        public const string DropBuilding = "ok_copy_drop_building";
        public const string TakePiece = "ok_copy_take_piece";
        public const string DropPiece = "ok_copy_drop_piece";
        public const string TakeGroup = "ok_copy_take_group";
        public const string DropGroup = "ok_copy_drop_group";
        public const string TakeType = "ok_copy_take_type";
        public const string DropType = "ok_copy_drop_type";

        public const string Added = "ok_copy_added";
        public const string Removed = "ok_copy_removed";
        public const string Cleared = "ok_copy_cleared";
        public const string NameTopic = "ok_copy_name";
        public const string DefaultName = "ok_copy_default";
        public const string Saved = "ok_copy_saved";
        public const string Refused = "ok_copy_refused";
        public const string Gone = "ok_copy_gone";
    }
}
