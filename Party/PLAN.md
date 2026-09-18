# Party plan

Shared parties: membership, chat, health bars, map visibility, friendly-fire protection, an API other mods can
build on. This document is the design and the roadmap while the mod is unfinished, written from the user's
specification and the game's decompiled assembly, the way every mod in this workspace is built (see the
workspace `CLAUDE.md` and root `CLEANROOM.md`).

## Identity and ownership

- A player is identified by `PlayerProfile`'s persistent ID (the number in the character save, `ZDOVars.s_playerID`
  on their own character ZDO, and already tracked by the game server-side as `ZNetPeer.m_playerID` once the
  `PlayerID` RPC arrives - verified in `ZNet.RPC_PlayerID`). Never by display name: two players can share a name.
- The server is authoritative for the roster. On a dedicated server every player is a `ZNetPeer`; on a hosted game
  the host has no peer of itself, so its own identity comes from `Player.m_localPlayer.GetPlayerID()` once it
  spawns (`Player.OnSpawned`), exactly the join hook Lockstep already uses for the same reason.
- Persistent storage is server-side only, one YAML file per world (`BepInEx/config/Party.<world>.parties.yml`),
  written on every change and polled for admin edits like every other file in this workspace. Parties are not
  ZDO state: there is nothing to attach them to that every client would load, and the spec requires them to
  survive a server restart, not just a disconnect.

## Command surface: collision handling

The user asked for **detection with a `/party` fallback** over pure documentation. Decision, since detection
timing depends on Harmony postfix order on `Terminal.InitTerminal` and that order is not fully controllable:

- `/party invite|leave|remove|promote|p <...>` is **always** registered, first, and is the form the README leads
  with. It can never collide: no other mod plausibly owns the literal word `party` as a namespaced subcommand.
- The short forms `/invite`, `/leave`, `/remove`, `/promote`, `/p` are registered **only if `Terminal.ConsoleCommand`
  has no entry for that word yet** at the moment our `InitTerminal` postfix runs. Our postfix carries
  `HarmonyPriority(Priority.Low)` so as many other mods as possible have already registered by the time we check -
  this is a best effort, not a guarantee, and is documented as such in the README rather than promised as perfect.
- A short form that lost the race is simply not registered; the long form still works. Nothing is ever silently
  shadowed.

## RPCs

Every RPC name is prefixed `Party_`. All are registered on `ZRoutedRpc.instance`; handlers no-op unless the
condition they need (`IsServer`, a receiving client) holds, the pattern already used by Lockstep's `ProgressServer`.

- `Party_Invite` (member -> server, string targetName): creates a party of one, leader themselves, if the sender
  has none yet. Any member may invite - the spec marks only `/remove` and `/promote` "leader only", not `/invite`.
- `Party_InvitePrompt` (server -> invitee): inviter name, expiry seconds. Client shows the accept/decline prompt.
- `Party_InviteRespond` (invitee -> server, bool accept). The outcome (accepted / declined / timed out / an error)
  reaches the inviter through `Party_Reply` like every other command answer, rather than a dedicated RPC.
- `Party_Leave`, `Party_Remove` (string target), `Party_Promote` (string target) - member/leader -> server; the
  latter two check leadership server side.
- `Party_Reply` (server -> one sender, string text): every command's answer, success or failure, the same
  mechanism as Lockstep's reply RPC.
- `Party_Roster` (server -> each member, on membership change only): that player's own party snapshot - id, name,
  online flag, per member; sent to nobody outside the party, per spec ("clients only ever learn about their own").
- `Party_VitalsReport` (member -> server, ~1/s while alive and in a party: health / stamina / eitr percentage,
  position, position validity): a dedicated server never runs player game logic, so vitals cannot be read off a
  server-side `Character` - they have to be self-reported, exactly like the game's own `RefPos` sharing.
