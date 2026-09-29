# DevBridge

A dev-only BepInEx plugin that lets an agent (or a script) see and drive the running game over HTTP on localhost:
screenshots, the live UI tree, clicks, key presses, mouse, console commands, the log, reflection on any object, and
the ZDOs of nearby objects. It exists to test the mods in this workspace in the real game.

It is **never packed, uploaded or shipped**. It has no `thunderstore/` folder, so `pack.ps1 -All` skips it, and no
mod depends on it. It belongs in the `LocalTesting` profile only.

## Install

```
dotnet build DevBridge/DevBridge/DevBridge.csproj -c Release
```

The build copies `DevBridge.dll` into the `LocalTesting` profile's plugins folder. Start the profile from r2modman;
the log shows `DevBridge listening on http://127.0.0.1:7780/`. Rebuilding while the game runs takes effect on the
next start: BepInEx does not reload plugins.

The port is set in `BepInEx/config/com.DevBridge.cfg` (default 7780). A second game, or a dedicated server with
DevBridge on the same machine, gets the next free port up to 7789. On a dedicated server everything except
screenshots and input works.

## Using it

`curl -s http://127.0.0.1:7780/help` lists every endpoint with its arguments. Arguments go in the query string or a
form body, so text with spaces is easiest as `curl -s -G --data-urlencode "cmd=spawn Troll 1" .../console`. In
PowerShell call `curl.exe`, not the `curl` alias.

| Endpoint | What it does |
| --- | --- |
| `/status` | state (`starting`, `menu`, `loading`, `ingame`), world, network role, player, open screens, time, plugins |
| `/wait` | `seconds=N`, or `for=menu`, `for=player` (in the world, loading screen gone), `for=log&grep=text` |
| `/screenshot` | writes a PNG or JPG (`out=`, `maxWidth=1600`, `crop=x,y,w,h`), replies with the path and sizes |
| `/ui`, `/find` | indented tree of the live hierarchy with components, text, sprites and screen rectangles; search by text, name or component |
| `/click`, `/hover` | UI pointer events on an element by `path=` or at screen pixel `x=&y=` (top-left origin) |
| `/key`, `/type` | real key presses through the Input System (`tap=E`, `hold=W&seconds=2`, `down=`/`up=`); set an input field's text |
| `/mouse` | the mouse device: move to `x,y`, turn with `dx,dy`, `click=left`, `down`/`up`, `scroll` |
| `/console` | runs a console command and returns what the console printed |
| `/log` | numbered recent log lines (`since=`, `level=warning`, `grep=`), or the console's (`buffer=console`) |
| `/eval` | reflection: `Player.m_localPlayer.m_runSpeed`, `$hover.Character.GetLevel()`, `Terminal.m_cheat = true`, `members=1` |
| `/nearby` | networked objects near the player with creature, item, container and piece details and ZDO ids |
| `/zdo` | every value a ZDO stores (`id=`, `hover=1`, `nearest=Troll`), key names recovered from the game's and plugins' strings |

Screen coordinates are always pixels of the game window from the top-left, the same as in a full-size screenshot. When
a screenshot is shrunk (`maxWidth`), scale by `screen / image` from its reply before clicking.

### A test loop

1. Build the mod; ask the user to start (or restart) the `LocalTesting` profile.
2. `wait?for=menu&timeout=300`, then either the user loads a world or the agent clicks through the menu.
3. `wait?for=player&timeout=300`, then `status`.
4. Set up the scene with `console` (`devcommands`, `god`, `spawn`, `pos`) and `eval`.
5. Act with `key`, `mouse`, `click`; look with `screenshot`, `ui`, `nearby`, `zdo`; read errors with
   `log?level=warning&since=N`.

## Notes and limits

- The game keeps running and accepting injected input while it is in the background (the bridge turns on
  `runInBackground` and the Input System's ignore-focus mode). Keep the window visible, not minimized, or screenshots
  come out blank, and keep the real mouse cursor off the game window, since the OS still sends it mouse moves there.
- `/click` sends pointer events straight to UI elements; it cannot trigger anything that polls the mouse. Use `/mouse`
  for that (drags, camera, world interaction).
- Text typed into input fields is set with `/type`; key presses from `/key` do not type characters into them.
- Release anything held with `/key?up=all` when done.
- The bridge never launches or closes the game. Starting and restarting it stays with the user.
- Listening on 127.0.0.1 only, with no authentication: any local process can drive the game while it runs.
