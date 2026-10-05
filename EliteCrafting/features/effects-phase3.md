# Phase 3 effects

The effects behind the inscriptions added with item classes and tier ladders (classes-and-tiers.md; user decisions
2026-10-05). Every id below is named by `Config/EliteCrafting_inscriptions.yml`; an id is registered only when its code
exists ("a registered id is a promise"). Registration lives in `Effects/EffectCatalog.Phase3*.cs`, one partial file per
area: `RegisterPhase3Combat`, `RegisterPhase3Survival`, `RegisterPhase3Gathering`, `RegisterPhase3Throwing`.

Rules for every effect (effects-runtime.md and the workspace CLAUDE.md apply):

- **Multiplayer first.** Work on a dedicated server with several players. Decide world changes on the owner of the thing
  changed; draw on every client; values another peer needs travel as a float on the player's own ZDO (`PlayerStats`,
  written when it changes, readers clamp to the cap) or with the hit. Damage to another character goes through
  `Character.Damage` (the game routes it to the owner).
- **Read the summed value, never the YAML.** Player-global effects read `EffectTotals` / the aggregate channel; item-local
  effects read the item's own sum (`ItemLocalSums`). Caps are applied by the channel layer; a hook clamps only what it
  must (a probability to 100%).
- **Health-critical variants** (`condition: health_critical`) of an existing effect must keep working: the conditional
  channel is a separate channel of the same effect.
- Small units (24-line methods, 300-line classes), PatchGuard on patches, no transpilers unless no prefix/postfix can do it.
- Game signatures come from decompiling `assembly_valheim.dll` into the scratch folder. Other mods' code may be read to
  learn how something can be done; never copy it.

