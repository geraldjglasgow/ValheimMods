# Wayfare - PLAN

Design and roadmap while the mod is unfinished. Written from the user's spec and the game's decompiled assembly
(`assembly_valheim.dll`, build 25253764, decompiled into `~/scratch/decomp/Wayfare/`, never into this repo),
the way every mod in this workspace is built - see the workspace `CLAUDE.md` and root `CLEANROOM.md`. This
document is deleted (or trimmed to a changelog note) once the mod is verified against `SPEC.md`.

## What this mod is

Replaces Valheim's tag-pairing portal connections with map-based targeting: walk into any portal, the world map
opens in a targeting mode with an icon on every portal you may target, click one to teleport there. Tags become a
label only. Portals gain three access modes (Public / Private / Admin) enforced server-side. See `SPEC.md` for the
full behaviour and the test checklist.

## Why not reuse the vanilla portal `TeleportWorld` connection at all

`TeleportWorld.Teleport()` reads `m_nview.GetZDO().GetConnectionZDOID(ZDOExtraData.ConnectionType.Portal)` - the
tag-paired target vanilla's `Game.ConnectPortalsCoroutine` maintains server-side every 5 seconds. Wayfare's
targeting is independent of that connection entirely: we never read or write it. `Game.ConnectPortalsCoroutine`
keeps running (it is server code we do not patch out), but nothing reads its result any more: the portal's look
(glow, connect sound, swirl, the hover's "connected") comes from `TeleportWorld.HaveTarget`/`TargetFound`, which
`Portals/PortalOpenPatch.cs` answers true for every portal. Every portal reaches every other, tagged or not, and the
tag is only its name (the user's rule, 2026-10-05; 0.2.0 had briefly required a tag and left untagged portals dark,
off the map and refused).
No vanilla teleport ever fires because entering a portal's trigger is intercepted (below) before
`TeleportWorld.Teleport()` runs.

## How entering a portal is intercepted

`TeleportWorldTrigger.OnTriggerEnter` is the only call site of `TeleportWorld.Teleport(Player)` (found by listing
every type referencing `TeleportWorld`/`Portal`/`Trigger` in the decompiled assembly and reading each). It is a
plain `OnTriggerEnter(Collider)` that checks the collider belongs to `Player.m_localPlayer` and calls
`m_teleportWorld.Teleport(component)` - nothing else. A prefix patch on `TeleportWorldTrigger.OnTriggerEnter` skips
the original (returns false) for the local player and instead opens map targeting for that portal
(`Targeting/TargetingSession.cs`). A non-local player's collider (seen by every client, since portal triggers exist
on every client) is ignored exactly as vanilla ignores it.

## How a portal is discovered as a portal, without a hardcoded prefab list

