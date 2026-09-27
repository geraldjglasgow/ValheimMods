using System;
using System.Collections.Generic;
using EarthWright.Core;

namespace EarthWright.Terrain
{
    /// <summary>What a guard sees. On the sender <see cref="Comp"/> is null; on the owner it is the compiler about to change.</summary>
    public sealed class GuardContext
    {
        public TerrainEdit Edit;

        /// <summary>Owner side: the routed-RPC sender peer (the server for a relayed edit). Sender side: 0.</summary>
        public long SenderPeer;

        /// <summary>Owner side: the compiler the edit is applied to.</summary>
        public TerrainComp Comp;

        /// <summary>Owner side: the edit is privileged and came through the server, which checked the sender is an admin.</summary>
        public bool ServerApproved;

        public bool OnOwner => Comp != null;
    }

    /// <summary>
    /// Checks that may refuse an edit. A guard returns null to allow, or a reason (a $token or plain text) to refuse.
    /// Sender guards run on the player's machine before anything is sent (the reason is shown to the player); owner
    /// guards run on the machine that owns each compiler, just before the edit is applied (the reason is sent back to
    /// the sender). Protection registers both kinds; any module may add its own.
    /// </summary>
    public static class EditGuards
    {
        private static readonly List<(string Name, Func<GuardContext, string> Check)> sender = new List<(string, Func<GuardContext, string>)>();
        private static readonly List<(string Name, Func<GuardContext, string> Check)> owner = new List<(string, Func<GuardContext, string>)>();

        public static void AddSender(string name, Func<GuardContext, string> check) => sender.Add((name, check));

        public static void AddOwner(string name, Func<GuardContext, string> check) => owner.Add((name, check));

        /// <summary>The first refusal of the sender guards, or null.</summary>
        public static string CheckSender(TerrainEdit edit) => FirstRefusal(sender, new GuardContext { Edit = edit });

        /// <summary>The first refusal of the owner guards, or null.</summary>
        public static string CheckOwner(GuardContext context) => FirstRefusal(owner, context);

        private static string FirstRefusal(List<(string Name, Func<GuardContext, string> Check)> guards, GuardContext context)
        {
            foreach (var guard in guards)
            {
                string reason = Safe.Call(guard.Name, () => guard.Check(context), null);
                if (!string.IsNullOrEmpty(reason))
                    return reason;
            }
            return null;
        }
    }

    /// <summary>
    /// Hooks around sending and applying edits.
    /// <list type="bullet">
    /// <item><see cref="Building"/>: sender, before the guards; may amend the edit (flags, filters).</item>
    /// <item><see cref="BeforeSend"/>: sender, after the guards passed, before the edit leaves (the undo snapshot).</item>
    /// <item><see cref="Sent"/>: sender, after the edit left.</item>
    /// <item><see cref="Applied"/>: owner, after the edit changed a compiler and was saved.</item>
    /// </list>
    /// </summary>
    public static class EditEvents
    {
        public static event Action<TerrainEdit> Building;
        public static event Action<TerrainEdit> BeforeSend;
        public static event Action<TerrainEdit> Sent;
        public static event Action<TerrainComp, TerrainEdit> Applied;

        internal static void RaiseBuilding(TerrainEdit edit) => Safe.Run("EditEvents.Building", () => Building?.Invoke(edit));

        internal static void RaiseBeforeSend(TerrainEdit edit) => Safe.Run("EditEvents.BeforeSend", () => BeforeSend?.Invoke(edit));

        internal static void RaiseSent(TerrainEdit edit) => Safe.Run("EditEvents.Sent", () => Sent?.Invoke(edit));

        internal static void RaiseApplied(TerrainComp comp, TerrainEdit edit) => Safe.Run("EditEvents.Applied", () => Applied?.Invoke(comp, edit));
    }
}
