# EliteCrafting - specification: Sockets and gems

Decided with the user on 2026-10-07 (PLAN.md Decisions log). Gear can carry up to three **sockets**; the **Dvergr
Chisel** cuts one into an item that has none; eleven very rare **gems** fill them, each giving a stat that depends on
the item's base, its tier rolled for the item when the gem is set. Modelled on Path of Exile 2's socketables, in our
own words. One switch, `Gems and sockets` (`2 - Runes`, synced, default on).

**Status: built 2026-10-07, not tested in game.**

# 1. Which items take sockets

Item classes by group (`Sockets/GemCatalog.BaseOf`): `onehand`, `twohand`, `ranged` are **weapons**; `magic` are
**staves**; `armour` and `offhand` (shields; not the `light` class, torches) are **armour**. Tools, trinkets, utility
items, torches and anything without a class take none.

# 2. Item data and drops

| Key | Value | Example |
|---|---|---|
| `ecf_sockets` | socket count, 1-3 (absent = none; above 3 reads as 3) | `2` |
| `ecf_gems` | filled sockets in order, `gem:inscription:grade:value;...` | `gem_thor:stormbrand:5:6` |

The grade and value are stored like an inscription's (item-data.md section 4). Gems are never removed, only replaced in
place, so the empty sockets are always the last ones. An entry that does not parse is kept verbatim in its socket. A
Normal item may carry both keys. The keys were retired on 2026-10-02 and came back here; no released version wrote them.

**Drops.** Every pre-rolled Magic or Rare item (creatures, bosses, chests) whose base takes sockets draws its count once
(`Sockets/SocketDrops`, fixed weights): Magic 80/15/4/1, Rare 60/25/11/4 for 0/1/2/3 sockets.

# 3. The gems

Fixed in code (`Sockets/GemCatalog`); the inscription's ladder, caps and `enabled` still come from the YAML.

| Gem | Weapons | Staves | Armour and shields |
|---|---|---|---|
| Surtr's | Emberbrand | Primal Fury | Flameward |
| Ymir's | Rimebrand | Seidr Thrift | Frostward |
| Thor's | Stormbrand | Thor's Chain | Stormward |
| Nidhogg's | Venombrand | Lingering Wounds | Venomward |
| Hel's | Spiritbrand | Soul Reaper | Spiritward |
| Tyr's | Honed Might | Grave Command | Resolute |
| Freyja's | Blood Drinker | Blood Thrift | Vigor |
| Odin's | Seidr Siphon | Seidr Flow | Wellspring |
| Skadi's | Wind Siphon | Grave Vigor | Endurance |
| Heimdall's | Keen Eye | Swift Casting | Mist Veil |
| Sleipnir's | - | - | Fleetfoot (movement speed) |

A gem's stat counts like the same inscription rolled on the item (`ItemState.EffectRolls`: the active inscriptions,
then the active gems): it adds to a matching inscription, channel caps apply, and it does not count toward the rarity's
prefix or suffix limits. A disabled or removed inscription leaves the gem dormant (shown greyed), like an inscription.

# 4. Setting a gem (`verb: gem`)

Click the gem onto the item, as with a rune (same checks: own inventory, not sealed, the equipped rule, one gem used).

- Refused without a socket (`gem_no_socket`), or when the gem has no stat for the base or its inscription is disabled
  (`gem_wrong_item`).
- **The roll** (`Sockets/GemRolls`): a tier of the inscription's ladder that the item level has unlocked, weaker tiers
  more often (the tier weights), the whole ladder open (as on a best-fit class); below the ladder's first unlock, the
  weakest tier. The value is uniform in the tier's range, a scaled inscription times the class's damage_scale.
- It fills the next empty socket. With every socket full the player **picks the socket to replace** (user decision):
  the game's yes/no popup asks once per filled socket in order ("Socket 2 holds Thor's Gem: +6 lightning damage.
  Replace it with Surtr's Gem? The old gem is lost."); Yes re-runs every check aimed at that socket and replaces it,
  No moves to the next, No on the last keeps everything (`Stones/GemChooser`).

# 5. The Dvergr Chisel (`verb: socket`)

One socket into an item whose base takes sockets and that has none, any rarity (`no_socket_here`, `has_sockets`). A
dropped item with sockets never gets more from the chisel.

# 6. Other runes, display

- Cleansing, Shaping, Recasting, Ascension and the Serpent leave sockets and gems alone (user decision: Cleansing
  keeps both). A sealed item takes no gem and no chisel.
- Tooltip, after the inscriptions: one line per gem in its colour ("Thor's Gem: +6 lightning damage  T5"), then
  "Empty socket" for each free one (`Display/GemLines`). `ecraft inspect` lists the gems and the socket count.
- **The Rune Table's Sockets tab** (`Table/Window/SocketTab`, `SocketPane`, `SocketText`; hidden while the switch is
  off): the gear you carry that takes sockets on the left; on the right its description (sockets and gems, what the
  chosen stone would do here, a gem's tier range at this item level), the rune row showing the Dvergr Chisel and each
  gem you carry (as many as the row's seven slots hold), the essence row showing the item's sockets (a gem's icon, a
  faint mark for an empty one; click a filled one to aim the gem there), the cost and **Set gem** / **Cut socket**. The
  press runs the same pipeline paid from your inventory (the table stores runes only); a gem aimed at a filled socket
  asks once (`GemChooser.Confirm`), a gem onto a full item with no socket picked asks socket by socket.

# 7. Prefabs and drops

`StoneCatalog.ChiselId` and `GemIds` (beside the seven `BuiltInIds`; `AllIds` is every one): prefabs `ECF_DvergrChisel`,
`ECF_Gem<God>`, built like the runes. Their models and icons come from the bundle `ecf_gems` (ValheimAssets
`Assets/Items/Gems`, assets `ecf_<id>` and `ecf_<id>_icon`, `StoneTablets.Gems`, gloss 0.45), worn like the rune
tablets (`StoneTablets.Runes`, `ecf_runes`); without the bundle the gems are tinted copies of the Ruby and the chisel
tinted iron nails (`StoneBases` groups `Gem`, `Tool`; tints in `StoneTints`).

Default drops (economy YAML): in `drops.runes`, the chisel weight 5 and each gem 0.5 in every tier (any gem about one
kill in 3,200 in the Meadows, one in 760 in the Deep North, more with stars). **Boss gems are code** (`Sockets/BossGems`,
added to a boss-map boss's drops in `LootPlanner.PlanBoss`, keyed on `LootInput.Prefab`), so servers whose own economy
file predates the gems still get them: every boss the chisel 25%; Eikthyr Thor's and Sleipnir's 10% each, the Elder
Freyja's 15%, Bonemass Nidhogg's 15%, Moder Ymir's and Skadi's 20%, Yagluth Surtr's and Tyr's 20%, the Queen Odin's
and Heimdall's 20%, the Fader Hel's 25%.

`Gems and sockets` off: no sockets on drops, the chisel and gems neither drop nor work (`gems_off`), the Rune Table
hides its Sockets tab; gems already set keep their stats.
