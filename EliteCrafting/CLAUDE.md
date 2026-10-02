# CLAUDE.md - EliteCrafting

Crafting stones and magic item affixes across six rarities. Read `../CLEANROOM.md` first, then the workspace
`../CLAUDE.md` (layout, small units: methods at most 24 lines brace to brace including lambdas, classes at most 300
lines, one responsibility; multiplayer first; display preferences unsynced). The behaviour lives in `SPEC.md` and
`features/`; `PLAN.md`'s Decisions log overrides them where they disagree. Game signatures: `~/scratch/specs/ec-game-notes.md`
and the full decompile `~/scratch/decomp/full/assembly_valheim.decompiled.cs` (grep it; never copy from it).

Identity: GUID `com.EliteCrafting`, name `EliteCrafting`, version in `EliteCrafting.cs` (`PluginVersion`), custom-data and
ZDO key prefix `ecf_`, console command `ecraft`, YAML families `EliteCrafting_inscriptions*.yml` / `EliteCrafting_economy*.yml`.
**Vocabulary (user decision 2026-10-01):** players, the console, the .cfg, the YAML and item data say **inscription**;
the code and the `features/` specs keep the internal word **affix** (`AffixDef`, `AffixRoll`, `features/affixes.md`). Item
data key `ecf_inscriptions` (the old `ecf_affixes` is read when the new key is absent and removed on the next write).
Libraries merged: Charter, ConfigReload, PatchGuard, YamlDotNet (ECR's pattern). **No YamlConfig, no SyncedConfig, no
ItemCopies** (DECISIONS.md IMP-1: live stones share their prefab's `SharedData` instead).

**Status (2026-09-24): Phase 1 (0.1.0) and Phase 2 (0.2.0) code complete, integrated and reviewed, builds clean; nothing
tested in game yet.** The judgement calls made while building are DECISIONS.md "Settled during Phase 1 implementation"
(IMP-1 to IMP-124, Phase 2 from IMP-65). Next: the in-game test plan in `features/multiplayer.md` section 6 (steps 1-21
Phase 1, 22-38 Phase 2), then packaging. Phase 3 waits on AFX-8. **2026-10-01:** affix tiers count down (T1 strongest)
and sockets, gems and catalysts (`features/sockets.md`) built, builds clean, not tested in game.

## Build

```
~/scratch/ec-build.sh          # always this: it serializes builds between parallel agents
```

It must end with 0 errors before you hand back. Output: `dist/EliteCrafting.dll`.

## Folder ownership

| Folder | Owner | Responsibility |
|---|---|---|
| `EliteCrafting.cs` | spine | plugin entry; calls every `*Feature.Init`. Nobody else edits it |
| `Core/` | spine | `Log`, `Embedded` (resources), `EnumIds<T>` (snake_case ↔ enum), `Numbers` (invariant), `Ids`, `Colors` |
| `Affixes/` | spine | item state in `m_customData`: `ItemKeys`, `AffixRoll`, `ItemState`, `ItemStateBuilder`, `ItemStateCache`, codec, writer, migrations |
| `Rules/` | spine | YAML models, loading, merge, validation, Charter sync, hot reload, `ActiveRules` |
| `Config/` | spine | `ServerBinding` (the one Charter), `ModSettings` (the .cfg) |
| `Rolling/` | contract spine, **implementation: Stones** | `ItemRoller`, `RollContext`, `RollOutcome` |
| `Items/` | Items | `ItemSlots` and `ItemTier` are spine (implemented); prefab registration, upgrade carry-over, stack warning go in `ItemsFeature` + new files |
| `Stones/` | Stones | click gesture, pipeline, refusals, verbs |
| `Effects/` | Effects | `EffectRegistry`/`EffectDef`/`ItemEffects` are contract; `EffectCatalog` (the registered ids) and everything else is Effects' |
| `Display/` | Display | names, tooltip block, ground glow, crafting panel upgrade tab, item / armor stand hovers |
| `Loot/` | Loot | stone drops and pre-rolled gear drops, chests, killer loot-find stats, the Elite Creatures Reborn hook |
| `Salvage/` | Salvage | the Salvage key (grinding a magic item into shards), fusing shards into stones (`SalvageFeature.Init`, after Commands) |
| `Commands/` | Commands | `ecraft` |
| `Localization/` | spine | `Words` (namespace `EliteCrafting.Text`) |
| `translations/English.<area>.yml` | each area its own file | English words, key without `$` → text |
| `config/*.yml` | spine | embedded default YAML (generated, see below) |

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
- Build with `~/scratch/ec-build.sh`.
- Namespace traps: the game has global classes `Settings` and `Localization`; ours are `ModSettings` and namespace
  `EliteCrafting.Text`. The rules facade is `ActiveRules`, not `Rules` (a class named like the namespace
  `EliteCrafting.Rules` would not bind from sibling namespaces).

## Contracts

**Sockets** (`features/sockets.md`): `ItemState.SocketCount`, `Gems` (`SocketGem`: gem stone id + `AffixRoll`),
`EmptySockets`, `GemDefinitionAt(i)`, `CatalystFamily`/`CatalystQuality`, `CatalystFactor(affixId)` and `EffectRolls`
(active affixes + defined gems, catalyst applied: what Effects read). Builder: `SetSockets`, `SetGem` (FIFO, returns the
broken gem), `SetCatalyst`. Keys `ecf_sockets`, `ecf_gems`, `ecf_catalyst` (`SocketCodec`). Verbs `socket`, `gem`,
`catalyse` (`Stones/SocketVerbs`); `StoneResult.Breaking` makes a use ask first. Drops: `Loot/DropSockets`.

**Item state** (`Affixes/`, item-data.md). `ItemState.Read(ItemData)` → immutable `ItemState` (cached; plain items
return `ItemState.Empty` without a lookup). `IsMagic`, `RarityId`, `Rarity` (resolved `RarityDef`, null for Common or
unknown), `IsUnknownRarity`, `Affixes` (`AffixRoll` id/tier/value), `DefinitionAt(i)` (null = orphaned),
`IsActiveAt(i)` (defined and enabled; otherwise dormant), `IsBoundAt(i)`, `BoundId`, `Refine`, `IsSealed`/`SealedReason`,
`HasSigil`/`SigilId`, `Unreadable`, `IsNewerFormat` (stones refuse). To change an item:
`state.ToBuilder()` (`SetRarity`, `AddAffix` (appends), `RemoveAffix`, `ReplaceAffix` (in place), `ClearAffixes`,
`SetBound`, `SetRefine`, `Seal`, `SetSigil`) → `Build()` → `ItemState.Write(item, newState)`, the only write path
(refuses stackable items, stones, newer-format states). `ItemStateCache.Written` fires after every write.

**Rules** (`Rules/`). `ActiveRules.Current` (immutable `RuleSet`, swapped atomically) and `ActiveRules.RulesChanged`.
`RuleSet.Affixes` (`AffixRules`: `Get(id)`, `Affixes`, `Channels` (`ChannelDef`, index = `AffixDef.ChannelIndex`, `Cap`),
`Pool(slot, mythicOnly)`, health-critical thresholds) and `RuleSet.Economy` (`EconomyRules`: `Rarities` in ladder order,
`Rarity(id)`, `Next`/`Previous`, `BaseRarity`, `MythicRarity`, `Rolling`, `Stones`, `Stone(id)`, `StoneForPrefab`,
`Sigils`, `ItemTiers`, `Biomes`/`BiomeTier`, `Drops`, `StoneDraw(tier)`, `GearRarityDraw(tier, boss)`).
`StoneCatalog` lists the 60 built-in stone ids (27 stones + 16 essences + the chisel, 8 gems and 8 catalysts of
sockets.md, `IsEssence`, `IsGem`, `IsCatalyst`, `EssenceFamilies`), their prefab
names, the 16 reserved `ECF_CustomNN` and the 5 salvage `ShardIds` (`IsShard`).
Phase 2 model: `StoneVerb.Imbue` and `StoneDef.Family`; `EconomyRules.EssenceFamilies` / `Family(id)`;
`EconomyRules.Salvage` (`SalvageRules`: `Confirm`, `Stations`, `Yields`/`YieldOf(rarity)`, `Fragments`/`Fragment(id)`/
`FragmentForPrefab`, `FragmentDef`, `SalvageYield`); `Drops.Chests` (`StoneChance`, `GearChance`, `Containers`: prefab →
`CreatureDrop`); `Drops.Ecr` (`EcrDrops`). `FamilySpec` id lists are dotted paths (`rarities`, `stones`,
`salvage.fragments` merge by id, IMP-109). `ActiveRules.Compose` runs `EssenceMemberChecks` (log only).
`ActiveRules.ReloadLocal()` re-reads this machine's files (`ecraft reload`) and returns a `FamilyReload` per family
(`Applied`, `Rejected`, `Bound`, `NoFiles`). `ActiveRules.SourcesInForce(FamilySpec.Affixes|Economy)` → `RuleSources`
(internal): the file texts the running model was built from, in layer order, `DefaultsLayered`, `FromServer` (a bound
player gets the server's texts). `StoneDef.Description` and `StoneDef.ItemWeight` (YAML `description`, `item_weight`).

**Items** (`Items/`). `ItemSlots.Classify(item)` → `SlotInfo` (slot, hands, traits, governing skills; cached per
SharedData), `SlotOf`, `IsMagicBase`, `IsStone`, `Satisfies(slotInfo, affix.Requires)`, `Id(slot)`/`TryParse`.
`ItemTier.Of(item | prefabName)`, `ItemTier.Explain(prefab)` (tier + source, for `ecraft tiers`), `ItemTier.PrefabName(item)`,
`ItemTier.RefreshRecipes()` (recipe index up to date, tier cache dropped when rebuilt), `ItemTier.HasRecipe(prefab)`
(never call the internal `RecipeIndex.Refresh` from outside Items). Stone prefabs: `StonePrefabs.Get(stoneId)`,
`GetByPrefabName`, `IsRegistered`, `IsBuilt`, `GetShard(shardId)`, `IsStonePrefab(prefab)` (a stone, not a shard: the
stone click take-over keys on it, so shards swap and merge vanilla, IMP-103) (43 built-in + 16 reserved + 5 shards,
`StoneEntry.ShardId`/`IsShard`; built from code on every peer, registered in
ObjectDB and ZNetScene before any inventory or ZDO; every live stone is linked to its prefab's `SharedData` in an
`ItemDrop.Awake` postfix, so the economy YAML's name, description, stack and weight reach every stack). A stone is
recognised by its drop prefab name (`ECF_...`, `ItemSlots.IsStone`, true for shards too; never a magic base); spawned
stones set `m_worldLevel` so they stack. Essence family lines and shard fuse lines are part of the item description
(`Items/ItemDescriptions`, IMP-102/124).
`StoneStackGuard` keeps a stone stack whole when it loads larger than the current max stack (IMP-7).
`StoneVisuals.Tint(stoneId)`, `HasTint(stoneId)`, `TintOfPrefab(prefab)`, `GradeScale(grade)`, `Headless` (stable:
Display reads them; a stone without a tint glows white).

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

**Rolling** (`Rolling/`, implemented by Stones). `ItemRoller.RollFresh(state, rarity, ctx)`,
`AddAffixes(state, count, ctx)`, `RerollValues(state, ctx[, keepId])`, `RemoveOne(state, pick, ctx, [keepId,] out removedId)`,
`Promote(state, toRarity, ctx)`, `Swap(state, pick, ctx, keepId, out removedId, out addedId)`, and Phase 2's
`Reroll(state, rarity, ctx, keepId)` (Upheaval: fresh roll keeping the bound and the preserved affix),
`RollChaotic(state, rarity, ctx)` (Serpent: everything out, any tier, ceiling ignored), `Demote(state, to, ctx)`
(Serpent, rarity.md section 3) and `Imbue(state, rarity, ctx, ImbueRequest(family, familyFloor, keepId), out
guaranteedId)` (essences; `RollFailure.NoFamilyMatch`, `AffixDraw.Only` restricts a draw to a family) → `RollOutcome` (new
state or `RollFailure`); pure, never write. `RollContext.For(item, tierFloor)`; `RollContext.Random` defaults to
`RollRandom.Create()` (independently seeded; never `new Random()` per roll, IMP-2).

**Loot** (`Loot/`). Runs on the dying creature's ZDO owner only (`DeathPatch`, prefix on `Character.OnDeath`).
Debug surface: `LootRoller.Simulate(tier, stars, kills, creaturePrefab?)` → `LootSimulation` (`ToString` prints totals),
`LootRoller.SpawnAt(position, tier, stars, creaturePrefab?)`, `LootPreview.Explain(Character)`, `GearPool.Bases` and
`GearPool.ForTier(tier)` (the drop-eligible bases), `GearFactory.Build(...)` (a pre-rolled item, written through
`ItemState.Write`). ZDO keys (effects-runtime.md section 7): `ecf_ally_hit` (creature), `ecf_filled` (a world container
rolled once, on its owner, when the game fills it: `Loot/ChestFillPatch`), the player's own `ecf_find_rarity`,
`ecf_find_stones`, `ecf_find_trophy`, `ecf_find_coins` (`Loot/FindPublisher`, read from the last hitter by the creature's
owner). Elite Creatures Reborn keys, read only, only when ECR's GUID is loaded: `ecr_resolved`, `ecr_stars`,
`ecr_asp_worthless`, `ecr_tier` (`Loot/EcrKeys`; `ecr_tier` is not written by ECR yet, ECR-6).

**Stones** (`Stones/`). `InventoryGui.OnSelectedItem` prefix (local player), `StonePipeline.Evaluate(job)` (read-only,
14 steps, dry run), `ConfirmGate.Pass`, then `StoneCommit` (one `ItemState.Write`, then the cost). All 17 verbs
(`StoneVerbs`), sigils pending in `ecf_sigil` (`PendingSigil`), quality in `ecf_refine`.

**Salvage** (`Salvage/`). `SalvageInput` reads the `Salvage key` (local, default End; Shift confirms in the player's
confirm mode) over a hovered own-inventory magic item; `GrindChecks` → `Grinder` (item removed, shards added in one
frame, local player); right-click on a shard stack fuses (`Fuser`, Shift: every full set). Everything runs on the
client that owns the inventory, under the synced `salvage:` rules and the synced `Salvage` switch.

**Settings** (`Config/ModSettings`): gameplay entries are Charter clauses (synced, locked while the server binds);
display, glow, confirm, the Salvage key and diagnostics are local. Phase 2: `8 - Salvage` (`Salvage` synced, `Salvage
key` local) and `9 - Elite Creatures Reborn` (`Synergy`, synced, default off). `ServerBinding.Charter` is the only Charter.

**Words** (`Localization/Words`, namespace `EliteCrafting.Text`): `Words.Localize(text, params words)`, `Words.Add`,
`Words.Has`, and `Words.Changed` (after a language setup and after `Add`; drop cached localized text there).

**Display** (`Display/`). Draws on the viewing client only; its caches drop on `ItemStateCache.Written`,
`ActiveRules.RulesChanged`, `Words.Changed` and a display setting change. No state of its own in items or ZDOs.
Surfaces: grid tooltip title, tooltip block (`ItemData.GetTooltip` postfix), ground hover, pickup message, ground glow,
and in Phase 2 the crafting panel's upgrade tab (`CraftingPanel`: the upgrade target's block under the recipe, names
colored through the label color) and item / armor stand hovers (`StandHover`, the item decoded from the stand ZDO by
`StandItems` once per ZDO revision). Every per-frame surface memoises its last input and output (IMP-121-123).

## YAML defaults

`config/EliteCrafting_inscriptions.yml` is generated from `features/affixes.md` (section 4 catalog) by scripts kept outside
the repo; `config/EliteCrafting_economy.yml` is written from `features/economy-yaml.md` plus the Phase 2 sections
(`essence_families`, `salvage`, `drops.chests`, `drops.ecr`). Both load with 0 errors and 0 warnings.
**Affix tiers count down** (user decision 2026-10-01): in the YAML, the tooltip and the console, tier 1 is the
strongest row. The code, the tier window and stored item data use the strength grade instead (grade = 8 - tier, so it
lines up with item and biome tiers); `Core/AffixTierNumbers` is the only conversion, called by `AffixTierParser`,
`StoneParser` (`tier_floor`), `AffixLines` and the commands. The catalog in `features/affixes.md` writes `T7–T1`,
weakest first. **Decision: the default affix file lists only affixes whose effect is registered** (0.2.0: 162 affixes on 115 effects;
0.1.0 had 59 on 34; the 13 Mythic-only affixes wait for Phase 3). An affix naming an unregistered effect is an error even when `enabled: false`, so typos never hide behind a
disabled flag. Later phases add their affixes to the defaults as their effects are registered; because the built-in
defaults are a layer under the owner's files, new affixes reach existing servers without anyone editing a file.

Merge rules (configuration.md section 3): layers = built-in defaults (unless the main file says
`use_defaults: false`), the main file, then other files by name. Id lists (`affixes`, `rarities`, `stones`) merge by
id field by field; maps merge by key; other lists and scalars are replaced; a key set to null is removed. Errors reject
the family and keep the previous rules; at startup the author falls back to the built-in defaults alone and publishes
that. Sync: each family is one Charter article (the file texts); a bound player builds from the built-in defaults plus
the server's files.

## Player reference

The store README links here for the full reference. Moved unchanged from the README on 2026-09-30, when the README
was cut to the store's short shape; keep it in step with the code and the default YAML.

### Stones

Pick up a stack of stones in the inventory and click it onto an item in your own inventory (not in an open chest).
One stone is used per success. A stone dropped on another stone stacks or swaps as usual. The Stone of Unmaking and the
Serpent Stone cannot be undone and ask first: hold Shift while you click (or switch `Confirm destructive stones` to a
dialog).

| Stone | Works on | Does | Drops from |
|---|---|---|---|
| Stone of Awakening | Common | Makes the item Uncommon with one inscription | everywhere; Eikthyr |
| Stone of Ascension | Uncommon | Makes it Rare, keeps its inscriptions and adds to Rare's minimum | Black Forest and later; the Elder |
| Stone of Exaltation | Rare | Makes it Epic, keeping its inscriptions | Mountain and later; Moder, Yagluth |
| Stone of Transcendence | Epic | Makes it Legendary, keeping its inscriptions | Mistlands and later; the Queen, the Fader |
| Stone of Apotheosis | Legendary | Makes it Mythic, keeping its inscriptions | Ashlands, very rare; sometimes the Fader |
| Lesser / Greater Stone of Growth | Uncommon, Rare / Epic and up | Adds one inscription, if the rarity has room | Black Forest / Mountain and later |
| Lesser / Greater Stone of Turmoil | Uncommon, Rare / Epic and up | Removes one random inscription and rolls a new, different one | everywhere / Mountain and later |
| Lesser / Greater Stone of Upheaval | Uncommon, Rare / Epic and up | Rerolls every inscription, keeping the rarity | Black Forest / Mountain and later |
| Lesser / Greater Stone of Perfection | Uncommon, Rare / Epic and up | Rerolls the numbers, keeping the inscriptions | Swamp / Plains and later |
| Lesser / Greater Stone of Severing | Uncommon, Rare / Epic and up | Removes one random inscription (not below the rarity's minimum) | Black Forest / Mountain and later |
| Stone of Unmaking | any magic item | Strips it back to Common; Honing, Tempering and a pending sigil stay | everywhere |
| Serpent Stone | any magic item | Corrupts it and **seals** it for good, with one of five outcomes: nothing more, an extra inscription past the cap, a chaotic reroll that ignores tier limits, one rarity up (a 7th inscription on a Mythic) or one rarity down | Swamp and later; Bonemass |
| Stone of Binding | any magic item | Locks one random inscription: it survives Turmoil, Upheaval, Perfection and Severing. One at a time | Plains and later; Yagluth |
| Stone of Chance | Common | Turns it into a random rarity: 50% Uncommon, 30% Rare, 15% Epic, 5% Legendary, never Mythic | everywhere |
| Stone of Reflection | any magic item | Makes a copy, inscriptions and bonuses included; the copy is sealed | astronomically rare, Mistlands and later |
| Honing Stone | weapons | +1% damage per use, up to +10% | everywhere |
| Tempering Stone | armor, capes, shields | +1% armor (block on a shield) per use, up to +10% | everywhere |
| Sigil of Preservation | any item | The next reroll-type stone leaves the strongest inscription untouched | Mountain and later |
| Sigils of War, Warding, Fortune | any item | The next added inscription comes from offense, defense or utility | Swamp and later |
| Sigil of Culling | any item | The next removal takes the weakest inscription instead of a random one | Swamp and later |

A sigil sits on the item as "Pending" until a stone it steers uses it; one at a time. A sealed item takes no stone,
essence or sigil again. Every stone's odds, costs and the rarities it accepts are in `EliteCrafting_economy.yml`.

A refusal says why, for example "The Stone of Ascension does not work on Rare items", "This item cannot hold another
inscription", "No inscription can roll on this item", or "Unequip this item first" when the server does not allow changing
equipped items.

### Essences

Sixteen essences, a Lesser and a Greater for each of eight families. An essence rerolls a magic item like a Stone of
Upheaval (a bound inscription stays), and **one of the new inscriptions always comes from its family**. A Lesser essence works on
Uncommon and Rare items; a Greater one works on every magic rarity and rolls its family inscription at the best tier the
item allows. The essence's tooltip lists the inscriptions it can guarantee.

| Family | Biome | Drops from | Its inscriptions lean toward |
|---|---|---|---|
| Storm | Meadows | Meadows creatures, Eikthyr | lightning, speed, jumping, parries |
| Grove | Black Forest | Black Forest creatures, the Elder | blunt damage, woodcutting, regeneration, thorns, standing firm |
| Venom | Swamp | Swamp creatures, Bonemass | poison, leeching, cleansing, wading |
| Frost | Mountain | Mountain creatures, Moder | frost, cold, climbing, falling, stamina |
| Battle | Plains | Plains creatures, Yagluth | raw damage, armor, blocking, carrying |
| Seidr | Mistlands | Mistlands creatures, the Queen | eitr, magic, runes, the mist |
| Ember | Ashlands | Ashlands creatures, the Fader | fire, heat, light |
| Tide | Ocean | serpents | the sea: sailing, swimming, fishing, sea creatures |

Each boss always drops a Lesser essence of its family and sometimes a Greater one. Families are configurable in
`essence_families`.

### Sockets, gems and catalysts

A dropped magic item can have **sockets** (0 to 4; most have none). A **Jeweller's Chisel** cuts one more into any gear,
up to two. Sockets don't count toward the rarity's inscription limit, and no inscription stone touches them.

A **gem** clicked onto a socketed item fills a socket with one inscription of its family, chosen by the item's slot (a Frost
Gem gives Rimebrand on a weapon, Frostward on a helmet or cape, Endurance on a chest) and rolled for the item's own
tier. The gem's description lists what it gives where. When every socket is full, the next gem breaks the oldest one;
that asks first. There is no way to take a gem out.

A **catalyst** strengthens every inscription and gem of its family on the item by 1% per use, up to +20% (shown on the
tooltip and in the boosted values). A catalyst of another family replaces it and starts over at 1% (it asks first).

| Item | Drops from |
|---|---|
| Jeweller's Chisel | everywhere |
| Storm, Grove, Venom, Frost, Battle, Seidr, Ember Gem | its biome (Meadows ... Ashlands); its biome's boss half the time |
| Tide Gem | sea serpents |
| the eight catalysts | like the gems, without the bosses |

### Salvage

Hover a magic item in your own inventory and press **Shift + End** (`8 - Salvage / Salvage key`; without Shift it only
asks, or it follows your `Confirm destructive stones` mode). The item is ground into **two shards** of the ascension
stone that made its rarity: an Uncommon into Shards of Awakening, a Rare into Shards of Ascension, up to Mythic and the
Shards of Apotheosis. **Right-click** five shards to fuse a stone (ten for Apotheosis); **Shift + right-click** fuses
every full set. The loop always loses: a ground item returns at most 40% of one stone.

Equipped items and items with a pending sigil are refused; a sealed item grinds like any other. A server can switch
grinding off (`Salvage`, synced), require a crafting station nearby and change every number in the `salvage` section.

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
| `ecraft list inscriptions\|stones\|rarities [<filter>]` | everyone* | The configuration in force, filtered by slot, category, rarity or id; `stones` also lists the essence families and the shards |
| `ecraft give <stone>\|<shard>\|all [count]` | admin | Stones, essences or shards into your inventory |
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
| `BepInEx/config/com.EliteCrafting.cfg` | Switches and preferences. Gameplay keys (inscription effects, modifying equipped items, stone and gear drops, command access, salvage, the Elite Creatures Reborn synergy) follow the server; display, ground glow, the confirm mode, the Salvage key and diagnostics are per player |
| `BepInEx/config/EliteCrafting_inscriptions.yml` | Every inscription: effect, slots, category, tiers, weights, caps |
| `BepInEx/config/EliteCrafting_economy.yml` | Rarities and colors, rolling rules, stones, sigils, essence families, salvage, item tiers, biomes and drop tables (creatures, bosses, chests, Elite Creatures Reborn) |
| `EliteCrafting_inscriptions_<anything>.yml`, `EliteCrafting_economy_<anything>.yml` | Your own additions, read after the main file in name order; they change only what they name |
| `EliteCrafting.translations.<Language>.yml` | Your own words for a language, key to text, over the built-in English |

The main YAML files are written once with the full defaults and never rewritten. The built-in defaults always sit
underneath, so a later release's new inscriptions reach your server without editing anything; `use_defaults: false` in a
main file makes the files the whole configuration. A file with an error is reported in the log with file and line,
and the previous rules stay in force. Turn an inscription off with `enabled: false` (items that have it keep it, greyed and
inert, and get it back when you turn it on) or stop it rolling with `weight: 0`.