Vanilla's own "is this ZDO a portal" test is `Game.instance.PortalPrefabHash.Contains(prefabHash)`
(`ZDOMan.AddIfPortal`), and `PortalPrefabHash` is filled once in `Game.Awake` from a **hardcoded serialized list**,
`m_portalPrefabs` - exactly what SPEC item 5 forbids relying on for discovery. That hash list is also what puts a
ZDO into `ZDOMan.m_portalObjects` - the portal list that is always loaded **server-side** (its own save chunk,
kept whole for `Game.ConnectPortals`). An earlier revision of this document claimed that list is distributed to
every peer regardless of distance; that was wrong - `AddIfPortal` in `ZDOMan.RPC_ZDOData` classifies whatever
happens to arrive, but the only send path (`ZDOMan.CreateSyncList` → `FindSectorObjects`) covers sectors near the
peer, so a pure client only ever holds the portals it has been near this session. SPEC item 1 ("every portal the
player may target") is therefore met by Wayfare's own sync: on the registry's ~5s tick, and when the player walks
into a portal, a client sends `wf_RequestPortals` (routed, no target = the server) with the version of the list it
holds, and the server replies `wf_PortalList` with a snapshot built from its authoritative `GetPortalList()`, followed
by that list's version, only when the version differs (`Portals/PortalSync.cs`). The version folds every portal ZDO's
id, data revision and position (`PortalListVersion`), so an unchanged list is never built or sent again; one built
list is sent to every client that asks while it is current. For the same reason the `wf_TeleportGranted`
reply carries the destination position and rotation - the requesting client usually holds no ZDO for the target
portal. And because widening `PortalPrefabHash` happens after `ZDOMan` has already loaded/received ZDOs,
`PortalDiscovery` re-classifies existing ZDOs whose prefab it just added (`ZDOMan.AddIfPortal`); without that the
save-writer (`GetSaveClonePerChunk` skips hash-listed ZDOs from regular chunks) would silently drop a modded
portal from the next save. That pass over every ZDO runs in a postfix on `ZDOMan.LoadChunks`/`Load`, inside the
world load, whenever the scene is ready by then (and again for every later world load of the same game); only
otherwise in the ticker's first run.

Decision: a postfix on `Game.Awake` walks every prefab registered in `ZNetScene` (after it has registered them;
`ZNetScene.Awake` postfix, `Priority.Low`, matching OpenKeep's own scene-scan precedent), finds every one carrying a
`TeleportWorld` component, and adds any prefab hash not already in `Game.instance.PortalPrefabHash`. This is
component-based discovery (SPEC item 5) that also **opts every modded portal into vanilla's own world-wide portal
channel** - on every machine, since every machine (server and every client) runs this same scan over the same
installed prefabs, which is only reliable because the mod is required on the server (SPEC's own multiplayer
requirement, and stated in the README). A portal prefab that ships only client-side would still be discoverable
locally but would not get world-wide distribution; that limitation is inherent to how Valheim networks ZDOs and is
called out in `SPEC.md`, not something Wayfare's code can fix.

Wayfare's own portal list is a live query (`Portals/PortalRegistry.cs`): every ~5 seconds (the same cadence as
vanilla's own reconnect tick, so a portal is never stale longer than vanilla's own tag pairing would be) it checks
the list's version and, when it changed, re-reads `ZDOMan.instance.GetPortalList()` and rebuilds the
visible/targetable set (on a client: asks the server, above). Nothing is cached indefinitely (SPEC item 4).

## Access modes: where enforcement lives

Two different actions need two different authorities, and neither is "trust the client":

- **Setting a portal's mode** goes through the server, then the owner. A client knows only one peer, the server,
  so a client that owns a portal cannot tell who sent it an RPC: an earlier version handled `wf_SetMode` on the
  owner and credited the owner's own player with everyone's requests. Now the client sends `wf_RequestSetMode`
  (routed, ZDOID + mode) to the server, which resolves the sender's player and admin flag, checks the portal and
  `MayCycle`, and writes the change itself when it owns the ZDO or nobody does; otherwise it forwards `wf_SetMode`
  (player id, admin flag, mode) on the portal's `ZNetView` to the owner, which accepts it only from the server and
  checks again against its own copy before writing. Sea gate edits (mode, name) take the same path
  (`wf_SeaGateEdit`).
- **Targeting a portal** (`Targeting/RPC_wf_RequestTeleport`) is different: the portal a player wants to target is
  usually not owned by that player's own machine, and trusting "whichever client owns this ZDO right now" to police
  every other player's teleport would let a malicious client that happens to own a portal wave through requests
  its owner should refuse (or refuse ones it should allow). Decision: this one is routed specifically to **the
  server** (`ZRoutedRpc.instance.InvokeRoutedRPC("wf_RequestTeleport", ...)` with no explicit target peer routes to
  `GetServerPeerID()`), which is the single authoritative party for the whole session on both a dedicated server and
  a hosted game, and already holds every ZDO regardless of who currently owns it for replication. The server checks
  mode + owner + admin status and replies `wf_TeleportGranted` (with the destination position/rotation) or
  `wf_TeleportDenied` (with a reason) directly to the requesting peer. On single player / a listening host the
  server is the same process, so the RPC resolves synchronously with no network round trip
  (`ZRoutedRpc.InvokeRoutedRPC` special-cases `targetPeerID == m_id`).
- The requesting **client** performs the actual teleport itself, on grant, by calling `Player.TeleportTo(pos, rot,
  distantTeleport: true)` - the same method `TeleportWorld.Teleport()` calls, so carry-item checks
  (`Player.IsTeleportable`), the fade effect and the 2-second cooldown are all the game's own (`Player.UpdateTeleport`,
  `m_teleportCooldown`). The world-blocking global keys (`NoPortals`, `NoBossPortals`) are replicated state every
  client already has, so they are checked client-side before ever sending the request (fail fast) and are not
  re-checked by the server - same authority model vanilla already uses for them.

Portal mode and owner are ordinary ZDO fields (`wf_mode` int, `wf_owner` long), so they replicate to every client the
same way position or the tag string does - no separate sync RPC needed for display.

## Where the map UI lives

