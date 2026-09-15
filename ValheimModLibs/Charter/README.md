# Charter

Lets a server's configuration bind every player on it. The server writes the charter; players live under it.
Our own code, GPL-3.0 like the rest of ValheimModLibs, written from `SPEC.md` and the game's decompiled assembly only.

## Vocabulary

Charter: one mod's set of settings the server can make binding. Clause: one `ConfigEntry<T>` registered with it.
Article: a pushed value that is not an entry (string, `List<string>`, int, float, bool); an ordinary article
travels only while the charter binds, a standing one (`standing: true`, state rather than settings) always.
Binding: the master switch; while the author's binding is on, clauses and ordinary articles apply to every player. Author: the side whose
values are the truth (the server; a player under no binding is the author of its own values). Steward: a player
on the server's admin list, or the host; stewards may amend bound clauses, the change goes to the author.

## API

```csharp
Charter charter = new(GUID, "My Mod", Version, oldestAccepted: null, mandatory: true);
Clause<float> chance = charter.Clause(config.Bind("Feature", "Chance", 10f, "Percent."));
charter.Clause(config.Bind("Display", "Show icons", true, "Client side."), local: true);   // never pushed, never bound
charter.Binding(config.Bind("General", "Bind Players", true, "Server only. When on, every player uses the server's values."));   // bool only
Article<string> summary = new(charter, "summary", "", standing: true);   // pushed even while unbound
Article<List<string>> files = new(charter, "files", new());               // ordinary: only while bound
summary.Changed += () => Redraw(summary.Value);
summary.Assign("...");                        // on the author of its own values; warning on a bound player
Charter.Install(harmony);                     // once, after all registrations (the SyncedConfig facade does it in Finish)

charter.IsBound; charter.IsAuthor; charter.MayAmend; charter.IsSteward; charter.LastPush; charter.PushedBytes;
charter.Pushed += first => ...; charter.BindingChanged += ...; charter.StewardChanged += ...;
Charter.Verbosity = Verbosity.Trace;          // Quiet, Normal, Trace; also `charter trace on|off` in the console
chance.AuthorValue; chance.OwnValue;          // the server's value and the value in this player's own .cfg
```

Console: `charter` / `charter status`, `charter diff [title]`, `charter versions`, `charter trace on|off`.

## How it works

- A push is a `ZPackage` per fragment: protocol 1, guid, per-peer sequence, fragment index and count, the
  compression flag, then up to 64 KiB of bytes. The body is flags (bound, steward), clause records (section, key,
  TOML), article records (name, type tag, value) and a trailing notice string. Bodies above 1 KiB travel GZipped;
  a body above 8 MiB is refused on the author with an error line.
- The first push goes out in a `ZNet.RPC_PeerInfo` postfix on the server, once the game accepted the peer. Later
  pushes carry only what changed in that frame. Fragments leave one per frame per peer.
- On the player every push is applied on receipt: bound clauses take the author's value (written to the entry
  only, never to the .cfg), articles raise `Changed`, then `Pushed(first)`. Player edits to a bound clause are
  reverted; a steward's edit is sent to the author, which sets and saves its entry and pushes it back. When the
  connection ends the entries return to the player's own values.
- The family join check: every charter registers in `AppDomain` data under `"Charter.Family"`; the first copy to
  `Install` leads, sends `Charter_Family` to a new peer, compares the answer, refuses with `Charter_Refuse` (a
  six-character refusal code in both logs) and disconnects a second later. The player shows the message on the
  connection-failed panel through a `FejdStartup.ShowConnectError` postfix.

## Decisions the specification left open

- **Rejection notice.** The push body ends with a notice string (empty normally). When the author rejects an
  amendment it pushes the clause's current value plus the reason to that peer only, which reverts the steward's
  entry and logs the reason; no extra RPC name is needed. `Charter_<guid>_Ack` is not used: fragments go out one
  per frame and are held while the peer's socket still queues more than 512 KiB, which is enough flow control.
- **Own value tracking.** `OwnValue` is the entry's value at registration, updated on every entry change the
  library did not make itself (a .cfg edit, the hot reload, the configuration manager, mod code). BepInEx has
  already written such an edit to the player's .cfg before the library sees it, so after a disconnect the entry
  shows what the file holds, as the checklist demands.
- **Unbound pushes.** While the binding is off the first push carries the binding clause and the standing
  articles only; the full set of clauses and ordinary articles follows in one push when the binding turns on.
  A player also ignores an ordinary article record that would arrive while unbound.
- **Ordinary articles at unbind.** Like at disconnect, an article keeps its last value when the binding turns
  off; the library remembers no own value for articles (a YAML hub re-reads its own files on its next reload).
- **Binding clause on players.** The binding entry is applied on players even while unbound, so F1 shows the
  server's switch; it is restored to the player's own value at disconnect like every other clause.
- **Assign on a player.** `Article.Assign` is allowed wherever `IsAuthor` is true, so an unbound player (or a
  player on a server without the mod) can assign its own local value; only a bound player gets the warning.
- **Steward changes at runtime.** The server checks the admin list once a second and sends a flags-only push to
  a peer whose steward status changed.
- **Verbosity across copies.** `charter trace` reaches every copy through one more AppDomain slot per guid,
  `"Charter.Verbosity.<guid>"` holding an `Action<int>`.
- **Empty descriptions.** Entries bound with no description share BepInEx's one `ConfigDescription.Empty`; the
  read-only tag is not added there because it would mark every such entry of every mod.
- **Player self-refusal.** When the player's lead finds the server lacks one of its mandatory mods, it stores the message
  and closes the socket; the game's own peer update then reports the disconnect and the panel shows the message.
- **Frame source.** A hidden `DontDestroyOnLoad` MonoBehaviour (`Ticker`) drives the once-per-frame work of every
  charter of a copy: flushing changes, sending fragments, the steward check, session end detection.
- **References.** `assembly_utils` is referenced besides `assembly_valheim`: `ZPackage.Write` has overloads taking
  its `Vector2i`/`Vector2s`, and overload resolution needs the assembly even though no such value is written.
- **Class named like its namespace.** Consumers outside the namespace write `Charter.Charter` for the class.
