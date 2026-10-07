# Changelog

## 0.2.1

- Store page: a fuller overview, with every endpoint listed.

## 0.2.0

- New `/heap`: memory now; `seconds=` puts each hitch beside garbage collections, log lines and spawned objects.
- `/heap?objects=1` counts Unity objects by type, `statics=1` sizes mods' static collections; `mark=`/`diff=` show growth.
- `/events` records from the first `/events` or `/scenario` call, not from game start.
- `/zdo` reads key names on first use; the game no longer scans every plugin at start.
- `/swap` catches new copies as they appear; `/hitbox` draws at most 600 lines.
- Less work every frame for event, log and console capture.

## 0.1.1

- Discord link on the store page.

## 0.1.0

- First release: drive the running game over HTTP on 127.0.0.1, for modders and their AI agents.
- Screenshots, UI tree, clicks, keys, mouse, console, log, eval, ZDOs.
- Time control, contact sheets, overlays, events, traces, per-mod frame cost.
- Scenarios, client/server comparison, live tuning, prefab look swaps, DLL hot reload, asset bundle stage.
- Requests from web browsers are refused.
