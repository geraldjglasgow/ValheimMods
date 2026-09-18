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
keeps running (it is server code we do not patch out) and still shows "$piece_portal_connected" in the hover text
and plays the connected effect when two same-tagged portals exist, which is cosmetic noise but harmless - nothing
in Wayfare depends on that connection, and no vanilla teleport ever fires from it because entering a portal's
trigger is intercepted (below) before `TeleportWorld.Teleport()` runs.

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
`m_portalPrefabs` - exactly what SPEC item 5 forbids relying on for discovery. But that hash list is also what puts
a ZDO into `ZDOMan.m_portalObjects`, the one category of ZDO the game distributes to *every* connected peer
regardless of distance (confirmed in `ZDOMan.CreateNewZDO` and the receive path in `ZDOMan.RPC_ZDOData`, both call
`AddIfPortal` unconditionally on every machine, client or server, the instant a ZDO is created or received). That
global distribution is exactly what SPEC item 1 needs ("every portal the player may target", not just loaded ones)
and item 4's "game's own ~5s sync cadence" describes (`Game.ConnectPortalsCoroutine`'s `WaitForSeconds(5f)` loop is
the visible half of this channel).

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
vanilla's own reconnect tick, so a portal is never stale longer than vanilla's own tag pairing would be) it re-reads
`ZDOMan.instance.GetPortalList()` and rebuilds the visible/targetable set. Nothing is cached indefinitely (SPEC
item 4).

## Access modes: where enforcement lives

Two different actions need two different authorities, and neither is "trust the client":

- **Setting a portal's mode** (`Portal/RPC_wf_SetMode`) follows the exact shape vanilla already uses for
  `TeleportWorld.RPC_SetTag`: a routed RPC on the portal's own `ZNetView`, handled only where
  `m_nview.IsOwner()` is true. The owner is whichever machine currently replicates that specific portal (any
  client or the server - the same as any other player-placed piece), so "server/owner side" here is the owner
  half: nothing is decided by the requesting client, only applied by whichever machine already has write authority
  for that ZDO, exactly as `RPC_SetTag` already works for the tag.
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
run alongside ours). Decision: Wayfare draws its own overlay, a set of plain `UnityEngine.UI` elements parented
under `Minimap.instance.m_pinRootLarge` (created once, shown/hidden with our own targeting flag), and while
targeting is active a Harmony prefix on `Minimap.OnMapLeftClick`/`OnMapMiddleClick` intercepts the click, checks it
against our own icon rects, and skips the vanilla handler entirely (`return false`) so no vanilla pin is placed or
toggled while targeting. Left the map (any other mode) or pressed cancel (Escape, already the game's own map-close
binding) exits targeting through the same `SetMapMode` postfix that tears the overlay down.

No custom art ships: the portal icon and the favourite-star overlay are small procedural textures built once at
runtime (`Targeting/IconFactory.cs`, `Texture2D.SetPixels` + `Sprite.Create`) - a filled circle for a plain portal,
the same circle with a ring for a favourite, both in a Wayfare-owned colour. This keeps the clean-room boundary
simple (nothing borrowed, nothing to ship as a binary asset) and needs no `thunderstore/` art beyond the store
icon. Labels (the portal's tag) use a plain `UnityEngine.UI.Text` with Unity's built-in legacy font
(`Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf")`) - `TMP_Text` needs a `TMP_FontAsset` we do not have one
of.

## Favourites

Per player, client-side, keyed by `ZDOID` (its string form, stable across a portal rename since the tag is not part
of the key) - stored in `Player.m_customData["Wayfare.favourites"]` as a comma-separated set, the same shape and
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
    Keys.cs                      hotkey polling helper (KeyboardShortcut + no-text-input guard)
  Portals/
    PortalDiscovery.cs            Game.Awake postfix: TeleportWorld-component scan into PortalPrefabHash
    PortalRegistry.cs             5s live re-read of ZDOMan.GetPortalList(), the current portal snapshot
    PortalInfo.cs                 readonly struct: ZDOID, position, tag, mode, owner
    PortalFields.cs               wf_mode / wf_owner ZDO get/set, the Mode enum, default-mode config lookup
    PortalAccess.cs               May(PortalInfo, playerID, isAdmin) - the one access rule, used by client display
                                  filtering and by the server's teleport grant
    ModeCycle.cs                  TeleportWorld.Interact prefix (alt) + RPC_wf_SetMode owner-side handler
  Targeting/
    TargetingSession.cs           TeleportWorldTrigger.OnTriggerEnter prefix; open/close targeting, the source
                                  portal reference, guarded against the load-order incident
    TeleportGate.cs               client: send wf_RequestTeleport, await grant/deny; server: validate + reply
    MapOverlay.cs                 builds/destroys the icon+label objects under m_pinRootLarge
    IconFactory.cs                procedural circle / favourite-ring sprites, built once and cached
    MapClickPatch.cs              Minimap.OnMapLeftClick / OnMapMiddleClick prefixes: hit-test our icons first
    FavouritesPanel.cs            the left-side favourites list UI
    PlayerFavourites.cs           Player.m_customData persistence, keyed by ZDOID string
  HotkeyToggle.cs                 Player.Update postfix: default P toggles portal icons on the small/non-targeting
                                  map (display-only, unsynced)
Wayfare/Wayfare/config/           (none yet - no YAML needed, cfg-only)
```

## Own names

- Plugin GUID `com.Wayfare`, config file `com.Wayfare.cfg` (BepInEx's default from the GUID).
- ZDO keys: `wf_mode` (int, `Portals.Mode`), `wf_owner` (long, player id).
- RPCs: `wf_SetMode` (on the portal's `ZNetView`), `wf_RequestTeleport` / `wf_TeleportGranted` /
  `wf_TeleportDenied` (routed, `ZRoutedRpc`).
- Player custom data: `Wayfare.favourites`.
- Localization keys: `$wf_*`.
- Console: none added - nothing in the spec needs one, and Wayfare's state is either visible on the map itself or
  server-enforced, unlike Lockstep/OpenKeep's admin-facing commands.

## Open questions the spec left for this document (decided here, not asked)

- **Unowned portal default mode**: SPEC item 8 already requires a config toggle
  (`Map / Unowned Portals Are Public`, synced, default true) - `PortalAccess.May` reads it whenever `wf_owner` is
  `0`/absent, rather than treating "no mode set" as a fourth silent state.
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