| id | route, scope, values, param, polarity, cap | what it does | inscription |
|---|---|---|---|
| **Combat** | | | |
| `added_damage` | Hook, ItemLocal, Flat, DamageType, Raise | This weapon's hits deal X more of the param type (postfix `ItemData.GetDamage`, like `brand_damage`, after quality). Fire and poison land partly over time, as the game does. | emberbrand, rimebrand, stormbrand, venombrand, spiritbrand, bonebreaker, keen_edge, needlepoint |
| `attack_speed` | Hook, ItemLocal, Percent, None, Raise, 15 | Attacks with this weapon play X% faster (animation and the attack's timing). Must look the same to other players. Health-critical variant. | quickened, quickened_hc |
| `cast_speed` | Hook, ItemLocal, Percent, None, Raise, 25 | The same for a staff's attacks. | swift_casting |
| `chain_lightning` | Hook, ItemLocal, Percent, None, Raise, 100 | A hit has a 15% chance to arc to up to 3 other enemies within 8 m, each taking X% of the hit's damage as lightning, with a lightning visual every client sees. | thors_chain |
| `explosive_shot` | Hook, ItemLocal, Percent, None, Raise, 100 | A projectile from this bow or crossbow also hits every other enemy within 3 m of its impact for X% of its damage, with an impact visual. | bursting_shot |
| `paralyze` | Hook, ItemLocal, Flat (s), None, Raise, 3 | Hits paralyse the target (cannot move or attack) for X s; bosses immune. A status effect in ObjectDB on every peer, applied by the target's owner (the `on_hit_slow` pattern). | numbing_blow |
| `knockback_dealt` | Hook, ItemLocal, Percent, None, Raise, 100 | This weapon's hits push X% further. | mighty_blows |
| `penetration` | Hook, ItemLocal, Percent, None, Raise, 50 | Hits ignore X% of the target's resistance (damage modifiers) to each damage type: X% of the reduction the target would apply is bypassed. Weaknesses unchanged. | sundering |
| `crit_chance` | Hook, PlayerGlobal, Percent, None, Raise, 25 | New critical hits: each hit the local player deals (weapon, projectile, spell) has X% to be critical. | keen_eye |
| `crit_damage` | Hook, PlayerGlobal, Percent, None, Raise, 100 | A critical hit deals x(1.5 + X/100). A "Critical!" text shows at the hit for the attacker. | brutal_strikes |
| `dot_duration` | Hook, PlayerGlobal, Percent, None, Raise, 100 | Burning, poison and frost the player inflicts last X% longer. The target's owner applies it, reading the attacker's player-ZDO value. | lingering_wounds |
| **Survival** | | | |
| `mead_duration` | Hook, PlayerGlobal, Percent, None, Raise, 100 | Status effects from meads last X% longer. | long_brew |
| `mead_potency` | Hook, PlayerGlobal, Percent, None, Raise, 50 | Health, stamina and eitr meads restore X% more. | potent_brew |
| `mead_save` | Hook, PlayerGlobal, Percent, None, Raise, 50 | X% chance drinking a mead does not use it up. | bottomless_flask |
| `food_values` | Hook, PlayerGlobal, Percent, None, Raise, 30 | Food eaten gives X% more health, stamina and eitr. | hearty_appetite |
| `food_regen` | Hook, PlayerGlobal, Percent, None, Raise, 50 | Food health regeneration is X% higher. | well_fed |
| `rested_duration` | Hook, PlayerGlobal, Percent, None, Raise, 100 | Rested lasts X% longer when it is applied. | deep_rest |
| `max_adrenaline` | Hook, PlayerGlobal, Flat, None, Raise | +X maximum adrenaline (the trinket mechanic). | battle_rush |
| `adrenaline_gain` | Hook, PlayerGlobal, Percent, None, Raise, 100 | Blocks and parries give X% more adrenaline. | blood_up |
| `block_restore` | Hook, PlayerGlobal, Flat, Resource, Raise | Blocking or parrying a hit restores X of the resource (health in the defaults). | shield_mend |
| `boss_damage_taken` | Hook, PlayerGlobal, Percent, None, Lower, 40 | Bosses deal X% less damage to the player. | forsaken_ward |
| `burning_taken` | Hook, PlayerGlobal, Percent, None, Lower, 75 | Burning and lava damage taken X% lower. | ember_skin |
| `burning_decay` | Hook, PlayerGlobal, Percent, None, Raise, 75 | Burning on the player ends X% sooner. | quench |
| `air_jump` | Hook, PlayerGlobal, Flag, None, Raise | One extra jump while in the air, reset on landing. | windstep |
| **Gathering and crafting** | | | |
| `chop_damage` | Hook, ItemLocal, Percent, None, Raise, 100 | This axe's hits on trees and logs deal X% more chop damage. | timber_bite |
| `pickaxe_damage` | Hook, ItemLocal, Percent, None, Raise, 100 | This pickaxe's hits on rock and ore deal X% more pickaxe damage. | stone_bite |
| `butcher_yield` | Hook, PlayerGlobal, Flat, None, Raise | Animals (non-hostile wildlife: deer, boar, hare, lox, etc.) the player kills drop X more of their meat and hide drops. Applied by the creature's owner from the killer's player-ZDO value. | butchers_cut |
| `bait_save` | Hook, PlayerGlobal, Percent, None, Raise, 75 | X% chance a fishing cast keeps its bait. | patient_line |
| `fish_size` | Hook, PlayerGlobal, Percent, None, Raise, 100 | Caught fish are more likely to be a higher quality (size): X% better odds. | big_catch |
| `reel_stamina` | Hook, PlayerGlobal, Percent, None, Lower, 60 | Reeling costs X% less stamina. | steady_reel |
| `craft_save` | Hook, PlayerGlobal, Percent, None, Raise, 30 | X% chance a craft (not an upgrade) uses no materials. | thrifty_hands |
| `craft_extra` | Hook, PlayerGlobal, Percent, None, Raise, 30 | X% chance a craft (not an upgrade) makes one more. | bountiful_forge |
| `trader_discount` | Hook, PlayerGlobal, Percent, None, Raise, 30 | Traders (Haldor, Hildir, the Bog Witch) charge X% less; the shown price and the coins taken agree. | silver_tongue |
| `ship_damage_taken` | Hook, PlayerGlobal, Percent, None, Lower, 75 | The ship the player steers takes X% less damage. Applied by the ship's owner from the steering player's ZDO value. | sea_ward |
| `free_build` | Hook, ItemLocal, Flag, None, Raise | While this hammer or hoe is in hand, building ignores the crafting-station requirement. | masterbuilder |
| **Throwing** | | | |
| `throwable` | Hook, ItemLocal, Flag, None, Raise | This one-handed weapon's secondary attack throws it like a spear: it flies, hits, and lands as a pickable item. Its own damage. | throwing_grip |
| `recall` | Hook, ItemLocal, Flag, None, Raise | When this thrown weapon lands, it returns to the thrower's inventory (and slot) instead of lying on the ground. Spears too. | returning |
| `apportation` | Hook, ItemLocal, Flag, None, Raise | When this thrown weapon hits an enemy, the thrower teleports to the impact point. | bifrost_step |

Existing effects the new inscriptions reuse (no new code): `damage_dealt:all` (wrath), `stamina_recovery@health_critical`,
`eitr_recovery@health_critical`, `item_block@health_critical`, `parry_bonus@health_critical`, `damage_taken:spirit`,
`leech:stamina`.
