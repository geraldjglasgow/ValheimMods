namespace OpenKeep.Blueprints.Sites
{
    /// <summary>
    /// The keys of the construction sites' words (<c>ok_site_*</c>). Their English is in
    /// <c>translations/English.blueprints.yml</c>; texts with values carry $1, $2 placeholders for <c>Lang.Format</c>.
    /// </summary>
    public static class SiteWords
    {
        public const string MarkerName = "ok_site";
        public const string Title = "ok_site_title";
        public const string BuiltCount = "ok_site_built";
        public const string Needed = "ok_site_needed";
        public const string HasAll = "ok_site_hasall";
        public const string Deliver = "ok_site_deliver";
        public const string TakeDown = "ok_site_takedown";
        public const string PieceByPiece = "ok_site_piecebypiece";
        public const string AllAtOnce = "ok_site_allatonce";
        public const string FreeBuild = "ok_site_free";

        public const string Placed = "ok_site_placed";
        public const string Delivered = "ok_site_delivered";
        public const string NotDelivered = "ok_site_notdelivered";
        public const string NoAnswer = "ok_site_noanswer";
        public const string NothingToGive = "ok_site_nothingtogive";
        public const string NothingNeeded = "ok_site_nothingneeded";
        public const string NotYours = "ok_site_notyours";
        public const string TakenDown = "ok_site_takendown";
        public const string Refused = "ok_site_refused";
        public const string NoPrefab = "ok_site_noprefab";
        public const string Overlaps = "ok_site_overlaps";
    }
}
