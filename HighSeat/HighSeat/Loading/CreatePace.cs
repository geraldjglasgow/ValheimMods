using System;
using System.Collections.Generic;
using System.Diagnostics;
using HarmonyLib;

namespace HighSeat
{
    /// <summary>
    /// How fast the server makes instances of the objects it loads (<see cref="PlayerAreas"/>): to a time budget a pass,
    /// in place of the game's fixed count. The game's ZNetScene.CreateObjects makes at most 10 new instances a pass (30
    /// passes a second), 100 behind a loading screen (no player of its own, which is always so on a dedicated server, or
    /// a hosting player arriving or waiting to respawn), or a hundredth of those waiting when that is more. With every
    /// player's area listed that is often far too much at once: 100 players joining a hosted game made 10 player bodies
    /// with their gear a pass, 21 ms (measured 2026-10-31). Here a pass makes instances until its time is spent:
    /// <see cref="HostedMs"/> while a hosting player plays, <see cref="ArrivingMs"/> behind the host's own loading screen
    /// (the game hurries there too, for the host's own arrival), <see cref="DedicatedMs"/> on a dedicated server, which
    /// draws nothing. The clock runs from the first object (picking them, <see cref="CreateOrder"/>, costs less than the
    /// game's own sort) and is read before each one, so a pass makes at least one whenever one can be made and goes over
    /// by at most the last one it made; what is left waits for the next pass, still in the game's order. Everything else
    /// is the game's: the near objects first, and only once the server's own area has its ground
    /// (ZoneSystem.IsActiveAreaLoaded); those not made yet, keyed and handed out in the game's order
    /// (<see cref="CreateOrder"/>); each skipped while a location or dungeon in its zone still loads
    /// (ZoneSystem.IsZoneReadyForType); then the distant ones not made yet, in the list's order, while time is left; each
    /// made through the game's own CreateObject, and one that cannot be made (its prefab is unknown here) taken over and
    /// destroyed with the game's own log line. This runs as the last prefix of CreateObjects and only for PlayerAreas'
    /// call (<see cref="Create"/>), so every other mod's prefix there runs first (Elite Creatures Pack marks its unknown
    /// creatures as made there, so they are left alone, not destroyed), a mod that skips the method still skips it, and
    /// its postfixes run as ever. AreaLoading (Wayfare's quick jumps, OpenKeep's bed respawn) needs nothing from here: its
    /// postfix on CreateDestroyObjects runs after this pass, reads the lists PlayerAreas leaves filled and, while a
    /// hosting player arrives, makes more of them within its own 15 ms in zones whose ground is in, skipping what this pass
    /// made. If anything here throws, it is logged once and the game's own CreateObjects runs for the rest of the session
    /// (the world); a new world starts afresh.
    /// </summary>
    [HarmonyPatch(typeof(ZNetScene), nameof(ZNetScene.CreateObjects))]
    internal static class CreatePace
    {
        /// <summary>Milliseconds a pass spends making instances on a hosted game while the host plays (the game: 10).</summary>
        public const double HostedMs = 3.0;

        /// <summary>The same behind the hosting player's own loading screen, arriving or waiting to respawn, where the game
        /// makes ten times as many (about 100 objects of an ordinary kind) and the host sees only the loading screen.</summary>
        public const double ArrivingMs = 20.0;

        /// <summary>The same on a dedicated server: nothing is drawn, but every player's area is made there as players
        /// move, all the time, so less than a loading screen's short burst.</summary>
        public const double DedicatedMs = 10.0;

        private static readonly double TicksPerMs = Stopwatch.Frequency / 1000.0;
        private static readonly Stopwatch Clock = new Stopwatch();
        private static ZNetScene _scene;
        private static bool _armed;
        private static bool _failed;
        private static long _budget;
        private static int _made;

        /// <summary>Near objects without an instance at the start of the last pass that looked at them.</summary>
        public static int Waiting { get; private set; }