- `Party_VitalsDeliver` (server -> that member's online party members): relays one report. This single channel
  feeds both the health panel and the always-on map pins, since both need current health and position.
- `Party_Chat` (member -> server, string text) and `Party_ChatDeliver` (server -> every online member, sender
  name + text): the server is the only side that knows the roster, so chat fan-out has to go through it.
- `Party_Ping` (member -> server, Vector3) and `Party_PingDeliver` (server -> online party members, name +
  position): reuses the same server-relay shape as chat.

1 second was chosen for the vitals tick as a judgement call: fast enough to read as "live" on a health bar,
cheap enough not to matter at party-size-8. Argue with it in `PartyRpcClient` if it feels wrong in testing.

## Friendly fire

`Character.RPC_Damage` (decompiled, private, runs only on the ZDO owner - i.e. on the victim's own client for a
player) already contains vanilla's own PvP short-circuit:
`(IsPlayer() && !IsPVPEnabled() && attacker != null && attacker.IsPlayer() && !hit.m_ignorePVP) -> return`.
A prefix placed right alongside that check - same method, same owner-side trust model the game already uses for
PvP - adds one more condition: attacker and victim both resolve a persistent player ID, both are members of the
same party, and the friendly-fire config is on. Being in the same method that already gates melee, projectile and
AoE damage (`HitData` carries the source regardless of how it was dealt) covers "melee, projectiles and area
damage alike" for free, and touching nothing else leaves creature, fall, fire and drowning damage untouched. No
server round trip: exactly like vanilla PvP, this is the victim's own client deciding, which is the ownership
model this workspace already builds every feature on.

## Floating names and map pins

- Names: `EnemyHud` (the game's single class for every floating name-and-bar, players included, confirmed by
  `m_baseHudPlayer` and `HudData.m_name`) is publicized by the csproj like the game assembly always is here, so
  its private `m_huds` dictionary and `HudData.m_name` (a `TextMeshProUGUI`) are reachable directly - no
  reflection needed, matching what `Publicize="true"` is for. A polling `MonoBehaviour` (not a Harmony patch:
  nothing needs to be intercepted, only read and re-colored every frame) recolors the name text of any tracked
  character whose player ID is a party member. Left alone otherwise, so nothing changes for strangers.
- Leader marking has no art asset pipeline in this repository yet, so the leader is marked by decoration, not a
  new icon: a distinct display color (its own local setting) plus a bold/size bump on the nameplate text and a
  double-size map pin, rather than a custom sprite. Written down here because it is a judgement call, not a spec
  requirement.
- Map/minimap pins: `Minimap.AddPin(pos, PinType.Player, name, save:false, isChecked:false, ownerID:<player id>)`
  per online member other than the local player, position and existence refreshed every vitals tick;
  `PinData.m_iconElement.color` set to the party color. This runs regardless of that player's own "share position"
  setting, per spec - Party pins are a separate, additional set of pins, never reusing or overriding the vanilla
  shared-position pin.

## Chat

No new `Talker.Type` can be added (it is a compiled enum), so party chat never goes through `Talker`/`Chat.SendText`
at all: `Party_Chat` carries the raw text to the server, the server relays it to online members only, and each
client formats its own colored line (its own color preference, its own party) and calls `Chat.instance.AddString`
- fully custom, no shared vocabulary with vanilla chat types. Toggle mode is a local per-client bool that, when on,
routes the player's chat submission into `Party_Chat` instead of the normal send path, with an indicator drawn in
the chat window.

## Configuration

Gameplay settings are Charter clauses (`SyncedConfiguration.Bind`, `synced: true`, the workspace's standard
server-push-and-lock mechanism, same as ShipConfig/Lockstep): `Max Party Size` (default 8), `Friendly Fire`
(default on), `Invite Timeout Seconds` (default 60), and the one `Lock Configuration` binding switch that makes
all of them non-amendable by players when a server turns it on (default on, matching every other mod here).

Display settings are bound `synced: false` (Charter `local: true`): party color, leader color, ping modifier key,
and every health-panel layout value (position, scale, opacity, bar size, row spacing, font, show-own-row,
show-stamina, show-eitr). Never pushed, per spec.

## Health panel drag / ESC

The spec says "press ESC to free your mouse while you're moving it," which implies the panel is normally not
draggable because the game owns the cursor during play. Decision: a console command `/party panel edit` (and
`/party panel done`) toggles panel-edit mode; while it is on, `Cursor.lockState`/`Cursor.visible` are forced open
so the panel's `GUI.Window` can be dragged, and pressing Escape exits edit mode and returns the cursor to the
game. This is the concrete trigger the spec left open, written down so it can be argued with.

## API

`Party.PartyApi` (public static class, its own events with plain `Action<...>` delegates) - `IsInParty`,
`GetMembers`, `AreInSameParty`, `GetLeader`, plus `PartyChanged`, `MemberJoined`, `MemberLeft`, `LeaderChanged`.
Soft dependency is the standard BepInEx shape, not reflection: a consumer adds a compile-time reference to
`Party.dll`, declares `[BepInDependency(Party.PartyPlugin.PluginGuid, BepInDependency.DependencyFlags.SoftDependency)]`,
and guards every call with a `Chainloader.PluginInfos.ContainsKey(...)` check so a server without Party is simply
seen as party-less. The README carries the copy-paste version of this.

## Status (2026-09-18)

Design pass complete; implementation follows the order: config -> server roster/RPCs -> commands -> chat ->
client state -> health panel -> nameplates -> map pins -> friendly fire -> ping -> API -> docs. Nothing tested
in game yet; this file gets deleted once the mod is verified against the spec, per the workspace's black-box
process, and the README/CHANGELOG carry the behaviour after that.
