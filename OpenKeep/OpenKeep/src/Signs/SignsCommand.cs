using System.Collections.Generic;
using OpenKeep.Core;
using UnityEngine;

namespace OpenKeep.Signs
{
    /// <summary>
    /// <c>openkeep signs</c> lists the loaded containers with their sign state; <c>openkeep signs reset</c> (admin or
    /// host on a server) clears the hammer opt-out on every loaded container; <c>openkeep signs rewrite</c> refreshes
    /// the sign of every loaded container the local game owns. Called from Core's command by reflection.
    /// </summary>
    public static class SignsCommand
    {
        public static void Run(Terminal.ConsoleEventArgs args)
        {
            string sub = args.Length > 2 ? args[2].ToLowerInvariant() : "";
            switch (sub)
            {
                case "": List(args); break;
                case "reset": Reset(args); break;
                case "rewrite": Rewrite(args); break;
                default: args.Context.AddString("Usage: openkeep signs [reset | rewrite]"); break;
            }
        }

        private static void List(Terminal.ConsoleEventArgs args)
        {
            Vector3 origin = Player.m_localPlayer != null ? Player.m_localPlayer.transform.position : Vector3.zero;
            IReadOnlyCollection<Container> all = ContainerScan.All();
            args.Context.AddString($"OpenKeep: {all.Count} loaded containers:");
            foreach (Container container in all)
            {
                if (!Valid(container))
                    continue;
                float distance = Vector3.Distance(origin, container.transform.position);
                string owner = container.m_nview.IsOwner() ? "" : " (owned elsewhere)";
                args.Context.AddString($"  {ContainerScan.PrefabName(container)}  {distance:0.0} m  {State(container)}{owner}");
            }
        }

        private static string State(Container container)
        {
            ZDO zdo = container.m_nview.GetZDO();
            if (SignLinks.NoSign(zdo))
                return Language.Localize("$ok_signs_optedout");
            ZDO sign = SignLinks.LinkedSign(zdo);
            if (sign == null)
                return Language.Localize("$ok_signs_nosign");
            return Language.Localize(SignWriter.MayWrite(sign) ? "$ok_signs_sign" : "$ok_signs_playertext");
        }

        private static void Reset(Terminal.ConsoleEventArgs args)
        {
            if (ZNet.instance != null && !ZNet.instance.LocalPlayerIsAdminOrHost())
            {
                args.Context.AddString("OpenKeep: only an admin or the host can reset signs on a server.");
                return;
            }
            int count = 0;
            foreach (Container container in ContainerScan.All())
            {
                ZDO zdo = Valid(container) ? container.m_nview.GetZDO() : null;
                if (zdo == null || !SignLinks.NoSign(zdo) || SignLinks.InUseByAnother(zdo) || !SignLinks.Claim(zdo))
                    continue;
                SignLinks.SetNoSign(zdo, false);
                count++;
            }
            args.Context.AddString("OpenKeep: " + string.Format(Language.Localize("$ok_signs_reset"), count));
        }

        private static void Rewrite(Terminal.ConsoleEventArgs args)
        {
            int count = SignRefresh.RewriteAll();
            args.Context.AddString("OpenKeep: " + string.Format(Language.Localize("$ok_signs_rewritten"), count));
        }

        private static bool Valid(Container container)
        {
            return container != null && container.m_nview != null && container.m_nview.IsValid();
        }
    }
}
