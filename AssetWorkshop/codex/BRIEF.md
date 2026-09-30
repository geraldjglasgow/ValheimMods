# Brief: `<asset name>`

Copy this file to `assets/<name>/BRIEF.md` and fill it in before modelling. Every field is answered from the codex
page named beside it; write "none" (and why) rather than leaving a field empty. The brief is the plan the model, the
paint, the rig and the mod code are built from, and the checks at the end are how the result is judged.

## 1. What it is and where it lives

> One line each. The biome and tier choose its palette, its materials and its budget (`biomes.md`, `models/<kind>.md`).

- Name (the prefab name, prefixed with the mod: `openkeep_supply_crate`):
- What it is, in one sentence:
- Mod:
- Biome (where it is found, used or built):
- Tier (as the codex data numbers them: 0 Meadows, 1 Black Forest, 2 Swamp, 3 Mountain, 4 Plains, 5 Mistlands,
  6 Ashlands; the tier of what it is made of):
- Who uses it (crafted and held by the player, carried by a creature, placed in the world, dropped as loot):

## 2. Category

> A key from `codex/data/*.json` (`weapon.axe_2h`, `shield.round`, `piece.wall`, `env.rock_small`, `env.tree_meadows`,
> `env.grass`, `location.ruin`, `creature.quadruped` ...). List them with `python -c "import sys; sys.path.insert(0, 'codex/tools'); import codexdata; print(codexdata.keys())"`.
> The key goes into `model.py` as `CATEGORY`; the style check and the lineup read it. Copy the category's numbers here.

- Category key:
- Label and sample count:
- Triangles (min, p25, median, p75, max):
- Texture px:
- Texel density (px per metre):
- Longest side (m):

## 3. Game references

> Three to six prefabs to line up with: the category's `references` first, then any the asset must sit beside (the
> creature that carries it, the piece it stands on). Say what each one is for. `models/<kind>.md` names the best ones.

| Prefab (path under the reference export) | What to take from it (silhouette, size, paint, detail level) |
| --- | --- |
| | |

## 4. Silhouette

> One sentence: what reads at 10 m in the game's light (`look.md`). Big shapes, few parts, one clear feature.

-

## 5. Parts

> The parts an asset of this kind is made of (`models/<kind>.md`), with sizes in metres and their share of the whole.
> Nothing smaller than two texels is modelled: at the category's texel density that is the smallest part; smaller
> detail is paint (`look.md`). Conventions: metres, Z up, front towards -Y, the origin is the pivot (on the ground under
> a piece or prop; the grip for a held item, where the game puts its `attach` point).

- Origin and orientation:
- Held items: the fist at the origin, which way the head or blade points and which way its edge faces, in the attach
  frame (`models/weapons.md` section 1); the `attach` node's scale is the size in the hand:
- Dropped items: the size on the ground (drawn 1.5 to 3 times real size for materials, not for weapons;
  `models/items.md`):
- Building pieces: the snap pattern (points and extent on the 1 m grid, `models/pieces.md`), the `New`/`Worn`/`Broken`
  looks and the destruction chunks:

| Part | Size (m) | Proportion of the whole | Material family | Region | Notes |
| --- | --- | --- | --- | --- | --- |
| | | | | | |

## 6. Budget

> From the category (section 2). Aim at the median; stay inside min to max. `TEXTURE_SIZE` and `AO_STRENGTH` go into
> `model.py`; the style check measures the result.

