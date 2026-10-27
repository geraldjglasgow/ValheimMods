# BlueprintShelf

A mod's blueprints in a window in the game's own panel look, opened at a station: the player's own files (with
folders) on the left, the world's shared pool on the server on the right. Made from OpenKeep's Blueprint Bench window
on 2026-10-23.

- Each side is a file explorer: the way up, folders, files; click, Ctrl / Shift + click, double click opens, right click
  or F2 renames, Delete deletes (the game's yes / no box first), drag onto a folder moves.
- Drag across, or Share / Share all / Take a copy: files go into the player's own part of the pool or come back as
  copies (never overwriting: " (2)", " (3)"), gzipped and sent in parts as routed RPCs.
- The server keeps the pool in `BepInEx/config/<PoolFolder>/<world>/<name>_<character id>/`, checks every request
  (switch, character, paths valid on Windows and Linux, sizes, counts, the host's check of the file) and lets only the
  sharer or an admin change a part.
- The window is copies of the crafting panel's parts (PackPanel's timber skin shed, so PackPanel skins the copy
  itself), treated as one of the game's windows through WindowInput.

What the host gives (`ShelfSettings`): its log name, canvas name, RPC prefix, pool folder, file extension, the
player's files (`IShelfFiles`; `ShelfFolder` for a plain folder of files), its words (`ShelfWords`: a word's key and a
key's text, by default the library's English), icons, limits, an on switch, a check of received files, the rename key,
the reach, and a row of its own buttons (`ShelfButton`) under the two sides. `Shelf.Folder`, `PickedFiles`, `Station`
and `Confirm` are for those buttons; `ShelfPrompt` is the game's name box.

```csharp
Shelf shelf = Shelf.Create(new ShelfSettings
{
    Name = "My mod plans", RpcPrefix = "MyMod_Shelf", PoolFolder = "MyMod.SharedPlans", CanvasName = "MyMod_Plans",
    Files = new ShelfFolder(Path.Combine(Paths.ConfigPath, "MyMod.Plans"), ".json", words),
    Words = words,
    Buttons = { new ShelfButton("Build it", () => shelf.PickedFiles.Count == 1, Build) },
}, harmony);
// at the station, after the ward check:
shelf.Open(this);
```

Create the shelf on every machine (a dedicated server keeps the pool). The patches (`ZNet.Awake` for the RPCs,
`ConnectPanel.Update` for F2, WindowInput's) go in once per merged copy, by hand. Consumers: OpenKeep (the Blueprint
Bench), AssetLab (Broadside's ship blueprint table).
