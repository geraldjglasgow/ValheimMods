using System;
using BepInEx.Configuration;
using OpenKeep.Core;

namespace OpenKeep.Store
{
    /// <summary><c>Find Key</c>: every nearby container holding the hovered item gets a line from the player and a
    /// floating "name x count" for Link Seconds; the containers are the Store targets, so a chest another player is
    /// using is marked too when it could be stacked into. Link Seconds is Reach's setting, read from the cfg by
    /// section and key so the modules stay independent; 5 seconds when it is not bound.</summary>
    public static class Finder
    {
        private const float DefaultSeconds = 5f;

        public static void Find(ItemDrop.ItemData item)
        {
            if (!StoreActions.Ready(out Player player) || item == null)
                return;
            string name = ItemNames.DisplayName(item);
            float seconds = LinkSeconds();
            int containers = 0;
            foreach (Container container in StoreTargets.Nearby(player))
            {
                int count = container.GetInventory().CountItems(item.m_shared.m_name, -1, false);
                if (count <= 0)
                    continue;
                containers++;
                LinkMarker.Create(player, container, name + " x" + count, seconds);
            }
            Messages.Center(containers > 0 ? StoreWords.Format(StoreWords.Found, name, containers) : StoreWords.Format(StoreWords.NotFound, name));
        }

        public static float LinkSeconds()
        {
            ConfigFile config = Plugin.Instance != null ? Plugin.Instance.Config : null;
            ConfigDefinition definition = new ConfigDefinition("1. Reach", "Link Seconds");
            if (config == null || !config.ContainsKey(definition))
                return DefaultSeconds;
            try
            {
                return Convert.ToSingle(config[definition].BoxedValue);
            }
            catch (Exception)
            {
                return DefaultSeconds;
            }
        }
    }
}
