using System.Collections.Generic;
using Hotkeys;
using PatchGuard;
using UnityEngine;
using UnityEngine.Rendering;

namespace Broadside
{
    /// <summary>
    /// The captain's broadside, on his own game: while he steers a launched Broadside ship (the game's ship control on a
    /// ship with Broadside's <see cref="KwShipBody"/>), <see cref="Key"/> fires every loaded
    /// cannon on the side he looks toward (<see cref="LooksPort"/>: his view, level, left or right of the ship's bow-stern
    /// line; a cannon's side is the way it was placed facing, <see cref="KwReloadCount.Port"/>), manned or not, one after
    /// another <see cref="Ripple"/> seconds apart from bow to stern, so the broadside rolls down the ship; turned to the other
    /// side and pressed again, that side's ripple runs beside the first. Each is ordered of its owner through the
    /// MannedWeapons library's ordered shot (<see cref="MannedWeapons.MannedWeapon.OrderShot"/>), and the owner decides: one
    /// nobody mans is handed to the captain's game, which fires it where it points as its gunner's lanyard would (the shot,
    /// the muzzle's blast and sound, a ship cannon's recoil, every peer from the cannon's ZDO), and then reloads it from his
    /// pack (<see cref="KwBroadsideReload"/>); one a gunner mans is fired on the gunner's game, where he has it laid, as his
    /// own pull of the lanyard would, and he reloads it as always. One still reloading or empty is skipped, and his top line
    /// says how many fired, how many reload from his pack and what the rest are short of (<see cref="KwArmWords.Broadside"/>).
    /// Meanwhile every empty cannon aboard nobody mans is loaded from his pack (<see cref="KwBroadsideLoad"/>).
    /// <see cref="ArcsKey"/> shows and hides each cannon's shot arc (<see cref="KwShotArcs"/>), both keys are on the helm
    /// key list (<see cref="KwHelmKeys"/>, the cannons' lines <see cref="KwHelmRows"/>), and the reloads count down on the
    /// ship panel (<see cref="KwReloadRows"/>). The keys are read through the
    /// Hotkeys library (never while he types, only when the game takes his input): U and J, which neither the game,
    /// Broadside's own helm keys (H nudges, B bails) nor our other mods use at a helm (the user's pick of a free key, 2026-10-26: not N,
    /// GrindstoneSkills' Fog Lift; not F, the game's Forsaken Power; not K, GrindstoneSkills' Wind Call; EarthWright reads
    /// U only with a hoe in hand).
    /// </summary>
    public static class KwBroadside
    {
        public const KeyCode Key = KeyCode.U, ArcsKey = KeyCode.J;
        public const string KeyName = "U", ArcsKeyName = "J";

        /// <summary>Seconds between one cannon's shot and the next.</summary>
        public const float Ripple = 0.15f;

        /// <summary>
        /// Seconds the list of cannons aboard is kept at most: it is looked up again at once when the ship's own children
        /// change (Broadside makes each piece built on a launched ship its child, and a broken one goes), else this often,
        /// for one further down.
        /// </summary>
        private const float Every = 5f;

        private static readonly List<(KwArm Arm, float At)> due = new List<(KwArm, float)>();
        private static readonly List<KwArm> list = new List<KwArm>();
        private static Ship? listed;
        private static float listedAt;
        private static int listedParts;

        /// <summary>Once, on a game that draws: the ticker that reads the keys and fires and reloads what is due.</summary>
        public static void Install()
        {
            if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null)
                return;
            GameObject ticker = new GameObject("Broadside_CannonsTicker");
            Object.DontDestroyOnLoad(ticker);
            ticker.AddComponent<KwBroadsideTicker>();
        }

        /// <summary>Each frame: the helm key list's lines, the keys, the cannons whose turn has come, their reloads, the shot arcs and the reload lines.</summary>
        public static void Tick()
        {
            Player? captain = Player.m_localPlayer;
            Ship? ship = captain != null ? Steered(captain) : null;
            List<KwArm>? aboard = ship != null ? Aboard(ship) : null;
            bool cannons = aboard != null && aboard.Count > 0;
            if (cannons && Hotkey.Pressed(Key) && captain!.TakeInput())
                Order(captain, ship!.transform, aboard!);
            if (cannons && Hotkey.Pressed(ArcsKey) && captain!.TakeInput() && !InventoryGui.IsVisible() && !Minimap.IsOpen())
                KwShotArcs.On = !KwShotArcs.On;
            KwHelmRows.Show(cannons);
            Fire(captain);
            KwBroadsideReload.Tick();
            KwBroadsideLoad.Tick(captain, cannons ? aboard : null);
            KwShotArcs.Tick(captain, ship, cannons ? aboard : null);
            KwReloadRows.Tick(ship, cannons ? aboard : null);
        }