`Minimap` already owns `MapMode`, `SetMapMode`, world<->map point conversion (`WorldToMapPoint`,
`MapPointToWorld`, `ScreenToWorldPoint`) and the click plumbing (`OnMapLeftDown/Up/Click`). Its own `PinData`/
`AddPin` system is built around player-placed, saved pins with a fixed `PinType` enum and its own click-to-toggle
behaviour (`OnMapLeftClick` toggles `m_checked` on the nearest pin) - reusing it for portal icons would fight that
behaviour (a portal icon is not a pin a player can drag to remove, and vanilla's own left-click handling would
run alongside ours). Decision: Wayfare draws its own overlay, a set of plain `UnityEngine.UI` elements on its own
layer (`MapIconLayer`: stretched over the large map and kept as its last child, so above every pin and marker; icons
anchored at its lower-left corner, where the game measures pin positions from), larger and pulsing while the player
is choosing a destination, and while
targeting is active a Harmony prefix on `Minimap.OnMapLeftClick` checks the click against each icon's click area (an
invisible rect as wide as the icon at the top of its pulse, held still; the nearest icon when two overlap, never the
portal the player stands at) and skips the vanilla handler (`return false`). The travel itself waits out the game's
double click window (the MapClicks library's `IconClick.Hold`), so a double click on an icon places a pin there
instead, and a right click on an icon removes a placed pin under it before it toggles a favourite (asked 2026-10-04:
the game's pins under portal icons stay placeable and removable). Outside targeting a left click is the game's. The
portal the player stands at is drawn still, with a small red "You are here" above it. Left the map (any other mode) or pressed cancel (Escape, already the game's own map-close
binding) exits targeting through the same `SetMapMode` postfix that tears the overlay down.

No custom art ships: the icon is the game's own portal map icon (the pin bar's portal, `Minimap.PinType.Icon4`, read
from `Minimap.m_icons` at runtime) tinted gold (asked 2026-10-04), with a procedural ring around a favourite
(`Targeting/IconFactory.cs`, `Texture2D.SetPixels` + `Sprite.Create`; a procedural circle stands in only if the map
has no portal icon). Nothing to ship as a binary asset and no `thunderstore/` art beyond the store icon. Labels (the portal's tag) use a plain `UnityEngine.UI.Text` with Unity's built-in legacy font
(`Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf")`) - `TMP_Text` needs a `TMP_FontAsset` we do not have one
of.

## Favourites

Per player, client-side, keyed by the portal's position rounded to the metre (stable across a rename, and across a
server restart, which a `ZDOID` is not: `ZDO.Load` renumbers every ZDOID on each world load) - stored in `Player.m_customData["Wayfare.favourites"]` as a comma-separated set, the same shape and
storage OpenKeep's `Favourites`/`CharacterData` already use for its own favourite marks (our own fresh
implementation, not shared code - the pattern is ours to reuse across our own mods, the vocabulary is not). This
travels with the character save, so it is genuinely per-player rather than per-installation.

## Stability: the load-order incident

The user's spec calls out a real incident: `Minimap.SetMapMode` (and by extension anything that reacts to it) can
run during `ZNet.LoadWorld`'s early key-setup, before `Minimap.instance`, its pin roots, or `Player.m_localPlayer`
exist. Every Wayfare patch that touches the overlay, the favourites panel, or cancels targeting starts with a guard
clause: `Minimap.instance == null`, `Player.m_localPlayer == null`, or our own overlay root `== null` all return
immediately, no-op. This is checked explicitly by the headless dedicated-server load test in `SPEC.md`'s checklist
(a dedicated server never has a `Minimap` at all - `Minimap.instance` is always null there - so every guard is
exercised on every server start, not just as an edge case).

## Layout

```
Wayfare/Wayfare/src/
  Plugin.cs                     entry: WayfareConfig.Initialize, Harmony.PatchAll, Synced.Finish, Guard.Install
  Core/
    WayfareConfig.cs             SyncedConfiguration bindings (General, Map, Favourites sections)
    Language.cs                  $wf_ words, Localization.SetupLanguage postfix
    (hotkeys)                    the Hotkeys library (ValheimModLibs): fires while W is held, nothing while typing
  Portals/
    PortalDiscovery.cs            Game.Awake postfix: TeleportWorld-component scan into PortalPrefabHash
    PortalRegistry.cs             5s check of the portal list (re-read only when its version changed), the snapshot
    PortalSync.cs                 wf_RequestPortals / wf_PortalList with the list's version, PortalListVersion
    PortalInfo.cs                 readonly struct: ZDOID, position, tag, mode, owner
    PortalFields.cs               wf_mode / wf_owner ZDO get/set, the Mode enum, default-mode config lookup
    PortalAccess.cs               May(PortalInfo, playerID, isAdmin) - the one access rule, used by client display
                                  filtering and by the server's teleport grant
    ModeCycle.cs                  TeleportWorld.Interact prefix (alt) + RPC_wf_SetMode owner-side handler
    CycleHover.cs                 the Alt+E hover line, localized once per mode, language and input device
    PortalOpenPatch.cs            TeleportWorld.HaveTarget / TargetFound postfixes: every portal open
  Targeting/
    TargetingSession.cs           TeleportWorldTrigger.OnTriggerEnter prefix; open/close targeting, the source
                                  portal reference, guarded against the load-order incident
    TeleportGate.cs               client: send wf_RequestTeleport, await grant/deny; server: validate + reply
    MapOverlay.cs                 places/hides the portal icons on MapIconLayer (kept and reused), hit-tests their click areas
    PortalIcon.cs                 one icon: gold game portal sprite, favourite ring, tag, "You are here", click area
    IconFactory.cs                the game's portal sprite, the gold and red, the procedural favourite ring
    MapClickPatch.cs              Minimap.OnMapLeftClick / RemovePinUnderPointer prefixes: hit-test our icons first
    FavouritesPanel.cs            the left-side favourites list UI
    PlayerFavourites.cs           Player.m_customData persistence, keyed by ZDOID string
  HotkeyToggle.cs                 Player.Update postfix: default P toggles portal icons on the small/non-targeting
                                  map (display-only, unsynced)
  QuickJumps/                     section `Jump Speed` (see "Jump speed" below)
    JumpSpeed.cs                  Install: the AreaLoading library's loader and Wayfare's reason to hurry
    JumpTiming.cs                 Quick Portals: the teleport timer run faster by distance, the server settle hold
    JumpScreen.cs                 Screen Only When Loading: no teleport screen into a loaded area, a faster fade
    JumpPatches.cs                Player.UpdateTeleport prefix, Player.TeleportTo postfix, Hud.UpdateBlackScreen prefix
Wayfare/Wayfare/config/           (none yet - no YAML needed, cfg-only)
```

