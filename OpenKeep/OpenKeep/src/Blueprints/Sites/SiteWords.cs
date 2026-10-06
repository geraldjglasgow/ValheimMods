using OpenKeep.Core;

namespace OpenKeep.Blueprints.Sites
{
    /// <summary>
    /// The words of the construction sites (keys <c>ok_site_*</c>). Texts with numbers carry $1, $2 placeholders that
    /// <see cref="BlueprintWords.Format"/> fills.
    /// </summary>
    public static class SiteWords
    {
        public static string MarkerName { get; private set; }
        public static string Title { get; private set; }
        public static string BuiltCount { get; private set; }
        public static string Needed { get; private set; }
        public static string HasAll { get; private set; }
        public static string Deliver { get; private set; }
        public static string TakeDown { get; private set; }
        public static string PieceByPiece { get; private set; }
        public static string AllAtOnce { get; private set; }
        public static string FreeBuild { get; private set; }
        public static string Placed { get; private set; }
        public static string Delivered { get; private set; }
        public static string NotDelivered { get; private set; }
        public static string NoAnswer { get; private set; }
        public static string NothingToGive { get; private set; }
        public static string NothingNeeded { get; private set; }
        public static string NotYours { get; private set; }
        public static string TakenDown { get; private set; }
        public static string Refused { get; private set; }
        public static string NoPrefab { get; private set; }
        public static string Overlaps { get; private set; }

        public static void Register()
        {
            MarkerName = Language.Add("ok_site", "Construction site");
            Title = Language.Add("ok_site_title", "$1 (construction site of $2)");
            BuiltCount = Language.Add("ok_site_built", "Built $1 of $2 pieces");
            Needed = Language.Add("ok_site_needed", "Still needed: $1");
            HasAll = Language.Add("ok_site_hasall", "Everything it needs is here");
            Deliver = Language.Add("ok_site_deliver", "Hand over materials");
            TakeDown = Language.Add("ok_site_takedown", "Take the site down (its materials are handed back)");
            PieceByPiece = Language.Add("ok_site_piecebypiece", "Builds piece by piece as materials come in");
            AllAtOnce = Language.Add("ok_site_allatonce", "Builds all at once when everything is here");
            FreeBuild = Language.Add("ok_site_free", "Builds at once, without materials");
            AddMessages();
        }

        private static void AddMessages()
        {
            Placed = Language.Add("ok_site_placed", "Construction site $1 placed: hand it the materials (E on its post)");
            Delivered = Language.Add("ok_site_delivered", "Handed over: $1");
            NotDelivered = Language.Add("ok_site_notdelivered", "The site did not take the materials; they are back with you");
            NoAnswer = Language.Add("ok_site_noanswer", "The site did not answer yet; the materials come back if it never takes them");
            NothingToGive = Language.Add("ok_site_nothingtogive", "You carry nothing this site still needs");
            NothingNeeded = Language.Add("ok_site_nothingneeded", "This site has everything it needs");
            NotYours = Language.Add("ok_site_notyours", "Only $1 or an admin can take this site down");
            TakenDown = Language.Add("ok_site_takendown", "$1 taken down: its materials lie at its post");
            Refused = Language.Add("ok_site_refused", "Only the site's builder or an admin can take it down");
            NoPrefab = Language.Add("ok_site_noprefab", "The construction site post is missing in this game");
            Overlaps = Language.Add("ok_site_overlaps", "The construction site $1 already covers part of this spot");
        }
    }
}
