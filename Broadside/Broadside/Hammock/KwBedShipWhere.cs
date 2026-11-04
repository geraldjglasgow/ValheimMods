using System;
using System.Collections.Generic;
using HarmonyLib;
using PatchGuard;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// Where a bed's ship is (<see cref="KwBedRespawn"/>): a player's game respawning at a bed aboard a ship it does not
    /// have asks the server once (<see cref="AskCall"/>, the ship's own id), and the server answers that game alone
    /// (<see cref="AnswerCall"/>: the id, whether the ship is there, its place and turn) from its own ZDOs
    /// (<see cref="KwShipFinder.ById"/>), the answer kept until the respawn takes it. Sent only then, a few bytes each way.
    /// Also, on a player's game, the profile's spawn point kept with a bed aboard once a second (<see cref="KwBedSpawn.Keep"/>),
    /// while the ship sails. Made with the network (<see cref="KwBedShipWhereWake"/>) on every peer; a dedicated server only answers.
    /// </summary>
    public sealed class KwBedShipWhere : MonoBehaviour
    {
        public const string AskCall = "BS_BedShipWhere", AnswerCall = "BS_BedShipIs";
        private const float Every = 1f;
        private static readonly Dictionary<long, (bool Found, Vector3 At, Quaternion Turn)> answers = new Dictionary<long, (bool, Vector3, Quaternion)>();
        private float next;

        public static void Install(ZNet net)
        {
            ZRoutedRpc rpc = ZRoutedRpc.instance;
            rpc.Register<long>(AskCall, (sender, ship) => Guard.Run("Broadside bed aboard: where", () => Answer(sender, ship)));
            rpc.Register<ZPackage>(AnswerCall, (_, package) => Guard.Run("Broadside bed aboard: there", () => Take(package)));
            GameObject holder = new GameObject("BS_bed_aboard");
            holder.transform.SetParent(net.transform, false);
            holder.AddComponent<KwBedShipWhere>();
        }

        // On a player's game: the spawn point kept with its ship, once a second.
        private void Update()
        {
            if (Time.time < next)
                return;
            next = Time.time + Every;
            if (Player.m_localPlayer != null)
                Guard.Run("Broadside bed aboard", KwBedSpawn.Keep);
        }

        /// <summary>Asks the server where the ship of this id is; an earlier answer for it is dropped.</summary>
        public static void Ask(long ship)
        {
            answers.Remove(ship);
            ZRoutedRpc.instance?.InvokeRoutedRPC(AskCall, ship);
        }

        /// <summary>The server's answer for this ship, taken (once); false while none has come.</summary>
        public static bool Answered(long ship, out bool found, out Vector3 at, out Quaternion turn)
        {
            (found, at, turn) = (false, Vector3.zero, Quaternion.identity);
            if (!answers.TryGetValue(ship, out var answer))
                return false;
            answers.Remove(ship);
            (found, at, turn) = answer;
            return true;
        }

        // On the server: the ship's place and turn from its ZDO, back to the asking game.
        private static void Answer(long sender, long ship)
        {
            if (ZNet.instance == null || !ZNet.instance.IsServer())
                return;
            ZDO? zdo = KwShipFinder.ById(ship);
            ZPackage package = new ZPackage();
            package.Write(ship);
            package.Write(zdo != null);
            package.Write(zdo != null ? zdo.GetPosition() : Vector3.zero);
            package.Write(zdo != null ? zdo.GetRotation() : Quaternion.identity);
            ZRoutedRpc.instance.InvokeRoutedRPC(sender, AnswerCall, package);
        }

        private static void Take(ZPackage package)
        {
            long ship = package.ReadLong();
            bool found = package.ReadBool();
            Vector3 at = package.ReadVector3();
            answers[ship] = (found, at, package.ReadQuaternion());
        }
    }

    /// <summary>ZNet.Awake postfix: the bed lookup's calls and the keeper made with the network, on every peer.</summary>
    [HarmonyPatch(typeof(ZNet), nameof(ZNet.Awake))]
    public static class KwBedShipWhereWake
    {
        [HarmonyPostfix]
        private static void Postfix(ZNet __instance)
        {
            try
            {
                KwBedShipWhere.Install(__instance);
            }
            catch (Exception e)
            {
                // Never out of ZNet.Awake: a throw there stops the network from starting.
                Log.Error($"Broadside bed aboard could not be made: {e}");
            }
        }
    }
}
