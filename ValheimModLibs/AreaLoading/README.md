# AreaLoading

Arriving somewhere far, quicker: a portal jump or a respawn.

- `AreaLoader.Install(harmony, report)`: once in the mod's `Awake`. Installs two postfixes once per merged copy, by
  name (`ZoneSystem.Update`, `ZNetScene.CreateDestroyObjects`); `report` gets the "loaded the land around you in ..."
  log line.
- `AreaLoader.When(busy)`: a reason to hurry, asked every frame (keep it cheap). While one holds, on a machine that
  draws the world and is connected, the land around the player loads as fast as the frame budget allows (20 ms of
  `CreateLocalZones` calls instead of one zone per 0.1 s) and the objects the game has listed are created zone by
  zone as each zone's land is in (15 ms per pass, the game's own order and `CreateObject`), instead of none until the
  whole simulation area is loaded. The screen should be black then; the frames are longer.
- `AreaSettle.Settled(point)`: on a client of a server, true once the number of objects known in the 3x3 zones around
  the point has not changed for 0.5 s (the server sends the nearest first); always true on the server or alone. Use it
  to hold a quickened landing until the target's objects have arrived, never beyond the game's own wait.
- `QuickWait.Seconds(metres, range, shortest, full)`, `QuickWait.Speed(seconds, full)`: a wait that grows with
  distance, run as a faster timer; `QuickWait.MapDistance(a, b)`: distance on the ground plane.

```csharp
AreaLoader.Install(harmony, Logger.LogInfo);
AreaLoader.When(() => Player.m_localPlayer != null && Player.m_localPlayer.IsTeleporting());
```

Each mod's copy hurries only for the reasons it registered, so two mods (one for jumps, one for respawns) never
double up. Merged into the consuming mod with ILRepack like every library here. Consumers: Wayfare (portal jumps),
OpenKeep (respawns). Moved out of OpenKeep's Homestead section on 2026-10-04 when its quick portals went to Wayfare.
