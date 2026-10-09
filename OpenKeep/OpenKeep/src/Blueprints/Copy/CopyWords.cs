using OpenKeep.Core;

namespace OpenKeep.Blueprints.Copy
{
    /// <summary>
    /// The Copy building tool's words (keys "ok_copy_*"; the entry's own name and description, ok_copy and ok_copy_desc,
    /// are in <see cref="BlueprintWords"/>). Texts with numbers carry $1, $2 placeholders that
    /// <see cref="BlueprintWords.Format"/> fills.
    /// </summary>
    public static class CopyWords
    {
        public static string Title { get; private set; }
        public static string TitleOne { get; private set; }
        public static string TitleEmpty { get; private set; }
        public static string AimHint { get; private set; }
        public static string Aiming { get; private set; }
        public static string TakeBuilding { get; private set; }
        public static string TakeRest { get; private set; }
        public static string DropBuilding { get; private set; }
        public static string TakePiece { get; private set; }
        public static string DropPiece { get; private set; }
        public static string TakeGroup { get; private set; }
        public static string DropGroup { get; private set; }
        public static string TakeType { get; private set; }
        public static string DropType { get; private set; }
        public static string Keys { get; private set; }
        public static string Added { get; private set; }
        public static string Removed { get; private set; }
        public static string Cleared { get; private set; }
        public static string NameTopic { get; private set; }
        public static string DefaultName { get; private set; }
        public static string Saved { get; private set; }
        public static string Refused { get; private set; }
        public static string Gone { get; private set; }

        public static void Register()
        {
            Title = Language.Add("ok_copy_title", "Selected: $1 pieces in $2 buildings, footprint $3 x $4 m");
            TitleOne = Language.Add("ok_copy_title_one", "Selected: $1 pieces in one building, footprint $2 x $3 m");
            TitleEmpty = Language.Add("ok_copy_title_empty", "Nothing selected yet");
            AimHint = Language.Add("ok_copy_aim", "Aim at a building to copy it");
            Aiming = Language.Add("ok_copy_aiming", "Aiming at: $1");
            Keys = Language.Add("ok_copy_keys", "<color=yellow>Click</color> one piece  <color=yellow>Shift + click</color> its whole building  " +
                "<color=yellow>G + click</color> its joined pieces of one type  " +
                "<color=yellow>Shift + G + click</color> every piece of that type in the building  <color=yellow>Enter</color> save as a blueprint  " +
                "<color=yellow>Backspace</color> clear");
            AddActions();
            AddMessages();
        }

        private static void AddActions()
        {
            TakeBuilding = Language.Add("ok_copy_take_building", "Shift + click selects its building: $1 pieces");
            TakeRest = Language.Add("ok_copy_take_rest", "Shift + click selects the rest of its building: $1 more of $2 pieces");
            DropBuilding = Language.Add("ok_copy_drop_building", "Shift + click lets its building go: $1 pieces");
            TakePiece = Language.Add("ok_copy_take_piece", "click selects this piece (hold Shift for its building)");
            DropPiece = Language.Add("ok_copy_drop_piece", "click lets this piece go");
            TakeGroup = Language.Add("ok_copy_take_group", "G + click selects its $1 joined pieces of this type");
            DropGroup = Language.Add("ok_copy_drop_group", "G + click lets its $1 joined pieces of this type go");
            TakeType = Language.Add("ok_copy_take_type", "Shift + G + click selects all $1 pieces of this type in its building");
            DropType = Language.Add("ok_copy_drop_type", "Shift + G + click lets all $1 pieces of this type in its building go");
        }

        private static void AddMessages()
        {
            Added = Language.Add("ok_copy_added", "$1 pieces selected, $2 in all");
            Removed = Language.Add("ok_copy_removed", "$1 pieces let go, $2 left");
            Cleared = Language.Add("ok_copy_cleared", "Selection cleared");
            NameTopic = Language.Add("ok_copy_name", "Save the selection as the blueprint");
            DefaultName = Language.Add("ok_copy_default", "Building $1");
            Saved = Language.Add("ok_copy_saved", "Saved $1: $2 pieces");
            Refused = Language.Add("ok_copy_refused", "Not saved: $1");
            Gone = Language.Add("ok_copy_gone", "The selected pieces are gone: select them again");
        }
    }
}