        /// <summary>The game's CreateObjects for PlayerAreas' pass, paced here.</summary>
        public static void Create(ZNetScene scene, List<ZDO> near, List<ZDO> distant)
        {
            if (scene != _scene)
            {
                _scene = scene;
                _failed = false;
            }
            _armed = !_failed;
            try
            {
                scene.CreateObjects(near, distant);
            }
            finally
            {
                _armed = false;
            }
        }

        [HarmonyPriority(Priority.Last)]
        private static bool Prefix(ZNetScene __instance, List<ZDO> currentNearObjects, List<ZDO> currentDistantObjects,
            bool __runOriginal)
        {
            if (!_armed || !__runOriginal)
            {
                return true;
            }
            _armed = false;
            try
            {
                Run(__instance, currentNearObjects, currentDistantObjects);
                return false;
            }
            catch (Exception e)
            {
                Fail(e);
                return true;
            }
            finally
            {
                CreateOrder.Clear();
            }
        }

        // The game's CreateObjects with time in place of its count: the near objects, then the distant ones.
        private static void Run(ZNetScene scene, List<ZDO> near, List<ZDO> distant)
        {
            _budget = (long)(Budget(scene) * TicksPerMs);
            _made = 0;
            Clock.Reset();
            if (ZoneSystem.instance.IsActiveAreaLoaded())
            {
                MakeNear(scene, near);
            }
            MakeDistant(scene, distant);
        }

        // The game's own split (10 a pass, or 100 behind a loading screen) as time; a dedicated server is never drawn.
        private static double Budget(ZNetScene scene)
        {
            if (ZNet.instance.IsDedicated())
            {
                return DedicatedMs;
            }
            return scene.InLoadingScreen() ? ArrivingMs : HostedMs;
        }

        // The pass is over once it made at least one object and its time is up.
        private static bool Spent => _made > 0 && Clock.ElapsedTicks >= _budget;

        private static void MakeNear(ZNetScene scene, List<ZDO> near)
        {
            CreateOrder.Fill(near, ZNet.instance.GetReferencePosition());
            Waiting = CreateOrder.Count;
            Clock.Start();
            ZoneSystem zones = ZoneSystem.instance;
            while (CreateOrder.Count > 0 && !Spent)
            {
                ZDO zdo = CreateOrder.Next();
                if (zones.IsZoneReadyForType(zdo.GetSector(), zdo.Type))
                {
                    Make(scene, zdo, false);
                }
            }
        }

        // As the game: none once the near ones have used the pass up, else in the list's order while time is left.
        private static void MakeDistant(ZNetScene scene, List<ZDO> distant)
        {
            Clock.Start();
            for (int i = 0; i < distant.Count && !Spent; i++)
            {
                ZDO zdo = distant[i];
                if (!zdo.Created)
                {
                    Make(scene, zdo, true);
                }
            }
        }

        // One object as the game makes it: through its own CreateObject. One that cannot be made (its prefab is unknown
        // here) is taken over and destroyed, logged in the game's own words (its typo for the distant ones included).
        private static void Make(ZNetScene scene, ZDO zdo, bool distant)
        {
            if (scene.CreateObject(zdo) != null)
            {
                _made++;
                return;
            }
            if (!ZNet.instance.IsServer())
            {
                return;
            }
            zdo.SetOwner(ZDOMan.GetSessionID());
            ZDOID uid = zdo.m_uid;
            ZLog.Log(distant
                ? "Destroyed invalid predab ZDO:" + uid.ToString() + "  prefab hash:" + zdo.GetPrefab()
                : "Destroyed invalid prefab ZDO:" + uid.ToString());
            ZDOMan.instance.DestroyZDO(zdo);
        }

        // Logged once: from here on, and for the rest of the session, the game's own CreateObjects runs.
        private static void Fail(Exception e)
        {
            _failed = true;
            HighSeat.Log.LogWarning("paced object creation failed, so the game's own CreateObjects runs for the rest of " +
                $"this session: {e}");
        }
    }
}
