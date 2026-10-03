# CLAUDE.md - EliteCrafting

Runes and magic item inscriptions across three rarities. Read the workspace `../CLAUDE.md` first (layout, small units:
methods at most 24 lines brace to brace including lambdas, classes at most 300 lines, one responsibility; multiplayer
first; display preferences unsynced). The behaviour lives in `features/`; `PLAN.md`'s Decisions log overrides them
where they disagree. Game signatures: decompile `assembly_valheim.dll` with `ilspycmd` into the scratch folder and grep
it (never copy from it).

Identity: GUID `com.EliteCrafting`, name `EliteCrafting`, version in `EliteCrafting.cs` (`PluginVersion`), custom-data and
ZDO key prefix `ecf_`, console command `ecraft`, YAML families `EliteCrafting_inscriptions*.yml` / `EliteCrafting_economy*.yml`.
**Vocabulary (user decisions 2026-10-01 and 2026-10-02):** players, the console, the .cfg, the YAML and item data say
**inscription** and **rune**; the code and the `features/` specs keep the internal words **affix** (`AffixDef`,
`AffixRoll`, `features/affixes.md`) and **stone** (`StoneDef`, `StoneVerb`, `Stones/`, `features/stones.md`, loc keys
`$ecf_stone_<id>`, `$ecf_msg_*`, the effect id `find_stones`). Item data key `ecf_inscriptions` (the old `ecf_affixes`
is read when the new key is absent and removed on the next write).
Libraries merged: Charter, ConfigReload, PatchGuard, YamlDotNet (ECR's pattern). **No YamlConfig, no SyncedConfig, no
ItemCopies** (live runes share their prefab's `SharedData` instead).

**Status (2026-10-02): never released.** The crafting was cut to six runes (user decision 2026-10-02, PLAN.md
Decisions log): Normal, Magic and Rare only; Awakening, Shaping, Ascension, Consecrated, Cleansing and the Serpent Rune.
Essences, sockets, gems, catalysts, the chisel, salvage and shards, sigils, binding, quality and the other stones are
gone, from the code, the YAML and the words. Builds clean, both default YAML families parse with no issue; nothing
tested in game yet. Next: the in-game test plan in `features/multiplayer.md` section 6, then packaging (no icon yet).

## Build

```
"/c/Program Files/dotnet/dotnet" build EliteCrafting/EliteCrafting.csproj -c Release
```

It must end with 0 errors before you hand back. Output: `dist/EliteCrafting.dll`, copied to `LocalTesting` too.

## Folder ownership

| Folder | Owner | Responsibility |
|---|---|---|
| `EliteCrafting.cs` | spine | plugin entry; calls every `*Feature.Init`. Nobody else edits it |
| `Core/` | spine | `Log`, `Embedded` (resources), `EnumIds<T>` (snake_case ↔ enum), `Numbers` (invariant), `Ids`, `Colors` |
| `Affixes/` | spine | item state in `m_customData`: `ItemKeys`, `AffixRoll`, `ItemState`, `ItemStateBuilder`, `ItemStateCache`, codec, writer, migrations |
| `Rules/` | spine | YAML models, loading, merge, validation, Charter sync, hot reload, `ActiveRules` |
| `Config/` | spine | `ServerBinding` (the one Charter), `ModSettings` (the .cfg) |
| `Rolling/` | contract spine, **implementation: Stones** | `ItemRoller`, `RollContext`, `RollOutcome` |
| `Items/` | Items | `ItemSlots` and `ItemTier` are spine (implemented); rune prefabs, upgrade carry-over, stack warning go in `ItemsFeature` + new files |
| `Stones/` | Stones | the runes: click gesture, pipeline, refusals, verbs |
| `Effects/` | Effects | `EffectRegistry`/`EffectDef`/`ItemEffects` are contract; `EffectCatalog` (the registered ids) and everything else is Effects' |
| `Display/` | Display | names, tooltip block, ground glow, crafting panel upgrade tab, item / armor stand hovers |
| `Loot/` | Loot | rune drops and pre-rolled gear drops, chests, killer loot-find stats, the Elite Creatures Reborn hook |
| `Commands/` | Commands | `ecraft` |
| `Localization/` | spine | `Words` (namespace `EliteCrafting.Text`) |
| `translations/English.<area>.yml` | each area its own file | English words, key without `$` → text |
| `config/*.yml` | spine | embedded default YAML (see below) |

## Rules for parallel agents

(Phase 1 was built by six parallel area agents plus an integration pass. The ownership table and these rules stay
the way to split later phases; a single agent working alone may cross folders but keeps each area's design.)

- Work in your own folder only. Never edit another area's files or the spine; if you need a contract changed, say so
  in your reply instead (what, why, the signature you want).
- Entry point: fill your `<Area>Feature.Init()`. Harmony patches are ordinary `[HarmonyPatch]` classes in your folder;
  the plugin's `PatchAll` applies them. Prefer postfix/prefix, no transpilers.
- Words: add your keys to your own `translations/English.<area>.yml` (create it; every `English.*.yml` is embedded and
  loaded). `Words.Add(key, text)` exists for data-driven keys. Keys follow `features/localization.md`.
- Precompute from the rules on `ActiveRules.RulesChanged`, never per frame; hot paths read `ItemState.Read(item)` only.
- Namespace traps: the game has global classes `Settings` and `Localization`; ours are `ModSettings` and namespace
  `EliteCrafting.Text`. The rules facade is `ActiveRules`, not `Rules` (a class named like the namespace
  `EliteCrafting.Rules` would not bind from sibling namespaces).

## Contracts

**Item state** (`Affixes/`, item-data.md). `ItemState.Read(ItemData)` → immutable `ItemState` (cached; plain items
return `ItemState.Empty` without a lookup). `IsMagic`, `RarityId`, `Rarity` (resolved `RarityDef`, null for Normal or
unknown), `IsUnknownRarity`, `Affixes` (`AffixRoll` id/tier/value), `AffixCount`, `DefinitionAt(i)` (null = orphaned),
`IsActiveAt(i)` (defined and enabled; otherwise dormant), `EffectRolls` (the active affixes: what Effects read),
`IsSealed`/`SealedReason`, `Unreadable`, `IsNewerFormat` (runes refuse). To change an item: `state.ToBuilder()`
(`SetRarity`, `AddAffix` (appends), `RemoveAffix`, `ReplaceAffix` (in place), `ClearAffixes`, `Seal`) → `Build()` →
`ItemState.Write(item, newState)`, the only write path (refuses stackable items, runes, newer-format states).
`ItemStateCache.Written` fires after every write. Keys kept: `ecf_v`, `ecf_rarity`, `ecf_inscriptions`, `ecf_sealed`,
`ecf_tier`. `ItemKeys.RetiredKeys` (`ecf_bound`, `ecf_refine`, `ecf_sigil`, `ecf_sockets`, `ecf_gems`, `ecf_catalyst`,
from the removed systems) are never read and are removed on the next write. `ItemMigrations` maps old rarity ids in
memory: `common` → Normal, `uncommon` → `magic`, `epic`/`legendary`/`mythic` → `rare`.

**Rules** (`Rules/`). `ActiveRules.Current` (immutable `RuleSet`, swapped atomically) and `ActiveRules.RulesChanged`.
`RuleSet.Affixes` (`AffixRules`: `Get(id)`, `Affixes`, `Channels` (`ChannelDef`, index = `AffixDef.ChannelIndex`, `Cap`),
`Pool(slot)`, health-critical thresholds) and `RuleSet.Economy` (`EconomyRules`: `Rarities` in ladder order,
`Rarity(id)`, `Next`/`Previous`, `BaseRarity`, `Rolling`, `Stones`, `Stone(id)`, `StoneForPrefab`, `ItemTiers`,
`Biomes`/`BiomeTier`, `Drops`, `StoneDraw(tier)`, `GearRarityDraw(tier, boss)`). `StoneCatalog.BuiltInIds` are the six
rune ids (`awakening`, `shaping`, `ascension`, `consecrated`, `cleansing`, `serpent`); `PrefabFor(id)` gives
`ECF_` + PascalCase id. The YAML tunes or disables them but cannot add a rune. `StoneVerb`: `Promote`, `Add`, `Strip`,
`Corrupt`; `CorruptOutcome`: `SealOnly`, `AddInscription`, `ChaoticReroll`. `Drops.Chests` (`StoneChance`,
`GearChance`, `Containers`: prefab → `CreatureDrop`); `Drops.Ecr` (`EcrDrops`). `FamilySpec` id lists `rarities` and
`runes` merge by id. `ActiveRules.ReloadLocal()` re-reads this machine's files (`ecraft reload`) and returns a
`FamilyReload` per family (`Applied`, `Rejected`, `Bound`, `NoFiles`). `ActiveRules.SourcesInForce(FamilySpec.Affixes|Economy)`
→ `RuleSources` (internal): the file texts the running model was built from, in layer order, `DefaultsLayered`,
`FromServer` (a bound player gets the server's texts). `StoneDef.Description` and `StoneDef.ItemWeight` (YAML
`description`, `item_weight`).

**Items** (`Items/`). `ItemSlots.Classify(item)` → `SlotInfo` (slot, hands, traits, governing skills; cached per
SharedData), `SlotOf`, `IsMagicBase`, `IsStone`, `Satisfies(slotInfo, affix.Requires)`, `Id(slot)`/`TryParse`.
`ItemTier.Of(item | prefabName)`, `ItemTier.Explain(prefab)` (tier + source, for `ecraft tiers`), `ItemTier.PrefabName(item)`,
`ItemTier.RefreshRecipes()` (recipe index up to date, tier cache dropped when rebuilt), `ItemTier.HasRecipe(prefab)`
(never call the internal `RecipeIndex.Refresh` from outside Items). Rune prefabs: `StonePrefabs.Get(runeId)`,
`GetByPrefabName`, `IsRegistered`, `IsBuilt`, `IsStonePrefab(prefab)` (the six runes, built from code on every peer,
registered in ObjectDB and ZNetScene before any inventory or ZDO; every live rune is linked to its prefab's
`SharedData` in an `ItemDrop.Awake` postfix, so the economy YAML's name, description, stack and weight reach every
stack). A rune is recognised by its drop prefab name (`ECF_...`, `ItemSlots.IsStone`; never a magic base); spawned
runes set `m_worldLevel` so they stack. `StoneStackGuard` keeps a rune stack whole when it loads larger than the
current max stack. `StoneVisuals.Tint(runeId)`, `HasTint(runeId)`, `TintOfPrefab(prefab)`, `Headless` (stable: Display
reads them; a rune without a tint glows white). A promote rune takes the colour of the rarity it produces.

**Effects** (`Effects/`). `EffectRegistry.TryGet/Get/IsRegistered/All`; a new effect is a line in `EffectCatalog`
(registration closes when the rules load). `ItemEffects.CollectLocal(list, scratch)` fills the local player's active
affixes on counted equipment (`ActiveAffix`: item, roll, def, slot, channel); `EquippedItems`, `CollectItem`,
`IsEquippedByLocalPlayer`, `Enabled` (the `Affix effects` switch). `EffectTotals.Snapshot()` → `EffectSnapshot`
(per channel sum, applied value after caps, sources; health-critical state; `States`: the Phase 2 runtime states in
force, e.g. `ward 12 left`, `evader's fury`, `momentum`) for `ecraft stats`. The aggregate is
rebuilt at most once per frame after `ItemStateCache.Written` on an equipped item, equipment set-up, inventory change,
spawn, rules change, the `Affix effects` switch and teleport end.
Network surface (all `features/effects-runtime.md` section 7): routed RPCs `ECF_KillRestore` (creature owner → the
killer's peer: Reaper / Soul Reaper) and `ECF_MeleeDodged` (attacker's peer → the dodger's peer: Evader's Fury), both
registered at `ZNet.Awake` on every peer, argument-less; the status effect `ECF_Hamstring` (in ObjectDB on every
peer); player-ZDO floats written by the player's own client when they change: `ecf_daze`, `ecf_light`, `ecf_demist`,
`ecf_taming`, `ecf_sail`, `ecf_yield_mining`, `ecf_yield_lumber`, `ecf_harvest` (readers clamp to the caps); summon ZDO
floats `ecf_summon_damage`, `ecf_summon_health` (the caster, at spawn). The four loot-find effects (`find_*`) are
registered here but applied by Loot.

**Rolling** (`Rolling/`, implemented by Stones). `ItemRoller.RollFresh(state, rarity, ctx)` (a dropped item),
`AddAffixes(state, count, ctx)` (Shaping, Consecrated, the Serpent's extra inscription), `Promote(state, toRarity, ctx)`
(Awakening, Ascension: adds `max(new.min - count, promote_adds_at_least)`, not past the new maximum) and
`RollChaotic(state, rarity, ctx)` (the Serpent: every affix out, the count drawn in the rarity's range, any tier the
affix defines) → `RollOutcome` (new state or `RollFailure`: `NoEligibleAffix`, `NotMagicBase`, `Full`, `NewerFormat`);
pure, never write. `RollContext.For(item, tierFloor)`; `RollContext.Random` defaults to `RollRandom.Create()`
(independently seeded; never `new Random()` per roll).

**Loot** (`Loot/`). Runs on the dying creature's ZDO owner only (`DeathPatch`, prefix on `Character.OnDeath`).
Debug surface: `LootRoller.Simulate(tier, stars, kills, creaturePrefab?)` → `LootSimulation` (`ToString` prints totals),
`LootRoller.SpawnAt(position, tier, stars, creaturePrefab?)`, `LootPreview.Explain(Character)`, `GearPool.Bases` and
`GearPool.ForTier(tier)` (the drop-eligible bases), `GearFactory.Build(...)` (a pre-rolled item, written through
`ItemState.Write`). ZDO keys (effects-runtime.md section 7): `ecf_ally_hit` (creature), `ecf_filled` (a world container
rolled once, on its owner, when the game fills it: `Loot/ChestFillPatch`), the player's own `ecf_find_rarity`,
`ecf_find_stones`, `ecf_find_trophy`, `ecf_find_coins` (`Loot/FindPublisher`, read from the last hitter by the creature's
owner). Elite Creatures Reborn keys, read only, only when ECR's GUID is loaded: `ecr_resolved`, `ecr_stars`,
`ecr_asp_worthless`, `ecr_tier` (`Loot/EcrKeys`; `ecr_tier` is not written by ECR yet).

**Stones** (`Stones/`, the runes). `InventoryGui.OnSelectedItem` prefix (local player), `StonePipeline.Evaluate(job)`
(read-only, 10 checks then the verb as a dry run), `ConfirmGate.Pass` (Cleansing and Serpent: `confirm: true`), then
`StoneCommit` (one `ItemState.Write`, then the cost). Four verbs (`StoneVerbs`): `PromoteVerb`, `AddVerb`, `StripVerb`,
`CorruptVerb`. The Serpent draws its outcome by weight; an outcome that cannot be carried out falls back to sealing
only, and every outcome seals (`ecf_sealed = serpent`). A sealed item refuses every rune.

**Settings** (`Config/ModSettings`): gameplay entries are Charter clauses (synced, locked while the server binds);
display, glow, confirm and diagnostics are local. Sections `1 - General`, `2 - Runes`, `3 - Drops`, `4 - Commands`,
`5 - Display (per player)`, `6 - Ground glow (per player)`, `7 - Diagnostics`, `8 - Elite Creatures Reborn` (`Synergy`,
synced, default off). `ServerBinding.Charter` is the only Charter.

**Words** (`Localization/Words`, namespace `EliteCrafting.Text`): `Words.Localize(text, params words)`, `Words.Add`,
`Words.Has`, and `Words.Changed` (after a language setup and after `Add`; drop cached localized text there).

**Display** (`Display/`). Draws on the viewing client only; its caches drop on `ItemStateCache.Written`,
`ActiveRules.RulesChanged`, `Words.Changed` and a display setting change. No state of its own in items or ZDOs.
Surfaces: grid tooltip title, tooltip block (`ItemData.GetTooltip` postfix), ground hover, pickup message, ground glow,
the crafting panel's upgrade tab (`CraftingPanel`: the upgrade target's block under the recipe, names colored through
the label color) and item / armor stand hovers (`StandHover`, the item decoded from the stand ZDO by `StandItems` once
per ZDO revision). Every per-frame surface memoises its last input and output.

## YAML defaults

`config/EliteCrafting_inscriptions.yml` is generated from `features/affixes.md` (section 4 catalog) by scripts kept outside
the repo; `config/EliteCrafting_economy.yml` is written from `features/economy-yaml.md` (rarities, rolling, runes,
item tiers, biomes, drops with chests and ecr). Both load with 0 errors and 0 warnings.
**Affix tiers count down** (user decision 2026-10-01): in the YAML, the tooltip and the console, tier 1 is the
strongest row. The code, the tier window and stored item data use the strength grade instead (grade = 8 - tier, so it
lines up with item and biome tiers); `Core/AffixTierNumbers` is the only conversion, called by `AffixTierParser`,
`StoneParser` (`tier_floor`), `AffixLines` and the commands. The catalog in `features/affixes.md` writes `T7–T1`,
weakest first. **Decision: the default affix file lists only affixes whose effect is registered** (162 affixes on 115
effects). An affix naming an unregistered effect is an error even when `enabled: false`, so typos never hide behind a
disabled flag. New affixes are added to the defaults as their effects are registered; because the built-in defaults
are a layer under the owner's files, new affixes reach existing servers without anyone editing a file.

Merge rules (configuration.md section 3): layers = built-in defaults (unless the main file says
`use_defaults: false`), the main file, then other files by name. Id lists (`affixes`, `rarities`, `runes`) merge by
id field by field; maps merge by key; other lists and scalars are replaced; a key set to null is removed. Errors reject
the family and keep the previous rules; at startup the author falls back to the built-in defaults alone and publishes
that. Sync: each family is one Charter article (the file texts); a bound player builds from the built-in defaults plus
the server's files.

## Player reference

The store README links here for the full reference; keep it in step with the code and the default YAML.

### Rarities

| Rarity | Inscriptions | Color |
|---|---|---|
| Normal | none: a plain item | white, no glow |
| Magic | 1-2 | green |
| Rare | 3-6 | blue |

### Runes

Pick up a stack of runes in the inventory and click it onto an item in your own inventory (not in an open chest).
One rune is used per success; a refused rune is kept. A rune dropped on another rune stacks or swaps as usual. The
Cleansing and Serpent Runes cannot be undone and ask first: hold Shift while you click (or switch
`Confirm destructive runes` to a dialog).

| Rune | Works on | Does | Drops from |
|---|---|---|---|
| Awakening Rune | Normal | Makes the item Magic with one inscription | everywhere, most of all in the Meadows; Eikthyr |
| Shaping Rune | Magic | Adds one inscription, up to two | everywhere; Eikthyr |
| Ascension Rune | Magic | Makes it Rare, keeps its inscriptions and adds one (two on a one-inscription item, Rare's minimum is three) | Black Forest and later; the Elder |
| Consecrated Rune | Rare | Adds one inscription, up to six | Swamp and later, more the later the biome; Moder, Yagluth, the Queen, the Fader, half the time Bonemass |
| Cleansing Rune | Magic, Rare | Strips it back to Normal: every inscription is lost | everywhere |
| Serpent Rune | Magic, Rare | Corrupts it and **seals** it for good, with one of three outcomes: nothing more (35%), one inscription past the cap, a 3rd on Magic or a 7th on Rare (35%), or a chaotic reroll: every inscription rolled again at any tier, ignoring the item's limits (30%) | Swamp and later; sometimes Bonemass, Yagluth and the Fader |

A sealed item takes no rune again. Every rune's odds, costs and the rarities it accepts are in
`EliteCrafting_economy.yml`.

A refusal says why, for example "The Ascension Rune does not work on Rare items", "This item cannot hold another
inscription", "No inscription can roll on this item", or "Unequip this item first" when the server does not allow
changing equipped items.

### Inscriptions

Each inscription has seven tiers, and they count down: **T1 is the strongest roll, T7 the weakest**. How strong an item can
roll depends on the item, not on where it dropped: a Meadows item rolls only T7, a Swamp item T5 to T7, an Ashlands
item T1 to T3. The tooltip shows each inscription's tier next to it.

| Where | Inscriptions |
|---|---|
| Weapons (damage) | Honed Might, Primal Fury, Nightstalker; the brands Emberbrand, Rimebrand, Stormbrand, Venombrand, Spiritbrand, Bonebreaker, Keen Edge, Needlepoint; Undead, Beast and Sea Slayer, Godslayer; Ambusher, Cruel Opening, Press the Advantage, Deathblow; Berserkergang (while health-critical) |
| Weapons (on hit and kill) | Reaper, Soul Reaper, Blood Drinker, Cornered Thirst, Seidr Siphon, Evader's Fury, Hamstring, Staggering Blows, Dazing Blows, Fafnir's Greed |
| Melee weapons | Balanced Grip, Long Reach, Sweeping Arc, Blood Price, Rune-Edged, Lone Blade, Steel Rhythm, Heartwood; Blade, Axe, Club, Knife, Spear, Polearm, Fist and Woodcutter's Mastery |
| Bows and crossbows | Easy Draw, Quick Windlass, Swift String, True Flight, Volley, Thrifty Quiver, Skirmisher; Bow and Crossbow Mastery |
| Staves | Seidr Thrift, Blood Thrift, Twincast, Grave-Lord's Command, Grave Vigor; Elemental and Blood Mastery |
| Shields | Stalwart, Perfect Guard, Repelling Guard, Tireless Guard, Keen Guard, Anchored Guard, Seidr Riposte, Shield Mastery |
| Armor: health and regeneration | Vigor, Endurance, Wellspring, Troll Blood, Second Wind, Seidr Flow, Mending, Stout Heart, Restless Mind, Purity, Resolute, Quick Recovery, Valhalla's Edge |
| Armor: protection | Hardened, Padded, Mailed, Riveted, Ironclad, Arrowward, Flameward, Frostward, Stormward, Venomward, Elemental Ward, the Fire, Frost, Lightning and Poison Bulwarks, Mist Veil, Bramblehide, Runic Ward, Ironroot, Coldblood, Ashen Skin; the Cornered Blood, Hide and Veil variants |
| Movement | Fleetfoot, Stride, Momentum, Pathfinder, Mountain Goat, Marshstrider, Spring-Heeled, Light Leap, Nimble, Soft Landing, Long Wind, Strong Swimmer, Raven's Glide, Ghostwalk, Soft Tread, Pack Mule, Cornered Flight, Wanderer's Mastery |
| Weather and world | Emberheart, Winterborn, Oilskin, Sealegs, Shadowmeld, Hearthlight (a light everyone sees), Mistbane, Fair Winds, Beast Whisperer, Hearthbound |
| Utility items and helmets | Broad Back, Magpie, Huginn's Eye, Mimir's Insight, Artisan's Mastery, Gourmand, Soulbound, Brewer's Haste, Forsaken Favour, Reflex Draught, Swift Draught, Harvester; loot find: Norns' Favour, Fateweaver, Trophy Taker, Hoardfinder |
| Tools | Builder's Reach, Tireless Hands, Green Thumb, Miner's Mastery, Angler's Mastery, Deep Vein |
| Most gear | Well-Forged and Everlasting (durability), Lightened and Gossamer (weight), Supple Fit (no movement penalty) |

`ecraft list inscriptions` in the console prints the full list with slots and tiers. Evader's Fury, Steel Rhythm and a
charged Runic Ward show an icon on the HUD while they are active.

### Console commands

Open the console with F5. Everything is under one command, `ecraft`. Output is English.

| Command | Who | Does |
|---|---|---|
| `ecraft help` | everyone | Lists the sub-commands you may run |
| `ecraft inspect [cursor\|hover\|ground\|<slot>]` | everyone* | An item's EliteCrafting data and what it means |
| `ecraft stats` | everyone* | Your summed inscription totals and the effects active right now |
| `ecraft list inscriptions\|runes\|rarities [<filter>]` | everyone* | The configuration in force, filtered by slot, category, rarity or id |
| `ecraft give <rune>\|all [count]` | admin | Runes into your inventory |
| `ecraft roll <rarity> <prefab\|slot> [tier]` | admin | A rolled magic item into your inventory |
| `ecraft reroll [cursor\|hover\|<slot>]` | admin | Rerolls an item's inscriptions, keeping its rarity |
| `ecraft inscribe <inscription> [tier] [value] [cursor\|hover\|<slot>]` | admin | Adds or replaces one inscription, for testing |
| `ecraft reload` | admin, on the machine whose files are in force | Re-reads the YAML, the translations and the `.cfg` now |
| `ecraft dump inscriptions\|economy\|items` | admin | Writes the merged configuration in force, or a survey of every item, to the config folder |
| `ecraft tiers` | admin | Writes `EliteCrafting_item_tiers_reference.yml`: every magic base with its tier and why |
| `ecraft ecr` | everyone* | The Elite Creatures Reborn synergy: installed or not, the switch, and what the creature you look at would pay |

\* unless the server turns `Read-only commands for everyone` off. `<slot>` is an equipment slot: `right`, `left`,
`head`, `chest`, `legs`, `cape`, `utility`.

### Files

| File | What |
|---|---|
| `BepInEx/config/com.EliteCrafting.cfg` | Switches and preferences. Gameplay keys (inscription effects, modifying equipped items, rune and gear drops, command access, the Elite Creatures Reborn synergy) follow the server; display, ground glow, the confirm mode and diagnostics are per player |
| `BepInEx/config/EliteCrafting_inscriptions.yml` | Every inscription: effect, slots, category, tiers, weights, caps |
| `BepInEx/config/EliteCrafting_economy.yml` | Rarities and colors, rolling rules, runes, item tiers, biomes and drop tables (creatures, bosses, chests, Elite Creatures Reborn) |
| `EliteCrafting_inscriptions_<anything>.yml`, `EliteCrafting_economy_<anything>.yml` | Your own additions, read after the main file in name order; they change only what they name |
| `EliteCrafting.translations.<Language>.yml` | Your own words for a language, key to text, over the built-in English |

The main YAML files are written once with the full defaults and never rewritten. The built-in defaults always sit
underneath, so a later release's new inscriptions reach your server without editing anything; `use_defaults: false` in a
main file makes the files the whole configuration. A file with an error is reported in the log with file and line,
and the previous rules stay in force. Turn an inscription off with `enabled: false` (items that have it keep it, greyed and
inert, and get it back when you turn it on) or stop it rolling with `weight: 0`.
