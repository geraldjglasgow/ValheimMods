# Getting Started

DevBridge opens the running game to HTTP on 127.0.0.1, so a script or an AI agent (an LLM with a shell) can see the
game, play it and check a mod with nobody at the keyboard: screenshots, the UI tree, clicks, keys, console commands,
the log, reflection, method traces, per-mod frame cost, scripted tests and hot reload of a mod's DLL.

This wiki describes version 0.1.0.

## Install

**Development profiles only, never a profile you play in.** There is no password: while the game runs, any program on
your computer can drive it, run code in it with `/eval` and load a DLL with `/reload`.

Install with r2modman or the Thunderstore app into a profile of its own, or put `DevBridge.dll` in `BepInEx/plugins`.
Needs BepInExPack for Valheim. It is local to the game it runs in: the server and other players do not need it, and no
mod depends on it. Start the profile; the log shows `DevBridge listening on http://127.0.0.1:7780/`.

- Listens on 127.0.0.1 only, never the network.
- Refuses web browsers with 403: any request with an `Origin`, `Referer` or `Sec-Fetch-Site` header, or a `Host` other
  than `127.0.0.1` or `localhost`, so a web page cannot reach it. curl, scripts and agent HTTP clients send none of
  these.
- Never launches or closes the game: starting and restarting it stays with you.

## The config file

`BepInEx/config/com.DevBridge.cfg`, created on first start.

| Section | Key | Default | Effect |
| --- | --- | --- | --- |
| `Server` | `Port` | `7780` | The first port tried. When it is taken (a second game, or a dedicated server with DevBridge on this machine) the next nine are tried, up to 7789 by default |

## First calls

Every endpoint takes GET or POST, its arguments in the query string or as form fields. Text with spaces is easiest with
`-G --data-urlencode`. In PowerShell call `curl.exe`, not the `curl` alias.

```
B=http://127.0.0.1:7780
curl -s $B/help                                    # every endpoint with its arguments
curl -s $B/status                                  # state (starting, menu, loading, ingame, server), world, role, player, open screens, plugins
curl -s "$B/wait?for=player&timeout=300"           # until the player is in a world and the loading screen is gone
curl -s "$B/screenshot?maxWidth=1280"              # writes a PNG (or out=<file.png|.jpg>), replies with its path and sizes
curl -s -G --data-urlencode "cmd=spawn Troll 1" $B/console
curl -s "$B/log?level=warning"                     # recent warnings and errors; the last line is next=N
```

- **Replies** are JSON or plain text. Errors are text starting `error:`: 400 a bad argument or missing target, 403 a
  browser, 404 no such endpoint, 500 an exception with its stack, 504 the game did not answer in time (loading,
  frozen, or the call needs a larger `timeout=`; most calls wait 30 s).
- **Screenshots** without `out=` go to `%TEMP%\DevBridge\`. Screen coordinates everywhere are game-window pixels from
  the top-left; a shrunk screenshot's reply gives `screen` and `image` sizes, so scale by `screen / image` before
  clicking.
- **Cheats**: `/console?cmd=devcommands` first (on a server, one you are admin on).

## A test loop

What an agent does to check a mod change:

1. Build the mod into the dev profile's plugins. Ask the user to start (or restart) the profile, or, with the game
   already running, `/reload?mod=<Mod>`.
2. `/wait?for=menu&timeout=300`; the user loads a world, or the agent clicks through the menu with `/click`; then
   `/wait?for=player&timeout=300` and `/status`.
3. Set the scene with `/console` (`devcommands`, `god`, `spawn`, `pos`) and `/eval`.
4. Act with `/key`, `/mouse`, `/click`. Look with `/screenshot` (then read the image), `/ui`, `/nearby`, `/zdo`.
5. Check `/log?level=warning&since=N` for new warnings, `/events?kinds=death&wait=30` for what happened.
6. Once it works, keep it as a [scenario](wiki:Scenarios) and rerun it after every change.

## Good to know

- The game keeps running and taking injected input in the background. Keep its window visible (not minimized) or
  screenshots come out blank, and keep the real mouse off it.
- `/click` sends UI pointer events; what polls the mouse (drags, the camera, the world) needs `/mouse`. `/key` does not
  type into input fields: use `/type`. Release held keys with `/key?up=all` when done.
- Slow motion, overlays, tuning, look swaps and the asset stage change this machine only.
- On a dedicated server most endpoints work; screenshots, contact sheets, overlays, look swaps and input do not, and
  `/time` needs `force=1`. [Endpoints](wiki:Endpoints) has `/sync` to compare it with a client on the same machine.

## Pages

- [Endpoints](wiki:Endpoints) - every endpoint in one line, grouped by what it is for
- [Scenarios](wiki:Scenarios) - scripted tests in JSON: steps, checks, an example

## Help

Full reference: https://github.com/geraldjglasgow/ValheimMods/blob/main/DevBridge/REFERENCE.md. Bugs and ideas:
https://github.com/geraldjglasgow/ValheimMods/issues - name the mod and its version. Source:
https://github.com/geraldjglasgow/ValheimMods (GPL-3.0).
