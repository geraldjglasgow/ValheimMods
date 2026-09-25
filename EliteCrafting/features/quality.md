# EliteCrafting - specification: Quality (Honing and Tempering)

One feature of the mod, specified on its own. The other feature files sit beside it; `../SPEC.md` is the whole-mod
behaviour document they are all drawn from.

This file covers the two quality stones, which make an item's own base numbers a little stronger, independently of
rarity and affixes. How stones are applied and refused in general is `stones.md` sections 1-4.

Numbers are defaults and all of them are configurable in the `stones:` entries `honing` and `tempering` of
`EliteCrafting_economy*.yml`.

**Status: built (Phase 2, 2026-09-23), not tested in game.** Ships in **Phase 2**.

---

# 1. A word about the name

The game already has a "quality": an item's **upgrade level** at the workbench (1-4 for most gear), shown in the
vanilla tooltip as *Quality*. This feature is a different thing. To keep the two apart:

- **Player-facing text never calls ours "quality".** The tooltip says *Honed +4%* or *Tempered +4%*
  (`$ecf_ui_honed`, `$ecf_ui_tempered`).
- In this spec, "quality" means ours (the verb id is `quality`); the game's is always "upgrade level".
- The custom-data key avoids the bare word too: it is `ecf_refine` (`item-data.md` section 3), one number of
  percent points, present only when above 0.

---

# 2. The two stones

| Stone | id | Slots | Improves | Step per use | Cap |
| --- | --- | --- | --- | --- | --- |
| Honing Stone | `honing` | `melee_weapon`, `ranged_weapon`, `magic_weapon` | the item's damage | +1% | +10% |
| Tempering Stone | `tempering` | `head`, `chest`, `legs`, `cape` | the item's armor | +1% | +10% |
| | | `shield` | the shield's block power | +1% | +10% |

- `step` and `cap` are per stone in YAML (integers, percent points). One stone per use at the default cost of 1.
- One number per item: an item has a single honing-or-tempering percentage. No item is in both stones' slot lists,
  so the two never meet on one item.
- **Tools, utility items** (belts, the Wishbone) and **dual-purpose weapons' blocking** are not covered. Tools stay
  excluded (`../DECISIONS.md` QLT-1): a honed pickaxe mines faster, which is a different kind of reward.

---

# 3. What the percentage does, precisely

All three are **postfixes on the game's own getters**, so the tooltip, the crafting preview and the actual hit or
block all read the same number and nothing is computed per frame. Verified in the decompile.

## Honing: damage

- The item's damage getter for a given upgrade level and world level returns the item's damage block: base damage,
  plus damage per upgrade level, plus the world-level bonus. **Honing multiplies that whole block by
  `1 + h/100`** - every damage type in it, physical, elemental and the chop/pickaxe values a weapon carries.
- It is therefore "this weapon is h% stronger", including an axe's chopping. Affixes that add damage (`affixes.md`)
  apply on top, so honing and a damage affix multiply. (Judgement call; the alternative - honing adds to the same
  sum as affix bonuses - would need every damage affix to know about honing.)
- **Bows and crossbows**: the projectile's damage is the weapon's damage plus the ammunition's. Honing raises the
  weapon's part only.
- **Magic weapons**: the staff's own damage block, the same getter. A staff with no damage of its own (a summoning
  staff) cannot be honed (`$ecf_msg_wrong_item_type`) - a stone that visibly does nothing should not be spendable.
- Durability, attack stamina, attack speed, knockback and the sneak-attack multiplier are untouched.

## Tempering on armor: armor

- The item's armor getter (base armor, plus armor per upgrade level, plus the world-level bonus) is multiplied by
  `1 + t/100`.
- Fractional results stay fractional, as the game's own armor sums already are. A cape with 1 armor tempered to
  +10% gives 1.1 armor; the tooltip shows one decimal only when the value has one.
- An armor piece with 0 armor (some cosmetic items) cannot be tempered (`$ecf_msg_wrong_item_type`).

## Tempering on a shield: block

- The shield's **base block power** getter (base block power plus block power per upgrade level) is multiplied by
  `1 + t/100`. The game derives the final block power from the base and the Blocking skill afterwards, so the
  percentage carries through to blocking and to the tooltip.
- Parry bonus, deflection force (knockback on block) and the shield's own armor field are untouched. "Armor (block)"
  in PLAN.md is read as *block power*, the number a shield actually defends with.

---

# 4. Independence from affixes

- Any rarity, **Common included**. Honing a plain vanilla sword is legal and useful.
- Rarity, affix count and affixes never change quality; quality never changes them.
- **Survives Unmaking** (PLAN.md): stripping an item to Common keeps its honing/tempering.
- **Survives every other stone**, including every Serpent outcome.
- **Copied by Reflection.**
- **Kept through workbench upgrades**: the percentage multiplies whatever upgrade level the item is at. The game
  recreates an upgraded item without its custom data; `item-data.md` section 1 carries our keys across, `ecf_refine`
  included.
- **Refused on sealed items** (`../DECISIONS.md` STN-1, QLT-5): sealed means no stone of any kind.

---

# 5. Edge cases

| Situation | Result | Message |
| --- | --- | --- |
| At cap (10%) | refused | `$ecf_msg_quality_capped` |
| Honing on armor, shield, tool, utility item | refused | `$ecf_msg_wrong_item_type` |
| Tempering on a weapon | refused | `$ecf_msg_wrong_item_type` |
| Weapon without damage / armor without armor / shield without block | refused | `$ecf_msg_wrong_item_type` |
| Stackable item (arrows) | refused | `$ecf_msg_not_magic_base` |
| Sealed | refused | `$ecf_msg_sealed` |
| Equipped | allowed; equipment rebuild so the new armor total applies | - |
| Pending sigil | not steered; stays pending | - |
| YAML lowers the cap below an item's value | the item keeps its value (values are fixed), further stones refused | `$ecf_msg_quality_capped` |
| YAML raises the step | future uses step by the new amount; the last step is clipped to the cap | - |

---

# 6. Display

- A tooltip line under the item name: **Honed +N%** or **Tempered +N%**, absent at 0.
- The vanilla damage, armor and block numbers in the tooltip already include the bonus (they come from the patched
  getters), so no second number is shown next to them.
- Whether the line is shown at all is a client display preference (unsynced), like the rest of the tooltip
  verbosity.

---

# 7. Multiplayer

The percentage is item custom data and replicates with the item. The getters run wherever the game calls them - the
attacker's owner for hits, the defender's owner for armor and block - and each reads the same replicated number, so
every peer agrees without a message.

---

# Build checklist

- [ ] Honing: damage getter postfix, whole damage block
- [ ] Tempering: armor getter postfix; shield base block power postfix
- [ ] Step, cap, per-slot filter, no-damage / no-armor refusals
- [ ] Survives Unmaking and every other stone; copied by Reflection; kept through workbench upgrades
- [ ] Tooltip line, player text never says "quality"

## Work log

| Date | What changed | Commit |
| --- | --- | --- |
| 2026-09-23 | Specified in Phase 0. | pending |
| 2026-09-23 | Reconciled: open questions moved to `../DECISIONS.md`. | pending |

---

# Decisions

Every question this file raised is answered in `../DECISIONS.md` (Quality: QLT-1 to QLT-6).
