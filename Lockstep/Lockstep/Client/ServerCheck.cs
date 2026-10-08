using System.Collections;
using UnityEngine;

namespace Lockstep
{
    /// <summary>
    /// A player on a server that does not run Lockstep. Only a server with Charter checks the players' mods, so the game
    /// lets them join, and without this every altar would be open with no word why. The server sends its first push as
    /// soon as a player connects, so a player who has spawned and heard nothing a few seconds later is told, once per
    /// connection: a message on screen, a chat line, the altar's hover and <c>lockstep status</c>.
    /// </summary>
    public static class ServerCheck
    {
        public const string NotRunning = "This server does not run Lockstep, so boss altars are not sealed. Install Lockstep on the server.";
        public const string NotRunningHover = "Not sealed: this server does not run Lockstep";
        private const float Wait = 10f;

        private static bool heard, started;
        private static Coroutine pending;

        /// <summary>True once a player of a remote server waited out the check without hearing from Lockstep there.</summary>
        public static bool Silent { get; private set; }

        public static void Initialize(Charter.Charter sync) => sync.Pushed += first => heard = true;

        /// <summary>The local player spawned: starts the check once per connection, on a player of a remote server only.</summary>
        public static void PlayerSpawned()
        {
            if (started || ZNet.instance == null || ZNet.instance.IsServer())
                return;
            started = true;
            pending = Lockstep.Instance.StartCoroutine(CheckLater());
        }

        private static IEnumerator CheckLater()
        {
            yield return new WaitForSecondsRealtime(Wait);
            pending = null;
            if (heard || ZNet.instance == null)
                yield break;
            Silent = true;
            Lockstep.Log.LogWarning(NotRunning);
            MessageHud.instance?.ShowMessage(MessageHud.MessageType.Center, NotRunning);
            Chat.instance?.AddString("Lockstep: " + NotRunning);
        }

        /// <summary>The connection ended: the next one is checked afresh.</summary>
        public static void Reset()
        {
            if (pending != null)
                Lockstep.Instance.StopCoroutine(pending);
            pending = null;
            heard = started = Silent = false;
        }
    }
}
