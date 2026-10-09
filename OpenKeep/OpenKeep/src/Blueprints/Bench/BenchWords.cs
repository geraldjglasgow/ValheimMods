using OpenKeep.Core;

namespace OpenKeep.Blueprints.Bench
{
    /// <summary>The Blueprint Bench's words (ok_bench_*). Texts with values carry $1, $2 for <see cref="BlueprintWords.Format"/>.</summary>
    public static class BenchWords
    {
        public static string Name { get; private set; }
        public static string Description { get; private set; }
        public static string Yours { get; private set; }
        public static string Pool { get; private set; }
        public static string Players { get; private set; }
        public static string Share { get; private set; }
        public static string ShareAll { get; private set; }
        public static string Take { get; private set; }
        public static string Rename { get; private set; }
        public static string Delete { get; private set; }
        public static string Remove { get; private set; }
        public static string Close { get; private set; }
        public static string You { get; private set; }
        public static string Asking { get; private set; }
        public static string Empty { get; private set; }
        public static string Hint { get; private set; }
        public static string Sharing { get; private set; }
        public static string Taking { get; private set; }
        public static string DeleteAsk { get; private set; }
        public static string RemoveAsk { get; private set; }
        public static string Deleted { get; private set; }
        public static string OnlyOwn { get; private set; }
        public static string NothingToShare { get; private set; }
        public static string Shared { get; private set; }
        public static string MoreRefused { get; private set; }
        public static string Took { get; private set; }
        public static string NotTaken { get; private set; }
        public static string TooBig { get; private set; }
        public static string NotBlueprint { get; private set; }
        public static string Full { get; private set; }
        public static string NotYours { get; private set; }
        public static string Gone { get; private set; }
        public static string Removed { get; private set; }
        public static string Unknown { get; private set; }
        public static string Failed { get; private set; }
        public static string NoAnswer { get; private set; }

        public static void Register()
        {
            Name = Language.Add("ok_bench", "Blueprint Bench");
            Description = Language.Add("ok_bench_desc", "Share blueprints with everyone in this world: put yours under your name, " +
                "take a copy of anyone's into your hammer's Blueprints tab.");
            Yours = Language.Add("ok_bench_yours", "Your blueprints");
            Pool = Language.Add("ok_bench_pool", "Shared at the bench");
            Players = Language.Add("ok_bench_players", "Everyone who shared");
            Share = Language.Add("ok_bench_share", "Share");
            ShareAll = Language.Add("ok_bench_shareall", "Share all");
            Take = Language.Add("ok_bench_take", "Take a copy");
            Rename = Language.Add("ok_bench_rename", "Rename");
            Delete = Language.Add("ok_bench_delete", "Delete");
            Remove = Language.Add("ok_bench_remove", "Remove");
            Close = Language.Add("ok_bench_close", "Close");
            You = Language.Add("ok_bench_you", "(you)");
            AddLists();
            AddResults();
        }

        private static void AddLists()
        {
            Asking = Language.Add("ok_bench_asking", "Asking the server...");
            Empty = Language.Add("ok_bench_empty", "Empty");
            Hint = Language.Add("ok_bench_hint", "Drag blueprints and folders across to share them or take a copy, onto a folder to move them. " +
                "Double click opens, right click or F2 renames, Ctrl / Shift + click picks several.");
            Sharing = Language.Add("ok_bench_sharing", "Sharing: $1 to go");
            Taking = Language.Add("ok_bench_taking", "Taking copies: $1 to go");
            DeleteAsk = Language.Add("ok_bench_deleteask", "Delete $1 from your blueprints? Folders go with everything in them.");
            RemoveAsk = Language.Add("ok_bench_removeask", "Remove $1 from the bench? Folders go with everything in them.");
            Deleted = Language.Add("ok_bench_deleted", "Deleted $1");
            OnlyOwn = Language.Add("ok_bench_onlyown", "Blueprints are shared into your own part of the bench");
            NothingToShare = Language.Add("ok_bench_nothing", "No blueprints in what you picked");
        }

        private static void AddResults()
        {
            Shared = Language.Add("ok_bench_shared", "Shared $1 blueprints at the bench");
            MoreRefused = Language.Add("ok_bench_morerefused", "$1 more not shared");
            Took = Language.Add("ok_bench_took", "Took $1 blueprints into $2");
            NotTaken = Language.Add("ok_bench_nottaken", "$1 blueprints could not be taken");
            TooBig = Language.Add("ok_bench_toobig", "$1 is too big to share");
            NotBlueprint = Language.Add("ok_bench_notblueprint", "$1 is not a readable blueprint");
            Full = Language.Add("ok_bench_full", "$1 not shared: a player can share at most " + BenchLimits.MaxPerPlayer + " blueprints");
            NotYours = Language.Add("ok_bench_notyours", "Only the player who shared it or an admin can change $1");
            Gone = Language.Add("ok_bench_gone", "$1 is no longer shared");
            Removed = Language.Add("ok_bench_removed", "Removed $1 from the bench ($2 blueprints)");
            Unknown = Language.Add("ok_bench_unknown", "$1 not shared: the server does not know your character yet");
            Failed = Language.Add("ok_bench_failed", "$1: the server could not change its files");
            NoAnswer = Language.Add("ok_bench_noanswer", "The server did not answer (it needs OpenKeep with blueprints on)");
        }
    }
}
