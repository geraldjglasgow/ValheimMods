# Party

Shared parties for Valheim: party chat, health bars, map pins and friendly-fire protection for your group.

## Features
- Parties with one leader and a server-set size cap. Invites are accepted or declined, and parties survive logging out
  and server restarts.
- Commands: `/party create`, `invite`, `leave`, `remove`, `promote`, `name`, `p`, `panel edit` and `status` (admin),
  with short forms `/invite`, `/leave`, `/remove`, `/promote` and `/p` unless another mod took the word.
  [All commands](https://github.com/geraldjglasgow/ValheimMods/blob/main/Party/PLAN.md#commands).
- Party chat: `/p <text>` speaks to the party, `/p` alone toggles party-chat mode.
- Health panel: each member's health, distance and ailments, draggable with `/party panel edit`; arrows point to
  members off screen.
- Floating names and map pins in your party color, even with position sharing off.
- Death notices: a chat line and a temporary map pin.
- Friendly fire: party members cannot hurt each other, even with PvP on.
- Party ping: hold Left Alt while pinging the map to ping only your party.
- [An API for other mods](https://github.com/geraldjglasgow/ValheimMods/blob/main/Party/PLAN.md#api-for-other-mods),
  without a hard dependency.

## Install
Needed on the server and every client. Install with r2modman or the Thunderstore app, or put `Party.dll` in
`BepInEx/plugins`.

## Configuration
`BepInEx/config/com.Party.cfg`. Every setting is described in the file and applies without a restart; the server's
gameplay values bind every player (`Lock Configuration`), the display stays your own. The server keeps the parties in
`BepInEx/config/Party.<world>.parties.yml`.

## Links
Bugs and ideas: https://github.com/geraldjglasgow/ValheimMods/issues (name the mod and version). Licence: GPL-3.0.

## Shout outs
- The BepInEx and Harmony teams, for the tools every Valheim mod stands on.
- Iron Gate Studio, for Valheim.
- Thunderstore, for hosting this page.
- The Valheim modding community, for the hard work and dedication that keeps enhancing an already great game.
- Every modder who keeps their mods open source so others can collaborate, learn and build on them.
