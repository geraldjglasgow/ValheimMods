# Party

Shared parties for Valheim: membership, chat, health bars, always-on map visibility, friendly-fire protection,
and a public API other mods can build on.

## Install

Client and server. Install on a dedicated server to have it enforce the gameplay rules (party size, friendly
fire, invite timeout) for everyone; without a modded server, Party still works peer-to-peer for whoever has it,
using the hosting player as the party's server.

## Commands

`/party invite|leave|remove|promote|p` always works. The short forms below are registered too, unless another
installed mod already owns that word - if a short form doesn't respond, use the `/party` form instead.

| Command | Short form | What it does |
| --- | --- | --- |
| `/party invite <name>` | `/invite <name>` | Invites an online player. If you have no party yet, this creates one and makes you its leader. Any member can invite, not just the leader. |
| `/party leave` | `/leave` | Leaves your current party. |
| `/party remove <name>` | `/remove <name>` | Leader only. Removes that member. |
| `/party promote <name>` | `/promote <name>` | Leader only. Hands leadership to that member. |
| `/party p [text]` | `/p [text]` | With text, sends a party-only chat message. With no text, toggles party-chat mode: everything you type goes to the party until you toggle it off again (a `[Party Chat]` indicator shows while it's on). |
| `/party panel edit` / `/party panel done` | - | Frees your mouse so you can drag the health panel; `done` (or Escape) returns to normal play. |

`/invite` tab-completes against everyone online; `/remove` and `/promote` tab-complete against your own party.

Every command answers you in chat, success or failure: no such player, you're not the leader, the party is full,
and so on. An invite is a prompt the other player can accept or decline; either way you find out what happened.

## Party membership

- One leader, size capped by a server setting (default 8). A player is in at most one party at a time.
- The server owns the roster. Members are tracked by their persistent player ID, never by name, so two players
  with the same name are never confused for one another.
- Logging out doesn't remove you - your party is still yours when you return, and parties survive a server
  restart. While you're offline you simply show as offline to your party.
- If the leader leaves, leadership passes to whoever has been in the party longest and is currently online.
- When the last member leaves, the party dissolves.

## Health panel

A panel on the left of the screen lists your party: name and a live health bar, offline members greyed out.
Drag it anywhere with `/party panel edit` (Escape or `/party panel done` to finish) - it remembers where you put
it. Position, scale, opacity, bar size, row spacing, font size, whether your own row shows, and whether stamina
and eitr show alongside health are all yours to set; none of it is pushed by the server.

## Colored names and map pins

A party member's floating name is drawn in your party color (the leader marked distinctly) instead of the usual
one when you're near them. Party members are always visible on your map and minimap in that color, even when
their own public-position sharing is off - this only applies within your party; everyone else is still subject
to the normal sharing rules.

## Friendly fire

On by default, a server setting: party members can't hurt each other even with PvP on, covering melee,
projectiles and area damage alike. It only protects against each other - creatures, falls, fire and drowning
are unaffected.

## Party ping

Hold a modifier key (Left Alt by default, rebindable in the config) while pinging the map to send a party-only
ping, drawn in your party color and marked private, instead of the normal server-wide shout ping.

## Configuration

Gameplay settings - `Max Party Size`, `Friendly Fire Protection`, `Invite Timeout Seconds` - are pushed from the
server and can be locked with `Lock Configuration` so clients can't override them. Display settings - colors,
the ping key, everything under `Health Panel` - are always personal to each player and never pushed.

## API for other mods

Other mods can check whether a player is in a party, fetch the members, ask whether two players share a party,
find the leader, and subscribe to change notifications, without hard-depending on Party - if it isn't installed,
your mod simply sees everyone as party-less.

```csharp
// 1. Compile-time reference to Party.dll (players don't need it referenced at runtime, only if they have Party).
// 2. Soft dependency: your mod loads fine whether or not Party is installed.
[BepInDependency(Party.PluginInfo.PluginGuid, BepInDependency.DependencyFlags.SoftDependency)]
public class MyPlugin : BaseUnityPlugin
{
    private bool partyLoaded;

    private void Awake()
    {
        partyLoaded = BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey(Party.PluginInfo.PluginGuid);
    }

    private bool SameParty(long playerIdA, long playerIdB) =>
        partyLoaded && Party.Api.PartyApi.AreInSameParty(playerIdA, playerIdB);
}
```

`Party.Api.PartyApi` is accurate for any player when queried on a dedicated server or the host; on a bare client
it only knows about the local player's own party, since the server never tells a client about anyone else's.

```csharp
bool inParty = PartyApi.IsInParty(playerId);
IReadOnlyList<PartyMemberInfo> members = PartyApi.GetMembers(playerId);
long? leaderId = PartyApi.GetLeader(playerId);
PartyApi.PartyChanged += playerId => { /* a party this player is in changed */ };
PartyApi.MemberJoined += (anchorId, joinedId) => { /* ... */ };
PartyApi.MemberLeft += (anchorId, leftId) => { /* ... */ };
PartyApi.LeaderChanged += (anchorId, newLeaderId) => { /* ... */ };
```

A player's persistent ID is `Player.GetPlayerID()` on their own character, or `PlayerProfile.GetPlayerID()`.

## Files

`BepInEx/config/Party.<world>.parties.yml` on the server: the roster, one entry per party. Written by the mod;
edit by hand only to fix a stuck state.

## Building

This mod lives in the `ValheimMods` workspace and follows its standard layout; see the workspace `CLAUDE.md` and
`Party/PLAN.md` (design notes and the judgement calls the specification left open) for how it's built and why it's
shaped the way it is.
