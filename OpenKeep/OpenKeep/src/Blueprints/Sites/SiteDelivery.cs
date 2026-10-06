using System.Collections.Generic;
using OpenKeep.Core;

namespace OpenKeep.Blueprints.Sites
{
    /// <summary>
    /// Handing materials to a construction site (E on its post): the player's machine takes out of the inventory what
    /// the site still lacks (as far as the player carries it) and holds it (<see cref="SiteParcel"/>). When this machine
    /// owns the post (or nobody does, and it claims the free post) the materials go into the store at once; otherwise
    /// they are sent to the post's owner as a request with an id (RPC <see cref="RpcName"/>,
    /// <see cref="SiteDeliveries"/>), which adds them to the store in the ZDO and always answers
    /// (<see cref="ReplyRpc"/>): yes, or no with a reason. A machine that no longer owns the post answers "not owner"
    /// and never takes it over, so it never writes its older copy of the store. On no, or when no answer ever comes, the
    /// materials go back to the player. The owner accepts a delivery only when every item is one the game knows, in a
    /// sane amount; otherwise it refuses all of it.
    /// </summary>
    public static class SiteDelivery
    {
        public const string RpcName = "OpenKeep_SiteDeliver";
        public const string ReplyRpc = "OpenKeep_SiteDelivered";
        public const string NotOwner = "not owner";
        public const string Bad = "bad";
        private const int MostKinds = 256;
        private const int MostAmount = 1000000;

        /// <summary>SiteHooks.MarkerCreated: the post takes deliveries and their answers.</summary>
        public static void Register(SiteMarker site)
        {
            site.View.Register<ZPackage>(RpcName, (sender, pkg) => BlueprintSafe.Run("OpenKeep site delivery", () => OnDeliver(site, sender, pkg)));
            site.View.Register<ZPackage>(ReplyRpc, (sender, pkg) => BlueprintSafe.Run("OpenKeep site delivery answer", () => SiteDeliveries.Receive(sender, pkg)));
        }

        /// <summary>E on the post by the local player.</summary>
        public static void Deliver(SiteMarker site, Player player)
        {
            string refusal = Refusal(site, player, out SiteParcel parcel);
            if (refusal != null)
            {
                player.Message(MessageHud.MessageType.Center, Language.Localize(refusal));
                return;
            }
            if (TryHere(site, parcel))
                return;
            SiteDeliveries.Send(site, parcel);
        }

        /// <summary>
        /// The post is this machine's (a free post is claimed, as the game claims a free object): the parcel goes into the
        /// store now and the player is told. False when another machine owns the post.
        /// </summary>
        internal static bool TryHere(SiteMarker site, SiteParcel parcel)
        {
            if (!site.View.HasOwner())
                site.View.ClaimOwnership();
            if (!site.View.IsOwner())
                return false;
            Told(parcel, AddToStore(site, parcel.Given));
            return true;
        }

        /// <summary>The answer is in: the parcel stays with the site and the player hears what was handed over, or it comes back.</summary>
        internal static void Told(SiteParcel parcel, bool taken)
        {
            if (taken)
                parcel.Keep();
            else
                parcel.Refund();
            Messages.Center(taken ? BlueprintWords.Format(SiteWords.Delivered, SiteCosts.Describe(parcel.Given, 8)) : SiteWords.NotDelivered);
        }

        /// <summary>Why nothing is handed over, or null with <paramref name="parcel"/> taken out of the inventory.</summary>
        private static string Refusal(SiteMarker site, Player player, out SiteParcel parcel)
        {
            parcel = null;
            if (!BlueprintSettings.Enabled)
                return BlueprintWords.Disabled;
            Dictionary<string, int> missing = SiteNeeds.Missing(site);
            if (missing.Count == 0)
                return SiteWords.NothingNeeded;
            parcel = SiteParcel.Take(player.GetInventory(), missing);
            return parcel.Given.Count == 0 ? SiteWords.NothingToGive : null;
        }

        internal static void WriteGiven(ZPackage pkg, Dictionary<string, int> given)
        {
            pkg.Write(given.Count);
            foreach (KeyValuePair<string, int> item in given)
            {
                pkg.Write(item.Key);
                pkg.Write(item.Value);
            }
        }

        /// <summary>The post's owner: adds a valid delivery to the store and answers; a machine that is not the owner says so.</summary>
        private static void OnDeliver(SiteMarker site, long sender, ZPackage pkg)
        {
            if (site == null || site.State == null || !site.View.IsValid())
                return;
            long id = pkg.ReadLong();
            if (!site.View.IsOwner())
            {
                Answer(site, sender, id, false, NotOwner);
                return;
            }
            Dictionary<string, int> given = ReadGiven(pkg);
            bool ok = given != null && AddToStore(site, given);
            Answer(site, sender, id, ok, ok ? "" : Bad);
        }

        /// <summary>The delivered amounts, or null when any entry is not a known item in a sane amount.</summary>
        private static Dictionary<string, int> ReadGiven(ZPackage pkg)
        {
            int kinds = pkg.ReadInt();
            if (kinds <= 0 || kinds > MostKinds)
                return null;
            Dictionary<string, int> given = new Dictionary<string, int>();
            for (int i = 0; i < kinds; i++)
            {
                string item = pkg.ReadString();
                int amount = pkg.ReadInt();
                if (amount <= 0 || amount > MostAmount || SiteCosts.SharedName(item) == null)
                    return null;
                SiteCosts.Add(given, item, amount);
            }
            return given;
        }

        private static bool AddToStore(SiteMarker site, Dictionary<string, int> given)
        {
            if (site.State == null || given.Count == 0)
                return false;
            Dictionary<string, int> store = site.State.Store;
            foreach (KeyValuePair<string, int> item in given)
                SiteCosts.Add(store, item.Key, item.Value);
            site.State.SetStore(store);
            return true;
        }

        private static void Answer(SiteMarker site, long peer, long id, bool ok, string reason)
        {
            ZPackage pkg = new ZPackage();
            pkg.Write(id);
            pkg.Write(ok);
            pkg.Write(reason);
            site.View.InvokeRPC(peer, ReplyRpc, pkg);
        }
    }
}
