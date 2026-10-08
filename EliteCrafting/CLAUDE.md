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
Libraries merged: Charter, ConfigReload, PatchGuard, BundlePrefabs (the rune tablets' bundle `assets/bundles/ecf_runes.*` and the loot beam's `ecf_lootglow.*`), YamlDotNet (ECR's pattern). **No YamlConfig, no SyncedConfig, no
ItemCopies** (live runes share their prefab's `SharedData` instead).

**Status (2026-10-03): 0.1.0 on Thunderstore, a work in progress, never tested in game.** The crafting was cut to six runes (user decision 2026-10-02, PLAN.md
Decisions log): Normal, Magic and Rare only; Awakening, Shaping, Ascension, Consecrated, Cleansing and the Sealed Rune.
The Recasting Rune (rerolls a Magic item) was added as the seventh on 2026-10-04 (user decision), untested. On
2026-10-07 (user decisions) the Shaping and Consecrated Runes were removed (Consecrated was a socket rune for an
hour; the Dvergr Chisel cuts sockets): **five runes**,
Magic holds exactly two inscriptions and Rare three (the Sealed Rune can add one more: four at most), every ladder has
at most 8 tiers, both YAML families are **format 3**, and **no magic gear drops** for now (`Loot/GearDrops.On` is a
`false` constant, the `Magic item drops` setting is gone; the gear path and YAML tables stay for later).
The Epic Loot integration was removed on 2026-10-05 (user decision): EliteCrafting ignores Epic Loot entirely and
behaves the same with or without it.
The old essences, sockets, gems, catalysts, the chisel, salvage and shards, sigils, binding, quality and the other stones
are gone, from the code, the YAML and the words (the Rune Table's essences, 2026-10-06, are a new and separate thing;
sockets and gems came back as a new design on 2026-10-07, `features/sockets.md`, built, never run in game). Builds clean, both default YAML families parse with no issue; nothing
tested in game yet. Next: the in-game test plan in `features/multiplayer.md` section 6, then packaging (no icon yet).
**Item classes, item levels and tier ladders (user decisions 2026-10-05, `features/classes-and-tiers.md`, which wins
over the older specs):** data-driven item classes replace the slot taxonomy, item levels run 1-8 (Deep North), every
inscription has its own tier ladder (1-16 tiers, generated exactly as the planning page does), rarities limit prefixes
and suffixes, and both YAML families and item data are format 2. The 38 Phase 3 effects the default YAML names
(`features/effects-phase3.md`) are merged and registered (2026-10-05); all of it untested in game.

## Build

```
"/c/Program Files/dotnet/dotnet" build EliteCrafting/EliteCrafting.csproj -c Release
```

It must end with 0 errors before you hand back. Output: `dist/EliteCrafting.dll`, copied to `LocalTesting` too.

## Folder ownership

| Folder | Owner | Responsibility |
|---|---|---|
| `EliteCrafting.cs` | spine | plugin entry; calls every `*Feature.Init`. Nobody else edits it |
| `Core/` | spine | `Log`, `Embedded` (resources), `EnumIds<T>` (snake_case ↔ enum), `Numbers` (invariant), `Ids`, `Colors`, `Callbacks<T>` (other mods' callbacks, guarded) |
| `Affixes/` | spine | item state in `m_customData`: `ItemKeys`, `AffixRoll`, `ItemState`, `ItemStateBuilder`, `ItemStateCache`, codec, writer, migrations |
| `Rules/` | spine | YAML models, loading, merge, validation, Charter sync, hot reload, `ActiveRules` |
| `Config/` | spine | `ServerBinding` (the one Charter), `ModSettings` (the .cfg) |
| `Api/` | spine | the public API (`features/api.md`): `EliteCraftingApi` (the facade other mods bind to) and its helpers; see "API" below |
| `Rolling/` | contract spine, **implementation: Stones** | `ItemRoller`, `RollContext`, `RollOutcome` |
| `Items/` | Items | `ItemClasses` (with `ItemClass`, `ClassInfo`, `ClassRegistry`, `ClassClassifier`) and `ItemTier` are spine (implemented); rune prefabs, upgrade carry-over, stack warning go in `ItemsFeature` + new files |
| `Stones/` | Stones | the runes: click gesture, pipeline, refusals, verbs |
| `Effects/` | Effects | `EffectRegistry`/`EffectDef`/`ItemEffects` are contract; `EffectCatalog` (the registered ids) and everything else is Effects' |
| `Display/` | Display | names, tooltip block, ground glow, crafting panel upgrade tab, item / armor stand hovers, icon backdrops (`Backdrops/`), long tooltip scrolling (`Tooltips/`) |
| `Loot/` | Loot | rune drops and pre-rolled gear drops, chests, killer loot-find stats, the Elite Creatures Reborn hook |
| `Commands/` | Commands | `ecraft` |
| `Table/` | Table | the Rune Table: piece, store, essences, trophies, its window (`Table/Window/`); `features/rune-table.md` |
| `Sockets/` | Sockets | which bases take sockets and what each gem gives (`GemCatalog`), the gem's roll, sockets on drops, the switch; `features/sockets.md` |
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
unknown), `IsUnknownRarity`, `Affixes` (`AffixRoll` id/tier/value; `Tier` is the strength grade on the inscription's own
ladder, 1 = its weakest tier T`k`, shown as `AffixDef.ShownTier(grade)` = T(k + 1 - grade)), `AffixCount`, `DefinitionAt(i)` (null = orphaned),
`IsActiveAt(i)` (defined and enabled; otherwise dormant), `EffectRolls` (the active affixes: what Effects read),
`IsSealed`/`SealedReason`, `Unreadable`, `IsNewerFormat` (runes refuse). To change an item: `state.ToBuilder()`
(`SetRarity`, `AddAffix` (appends), `RemoveAffix`, `ReplaceAffix` (in place), `ClearAffixes`, `Seal`) → `Build()` →
`ItemState.Write(item, newState)`, the only write path (refuses stackable items, runes, newer-format states).
`ItemStateCache.Written` fires after every write. Keys kept: `ecf_v`, `ecf_rarity`, `ecf_inscriptions`, `ecf_sealed`,
`ecf_tier`, `ecf_sockets`, `ecf_gems` (sockets.md section 2). `ItemKeys.RetiredKeys` (`ecf_bound`, `ecf_refine`, `ecf_sigil`, `ecf_catalyst`,
from the removed systems) are never read and are removed on the next write. `ItemMigrations` maps old rarity ids in
memory: `common` → Normal, `uncommon` → `magic`, `epic`/`legendary`/`mythic` → `rare`. `ItemKeys.CurrentFormat` is 2
(classes-and-tiers.md section 7): a format-1 item (grades 1-7 over seven tiers) is marked by `ItemMigrations.Upgrade`
(`StateData.LegacyGrades`) and converted each time it is resolved against rules (`ItemMigrations.ResolveGrades`,
`grade' = clamp(round(grade * k / 7), 1, k)` per defined id, values kept, orphans keep their grade); `ItemState`
keeps the unconverted source so a rules change re-converts, and the next write stores format 2.

**Rules** (`Rules/`). `ActiveRules.Current` (immutable `RuleSet`, swapped atomically) and `ActiveRules.RulesChanged`.
`RuleSet.Affixes` (`AffixRules`: `Get(id)`, `Affixes`, `Channels` (`ChannelDef`, index = `AffixDef.ChannelIndex`, `Cap`),
`Pool(classId)` → `PoolEntry` (def + `ClassFit` Best/Allowed, file order), health-critical thresholds) and
`RuleSet.Economy` (`EconomyRules`: `Rarities` in ladder order (`MaxPrefixes`/`MaxSuffixes`, `Limit(kind)`),
`Rarity(id)`, `Next`/`Previous`, `BaseRarity`, `Rolling` (`AllowedClosedFraction`, `PromoteAddsAtLeast`,
`CountWeights`), `Stones`, `Stone(id)`, `StoneForPrefab`, `Classes` (the YAML's `ItemClass`es in order; read them
through `Items.ItemClasses`), `ItemTiers`, `Biomes`/`BiomeTier`, `Drops` (tier lists of 8), `StoneDraw(tier)`,
`GearRarityDraw(tier, boss)`, tiers 1-8). **Format 2** (classes-and-tiers.md): `AffixDef` has `Kind` (YAML `affix`:
prefix/suffix), `Family`, `BestClasses`/`AllowedClasses` (`FitFor(classId)`), `Scaled`, `TierCount` (k) and `Tiers`
(`AffixTierDef`: `Grade` (1 weakest), `Shown` (1 strongest), `Level` (unlocking item level 1-8), `Min`, `Max`,
`Weight` (default the tier number), `Decimals`), `TierRow(grade)`, `ShownTier(grade)`, `GradeOf(shown)`. `tiers` is a
ladder map `{ count, from, min, max }` generated by `TierLadder` (planning-page arithmetic, verified row for row) or
explicit rows `{ tier, level, min, max, weight }`. Caps resolve per channel, most specific key first
(`effect:param@health_critical`, `effect@health_critical`, `effect:param`, `effect`, then the registry default), each
channel capped on its own. Removed keys (`slots`, `rolling.tier_window`, `drops.gear.slot_weights`) are warnings, never
read. Unknown class ids in an inscription are a warning logged at compose (`ClassChecks`, both families and the class
registry in). `RuleFormat`: both families carry `format: 3` (2026-10-07); an older main file on disk is renamed to
`<name>.v<old format>.bak` and the default written in its place, an old extra file or server text is skipped with a warning. `StoneCatalog.BuiltInIds` are the six
rune ids (`awakening`, `recasting`, `ascension`, `cleansing`, `serpent`; the Rune Table's list),
`ChiselId` (`dvergr_chisel`) and `GemIds` (`gem_<god>`, eleven) the socket stones, `AllIds` every one (`IsBuiltIn`,
`IsRune`, `IsGem`, `IsSocketStone`); `PrefabFor(id)` gives
`ECF_` + PascalCase id. The YAML tunes or disables them but cannot add a rune. `StoneVerb`: `Promote`, `Strip`,
`Corrupt`, `Reroll`, `Socket`, `Gem`; `CorruptOutcome`: `SealOnly`, `AddInscription`, `ChaoticReroll`. `Drops.Chests` (`StoneChance`,
`GearChance`, `Containers`: prefab → `CreatureDrop`); `Drops.Ecr` (`EcrDrops`). `FamilySpec` id lists `rarities` and
`runes` merge by id. `ActiveRules.ReloadLocal()` re-reads this machine's files (`ecraft reload`) and returns a
`FamilyReload` per family (`Applied`, `Rejected`, `Bound`, `NoFiles`). `ActiveRules.SourcesInForce(FamilySpec.Affixes|Economy)`
→ `RuleSources` (internal): the file texts the running model was built from, in layer order, `DefaultsLayered`,
`FromServer` (a bound player gets the server's texts). `StoneDef.Description` and `StoneDef.ItemWeight` (YAML
`description`, `item_weight`). `StoneDef.TierFloor` (YAML `tier_floor`, 0 = none): only the best that-many open tiers.

**Items** (`Items/`). Item classes are data (classes-and-tiers.md section 1): `ItemClasses.Classify(item)` →
`ClassInfo` (`Class` (`ItemClass`: id, group, name, rolls, damage_scale, drop_weight, match, items), `ClassId`,
`Rolls`, `DamageScale`, hands, traits, governing skills; cached per SharedData and recomputed after a rules change or a
registration), `ClassOf`, `Get(id)`, `All` (effective classes in order), `Version`, `IsMagicBase` (a class that rolls,
max stack 1, not a rune), `IsStone`, `Satisfies(classInfo, affix.Requires)`. Classification order: a class whose
`items` (or a claim) names the prefab, then classifier callbacks, then the first class with a `match` rule the item
meets, else none; a rune is never classified and PackPanel's backpacks (`$packpanel_backpack_`) never meet a match rule.
Internal registration API behind the public facade (`Api/`, api.md section 2), each bumping `ItemClasses.Version`:
`RegisterClass(ItemClass)` (registered classes sit under the YAML: a YAML class with the same id overrides only the
fields it names), `ClaimItems(classId, prefabs)`, `AddClassifier(id, Func<ItemData, string?>)` / `RemoveClassifier`
(guarded: a throw or an unknown class id is logged once and counts as no answer).
`ItemTier.Of(item | prefabName)` is the item level 1-8 (8 = Deep North), `ItemTier.Explain(prefab)` (level + source, for `ecraft tiers`), `ItemTier.PrefabName(item)`,
`ItemTier.RefreshRecipes()` (recipe index up to date, tier cache dropped when rebuilt), `ItemTier.HasRecipe(prefab)`
(never call the internal `RecipeIndex.Refresh` from outside Items). Rune prefabs: `StonePrefabs.Get(runeId)`,
`GetByPrefabName`, `IsRegistered`, `IsBuilt`, `IsStonePrefab(prefab)` (the five runes, built from code on every peer,
registered in ObjectDB and ZNetScene before any inventory or ZDO; each a copy of the Ruby wearing its rune tablet and icon from the embedded bundle `ecf_runes` (`StoneTablets`, every peer; without the bundle the old tinted group bases); every live rune is linked to its prefab's
`SharedData` in an `ItemDrop.Awake` postfix, so the economy YAML's name, description, stack and weight reach every
stack). A rune is recognised by its drop prefab name (`ECF_...`, `ItemSlots.IsStone`; never a magic base); spawned
runes set `m_worldLevel` so they stack. `StoneStackGuard` keeps a rune stack whole when it loads larger than the
current max stack. `StoneVisuals.Tint(runeId)`, `HasTint(runeId)`, `TintOfPrefab(prefab)`, `Headless` (stable: Display
reads them; a rune without a tint glows white). A promote rune takes the colour of the rarity it produces.

**Effects** (`Effects/`). `EffectRegistry.TryGet/Get/IsRegistered/All`; a new effect is a line in `EffectCatalog`
(registration closes when the rules load; external effects from the API are the one exception, see "API"). `ItemEffects.CollectLocal(list, scratch)` fills the local player's active
affixes on counted equipment, the trinket included (`ActiveAffix`: item, roll, def, item class, channel); `EquippedItems`, `CollectItem`,
`IsEquippedByLocalPlayer`, `Enabled` (the `Affix effects` switch). `EffectTotals.Snapshot()` → `EffectSnapshot`
(per channel sum, applied value after caps, sources; health-critical state; `States`: the Phase 2 runtime states in
force, e.g. `ward 12 left`, `evader's fury`, `momentum`) for `ecraft stats`. The aggregate is
rebuilt at most once per frame after `ItemStateCache.Written` on an equipped item, equipment set-up, an inventory change
that changed the counted gear (`GearSignature`; any other only recounts Fafnir's Greed coins), spawn, rules change, the
`Affix effects` switch and teleport end.
Network surface (all `features/effects-runtime.md` section 7): routed RPCs `ECF_KillRestore` (creature owner → the
killer's peer: Reaper / Soul Reaper) and `ECF_MeleeDodged` (attacker's peer → the dodger's peer: Evader's Fury), both
registered at `ZNet.Awake` on every peer, argument-less, sent only to a player whose player-ZDO int `ecf_wants` asks for
them (bit 1 kill restore, bit 2 Evader's Fury; written by its own client when it changes, `PlayerStats`); the status
effect `ECF_Hamstring` (in ObjectDB on every peer); player-ZDO floats written by the player's own client when they change: `ecf_daze`, `ecf_light`, `ecf_demist`,
`ecf_taming`, `ecf_sail`, `ecf_yield_mining`, `ecf_yield_lumber`, `ecf_harvest` (readers clamp to the caps); summon ZDO
floats `ecf_summon_damage`, `ecf_summon_health` (the caster, at spawn). The four loot-find effects (`find_*`) are
registered here but applied by Loot.

**Rolling** (`Rolling/`, implemented by Stones; classes-and-tiers.md sections 5 and 6). Candidates come from the item
class's pool (`best` or `allowed`), pass `requires`, are not on the item, share no exclusion group and keep the item
within its rarity's prefix or suffix limit for their kind (`AffixLimits`; the builder's rarity, so a promotion or fresh
roll uses the new one). Eligible tiers (`TierEligibility`): unlocked at the item level or below, weight above 0, on an
allowed class below the closed top (`tier >= 1 + floor(k * rolling.allowed_closed_fraction)`), then the rune floor keeps
the best that-many; chaotic rolls take any tier the inscription defines, uniformly (class pool and limits still apply).
Values are uniform in the tier's range at its decimals; `scaled` inscriptions are multiplied by the class's
`damage_scale` and rounded again (`RollMath.Scale`, decimal arithmetic). `ItemRoller.RollFresh(state, rarity, ctx)` (a dropped item),
`AddAffixes(state, count, ctx)` (the Sealed Rune's extra inscription, `ecraft reroll`), `Promote(state, toRarity, ctx)`
(Awakening, Ascension: adds `max(new.min - count, promote_adds_at_least)`, not past the new maximum; all or nothing),
the Recasting Rune being `RollFresh` at the item's own rarity (since 2026-10-05), and
`RollChaotic(state, rarity, ctx)` (the Sealed Rune: every affix out, the count drawn in the rarity's range, any tier the
affix defines) → `RollOutcome` (new state or `RollFailure`: `NoEligibleAffix`, `NotMagicBase`, `Full`, `NewerFormat`);
pure, never write. `RollContext.For(item, tierFloor)` (`Class` = `ClassInfo`, `Level` 1-8, `TierFloor`, `Chaotic`,
`LimitOverflow`: how far the Sealed Rune's add may pass the prefix and suffix limits); `RollContext.Random` defaults to
`RollRandom.Create()` (independently seeded; never `new Random()` per roll).

**Loot** (`Loot/`). Runs on the dying creature's ZDO owner only (`DeathPatch`, prefix on `Character.OnDeath`). No
magic gear drops for now (`GearDrops.On` is false, user decision 2026-10-07): the planners get `GearOn = false`.
Debug surface: `LootRoller.Simulate(tier, stars, kills, creaturePrefab?)` → `LootSimulation` (`ToString` prints totals),
`LootRoller.SpawnAt(position, tier, stars, creaturePrefab?)`, `LootPreview.Explain(Character)`, `GearPool.Bases` and
`GearPool.ForTier(tier)` (the drop-eligible bases at drop tiers 1-8, weighted by their class's `drop_weight`;
`GearBase`: `Class`, `Level`, `PoolTier`, `CapacityFor(rarity)` per kind and limit), `GearFactory.Build(...)` (a
pre-rolled item, written through `ItemState.Write`). The drops are held during the death and stored on the creature's ragdoll, spawned when it
dissolves with the vanilla loot, or dropped at once with no ragdoll (`Loot/CorpseLoot`, Ragdoll.Setup postfix and
Ragdoll.SpawnLoot prefix). ZDO keys (effects-runtime.md section 7): `ecf_corpse_loot` (ragdoll: the held items' saved
data), `ecf_ally_hit` (creature), `ecf_filled` (a world container
rolled once, on its owner, when the game fills it: `Loot/ChestFillPatch`), the player's own `ecf_find_rarity`,
`ecf_find_stones`, `ecf_find_trophy`, `ecf_find_coins` (`Loot/FindPublisher`, read from the last hitter by the creature's
owner). Elite Creatures Reborn keys, read only, only when ECR's GUID is loaded: `ecr_resolved`, `ecr_stars`,
`ecr_phantom_of` and `ecr_clone_of` (a Phantom copy or Cloning decoy: drops nothing), `ecr_tether` (the first of a
Tethered pair to fall drops nothing; each Twin pays, as in ECR), `ecr_tier` (`Loot/EcrKeys`;
`ecr_tier` is not written by ECR yet).

**Stones** (`Stones/`, the runes). `InventoryGui.OnSelectedItem` prefix (local player), `StonePipeline.Evaluate(job)`
(read-only, 10 checks then the verb as a dry run), `ConfirmGate.Pass` (Cleansing and the Sealed Rune: `confirm: true`), then
`StoneCommit` (one `ItemState.Write`, then the cost, paid from `StoneJob.StoneSource`: the player's inventory or the
open container this client owns, so a rune works straight from a chest). Rune verbs (`StoneVerbs`): `PromoteVerb`, `StripVerb`,
`CorruptVerb`, `RerollVerb` (Recasting: `ItemRoller.RollFresh` at the item's rarity, every affix replaced, two on Magic; refused on the base rarity).
The Sealed Rune draws its outcome by weight; an outcome that cannot be carried out falls back to sealing only, and every
outcome seals (`ecf_sealed = serpent`). A sealed item refuses every rune.

**Rune Table** (`Table/`, rune-table.md, user decision 2026-10-06; built, never run in game). Piece `ECF_RuneTable`
(`TablePrefab`: a workbench copy without its crafting station, model and icon from the bundle `ecf_runetable` via
`TableModel`, workbench look without it; `TableHammer`: hammer Crafting tab while `Rune Table` is on, cost 10 Wood,
10 Stone, 5 Greydwarf eye). `RuneTable` (hover, use key, the chest-style open handshake `ECF_RT_Open` / `ECF_RT_Opened`
that hands ZDO ownership to the opener), `TableStore` (ZDO ints `ecf_rt_rune_<id>` and one pool `ecf_rt_essence`, owner-only
writes), `Essences` (5, inscription ids each, used with the Ascension Rune only, `CostPerLevel` 10), `TrophyYields` (trophy prefab →
essence, 2-14 by biome, bosses 20-80; `IsBoss`: never taken by Sacrifice all), `TableSupply` (an `Stones/IRuneSupply`: the table's store only, stones and the essence
pool; carried stones are used by the normal click). `EssenceItem` (`ECF_Essence`, a Wisp copy, stack 100, every peer: ZNetScene.Awake
prefix, ObjectDB.Awake/CopyOtherDB postfixes), `TableShelf` (a tablet per held rune on the middle five of seven anchors) and
`TableBowl` (the bowl's glowing fill, a step per 50 essence, full at 500) and `TableGems` (one of each held gem kind
lying on the top's right-hand end) draw from the ZDO on every client, and `TableCast` plays a use: the owner's
payment counts it (`ecf_rt_cast` int, `ecf_rt_cast_stone` id), every client near plays the workbench craft sound and
`TableVortex` (bundle `ecf_tablefx`, effect `ecf_rune_vortex`, recoloured by `Display/ParticleRetint`) on the slab.
Stones takes a supply instead of a carried stack (`StoneJob.Create(player, rune, target, supply)`, `StonesHeld`,
`Again`), and `RollContext.Favoured` limits the draw to the chosen essence's inscriptions (`AffixDraw`), a guaranteed one. The window (`Table/Window/`) is a
pruned copy of the game's crafting panel shown inside it over its covered parts, so it slides with the inventory
(`PanelBuilder`, `PanelLayout`, `TablePanel`, `CraftingCover`, `TableWindow`
with its `InventoryGui` Show/Hide/Update patches) with three tabs (`InscribeTab`, `SacrificeTab`, `SocketTab`); `RuneStash` stores runes and carried essence (all, or one Ctrl + clicked stack through `TableCtrlClick`, an
`InventoryGui.OnSelectedItem` prefix ahead of OpenKeep's Route Modifier that clears the clicked item), takes a
rune kind out on Shift + click (`TakeOut`) and drops both (essence as items) when the table is destroyed.
The table's class line uses the class's own name (`TableWords.ClassName`), so a class another mod registered shows its word.
With Ascension and an essence, `EssencePills` (the roller's `AffixDraw.Candidates`) fills a `PillStrip` of `PillView`s
(`PillArt`) under the text; every hover text in the window is a `TipHover` shown by `HoverBox` beside the element
(right, else left), the game's tooltip box without its pointer-following `UITooltip`.

**Sockets and gems** (`Sockets/`, sockets.md, user decision 2026-10-07; built, never run in game). `ItemState.Sockets`,
`Gems` (`GemRoll`: gem id + the `AffixRoll` it gave), `FilledSockets`, `FreeSockets`, `GemSocketAt`, `GemDefinitionAt`,
`IsGemActiveAt`; `ItemStateBuilder.SetSockets(0-3)`, `SetGem(socket, gem)` (the next empty socket or a filled one,
replaced); `Affixes/GemRoll.cs` has the codec. `EffectRolls` = the active inscriptions then the active gems, so every
effect, cap and total reads gems with no other change. `GemCatalog.BaseOf(ClassInfo)` → `SocketBase` (Weapon: groups
onehand/twohand/ranged; Staff: magic; Armour: armour and offhand without `light`; None) and `StatFor(gem, base)`, the
roster in code. `GemRolls.Roll` (a tier the item level unlocked, whole ladder, weakest when none). `SocketDrops.Add` in
`GearFactory` (Magic 80/15/4/1, Rare 60/25/11/4). Verbs `SocketVerb` (chisel) and `GemVerb` (`Stones/SocketVerbs.cs`);
a gem into a full item returns `StoneResult.ChooseSocket()` (`NeedsSocket`), and the click opens `GemChooser` (one
yes/no popup per filled socket; Yes re-evaluates `StoneJob.AtSocket(player, socket)`; `Confirm(job, socket)` asks once
for a socket already picked). `SocketSwitch` (`Gems and sockets`): `LootPlanner` skips the chisel and gems while it is
off. `BossGems` (code, not YAML): one random gem per player within 50 m (`Loot/BossParty`) at a chance growing boss by boss (15% Eikthyr to 80% the Fader) and the
chisel 25%, keyed on `LootInput.Prefab`. Models:
`StoneTablets` is one instance per bundle (`Runes`: `ecf_runes`, `ecf_runetablet_<id>`; `Gems`: `ecf_gems`, `ecf_<id>`;
`For(id)`); without a bundle, tinted Ruby (gems) and iron nails (chisel), `StoneBases` groups `Gem`/`Tool`. The Rune
Table's third tab, Sockets (`Table/Window/SocketTab`, `SocketPane`, `SocketText`; `TableTab.Shown` hides it while the
switch is off), reuses the rune row for the chisel and carried gems and the essence row for the item's sockets.

**Settings** (`Config/ModSettings`): gameplay entries are Charter clauses (synced, locked while the server binds);
display, glow, confirm and diagnostics are local. Sections `1 - General`, `2 - Runes`, `3 - Drops`, `4 - Commands`,
`5 - Display (per player)`, `6 - Ground glow (per player)`, `7 - Diagnostics`, `8 - Elite Creatures Reborn` (`Synergy`,
synced, default off). `2 - Runes` has `Gems and sockets` (synced, default on; sockets.md), `Rune Table` (synced, default on; the table in the hammer and usable) and `Runes from salvage` (synced, default on; user decision 2026-10-06): the rune a
salvaged Magic or Rare item may give back, `Stones/SalvageRunes` (the first enabled promote rune working on the rarity
below the item's, at a fixed 25%), read by OpenKeep's Salvage through the API. `ServerBinding.Charter` is the only Charter.

**Words** (`Localization/Words`, namespace `EliteCrafting.Text`): `Words.Localize(text, params words)`, `Words.Add`,
`Words.Has`, and `Words.Changed` (after a language setup and after `Add`; drop cached localized text there).

**Display** (`Display/`). Inscription lines in tooltips are the short form (`ecf_affix_<id>_short`,
`translations/English.affixes_short.yml`, one per `_line` sentence; `AffixLines.Sentence(..., brief)`) at `Tooltip detail`
Compact and Standard, the whole sentence at Full and in the API and the table's texts (user 2026-10-07). Draws on the viewing client only; its caches drop on `ItemStateCache.Written`,
`ActiveRules.RulesChanged`, `Words.Changed` and a display setting change. No state of its own in items or ZDOs.
Surfaces: grid tooltip title, tooltip block (`ItemData.GetTooltip` postfix), ground hover, pickup message, ground glow (a light and the loot beam, `GlowBeams`: bundle `ecf_lootglow`, `Loot beam` switch),
the crafting panel's upgrade tab (`CraftingPanel`: the upgrade target's block under the recipe, names colored through
the label color) and item / armor stand hovers (`StandHover`, the item decoded from the stand ZDO by `StandItems` once
per ZDO revision). Every per-frame surface memoises its last input and output. The rarity backdrop behind every icon of a
magic item (`Backdrops/`: grids, hotbar, drag, crafting panel, radial menu, top-left messages; moved from PackPanel
2026-10-05, no setting), a red infinity sign (dark outline) on a sealed item's icon, lower left (`Backdrops/IconSeal`, `SealArt`, painted in code, driven by the backdrop so it shows on every surface the backdrop does; user request 2026-10-06), the item's sockets as small round marks in the lower right corner, first in the corner, second above, third to its left, an empty one a dark hollow and a filled one its gem's colour (`Backdrops/IconSockets`, `SocketArt`; Normal items too; user request 2026-10-07), and the scroll bar on a tooltip too tall for the screen (`Tooltips/TooltipScroll`, wheel or
right stick; OpenKeep's wheel cycling yields to its `ecf_tooltip_bar`); display.md sections 2 and 3.

## API

The public API for other mods (`features/api.md` has the endpoints and the JSON fields). `Api/EliteCraftingApi` is a
public static class (ILRepack internalizes only the merged libraries), `ApiVersion = 1`; `HasEndpoint` /
`GetEndpointNames` read its public static methods. Other mods bind by reflection through `ValheimModLibs/EliteCraftingLink`
(typed delegates, nothing when EliteCrafting is absent or older) and load after us (soft `BepInDependency` on
`com.EliteCrafting`). An endpoint is added, never changed; a removed one stays as a no-op. The contract:

- **Never throws.** Every endpoint runs through `ApiGuard.Run` (an internal failure is logged once per endpoint and
  answers false, null or 0). Foreign callbacks live in `Core/Callbacks<T>` lists (by id: providers, filters; by
  delegate: listeners, whose source is the declaring assembly); `ForeignFailures` logs a throw once per kind and source
  and it counts as no answer. Classifiers keep their own guard in `ClassRegistry`.
- **JSON is YAML.** `ApiJson.Map` reads a definition with the merged YamlDotNet and the rule parsers build the models:
  `ClassParser.ParseOne` (classes), `AffixParser.Parse` (an inscription, complete and valid on its own, its effect
  registered), `BossDropParser` (loot; bonus runes must be built-in ids), `ExternalEffectParser`. An error refuses the
  call, logged under the endpoint's name.
- **Under the YAML.** Classes in `ClassRegistry` (a YAML class with the id overrides only the fields it names).
  Registered inscriptions are `Rules/Loading/CodeLayer`, the bottom layer of every build of the inscription family
  (`FamilyBuilder.Merge`, under the built-in defaults and the files; `withCode: false` only in `RuleFamily.AdoptLocal`'s
  last-resort fallback), so a file entry with the id changes only the fields it names. Pool additions are applied to the
  parsed entries after the merge (`CodeLayer.ApplyPools`, called by `AffixFamilyParser`; best wins over allowed; an
  unknown inscription id is a warning). Levels in `Items/CodeLevels` (read by `TierDerivation` right after
  `item_tiers.items`; source `api` in `ecraft tiers`). Loot in `Loot/CodeLoot` (`CreatureProfiles.Build` takes the YAML's
  boss and creature entries first, else the code's, each whole).
- **Rebuild.** A registration after the rules loaded calls `Api/RuleRebuild.Request` (inscriptions, pools and external
  effects ask for the inscription family); its driver's `LateUpdate` runs `ActiveRules.RebuildForApi` at most once per
  frame: `RuleFamily.Rebuild` (the family from the texts it was last given: this machine's files on the author, the
  server's on a bound player, so a file rejected before the registration builds now), else `Compose()` again, so every
  cache keyed on the generation (item levels, gear pool, creature profiles, tooltips, the unknown-class check) refreshes.
  Before the first load nothing is scheduled: that load reads every registration.
- **External effects.** `EffectRegistry.RegisterExternal` adds an id (or replaces an external one) at any time, also
  after `Freeze`; one of our own ids is refused. `EffectKinds.Of` maps it to `ExternalGlobal` (player scope) or
  `ExternalItem` (item scope, inside the item-local range): no-ops in the aggregate and the item hooks, like the
  loot-find kinds, and `VerifyCatalog` accepts them. They roll, show (polarity, unit) and sum into channels like any
  effect; the registering mod applies them by reading the totals.
- **Player inscriptions** (`ApiPlayerLines`, 0.7.0, for PackPanel's stat sheet): `GetPlayerInscriptionsJson` lists the
  local player's active inscriptions summed per id and worded by `AffixLines.Sentence` (api.md section 7).
- **Totals** (`ApiTotals`, inscription units): the local player from the last rebuild (`AggregateBuilder.LastPlan` and
  `Sums`: the channels of the effect and param, capped, health-critical ones only while critical, item-local ones never);
  another player only what it publishes (loot find, `FindKeys`; shared stats, `PlayerStats.StatOf`), param-less; an item
  its own active rolls of the effect and param, summed per channel and capped. 0 while `Affix effects` is off.
- **Hooks.** `ItemEffects.EquippedItems` adds the equipment providers' items for a `Player` (deduplicated) and
  `IsEquippedByLocalPlayer` counts them, so they feed the aggregate, the loot-find publisher and the write-triggered
  rebuilds (local player only; providers are asked on every rebuild and every item-state write, so they must be cheap).
  `InvalidatePlayer` marks the effects dirty (rebuilt next frame) and republishes loot find. Magic-base filters are asked
  last by `ItemClasses.IsMagicBase` (drops, runes, commands, the API); a filter change refreshes the rules (the gear pool
  asks them). Item-changed listeners run right after `ItemStateCache.Written`, with the reason of the innermost
  `ItemChanges.Because` scope: `rune:<id>` (`StoneCommit`), `drop` (`GearFactory`), `command` (`RollerCall`), `api`
  (`ApiItems`), `other` outside every scope; `migration` is reserved (migrations are stored by the next write of another
  reason) and the upgrade carry-over copies the data onto the new item without a write. Loot-generated listeners run in
  `GearFactory.Build` after a pre-rolled item (creature or world container) was written.
- **Items.** `RollMagic` (`ItemRoller.RollFresh`) and `Cleanse` write through `ItemState.Write` on the caller's peer and
  refuse sealed and newer-format items. `DecorateIcon` (0.8.0) puts the icon marks (backdrop, seal, sockets) on another mod's icon. `GetSalvageRune` / `GetSalvageRuneChance` (0.8.0, `Stones/SalvageRunes`) read
  only: the rune prefab a salvage may give back and its chance; the salvaging mod rolls and adds it.
- **Multiplayer.** Registrations are code: every peer must make the same ones. Nothing is sent; the server's synced YAML
  still overrides what code registered.

## YAML defaults

Both default files are **format 3** (`format: 3` since 2026-10-07: Magic 2, Rare 3, five runes, ladders of at most 8
tiers; format 2 was classes-and-tiers.md, 2026-10-05), written from the planning page
(https://claude.ai/artifact/VmVJDaDhHFrCP6dhKt1LoM): `config/EliteCrafting_inscriptions.yml` has 207 inscriptions, each
with `affix` (prefix/suffix), `family`, `classes { best, allowed }`, optional `scaled`, and a tier ladder
`{ count, from, min, max }`; `config/EliteCrafting_economy.yml` has the rarities with `prefixes`/`suffixes`, `rolling`
(`allowed_closed_fraction`), the runes, the 33 item `classes` with their match rules, item levels 1-8 (Deep North
materials at 8), biomes and drops with eight-entry tier lists. With the real effect catalog (153 ids), both load with
0 errors and 0 warnings and every generated ladder matches the planning page's `tierRows` row for row (checked by a
scratch harness, 2026-10-05). **Tiers count down** (user decision 2026-10-01): in the YAML, the tooltip and the
console, T1 is the strongest row; each inscription has its own count k (1-8). Stored item data and the code use the
strength grade (1 = T`k`); the conversion lives on the definition (`AffixDef.ShownTier`/`GradeOf`), never global.
**Decision (kept): the default file lists only inscriptions whose effect is registered** in the build it ships with. An
inscription naming an unregistered effect is an error even when `enabled: false`, so typos never hide behind a
disabled flag. Because the built-in defaults are a layer under the owner's files, new inscriptions reach existing
servers without anyone editing a file.

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
| Magic | 2 (one prefix, one suffix) | green |
| Rare | 3 (at most two prefixes, two suffixes) | blue |

The Sealed Rune can add one more past that: a 3rd on Magic, a 4th on Rare. No item holds more than four.

### Runes

Pick up a stack of runes, from your inventory or straight from the chest you have open, and click it onto an item in
your own inventory (the item cannot be in the chest).
One rune is used per success; a refused rune is kept. A rune dropped on another rune stacks or swaps as usual. The
Cleansing and Sealed Runes cannot be undone and ask first: hold Shift while you click (or switch
`Confirm destructive runes` to a dialog).

| Rune | Works on | Does | Drops from |
|---|---|---|---|
| Awakening Rune | Normal | Makes the item Magic with two inscriptions | everywhere, most of all in the Meadows; Eikthyr |
| Recasting Rune | Magic | Replaces both inscriptions with two new ones; stays Magic | everywhere; Eikthyr |
| Ascension Rune | Magic | Makes it Rare, keeps its inscriptions and adds a third; at the Rune Table an essence chooses what it is | Black Forest and later, more the later the biome; every boss but Eikthyr (Bonemass half the time) |
| Cleansing Rune | Magic, Rare | Strips it back to Normal: every inscription is lost | everywhere |
| Sealed Rune | Magic, Rare | Corrupts it and **seals** it for good, with one of three outcomes: nothing more (35%), one inscription past the cap, a 3rd on Magic or a 4th on Rare (35%), or a chaotic reroll: every inscription rolled again at any tier, ignoring the item's limits (30%) | Swamp and later; sometimes Bonemass, Yagluth and the Fader |

A sealed item takes no rune again. Every rune's odds, costs and the rarities it accepts are in
`EliteCrafting_economy.yml`.

With OpenKeep, Magic and Rare gear can be salvaged; one time in four it also gives back the rune that raised it to its
rarity: an Awakening Rune from a Magic item, an Ascension Rune from a Rare one (`Runes from salvage`, on by default).

A refusal says why, for example "The Ascension Rune does not work on Rare items", "This item cannot hold another
inscription", "No inscription can roll on this item", or "Unequip this item first" when the server does not allow
changing equipped items.

### Gems and sockets

Weapons, staves, armour and shields can hold up to three sockets (shown as "Empty socket" lines and as small circles
in the icon's lower right corner). The **Dvergr Chisel** cuts one into such an item that has none. (Dropped gear
would carry sockets too, but no magic gear drops for now.) A **gem** clicked onto an item fills its next
empty socket with a stat that depends on the item; its tier is rolled for the item right then (the item's level decides
the best tier it can reach). With every socket full you pick which gem to replace; the old one is lost. Gems are
a boss reward: only bosses drop them, any gem, one at random, the later the boss the likelier (Eikthyr 15%, the Elder 20%, Bonemass 30%, Moder 40%, Yagluth 50%, the Queen 65%, the Fader 80%, rolled once for every player within 50 m of the boss when it dies; the chisel 25% from every boss).
Cleansing and the other runes leave sockets and gems alone; a sealed item takes neither. The Rune Table's **Sockets**
tab does the same from a window: pick the gear, the chisel or a gem you carry, and (to replace a gem) the socket.
`Gems and sockets` in `2 - Runes` turns the feature off.

| Gem | Weapons | Staves | Armour and shields |
|---|---|---|---|
| Surtr's | added fire damage | elemental damage | fire resistance |
| Ymir's | added frost damage | lower eitr cost | frost resistance |
| Thor's | added lightning damage | chain lightning | lightning resistance |
| Nidhogg's | added poison damage | longer damage over time | poison resistance |
| Hel's | added spirit damage | eitr on kill | spirit resistance |
| Tyr's | physical damage | summon damage | less stagger |
| Freyja's | life leech | lower health cost | health |
| Odin's | eitr leech | eitr regeneration | eitr |
| Skadi's | stamina leech | summon health | stamina |
| Heimdall's | critical hit chance | cast speed | avoid hits |
| Sleipnir's | - | - | movement speed |

### The Rune Table

Built with the hammer at a workbench (10 Wood, 10 Stone, 5 Greydwarf eye). Use it to open its window in the crafting
panel's place, with three tabs:

- **Inscribe**: pick a piece of your gear and a rune, then Inscribe. The rune comes from the table: its buttons use
  only what the table holds, and the rows count only that (runes you carry are used the normal way, clicked onto an
  item; the button is greyed only when the press would be refused). With the Ascension Rune you may also pick an essence: the third inscription is then one of that
  essence's own, for 10 essence per item level; the inscriptions it can give show as pills in the essence's colour
  (hover one for its range and tiers on this item); for every other rune the essences are greyed out. Store runes and
  essence (Store all, under the item's name) moves every rune, Dvergr Chisel, gem and Essence you carry into the table,
  and Ctrl + click on one in your inventory puts that stack in. Shift + click on a rune (on the Sockets tab: the chisel or a
  gem), or on the Essence slot, opens the game's split dialog: take out as many as you choose. The Sockets tab uses the
  table's chisels and gems. A table taken down drops everything it holds (its essence
  as Essence items).
- **Sacrifice**: trophies you carry become essence in the table's one pool: 2 (Meadows) to 14 (Ashlands) each, about five of the item's own biome per guaranteed inscription. Shift sacrifices
  every trophy of that kind; Sacrifice all trophies (under the trophy's name) every trophy the table takes except
  the bosses'. A boss trophy gives the most (Eikthyr 20, the Elder 30, Bonemass 40, Moder 50, Yagluth 60, the Queen 70, the Fader 80) and is taken only by a press on it, so one meant
  for the Forsaken altar is never lost by accident.
- **Sockets** (while `Gems and sockets` is on): pick a weapon, staff, armour piece or shield, then the Dvergr Chisel or
  a gem you carry, and to replace a gem the socket it sits in; Cut socket or Set gem uses one from your inventory
  (see Gems and sockets above).

Essence is the essence of a creature: the table's pool, and an item (**Essence**, a pale glowing orb, stacks to 100) when
it leaves the table. The essence row starts with it and how much you can spend (the pool plus what you carry); the
cost comes from the pool first. The table's offering bowl fills with essence, a step per 50, full at 500; its back shelf
shows a tablet for each rune it holds.

| Essence | Its inscriptions |
|---|---|
| Beast | weapon damage, critical hits, slayers, stamina, attack, draw and cast speed, reach |
| Stone | armour, blocking, parrying, health, staggering blows |
| Elemental | fire, frost, lightning and poison damage and resistance, warmth and the cold |
| Spirit | spirit damage, eitr, magic |
| Grave | leech, blood magic, summons, undead slaying |

One player uses a table at a time. `Rune Table` in `2 - Runes` turns it off: it leaves the hammer and cannot be used,
and what built tables hold is kept.

### Inscriptions

Each inscription has its own ladder of tiers (1 to 8), and they count down: **T1 is the strongest roll**. An item rolls
every tier its item level has unlocked (the item level is the biome of its materials, 1 Meadows to 8 Deep North),
weaker tiers more often; on an item class where an inscription is only allowed, its top third stays closed. A Magic
item holds at most one prefix and one suffix, a Rare item two of each. The tooltip shows prefixes first, then
suffixes, each with its tier; `ecraft inscription <id>` shows a whole ladder. (The table below still lists the
inscriptions of the format-1 catalog by slot; `ecraft list inscriptions <class>` lists the current ones.)

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

`ecraft list inscriptions` in the console prints the full list with their classes and tier counts. Evader's Fury, Steel Rhythm and a
charged Runic Ward show an icon on the HUD while they are active.

### Console commands

Open the console with F5. Everything is under one command, `ecraft`. Output is English.

| Command | Who | Does |
|---|---|---|
| `ecraft help` | everyone | Lists the sub-commands you may run |
| `ecraft inspect [cursor\|hover\|ground\|<slot>]` | everyone* | An item's EliteCrafting data and what it means |
| `ecraft stats` | everyone* | Your summed inscription totals and the effects active right now |
| `ecraft list inscriptions\|runes\|rarities [<filter>]` | everyone* | The configuration in force, filtered by item class, category, prefix/suffix, rarity or id |
| `ecraft inscription <id>` | everyone* | One inscription: its classes and every tier row with its range and unlock level |
| `ecraft classes` | everyone* | Every item class: its items, their item levels and its pool size |
| `ecraft give <rune>\|all [count]` | admin | Runes into your inventory |
| `ecraft roll <rarity> <prefab\|class> [level]` | admin | A rolled magic item into your inventory (a class picks a random base of it) |
| `ecraft reroll [cursor\|hover\|<slot>]` | admin | Rerolls an item's inscriptions, keeping its rarity |
| `ecraft inscribe <inscription> [tier] [value] [cursor\|hover\|<slot>]` | admin | Adds or replaces one inscription, for testing |
| `ecraft reload` | admin, on the machine whose files are in force | Re-reads the YAML, the translations and the `.cfg` now |
| `ecraft dump inscriptions\|economy\|items` | admin | Writes the merged configuration in force, or a survey of every item (with its class and level), to the config folder |
| `ecraft tiers` | admin | Writes `EliteCrafting_item_tiers_reference.yml`: every magic base with its class, item level (1-8) and why |
| `ecraft ecr` | everyone* | The Elite Creatures Reborn synergy: installed or not, the switch, and what the creature you look at would pay |

\* unless the server turns `Read-only commands for everyone` off. `<slot>` is an equipment slot: `right`, `left`,
`head`, `chest`, `legs`, `cape`, `utility`.

### Files

| File | What |
|---|---|
| `BepInEx/config/com.EliteCrafting.cfg` | Switches and preferences. Gameplay keys (inscription effects, modifying equipped items, rune drops, command access, the Elite Creatures Reborn synergy) follow the server; display, ground glow, the confirm mode and diagnostics are per player |
| `BepInEx/config/EliteCrafting_inscriptions.yml` | Every inscription: effect, prefix or suffix, item classes, category, tier ladder, weights, caps |
| `BepInEx/config/EliteCrafting_economy.yml` | Rarities, colors and prefix/suffix limits, rolling rules, runes, item classes, item levels, biomes and drop tables (creatures, bosses, chests, Elite Creatures Reborn) |
| `EliteCrafting_inscriptions_<anything>.yml`, `EliteCrafting_economy_<anything>.yml` | Your own additions, read after the main file in name order; they change only what they name |
| `EliteCrafting.translations.<Language>.yml` | Your own words for a language, key to text, over the built-in English |

The main YAML files are written once with the full defaults and never rewritten, except that a main file from before
format 3 is renamed to `<name>.v<old format>.bak` (`.v2.bak` for a file from 0.7.0) and written fresh
(extra files without `format: 3` are skipped with a warning). The built-in defaults always sit
underneath, so a later release's new inscriptions reach your server without editing anything; `use_defaults: false` in a
main file makes the files the whole configuration. A file with an error is reported in the log with file and line,
and the previous rules stay in force. Turn an inscription off with `enabled: false` (items that have it keep it, greyed and
inert, and get it back when you turn it on) or stop it rolling with `weight: 0`.
