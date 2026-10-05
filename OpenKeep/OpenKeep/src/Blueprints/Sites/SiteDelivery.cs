using System.Collections.Generic;
using OpenKeep.Core;
using UnityEngine;

namespace OpenKeep.Blueprints.Sites
{
    /// <summary>
    /// Handing materials to a construction site (E on its post): the player's machine takes out of the inventory what
    /// the site still lacks (as far as the player carries it) and sends the amounts to the machine that owns the post
    /// (RPC <see cref="RpcName"/>), which adds them to the store in the ZDO. The deliverer removes first, so nothing is
    /// ever counted twice; nobody owning the post is fixed by the deliverer taking it over before sending (a send to
    /// owner 0 would reach every machine). The owner accepts only items the game knows, in sane amounts.
    /// </summary>
    public static class SiteDelivery
    {
        public const string RpcName = "OpenKeep_SiteDeliver";
        private const int MostKinds = 256;
        private const int MostAmount = 1000000;

        /// <summary>SiteHooks.MarkerCreated: the post takes deliveries.</summary>
        public static void Register(SiteMarker site)
        {
            site.View.Register<ZPackage>(RpcName, (sender, pkg) => BlueprintSafe.Run("OpenKeep site delivery", () => OnDeliver(site, pkg)));
        }

        /// <summary>E on the post by the local player.</summary>
        public static void Deliver(SiteMarker site, Player player)
        {
            string refusal = Refusal(site, player, out Dictionary<string, int> given);
            if (refusal != null)
            {
                player.Message(MessageHud.MessageType.Center, Language.Localize(refusal));
                return;
            }
            if (!site.View.HasOwner())
                site.View.ClaimOwnership();
            site.View.InvokeRPC(RpcName, Write(given));
            player.Message(MessageHud.MessageType.Center, BlueprintWords.Format(SiteWords.Delivered, SiteCosts.Describe(given, 8)));
        }

        /// <summary>Why nothing is handed over, or null with <paramref name="given"/> taken out of the inventory.</summary>
        private static string Refusal(SiteMarker site, Player player, out Dictionary<string, int> given)
        {
            given = null;
            if (!BlueprintSettings.Enabled)
                return BlueprintWords.Disabled;
            Dictionary<string, int> missing = SiteNeeds.Missing(site);
            if (missing.Count == 0)
                return SiteWords.NothingNeeded;
            given = Take(player.GetInventory(), missing);
            return given.Count == 0 ? SiteWords.NothingToGive : null;
        }

        /// <summary>Removes from the inventory as much of each missing item as it holds; returns what was taken.</summary>
        private static Dictionary<string, int> Take(Inventory inventory, Dictionary<string, int> missing)
        {
            Dictionary<string, int> given = new Dictionary<string, int>();
            foreach (KeyValuePair<string, int> need in missing)
            {
                string name = SiteCosts.SharedName(need.Key);
                int n = name == null ? 0 : Mathf.Min(need.Value, inventory.CountItems(name));
                if (n <= 0)
                    continue;
                inventory.RemoveItem(name, n);
                given[need.Key] = n;
            }
            return given;
        }

        private static ZPackage Write(Dictionary<string, int> given)
        {
            ZPackage pkg = new ZPackage();
            pkg.Write(given.Count);
            foreach (KeyValuePair<string, int> item in given)
            {
                pkg.Write(item.Key);
                pkg.Write(item.Value);
            }
            return pkg;
        }

        /// <summary>The machine the delivery was sent to (taking the post over if the owner moved meanwhile) adds it to the store.</summary>
        private static void OnDeliver(SiteMarker site, ZPackage pkg)
        {
            if (site == null || site.State == null || !site.View.IsValid())
                return;
            if (!site.View.IsOwner())
                site.View.ClaimOwnership();
            Dictionary<string, int> store = site.State.Store;
            int kinds = Mathf.Min(pkg.ReadInt(), MostKinds);
            for (int i = 0; i < kinds; i++)
            {
                string item = pkg.ReadString();
                int amount = pkg.ReadInt();
                if (amount > 0 && amount <= MostAmount && SiteCosts.SharedName(item) != null)
                    SiteCosts.Add(store, item, amount);
            }
            site.State.SetStore(store);
        }
    }
}