## Own names

- Plugin GUID `com.Wayfare`, config file `com.Wayfare.cfg` (BepInEx's default from the GUID).
- ZDO keys: `wf_mode` (int, `Portals.Mode`), `wf_owner` (long, player id).
- RPCs: `wf_RequestSetMode` (routed, to the server), `wf_SetMode` (on the portal's `ZNetView`, from the server
  only), `wf_RequestTeleport` / `wf_TeleportGranted` / `wf_TeleportDenied` (routed, `ZRoutedRpc`).
- Player custom data: `Wayfare.favourites`.
- Localization keys: `$wf_*`.
- Console: none added - nothing in the spec needs one, and Wayfare's state is either visible on the map itself or
  server-enforced, unlike Lockstep/OpenKeep's admin-facing commands.

## Open questions the spec left for this document (decided here, not asked)

- **Unowned portal default mode**: Public, always (the user's rule, 2026-10-05: every portal is built Public). The
  `Unowned Portals Are Public` setting SPEC item 8 asked for was removed then; a portal with no `wf_mode` reads Public.
- **Who may cycle a portal's mode**: not specified. Decision: the interact prefix allows cycling when the portal is
  unowned, when the acting player already owns it, or when the acting player is an admin; otherwise it shows
  "$wf_notowner" and does nothing. Without this, any player could reassign ownership of someone else's Private
  portal by walking up and pressing the cycle key, which would make Private meaningless.
- **Portal prefabs without `TeleportWorld` on the root object**: `GetComponentInChildren<TeleportWorld>` is used
  during the prefab scan (not `GetComponent`) since a modded portal could carry the component on a child, matching
  how `ContainerScan` in OpenKeep resolves a root object override for ships/carts.
- **The favourites panel only shows while targeting is active.** Right-clicking an icon to favourite/unfavourite it
  works whenever icons are visible at all (targeting, or the hotkey toggle on the ordinary map), but the left-side
  list itself only appears during a targeting session - there is no source portal to click a favourite *to* outside
  one, and showing an inert list on the ordinary map would invite clicking something that does nothing.
- **A pure dedicated server needs its own portal-classification driver.** `PortalRegistry`'s 5-second tick only
  runs where there is a local player (nothing to show on a server with no UI), but `PortalDiscovery`'s
  `Game.PortalPrefabHash` augmentation is what the *server* relies on to (a) put a modded portal into the game's
  world-wide-distributed portal channel at all and (b) accept a `wf_RequestTeleport` for one. `PortalDiscovery` runs
  its own always-on ticker for exactly this reason, independent of whether a local player exists.
- **Disabling the mod via config also refuses server-side actions**, not just the client-side entry points
  (`TeleportGate.OnRequestTeleport` and `ModeCycle.OnSetMode` both check `Enabled` themselves) - a client that
  bypassed its own UI while the server has the mod turned off should still be refused, not accidentally let through
  because only the "normal" entry point checked the switch.

## Sea gates (built 2026-10-04, untested in game)

A portal for ships. Building in water is awkward in Valheim, so a sea gate is two pillars built on the ground, on
land or in shallow water, one on each side of a channel, a river mouth or a shore and an islet. Once the second pillar stands close enough to the
first, a portal surface fades in between them over the water. Every gate reaches every other: a ship that sails in
stops, its helmsman picks any other gate on the map, and the ship comes out there with everyone aboard, keeping its
speed. Ships only: a swimmer or a walker passing through the surface is ignored.

Decided with the user: the destination is picked at the helm each time, like walking into a portal (changed
2026-10-05; 0.1.0 and 0.2.0 set one destination per gate with E, which left a new gate "not connected"); E on a
pillar only names the gate; restricted cargo (ore, metal, eggs) blocks the jump like a portal, with a synced setting
to allow it; ships only. The overriding requirement is
that nobody may die or be lost at sea in a jump: every step below exists for that.

### What the game does that shapes this (verified in the decompiled assembly)

- **A ship floats only where water is loaded.** `Floating.GetWaterLevel` finds the water through the zone's
  `WaterVolume`; with none loaded it returns -10000, `Ship.CustomFixedUpdate` applies no buoyancy, and the ship
  drops. A ship must stay frozen until its destination has loaded.
- **`Player.UpdateTeleport` knows nothing about ships.** From 2 s on it holds the player at the target point with
  zero velocity, waits for `ZNetScene.IsAreaReady` (and at least 8 s for a distant teleport), then ends wherever
  `ZoneSystem.FindFloor` (a raycast down) finds a floor. When it finds none it ends at `GetSolidHeight + 0.5`,
  which at sea is the seabed. Wayfare must hold a crew member until the ship is under them and end the teleport
  itself.
- **`Player.TeleportTo` silently refuses a second teleport within 2 s** (`m_teleportCooldown`). The crew client
  clears the cooldown before calling it, or one player is left behind.
- **`ZSyncTransform` snaps any move over 5 m** instead of interpolating, so the other clients see the ship jump,
  not slide.
- **`Ship.UpdateOwner` hands the ship to a player aboard**, and the server hands a far-away object to a nearer
  peer. Ownership can change mid-jump, so the jump's state lives in the ship's ZDO and whoever owns the ship
  carries it on.
- **ZDOIDs are renumbered on every world load** (`ZDO.Load`: `m_uid.SetID(++ZDOID.m_loadID)`). Pairs and
  gates therefore use an id of our own (`wf_sg_id`, a random long), never a ZDOID.

### Building and pairing

- **The pillar** is its own piece, `WF_SeaGatePillar`, a copy of one of the game's own pieces made at runtime (no
  new asset until the user decides; a proper model would go through `../ValheimAssets` and AssetLab first). It is
  built with the hammer near a workbench; placeholder recipe. It must not carry `TeleportWorld`, or
  `PortalDiscovery` would make it a walk-in portal.
- **On the ground:** the pillar stands on land or on the bottom of water at most 2 m deep (changed 2026-10-04 from
  land only, which made building at the waterline fiddly). The game's placement ray passes through water for this
  piece, so the ghost sits on the seabed. The pillar is about 4.5 m tall, so its top stays at least 2.5 m above the
  sea: every gate still has a dry spot for the fallback below.
- **Pairing:** a new pillar pairs with the nearest unpaired pillar that is 10 to 15 m away (synced settings), within
  4 m of its height, with water at least `Min Water Depth` deep (default 1 m) at the
  midpoint and at points 15 m out on each side of the surface. A side that is too shallow is marked unusable as an
  exit; a pair with neither side deep enough does not pair.
- **Placement preview:** while a pillar ghost is held, a line runs to the pillar it would pair with, green, or red
  with the reason and its numbers ("Too shallow at the red marks: 0.8 m deep, a ship needs 1 m"). The nine measured
  spots show as posts from the seabed up out of the water (green deep enough, red not) and the two exit lanes as
  lines on the water, so the player sees exactly where to dig or what to move. Looking at a placed pillar that isn't
  paired shows the same, with "Fix it and the gate opens by itself".
- **Retry:** every 3 s, each unpaired pillar's owner checks again, so lowering the seabed, moving a pillar or removing
  a stray one opens the gate without rebuilding; a player within 40 m is told "The sea gate is open".
- **Stored:** each pillar gets `wf_sg_id` and `wf_sg_partner` (the partner's id) and `wf_sg_sides`. A pair counts
  only when each names the other; a one-sided link reads as unpaired. The anchor is the pillar with the smaller id
  (decided by the ids, so no write can race): it holds the gate's own fields, name, `wf_mode` and `wf_owner`, and its
  id is the gate's id (an old `wf_sg_dest` from 0.2.0 is left in place and never read). The other pillar forwards
  interactions to it. The new pillar writes its own ZDO; the partner's link is written by an owner-side RPC on the
  partner (`wf_SeaGatePair`), the same shape as `wf_SetMode`.
- **The surface** is client-side only: built when both pillars are loaded, a quad from below the water to the
  pillar tops wearing the game portal's own swirl effect, at full brightness. Destroying
  either pillar unpairs the other and the surface fades out.

### Name and access

- E on either pillar opens a text box to name the gate; Alt+E cycles the access mode, as on portals. Naming needs
  the same right as cycling the mode (unowned, own, or admin).
- The helmsman's picker (below) lists only sea gates, from the server: `wf_RequestSeaGates`/`wf_SeaGateList` (the
  same shape as `wf_RequestPortals`, with the list's version, sent when the picker opens and every 5 s while it is
  open, answered only when the list changed), built from the pillars the server holds. The server keeps an index of
  every pillar (`SeaGateScan`): one pass over the world spread over frames right after it loaded, then each pillar
  ZDO the server creates or receives (a `ZDOMan.AddIfPortal` postfix); no request ever searches the world. A jump
  request arriving before that first pass is done is left unanswered, and the ship's owner asks again 2 s later. It shows only gates `PortalAccess.May` allows for the helmsman.
- Sea gates never appear in the walk-in portal targeting list; they show on the map with an icon of their own.
- The `NoPortals` world modifier closes sea gates too.

### The jump

1. **Crossing (owner only).** Each frame the machine that owns a ship checks it against every loaded gate in the
   gate's own frame (`GateGeometry`; no physics triggers, so no layer questions). The ship stops when either end of
   its float box is a quarter of the ship's length through the surface inside the span, while its centre is still on
   the near side, so the ship is seen sailing in. Only a steered ship stops. With nobody at the helm, `NoPortals` or
   restricted cargo, nothing happens: the ship sails through and the crew sees why. Nothing ever half-happens.
2. **Stop and pick.** The owner freezes the ship where it is (`wf_sg_state` = 4, choosing; `wf_sg_speed`, a new
   `wf_sg_stop` id, `wf_sg_sgate`, `wf_sg_sside`). Every crew client (the ship it steers, `Player.GetControlledShip`,
   else the one it stands aboard, `Ship.GetLocalShip`) sees the state and opens the large map as the sea gate picker,
   the source gate in gold. Each sends its map pointer as a world point to every other crew member's peer
   (`wf_SeaGatePointer` on the ship's `ZNetView`: stop id, x, z; 10 Hz while moving, 2 Hz at rest, nothing off the
   map), drawn as an arrow with the player's name, the helmsman's gold (asked 2026-10-05, so the crew can point gates
   out). Only the helmsman picks; anyone else closing the map only closes their own. The helmsman's click sends `wf_SeaGatePick` (stop id, gate id) on the ship's `ZNetView` to the owner,
   which takes it only from the helmsman's machine and only for this stop, writes `wf_sg_picked` and asks the server:
   `wf_SeaGateRequest` (source gate id, picked gate id, ship). The server checks both gates exist and are paired,
   access to the picked gate for the helmsman and `NoPortals`, then replies `wf_SeaGateGrant` (both destination pillar
   positions, which sides are usable) or `wf_SeaGateDeny` (with the picked gate). A denial is told to the crew and
   clears the pick, the map stays open. Closing the map sends a pick of 0: the ship sails on with its speed and is
   not stopped by that gate again until it has left the 30 m zone or its centre has crossed (`wf_sg_sailon`,
   `wf_sg_sailside`, kept in the ZDO so a new owner knows). Leaving the helm or switching sea gates off sails on too.
3. **Cargo check.** The owner checks the ship's container (`Inventory.IsTeleportable`) and every crew member's
   `wf_sg_heavy` flag. Each client keeps that flag up to date on its own player ZDO, once a second while aboard a
   ship, from `Player.IsTeleportable`. `Allow Restricted Cargo` (synced, default off) skips both.
4. **Freeze.** On the grant the ship is already kinematic where it stopped; the owner writes the jump to the ship's
   ZDO: `wf_sg_jump` (id), `wf_sg_state` = 1 (frozen), `wf_sg_speed` (from the stop), `wf_sg_crew` (count from `Ship.m_players`, which includes remote
   players), `wf_sg_until` (timeout). It then sends `wf_SeaGateJump` to every crew member's peer: the jump id, the
   ship, the destination pose and the fallback spot.
5. **Crew (each client, for its own player).** Leave the helm or seat, remembering the helm. Record the player's
   position and facing in the ship's local space, measured against this client's own copy of the ship (the one the
   player is standing on, so lag doesn't matter). Clear the cooldown, then `TeleportTo` the destination with
   `distantTeleport: true`, which gives the game's own fade and zone streaming.
6. **Move.** 1.5 s later (`wf_sg_moveat`), once every crew member's teleport screen has fully faded in (the game
   fades it over 1 s) and every crew client has measured against a ship that hasn't moved, the owner puts the ship at the destination pose (`wf_sg_state` = 2, moved). The pose is the ship's pose relative to the source gate, carried over to the
   destination gate: the same position across the span, the same angle, half a ship length plus 4 m beyond the
   surface, at the same height above the water. The exit is on the same side as the entry, relative to the gate;
   if that side is unusable at the destination, the ship comes out on the other side, turned around.
7. **Settle (whoever owns the ship now).** Once the owner's game has loaded the destination and finds water there,
   it checks the exit pose for another ship or a large collider. If something is there, it slides the pose outward
   in 2 m steps, up to 20 m, until clear. Then it writes `wf_sg_state` = 3 (settled).
8. **Land (each crew client).** A `Player.UpdateTeleport` prefix holds the player the way vanilla does (hidden, at
   the target, zero velocity) but never lets vanilla finish. It waits until the ship's ZDO shows this jump settled
   and the local ship copy is within 1 m of its ZDO pose, and all of that has held for 1 s more (the loading screen
   stays up while the area finishes streaming in; the fallback ashore waits the same second). Then it places the player on the live ship at the
   recorded deck spot, resets `m_maxAirAltitude`, ends the teleport itself, starts the protection, sends
   `wf_SeaGateLanded` to the ship and takes the helm back if the player had it.
9. **Release (owner).** When every crew member has landed, or the timeout has passed, the ship goes back to
   physics with its stored speed along its new heading, `wf_sg_state` = 0. If nobody takes the helm, it coasts and
   slows as normal.

### Safety nets

- **Frozen means frozen everywhere.** Prefixes on `Ship.Awake` and `Ship.CustomFixedUpdate` keep any ship whose ZDO
  says `wf_sg_state` != 0 kinematic, with no buoyancy and no upside-down damage. This holds on every client and for
  every instance, including one recreated after its zone unloads mid-jump or after an ownership change.
- **Protection:** for `Protection Seconds` (default 5) after landing, the local player takes no damage, no fall
  damage and does not drown (prefix on `Character.RPC_Damage` for the local player, which owns its own character),
  and the ship takes no damage (`wf_sg_safe` time on the ship's ZDO, checked on the ship's `WearNTear`).
- **Fallback:** if the jump has not settled by `wf_sg_until` (`Crew Wait Seconds`, default 20 s), or the ship never
  appears, the player lands on dry ground beside the destination pillar instead (solid height, above sea level,
  2 to 8 m out, else the pillar's top), still protected, and reports landed so the ship isn't kept waiting. Never in
  open water, never in the air.
- **Disconnects:** if the owner leaves, the jump continues with the next owner from the ZDO. If a crew member
  leaves, the ship stops waiting at the timeout.

### Settings (section `Sea Gates`, synced)

`Enabled` (true), `Pillar Recipe`, `Allow Restricted Cargo` (false), `Min Gate Width` (10), `Max Gate Width` (15),
`Min Water Depth` (1), `Protection Seconds` (5), `Crew Wait Seconds` (20).

### Own names

- Prefab `WF_SeaGatePillar`.
- Pillar ZDO keys `wf_sg_id`, `wf_sg_partner`, `wf_sg_sides`, `wf_sg_name` (plus `wf_mode`/`wf_owner` on the
  anchor).
- Ship ZDO keys `wf_sg_jump`, `wf_sg_state`, `wf_sg_speed`, `wf_sg_crew`, `wf_sg_landed`, `wf_sg_until`,
  `wf_sg_safe`, and for the stop `wf_sg_stop`, `wf_sg_sgate`, `wf_sg_sside`, `wf_sg_picked`, `wf_sg_sailon`,
  `wf_sg_sailside`.
- Player ZDO key `wf_sg_heavy`.
- RPCs: `wf_SeaGatePair`, `wf_SeaGateUnpair` (on the pillar's `ZNetView`); `wf_SeaGateEdit` (routed, to the
  server) forwarded as `wf_SeaGateSetName`/`wf_SeaGateSetMode` (on the anchor's `ZNetView`, from the server
  only); `wf_RequestSeaGates`/`wf_SeaGateList` and `wf_SeaGateRequest`/`wf_SeaGateGrant`/`wf_SeaGateDeny`
  (routed, server); `wf_SeaGateJump` (routed, to each
  crew peer); `wf_SeaGateLanded`, `wf_SeaGatePick` and `wf_SeaGatePointer` (on the ship's `ZNetView`).

### Layout

```
Wayfare/Wayfare/src/SeaGates/
  foundation   SeaGateFields (keys, ids, RPC names), GateGeometry, SeaGateRegistry (loaded gates), JumpOrder,
               SeaGateWords, SeaGateDriver (per-frame ticks)
  pillar       SeaGatePiece, SeaGatePieceRecipe, SeaGateFooting, SeaGatePillar, SeaGatePillarHover
  pairing      SeaGatePairing, SeaGatePairRules, SeaGateDepth, SeaGatePreview, SeaGatePreviewLine
  surface      SeaGateSurface, SeaGateSurfaceView/Frame/Template/Effect/Fit/Particles/Sheet/Look
  list, map    SeaGateIndex, SeaGateIndexServer, SeaGateScan, SeaGatePicker, SeaGatePickerEdits, SeaGatePickerHint,
               SeaGatePointers, SeaGatePointerMarks, SeaGateMapDriver, SeaGateMapIcons, SeaGateMapSprites
  grant        SeaGateGrant, SeaGateGrantServer, CargoCheck
  ship         ShipJump, ShipFreeze, JumpShips, JumpCrossing, JumpChoice, JumpBegin, JumpPose, JumpSettle,
               JumpClearance
  crew         CrewJump, CrewHold, CrewSpot, CrewHelm, CrewFallback, CrewReport, CrewLogout, JumpProtection
```

### Details settled while building

- **Access is judged for the helmsman**: the grant request carries the ship, and the server reads its helm's `user`
  key when the asking machine owns it (a dedicated server owns ships near the world centre and has no player).
- **Landing reports survive ownership changes**: `wf_SeaGateLanded` carries (jump id, player id); the owner records
  each player's report in the ship's ZDO (`wf_sg_landed_<player>`), and the crew client re-sends once a second to the
  current owner until the ZDO shows it counted.
- **Release**: 1 s after the latest landing (`wf_sg_landedat`), so nobody standing 5 cm above the deck is left
  behind by a ship at full sail; water must be loaded under all five points the ship floats on. The ship waits
  max(crew wait, 5) + 5 s, so a held crew member's own fallback ashore always fires first; within 2 s of that the crew
  no longer lands on deck.
- **Settle**: a new owner waits 3 s in the zone before searching for a clear exit (objects stream in after the server
  learns its position); when two ships settle at one gate, the later jump (by `wf_sg_moveat`, then jump id) gives way.
- **Crew**: an order is obeyed whatever the local settings say; a crew member who cannot come answers at once; a
  player held in a jump who logs out is saved beside the destination gate, never at sea.
- **Crossing** counts only while the ship moves across (over 0.2 m/s); a ship drifting through with nobody at the
  helm is told why once.
- Not taken along: tamed animals and loose items on deck.

### Test checklist (dedicated server, two clients)

- A Longship at full sail with two players aboard, one at the helm and one on deck, into a gate: the ship stops, the
  map opens for both, each sees the other's named pointer, the deck player's click does not pick, the helmsman's
  click on a gate in a zone neither has visited this session
  jumps; both land on deck at their spots, the helmsman is back at the helm, the ship keeps its speed.
- Closing the map in the gate: the ship sails on through at its speed and does not stop again until it leaves or
  turns back through. A Private gate of another player picked: the reason shows, the map stays open.
- Two new gates, never touched: each reaches the other with no setup.
- A ship parked across the destination exit: the arriving ship is placed clear of it.
- The owner, then a crew member, disconnects mid-jump: the other player still lands on deck or on the fallback.
- Ore in the cargo, and ore in a crew member's pocket, with `Allow Restricted Cargo` off (no jump, reason shown)
  and on (jump).
- A destination with one shallow side: the ship exits on the deep side.
- Raft, Karve, Drakkar and a modded ship.
- A swimmer through the surface: nothing. Pillars never appear in the walk-in portal list.
- Server restart: pairs and names are kept. One pillar destroyed: the other unpairs and the surface goes.

## Jump speed (moved from OpenKeep on 2026-10-04, untested in Wayfare)

The user, 2026-10-04: "The fast teleporting option needs to be removed from openkeep and added to wayfare mod.
Wayfare is the teleportation mod". OpenKeep built it in its Homestead section (1.12.0, 2026-09-30, measured in game
there); it moved here with its key names into section `Jump Speed`: `Quick Portals` (true), `Quick Portal Range`
(10000, 10 to 20000), `Quick Portal Seconds` (0.5, 0 to 8), all synced; `Screen Only When Loading` (true; OpenKeep
called it `Portal Screen Only When Loading`) and `Quick Area Loading` (true), unsynced. Everything is gated on the
master `Enabled` too ("portals behave as vanilla" when off). The distance-scaled wait, the server settle and the fast
land and object loading are the AreaLoading library (`../ValheimModLibs/AreaLoading`), shared with OpenKeep, which
keeps them for its respawn; each mod's merged copy hurries only for its own reason, so with both installed a jump is
Wayfare's and a respawn OpenKeep's. An OpenKeep older than the move would hurry a jump a second time: release both
together.

- Quick Portals: `Player.UpdateTeleport` moves the player once `m_teleportTimer` passes 2 s and lands a long jump
  (`m_distantTeleport`) once it passes 8 s and the area is ready. A prefix on the local player's client advances the
  timer faster (map distance between `m_teleportFromPos` and `m_teleportTargetPos`: `Quick Portal Seconds` at 0 m,
  8 s at `Quick Portal Range`), never past 8 s, so the 15 s no-floor fallback and the area check stay the game's. The
  move comes after at most 0.25 s, since the target only loads once the player is there. Every long jump counts: the
  game's portals, Wayfare's map targeting, other mods' `TeleportTo(distantTeleport)`, the console's `goto`; dungeon
  doors are short jumps and are left alone. A sea gate jump is left alone too: its crew hold replaces the game's step
  and pins the crew to the ship (`CrewHold.Holding`), and its screen stays the game's.
- Screen Only When Loading: in the `TeleportTo` postfix the client asks `ZNetScene.IsAreaReady(target)`: true only
  when the target zone is loaded and every object the client knows there exists (about 100-150 m with the default
  simulation distance). Then `Hud.UpdateBlackScreen` fades out as when nothing holds the screen; otherwise a quick
  jump's screen goes black in 0.2 s (before the move) and the game's screen runs. Decided once per jump, so it never
  flickers; dead or sleeping players keep the game's screen.
- Quick Area Loading (measured in OpenKeep 2026-09-30: a 983 m jump took 10.6-11.8 s with the timer done at 4.3-4.7
  s): `ZoneSystem.Update` spawns one zone per 0.1 s and `ZNetScene` creates no near object until every zone of the
  simulation circle is in (57 zones at near distance 4). While the local player's jump loads behind the screen, the
  library calls `CreateLocalZones` for 20 ms a frame and creates the listed objects for 15 ms a `CreateDestroyObjects`
  run, in the game's order, only in zones whose land is loaded. Measured after: 1 km 2.1 s, 500 m 1.45 s, 250 m 0.7 s
  in single player. Not on a dedicated server.
- Server settle: on a server's client `IsAreaReady` is vacuous right after arriving (nothing received yet); the
  game's 8 s floor covered that. The landing holds until the ZDO count in the target's 3x3 sectors is unchanged for
  0.5 s (`AreaSettle`), never past the game's 8 s from the start; on the server or alone it is settled at once.
- Multiplayer: the jump runs on the jumping player's own client (the game's `UpdateTeleport` is owner-only); the
  speed keys come from the server.

### Jump speed test checklist

1. Two portals 20 m apart: through in about half a second (log `Wayfare: jump of 20 m takes 0.6 s`), no black
   screen or swirl (log `jump target ... is loaded already`). 1 km apart: about 2 s with the game's screen. 3 km: the
   game's 8 s, longer while the area loads. A dungeon door: unchanged. `Quick Portals = false`: 8 s.
   `Screen Only When Loading = false`: the screen for every jump. `Enabled = false`: the game's jumps.
2. Quick Area Loading (simulation distance 4): the 1 km jump's log says `Wayfare: loaded the land around you in ...`;
   `Quick Area Loading = false`: 11-12 s again. With OpenKeep too: a jump logs only Wayfare's line, a respawn only
   OpenKeep's.
3. Dedicated server, a big base 1 km from a portal: the jump lands on the base's floors (never on the ground below
   them), at most about 8 s. The server's `Quick Portal Seconds = 3` applies to every client.
4. Sea gate with Quick Portals on: the crew still lands on the deck after the ship settles, with the game's fade.