- Triangles (target):
- `TEXTURE_SIZE`:
- Texel density (target px per metre):
- `NORMAL_MAP` / `AO_STRENGTH` (0.2 with the paint recipes, which paint their own hollows) / `NORMAL_FROM_ALBEDO`
  (the family's strength, `paint.md`):

## 7. Materials

> By family (wood, iron, bronze, bone, leather, cloth, stone, hide, crystal ...): the paint recipe
> (`blender/workshop/paint.py`: `paint.make("<family>", name, **overrides)` or `paint.<fn>(name, ...)`; overrides
> `preset`/`dye`, `tint` (keeps each tone's luminance), `density`, `axis`, `region`; paint grey whatever a variant or
> star level will tint), the palette entries it uses (`palette.md`, linear
> RGB, sampled from the game) and how the game paints that family (`paint.md`: light baked in, big soft blotches,
> dark gaps, value and saturation ranges). The game shader the mod dresses it into comes from `shaders.md`.

| Family | Parts | Recipe and overrides | Palette entries | Notes |
| --- | --- | --- | --- | --- |
| | | | | |

- Building pieces: the set whose material is borrowed (`models/pieces.md`, "Materials and texture sharing"), and the
  WearNTear material type (wood, stone, iron, marble ...) that chooses its hit and destroy effects:

## 8. Paint regions

> The parts a mod may recolour, each a region (`regions.mark(material, "primary")`; names in `regions.COLOURS`:
> primary, secondary, trim, metal, leather, cloth, skin, glow, bone, wood ...). Unmarked materials fall into "base".
> `recolour.md` compares this with the game's own HSV and style textures; plan the variants for `variants.json`.

| Region | Materials / parts | What a variant changes |
| --- | --- | --- |
| | | |

- Variants (name: region operations, e.g. `blue: primary=hue:+0.6`):

## 9. Rig and animation (creatures and anything that moves)

> Choose a route (`rigs/README.md`: the contract, the three routes, the conventions and a checklist; then
> `rigs/player.md`, `humanoid.md`, `quadruped.md`, `flyer.md`, `serpent.md`, `other.md`, and `models/creatures.md`):
> (a) a game creature with new parts or clips, (b) a new body on an exact copy of a game skeleton
> (`blender/workshop/gamerig.py`, the Mossback in `assets/workshop_gamerig_demo`), or (c) a rig of our own. Name the
> head bone `Head`; list the attack triggers the creature's attack items fire; walk and run clips carry a `footstep`
> curve; attack states carry the tag `attack`.

- Route and source prefab:
- Bones and sockets (hands, back, head, mouth, attach points):
- Clips (idle, walk, run, attacks, stagger, death, sleep ...) and their lengths:
- Events (attack trigger frames, footsteps, sounds):
- Animator controller (reused, or parameters and states):

## 10. Effects

> Reuse a game effect by prefab name where one fits (`vfx/*.md` lists them by archetype); a new one names its archetype
> and the measured values it follows (particles, lifetime, size, texture, shader, light).

| When (hit, death, idle loop, equip, place, destroy) | Reuse (game prefab) or new (archetype, values) |
| --- | --- |
| | |

- Building pieces: the `vfx_Place_*` effect that fits the footprint (`data/pieces.json` `place_effects`):

## 11. Sounds

> Reuse a game sound by prefab name where one fits (`sfx/*.md`); a new one names its archetype and the measured
> length, loudness and variations it follows.

| When | Reuse (game prefab) or new (archetype, length, loudness, variations) |
| --- | --- |
| | |

## 12. Into the mod

> How it reaches the game (the workshop README, "In a mod: BundlePrefabs"; `shaders.md` for the material). Nothing of
> the game's goes into the bundle: game prefabs, materials and effects are borrowed at runtime by name. This is the
> plan only: the asset stays in AssetWorkshop, and nothing is copied into the mod or written in its code until the
> user decides to release it there (workspace rule, `CLAUDE.md` "Always").

- Bundle name (lower case) and the mod folder it is copied to:
- Game prefab to copy and build on (an item's ItemDrop prefab, a piece, a creature):
- Game material to dress into (`GameMaterials.Dress` / `Borrow`), and its shader:
- Textures (albedo, normal, variants or the regions mask for recolouring in the mod):
- Colliders (box, convex mesh) and physics:
- Icon (`<name>_icon.png`), recipe or placement, config entries:
- Multiplayer: what is decided on the owner, what every client draws, what lives in the ZDO:

## 13. Checks

> The asset is done when every line holds. Each LOW or HIGH in the style report is fixed or has its reason written here.

- [ ] `.\build.ps1 -Asset <name> -Lineup` builds; `out/preview.png` looked at, all four views.
- [ ] Style check (`out/style_report.txt`): every line PASS, or the reason for each LOW or HIGH:
- [ ] Lineup (`out/lineup/front.png`, `turn.png`, `close.png`) looked at beside the references: size, silhouette, value
      and saturation, detail level, texel size. What differs and what was changed:
- [ ] Paint regions (`out/<name>_regions.png`) cover the parts named in section 8; the variants sheet
      (`out/variants.png`) looks painted, not filtered.
- [ ] In the game through DevBridge (`LocalTesting` profile): screenshots at 3 m and 10 m in its biome, held, placed or
      fighting as it is used; its effects and sounds play; on a dedicated server with two players.
