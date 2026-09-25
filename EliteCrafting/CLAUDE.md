# CLAUDE.md - EliteCrafting

Crafting stones and magic item affixes across six rarities. Read `../CLEANROOM.md` first, then the workspace
`../CLAUDE.md` (layout, small units: methods at most 24 lines brace to brace including lambdas, classes at most 300
lines, one responsibility; multiplayer first; display preferences unsynced). The behaviour lives in `SPEC.md` and
`features/`; `PLAN.md`'s Decisions log overrides them where they disagree. Game signatures: `~/scratch/specs/ec-game-notes.md`
and the full decompile `~/scratch/decomp/full/assembly_valheim.decompiled.cs` (grep it; never copy from it).

Identity: GUID `com.EliteCrafting`, name `EliteCrafting`, version in `EliteCrafting.cs` (`PluginVersion`), custom-data and
ZDO key prefix `ecf_`, console command `ecraft`, YAML families `EliteCrafting_affixes*.yml` / `EliteCrafting_economy*.yml`.
Libraries merged: Charter, ConfigReload, PatchGuard, YamlDotNet (ECR's pattern). **No YamlConfig, no SyncedConfig, no
ItemCopies** (DECISIONS.md IMP-1: live stones share their prefab's `SharedData` instead).

**Status (2026-09-24): Phase 1 (0.1.0) and Phase 2 (0.2.0) code complete, integrated and reviewed, builds clean; nothing
tested in game yet.** The judgement calls made while building are DECISIONS.md "Settled during Phase 1 implementation"
(IMP-1 to IMP-124, Phase 2 from IMP-65). Next: the in-game test plan in `features/multiplayer.md` section 6 (steps 1-21
Phase 1, 22-38 Phase 2), then packaging. Phase 3 waits on AFX-8.

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
`StoneCatalog` lists the 43 built-in stone ids (27 stones + 16 essences, `IsEssence`, `EssenceFamilies`), their prefab
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
14 steps, dry run), `ConfirmGate.Pass`, then `StoneCommit` (one `ItemState.Write`, then the cost). All 14 verbs
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

`config/EliteCrafting_affixes.yml` is generated from `features/affixes.md` (section 4 catalog) by scripts kept outside
the repo; `config/EliteCrafting_economy.yml` is written from `features/economy-yaml.md` plus the Phase 2 sections
(`essence_families`, `salvage`, `drops.chests`, `drops.ecr`). Both load with 0 errors and 0 warnings.
**Decision: the default affix file lists only affixes whose effect is registered** (0.2.0: 162 affixes on 115 effects;
0.1.0 had 59 on 34; the 13 Mythic-only affixes wait for Phase 3). An affix naming an unregistered effect is an error even when `enabled: false`, so typos never hide behind a
disabled flag. Later phases add their affixes to the defaults as their effects are registered; because the built-in
defaults are a layer under the owner's files, new affixes reach existing servers without anyone editing a file.

Merge rules (configuration.md section 3): layers = built-in defaults (unless the main file says
`use_defaults: false`), the main file, then other files by name. Id lists (`affixes`, `rarities`, `stones`) merge by
id field by field; maps merge by key; other lists and scalars are replaced; a key set to null is removed. Errors reject
the family and keep the previous rules; at startup the author falls back to the built-in defaults alone and publishes
that. Sync: each family is one Charter article (the file texts); a bound player builds from the built-in defaults plus
the server's files.
