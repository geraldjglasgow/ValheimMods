# Party

Shared parties for Valheim: party chat, health bars, map pins and friendly-fire protection for your group.

## Features
- Parties: one leader and a size cap set by the server, kept through logging out and server restarts.
- Invites: the other player accepts or declines; any member can invite.
- Party names: the leader names the party, shown on the health panel.
- Party chat: messages only your party sees, or a mode that sends everything you type to the party.
- Health panel: each member's health, distance and ailments, draggable; arrows point to members off screen.
- Names and map pins in your party colour, even with position sharing off.
- Death notices: a chat line and a temporary map pin.
- Friendly fire: party members cannot hurt each other, even with PvP on.
- Party ping: hold Left Alt while pinging the map and only your party sees it.
- [An API for other mods](https://github.com/geraldjglasgow/ValheimMods/blob/main/Party/PLAN.md#api-for-other-mods),
  without a hard dependency.

## Commands
The short forms `/invite`, `/leave`, `/remove`, `/promote` and `/p` work too, unless another mod took the word.
- `/party create [name]`: starts a party with you as leader.
- `/party invite <name>`: invites an online player; starts a party if you have none.
- `/party leave`: leaves your party.
- `/party remove <name>`: the leader removes a member.
- `/party promote <name>`: the leader hands leadership to a member.
- `/party name [text]`: the leader names the party.
- `/party p [text]`: sends a party message; alone, turns party-chat mode on or off.
- `/party panel edit`: frees the mouse to drag the health panel; `done` or Escape ends it.
- `/party status`: lists every party on the server (admins).

## Install
Needed on the server and every client. Install with r2modman or the Thunderstore app, or put `Party.dll` in
`BepInEx/plugins`.

## Configuration
`BepInEx/config/com.Party.cfg`. Every setting is described in the file and applies without a restart; the server's
gameplay values bind every player (`Lock Configuration`), the display stays your own. The server keeps the parties in
`BepInEx/config/Party.<world>.parties.yml`.

## Links
Discord: https://discord.gg/DrFUyfuXzT

Bugs and ideas: https://github.com/geraldjglasgow/ValheimMods/issues (name the mod and version). Licence: GPL-3.0.

## Shout outs
- The BepInEx and Harmony teams, for the tools every Valheim mod stands on.
- Iron Gate Studio, for Valheim.
- Thunderstore, for hosting this page.
- The Valheim modding community, for the hard work and dedication that keeps enhancing an already great game.
- Every modder who keeps their mods open source so others can collaborate, learn and build on them.
