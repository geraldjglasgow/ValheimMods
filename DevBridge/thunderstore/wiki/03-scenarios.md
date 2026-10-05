# Scenarios

A scenario is a scripted test in JSON: a list of endpoint calls and checks that DevBridge runs inside the game and
reports pass or fail per step. Write one once a manual check works, then rerun it after every change.

```
curl -s "http://127.0.0.1:7780/scenario?file=C:/tests/inventory.json&timeout=60"
curl -s -H "Content-Type: application/json" --data-binary @inventory.json http://127.0.0.1:7780/scenario
```

The reply comes when the run ends: `passed`, the counts, and per step `ok`, `ms`, what it `got` (cut at 200
characters) and, when it failed, the `reason`. One scenario runs at a time. `timeout=` covers the whole run (default
300 s, max 595). Progress goes to the log as `[DevBridge] scenario <name>: ...`, and the end is a `scenario` event. Give
`file=` a full path (`/c/...` works too). The whole file is checked before anything runs, so a typo or an unknown key
fails at once.

## The file

| Key | Meaning |
| --- | --- |
| `name` | Shown in the reply and the log (default: the file name) |
| `description` | Free text |
| `setup`, `steps`, `cleanup` | Lists of steps, run in that order. A file that is just a list is `steps` |
| `continue_on_fail` | `true`: a failed step does not stop the run (default `false`) |

A failed step skips the rest of setup and steps and goes to cleanup; every cleanup step runs. A fifth of the time, at
most 15 s, is kept for cleanup.

## Steps

Each step is one of:

| Step | Does | Its value |
| --- | --- | --- |
| `{"do": "/console", "cmd": "god on"}` | Calls an endpoint; the other keys are its arguments. Not `/scenario` or `/events` | The reply |
| `{"eval": "Player.m_localPlayer.GetHealth()"}` | Runs an `/eval` | What follows `Type = `; a bool as `true` or `false` |
| `{"wait": 2}` | Waits that many seconds | - |
| `{"wait_event": "death", "grep": "Greyling", "timeout": 30}` | Waits for an event of that kind (`*` any), default 30 s, looking from the step before (`since`: `previous`, `run` or `now`) | The event as JSON |
| `{"log_clean": "warning", "grep": "MyMod", "ignore": ["..."]}` | Fails on log lines at that level or worse (`warning`, `error`, `fatal`) since the run started | - |

Any step may add:

| Key | Meaning |
| --- | --- |
| `name` | Its label in the reply and the log |
| `json` | A JSON path into the value: `state`, `network.server`, `[0].id`, `[?(@.prefab == 'Troll')]` |
| `expect` | Equal (`ingame`), or `>0`, `>=5`, `!=0`; numbers compare as numbers |
| `expect_contains`, `expect_not_contains`, `expect_regex` | Text checks, ignoring case |
| `expect_approx`, `tolerance` | A number within the tolerance (default 0.01) |
| `expect_count` | How many: a list's items, the values picked, or text lines (`0`, `>=1`) |
| `save` | Keeps the value; `${name}` in a later step is replaced by it |
| `optional` | Its failure is shown but does not fail the run |
| `continue_on_fail` | `true` or `false` for this step, over the file's |
| `args` | Endpoint arguments whose names clash with step keys, e.g. `"args": {"name": "Inventory"}` for `/find` |

Every check given must hold. A `timeout` in a `do` step goes to the endpoint.

## Example

Opens the inventory, takes a screenshot, closes it, and fails on any new warning:

```json
{
  "name": "inventory",
  "setup": [
    { "name": "in a world", "do": "/wait", "for": "player", "timeout": 20, "json": "met", "expect": "true" },
    { "name": "inventory closed", "eval": "InventoryGui.IsVisible()", "expect": "false" }
  ],
  "steps": [
    { "name": "press Tab", "do": "/key", "tap": "Tab" },
    { "wait": 0.5 },
    { "name": "it is open", "do": "/status", "json": "screens", "expect_contains": "inventory" },
    { "name": "screenshot", "do": "/screenshot", "maxWidth": 1280, "json": "path", "save": "shot" },
    { "name": "press Tab again", "do": "/key", "tap": "Tab" },
    { "wait": 0.5 },
    { "name": "it is closed", "eval": "InventoryGui.IsVisible()", "expect": "false" },
    { "name": "no new warnings", "log_clean": "warning" }
  ],
  "cleanup": [
    { "name": "release every key", "do": "/key", "up": "all" }
  ]
}
```

The reply's `saved.shot` holds the screenshot's path. More examples, including one that spawns, kills and checks a
creature: https://github.com/geraldjglasgow/ValheimMods/tree/main/DevBridge/scenarios. Back to
[Getting Started](wiki:Getting Started) or the [Endpoints](wiki:Endpoints).
