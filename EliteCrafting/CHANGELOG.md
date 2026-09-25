# Changelog

## 0.2.0

The economy: every stone works, essences, salvage, chests, and 103 new affixes.

- **All 27 stones work.** Greater Growth, Turmoil and Perfection for Epic, Legendary and Mythic items; Lesser and
  Greater Upheaval reroll every affix; Lesser and Greater Severing remove one; Unmaking strips an item back to Common
  (asks first; its Honed or Tempered bonus and a pending sigil survive).
- **Risk stones.** The Serpent Stone corrupts and seals an item with one of five weighted outcomes (nothing more, an
  extra affix past the cap, a chaotic reroll that ignores tier limits, one rarity up - a 7th affix on a Mythic - or one
  rarity down); Binding locks one affix against Turmoil, Upheaval, Perfection and Severing; Chance turns a Common into
  a random rarity (never Mythic by default); Reflection makes a sealed copy of an item, affixes and bonus included.
- **Sigils** of Preservation, War, Warding, Fortune and Culling: applied to an item, they steer the next stone they fit
  and are spent only when they do. **Honing and Tempering Stones** raise a weapon's damage, an armor piece's armor or a
  shield's block by 1% per use, up to 10%.
- **Essences**: sixteen new stones, a Lesser and a Greater for each of eight biome families - Storm (Meadows), Grove
  (Black Forest), Venom (Swamp), Frost (Mountain), Battle (Plains), Seidr (Mistlands), Ember (Ashlands) and Tide
  (Ocean). An essence rerolls a magic item like a Stone of Upheaval, and one of the new affixes always comes from its
  family; a Greater essence rolls that affix at the highest tier the item allows and works on every rarity. Each
  biome's creatures drop its own essences, each biome boss always drops a Lesser one, serpents drop Tide essences.
  The essence's tooltip lists the affixes it can guarantee. Families are configurable in `essence_families`.
- **Salvage**: hover a magic item in your inventory and press Shift + End (`8 - Salvage / Salvage key`; the confirm
  follows your `Confirm destructive stones` mode) to grind it into two shards of the ascension stone that made its
  rarity. Right-click five shards to fuse a stone (ten for Apotheosis), Shift + right-click to fuse them all. The loop
  always loses: a ground item returns at most 40% of one stone. A server can switch grinding off (`Salvage`, synced),
  require a station and change every number in the new `salvage` section.
- **Chests**: dungeon and ruin chests, buried treasure and every other world container the game fills with loot now
  also hold stones (30% per chest) and pre-rolled magic gear (10%), from the tables of the biome they stand in. Each
  container rolls once, when the game first fills it; player-built chests never do. `drops.chests.containers`
  retiers or blocks single container prefabs.
- **103 new affixes** (on 81 new effects and a few existing ones) - every easy and medium effect of the catalog
  outside the Mythic pool, 162 affixes in all. Offense: Undead, Beast and Sea Slayer and Godslayer, Ambusher,
  Deathblow, Cruel Opening, Press the Advantage, Staggering and Dazing Blows, Hamstring, Reaper and Soul Reaper
  (restore on a kill), Blood Drinker and Seidr Siphon (leech), Evader's Fury, Fafnir's Greed, Long Reach, Sweeping
  Arc, Blood Price, Rune-Edged, Lone Blade, Steel Rhythm. Ranged and staves: Swift String, True Flight, Thrifty
  Quiver, Volley, Skirmisher, Blood Thrift, Twincast, Grave-Lord's Command, Grave Vigor. Defense: Padded, Mailed,
  Riveted, Ironclad, the four wards and Elemental Ward, the four Bulwarks, Arrowward, Mist Veil, Bramblehide, Runic
  Ward, Resolute, Ironroot, Quick Recovery, Purity, Coldblood, Keen and Anchored Guard, Seidr Riposte, Mending, Stout
  Heart, Restless Mind. Movement and environment: Stride, Pack Mule, Momentum, Pathfinder, Mountain Goat,
  Marshstrider, Strong Swimmer, Raven's Glide, Soft Tread, Shadowmeld, Ashen Skin, Emberheart, Winterborn, Oilskin,
  Sealegs, Hearthlight (a light everyone sees), Mistbane. Utility: Hearthbound, Gourmand, Soulbound, Brewer's Haste,
  Forsaken Favour, Reflex and Swift Draught, Harvester, Beast Whisperer, Fair Winds, Deep Vein, Heartwood,
  Everlasting, Gossamer, Supple Fit.
