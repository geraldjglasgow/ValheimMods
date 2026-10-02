# EliteCrafting - specification: Sockets, gems and catalysts

One feature of the mod, specified on its own. Decided with the user on 2026-10-01: sockets hold **gems**, one gem per
essence family; a gem into a full item **breaks the oldest** (no removal); **catalysts** strengthen one essence
family. Everything else here is a judgement call made while building, open to the user's override.

**Status: built 2026-10-01, not tested in game.**

---

# 1. What the player sees

A dropped sword can come with sockets. A **Jeweller's Chisel** cuts one more into any gear, up to two. A **Frost Gem**
clicked onto a socketed sword fills a socket with Rimebrand, rolled for the sword's own tier; on a helmet the same gem
gives Frostward. When every socket is full, the next gem breaks the oldest one (the use asks first, like the Serpent
Stone). A **Frost Catalyst** clicked onto an item with a Frost affix or gem makes all of them 1% stronger, up to +20%;
a catalyst of another family throws that away and starts over (it asks first).

Sockets, gems and the catalyst sit beside the inscriptions: they do not count toward the rarity's affix cap, and every
affix stone (Unmaking, Upheaval, the Serpent Stone...) leaves them alone. A sealed item takes no stone, gems
included (STN-1).

# 2. Item data and drops

Three custom-data keys (`Affixes/ItemKeys`, written by `Affixes/SocketCodec`):

| Key | Value | Example |
|---|---|---|
| `ecf_sockets` | socket count, integer | `3` |
| `ecf_gems` | filled sockets oldest first: `gem:affix:grade:value;...` | `gem_frost:rimebrand:3:7;gem_storm:stormbrand:3:5` |
| `ecf_catalyst` | `family:quality` | `frost:12` |

The grade is the affix's strength grade like `ecf_inscriptions` (`item-data.md` section 4), not the tooltip tier. A key that
does not parse is kept verbatim and written back. Common items may carry all three. No format bump: an older build
leaves the keys alone.

**Drops.** Every dropped magic item (and `ecraft roll`) draws its socket count once from `drops.gear.sockets`, a list
of weights indexed by count: default `[60, 20, 12, 6, 2]` (0 to 4 sockets). No item ever has more than 6
(`StoneDef.SocketLimit`).

# 3. The Jeweller's Chisel (`verb: socket`)

One more socket while the item has fewer than the stone's `max_sockets` (default 2). Refused at that count
(`sockets_full`), so a dropped item with 3 or 4 never gets more from a chisel. Any rarity, Common included.

# 4. Gems (`verb: gem`)

- `affixes:` maps item slots to the affix id the gem gives there. A slot not listed refuses the gem (`wrong_item_type`,
  pipeline step 5); so does an affix that is unknown, disabled, fails its `requires` block on this item, or defines
  no tier the item can roll (`gem_no_fit`).
- The roll is the item's ordinary one: a tier from the item's tier window (`rarity.md` section 4, the stone's
  `tier_floor` if it has one), a value uniform in that tier's range (`Rolling/GemRolls`). A Meadows item's gem rolls
  T7, an Ashlands item's T1-T3.
- It fills the first empty socket. With every socket full the oldest gem breaks and the others move up one; that use
  is **destructive** and asks first in the player's confirm mode (`StoneResult.Breaking`, `ConfirmGate`).
- Refused without a socket (`no_sockets`). Two gems may give the same affix, and a gem may give an affix the item
  already has: they add up, channel caps still apply.
- Defaults (judgement calls): one gem per family, the family's affix that rolls on each slot, preferring affixes that
  start at tier 7 so early items can take them. Seidr's head gem (Seidr Flow) and Tide's chest gem (Sealegs) start
  late and refuse on early items.

# 5. Catalysts (`verb: catalyse`)

- `family:` an `essence_families` id; `step` (default 1) and `cap` (default 20) in percent points.
- Same family as the item's catalyst: quality rises by `step`, clipped to `cap`; at the cap refused (`catalyst_capped`).
- Another family: the catalyst is replaced and quality starts at `step`. When that throws quality away the use asks
  first.
- Refused when nothing on the item, affix or gem, active or dormant, belongs to the family (`catalyst_no_match`).
- Effect: every affix and gem of the family counts `1 + quality / 100` times its stored value; flags are not scaled.

# 6. Effects

`ItemState.EffectRolls` is what effects read (`Affixes/EffectRolls`): the active affixes, then the gems whose affix is
defined and enabled, values with the catalyst applied. `ItemEffects.CollectItem` and `ItemLocalSums` read it, so a gem
works exactly like the same affix rolled on the item, on a dedicated server as anywhere (the item data rides the
game's own item serialization; effects are computed on the wearer's client).

# 7. Display

After the affix lines: one line per gem, "Frost Gem: <its affix sentence>  T5", greyed while its affix is dormant; one
"Empty socket" line per free socket; "Frost catalyst +12%". Catalysed affix and gem values show the boosted number.
The gem and catalyst stones' descriptions list what they give per slot and which affixes they strengthen
(`Items/SocketDescriptions`). The Full-detail rarity line shows the item's best affix tier ("Rolls up to T1").

# 8. Stones, prefabs, drops

17 built-in stone ids (`StoneCatalog`): `chisel`, `gem_<family>` and `catalyst_<family>` for the eight families, prefabs
`ECF_Chisel`, `ECF_Gem<Family>`, `ECF_Catalyst<Family>`, cloned like every stone (`prefabs.md`): gems from the game's
cut gemstones, catalysts from a mead bottle, the chisel from nails (all fall back like PRF-1; verify in game). Gems and
catalysts take their family's essence tint; the chisel is brass.

Default drops: the chisel weight 40 in every tier; a family's gem 40 and catalyst 30 in its home tier only (Tide from
serpents, 10% each); each boss drops its family's gem half the time.
