namespace OpenKeep.Blueprints.Bench
{
    /// <summary>
    /// The keys of the Blueprint Bench's words (<c>ok_bench_*</c>). Their English is in
    /// <c>translations/English.blueprints.yml</c>; texts with values carry $1, $2 for <c>Lang.Format</c>. A word the
    /// server answers with travels as its token and its values (<see cref="BenchRpc.Tell"/>), read in the player's language.
    /// </summary>
    public static class BenchWords
    {
        public const string Name = "ok_bench";
        public const string Description = "ok_bench_desc";
        public const string Yours = "ok_bench_yours";
        public const string Pool = "ok_bench_pool";
        public const string Players = "ok_bench_players";
        public const string Share = "ok_bench_share";
        public const string ShareAll = "ok_bench_shareall";
        public const string Take = "ok_bench_take";
        public const string Rename = "ok_bench_rename";
        public const string Delete = "ok_bench_delete";
        public const string Remove = "ok_bench_remove";
        public const string Close = "ok_bench_close";
        public const string You = "ok_bench_you";

        public const string Asking = "ok_bench_asking";
        public const string Hint = "ok_bench_hint";
        public const string Sharing = "ok_bench_sharing";
        public const string Taking = "ok_bench_taking";
        public const string DeleteAsk = "ok_bench_deleteask";
        public const string RemoveAsk = "ok_bench_removeask";
        public const string Deleted = "ok_bench_deleted";
        public const string OnlyOwn = "ok_bench_onlyown";
        public const string NothingToShare = "ok_bench_nothing";

        public const string Shared = "ok_bench_shared";
        public const string MoreRefused = "ok_bench_morerefused";
        public const string Took = "ok_bench_took";
        public const string NotTaken = "ok_bench_nottaken";
        public const string TooBig = "ok_bench_toobig";
        public const string NotBlueprint = "ok_bench_notblueprint";
        public const string Full = "ok_bench_full";
        public const string NotYours = "ok_bench_notyours";
        public const string Gone = "ok_bench_gone";
        public const string Removed = "ok_bench_removed";
        public const string Unknown = "ok_bench_unknown";
        public const string Failed = "ok_bench_failed";
        public const string NoAnswer = "ok_bench_noanswer";
    }
}