- **Loot-find affixes** on helmets and utility items: Norns' Favour (magic gear from your kills rolls higher rarities
  more often), Fateweaver (more stones from your kills), Trophy Taker (trophies) and Hoardfinder (coins and treasure).
  They count for the player who lands the killing blow, whoever's machine rolls the loot.
- **Health-critical**: Valhalla's Edge raises the threshold, and the Cornered variants (Berserkergang, Cornered Thirst,
  Blood, Hide, Veil and Flight) switch on below it. Evader's Fury, Steel Rhythm and a charged Runic Ward show an icon
  on the HUD while they are active.
- **Every effect works on a dedicated server**: a kill credits the killer wherever the creature was simulated, and what
  other players or the world need (Hearthlight, Mistbane, taming, sailing, gathering, stagger length) is published on
  the player and applied by whoever owns the creature, ship or resource.
- **Display**: the workbench's upgrade tab shows the item you are upgrading in its rarity color with its affix block,
  and item and armor stands show a magic item's colored name and affixes on hover.
- **Elite Creatures Reborn synergy** (optional, off by default: `9 - Elite Creatures Reborn / Synergy`): with Elite
  Creatures Reborn installed, its elite stars raise stone and gear drops on their own table in `drops.ecr` (a
  three-star elite pays double). Whenever it is installed, its Cloven twin and Phantom husks drop nothing from
  EliteCrafting, so a split boss never pays twice. No dependency: without it nothing changes.
- **Commands**: `ecraft ecr` shows what the synergy sees; `ecraft give` accepts shard ids; `ecraft list stones` also
  shows the essence families and the shards; `ecraft stats` lists the effects active right now.
- **Upgrading a server that ran a 0.1.0 build:** its main `EliteCrafting_economy.yml` was written by 0.1.0 and still
  lists the 19 stones that were not ready then with `enabled: false`, and its boss `bonus` lists lack the essences. A
  main file wins over the built-in defaults for every entry and field it names, so those stones stay off and bosses
  drop no essences. Delete or rename it (the next start writes the 0.2.0 file) and keep your own changes in an
  `EliteCrafting_economy_<anything>.yml`. New affixes, essences and drop-table rows it does not name arrive on their own.

## 0.1.0

First release: the playable core.

- Six rarities, Common to Mythic, with the canonical colors; rarity is the affix count, affix tiers 1-7 follow the
  biomes, and every item's tier ceiling comes from its materials, its crafting station or an override.
- 59 affixes on 34 effects for weapons, shields, armor, capes, utility items and tools: damage, the eight brands,
  stamina and eitr costs, weapon and utility skills, maximum health, stamina and eitr, regeneration, armor, block,
  movement, jumping, fall damage, carrying capacity, pickup and map-reveal radius, skill gain, build range,
  durability and weight. Player-wide totals run through one hidden status effect, rebuilt only when equipment
  changes; per-item stats change the item's own numbers, so the vanilla tooltip shows them too. Totals are capped per
  channel.
- Eight working stones: Awakening, Ascension, Exaltation, Transcendence and Apotheosis climb the ladder; Lesser
  Growth adds an affix, Lesser Turmoil swaps one, Lesser Perfection rerolls the numbers. Click a stone stack onto an
  item in your inventory; refusals explain themselves and use nothing up; an optional confirm gate (hold Shift,
  dialog or off) for stones a server marks as needing it.
- The other 19 stones and the 16 reserved prefabs for server-made stones are registered already, with tints, grade
  sizes and tinted icons, so later releases and custom stones never orphan a stack. Stone stacks load whole even when
  a server changes the stack size.
- Creature drops: stones and pre-rolled Uncommon to Legendary gear on kills a player took part in, by biome tier,
  stars and boss; a hit from a player's tamed creature counts as the player's, tamed creatures themselves drop nothing.
- Rarity-colored names in the inventory, on the ground and in the pickup message; a tooltip block with three detail
  levels, dormant (greyed) affixes, and the rarity's own color; a ground glow on magic items capped to the nearest 25.
- Magic items keep their data through a workbench upgrade.
- Two YAML families (`EliteCrafting_affixes*.yml`, `EliteCrafting_economy*.yml`) layered over complete built-in
  defaults, validated with file and line, reloaded every five seconds, and bound from the server with the `.cfg`'s
  gameplay keys. A file with errors keeps the previous rules.
- The `ecraft` console command: `help`, `inspect`, `stats`, `list`, `give`, `roll`, `reroll`, `affix`, `reload`,
  `dump affixes|economy|items` and `tiers`.
- English text; a player's own `EliteCrafting.translations.<Language>.yml` overrides it.