        /// <summary>The launched Broadside ship the player steers now; null when he steers none.</summary>
        public static Ship? Steered(Player player)
        {
            Ship? ship = player.GetControlledShip();
            return ship != null && ship.GetComponent<KwShipBody>() != null ? ship : null;
        }

        /// <summary>The cannons aboard, bow first (looked up again when the ship's children change, else every <see cref="Every"/> seconds).</summary>
        public static List<KwArm> Aboard(Ship ship)
        {
            int parts = ship.transform.childCount;
            if (ship == listed && parts == listedParts && Time.time < listedAt + Every)
                return list;
            (listed, listedParts, listedAt) = (ship, parts, Time.time);
            list.Clear();
            list.AddRange(ship.GetComponentsInChildren<KwArm>());
            Vector3 at = ship.transform.position, ahead = ship.transform.forward;
            list.Sort((a, b) => Vector3.Dot(b.transform.position - at, ahead).CompareTo(Vector3.Dot(a.transform.position - at, ahead)));
            return list;
        }

        /// <summary>
        /// Whether the captain looks to port: his view (the game's camera, else his look), laid level, points left of the
        /// ship's bow-stern line; else to starboard (straight along it counts as starboard).
        /// </summary>
        public static bool LooksPort(Player captain, Transform ship)
        {
            Camera? view = Utils.GetMainCamera();
            Vector3 look = view != null ? view.transform.forward : captain.GetLookDir(), ahead = ship.forward;
            return ahead.z * look.x - ahead.x * look.z < 0f;
        }

        // The key: each loaded cannon on the side he looks toward, manned or not, queued Ripple after the one before (one
        // still waiting from the last press counted, not queued again); the count and the forecast of the reloads from his
        // pack (the unmanned ones'; a gunner reloads his own) on his top line.
        private static void Order(Player captain, Transform ship, List<KwArm> aboard)
        {
            KwVolley volley = new KwVolley { Port = LooksPort(captain, ship) };
            List<KwArm> his = new List<KwArm>();
            float at = Time.time;
            foreach (KwArm arm in aboard)
            {
                if (arm == null || !arm.Link.Valid || KwReloadCount.Port(ship, arm) != volley.Port)
                    continue;
                volley.Aboard++;
                bool waiting = due.Exists(d => d.Arm == arm);
                if (!waiting && !Count(arm, ref volley))
                    continue;
                if (!waiting)
                    due.Add((arm, at + Ripple * volley.Fired));
                if (!arm.Manned)
                    his.Add(arm);
                volley.Fired++;
            }
            volley.Reloads = KwBroadsideReload.Forecast(his, KwBroadsideReload.Store(captain), out volley.ShortOf);
            volley.Short = his.Count - volley.Reloads;
            captain.Message(MessageHud.MessageType.TopLeft, KwArmWords.Broadside(volley));
        }

        // Whether the cannon fires (loaded, manned or not); else counted as still reloading or not loaded.
        private static bool Count(KwArm arm, ref KwVolley volley)
        {
            if (arm.Orderable)
                return true;
            if (arm.State.Busy || KwBroadsideReload.Queued(arm))
                volley.Reloading++;
            else
                volley.Empty++;
            return false;
        }

        // Each queued cannon whose turn has come ordered of its owner (one gone since is passed over); the whole queue is
        // looked through, so a ripple begun on the other side meanwhile runs beside the first rather than after it.
        private static void Fire(Player? captain)
        {
            int i = 0;
            while (i < due.Count)
            {
                if (Time.time < due[i].At)
                {
                    i++;
                    continue;
                }
                KwArm arm = due[i].Arm;
                due.RemoveAt(i);
                if (captain != null && arm != null)
                    arm.OrderShot(captain);
            }
        }
    }

    /// <summary>The broadside's frame, on a game that draws (<see cref="KwBroadside.Install"/>); a throw is reported once a frame at most and never stops the game's.</summary>
    public sealed class KwBroadsideTicker : MonoBehaviour
    {
        private void Update()
        {
            try
            {
                KwBroadside.Tick();
            }
            catch (System.Exception e)
            {
                Guard.Report(e, "Broadside broadside");
            }
        }
    }
}
