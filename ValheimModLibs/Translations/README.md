# Translations

A mod's player-facing words in the game's language.

- `Lang.Install(harmony, assembly, "<Mod>", logger)`: once in the mod's `Awake`, after `PatchAll`. It loads the embedded
  English and installs one postfix on `Localization.SetupLanguage(string)`, by hand, once per merged copy.
- `Lang.Get(key)`, `Lang.Format(key, values...)` (fills `$1`, `$2` ...), `Lang.Token(key)` (`$key`), `Lang.Has(key)`,
  `Lang.Add(key, english)` (a word made from data at runtime), `Lang.Language`, `Lang.Version` and `Lang.Changed` (for
  caches of built text). Keys are written without the `$`.
- `LangWire.Message(key, values...)` and `LangWire.Read(text)`: text that travels to another peer as a key and values, so
  a server's announcement reaches every player in that player's own language.

## Where the words come from

English ships in the mod as `translations/English.yml` (or several `English.<part>.yml`), flat `key: "text"` maps,
embedded by one csproj line:

```xml
<EmbeddedResource Include="translations\*.yml" LogicalName="<Mod>.translations.%(Filename)%(Extension)" />
```

Whenever the game sets a language up (at start and on every change in its settings), the words of that language are
built, each source replacing the one before it key by key:

1. the embedded English;
2. embedded `translations/<Language>.yml` (and `<Language>.<part>.yml`), a translation shipped with the mod;
3. `<Mod>.translations.<Language>.yml` anywhere under `BepInEx/plugins`, a translation pack;
4. `<Mod>.translations.<Language>.yml` in `BepInEx/config`, a player's own file, which wins.

A translation may be partial: what it leaves out stays English. `<Language>` is the game's own name for it
(`Localization.GetSelectedLanguage()`: `English`, `German`, `Portuguese_Brazilian` ...). A translation mod that writes its
language into the game's English (so the game still says `English`) is served by an `English` file in 3 or 4. Every
word is then written into the game's word table and its cache of localized texts emptied, so `$key` tokens in item names,
status effects, hover texts and messages translate as the game's own words do. The log names the files used and how
many words they translated.

Only the game's own word table (`Localization.instance`, or the first one while the game is still making it) sets
`Lang.Language`. Some mods make spare tables set up in English to name their items in config files (OdinHorse and
others); a spare table gets our words in its language and leaves the player's language alone, so it cannot turn every
other mod's text back to English.

## Rules for the words

- Keys: lowercase `[a-z0-9_]`. The game ends a `$key` token at any of ` (){}[]+-!?/\&%,.:-=<>`.
- One line per word, the text double-quoted, `\n` for a line break. A real line break inside quotes makes YamlDotNet
  reject the whole file (logged with its line; every word of it shows as `[key]`).
- Whole sentences with `$1`, `$2` ... for values, so a translation can reorder them; markup stays in code.
- Never keep translated text in a static field or a field initialiser: build it at use, or rebuild a cache when
  `Lang.Version` changes.

Merged into the consuming mod with ILRepack like every library here (it uses YamlDotNet, which the mod merges too).
Consumers: EliteCreaturesReborn, EliteCreaturesPack, GrindstoneSkills, OpenKeep, PackPanel, EarthWright, Wayfare,
EliteEquipment, EliteBuildingPieces, Hearthhold, Party, ShipConfig, Lockstep, FeastMaster. EliteCrafting's
`Text/Words` is an older copy of the same idea (the same file names) and is to move onto it.
