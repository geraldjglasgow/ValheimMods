# Item icons

How the game's inventory icons are made, measured on 2026-09-29 by `measure/items_icons.py` over every classified
item's first icon: `data/items.json["icons"]` (sizes, and per category the five numbers of each measure). Contact
sheets at twice size on the inventory's dark slot colour: `codex/out/items/icons/<category>.png`; a 4x close-up of
twelve: `out/items/icons/closeup.png`.

## Where they live

- `ItemDrop.m_itemData.m_shared.m_icons` lists Sprite assets (`GameElements/Items/_icons/<Name>.asset`); 977 item
  prefabs have one, 17 (the Fallen Valkyrie's copies included) have one per variant (shields with styles: ShieldBanded
  4, ShieldBlackmetal 7, ShieldFlametal 5; CapeLinen 6), 525 (creature attacks, hair and beards) none.
- Every sprite is packed into one 4,096 x 4,096 BC7 atlas (`Texture2D/sactx-0-4096x4096-BC7-IconAtlas-7390861a.png`);
  the sprite's `m_RD.textureRect` is its box in the atlas, counted from the bottom left. `m_PixelsToUnits` 50, pivot
  in the middle.
- A mod's icon is a Sprite of its own (the workshop bundles `<asset>_icon.png` as `<asset>_icon`, see the workshop
  README).

## Size and framing

- **64 x 64 px**: 879 of 937 measured icons; the rest are trimmed a few pixels (62 x 64, 56 x 56, 63 x 62 ...).
- **Transparent background**, no frame, no slot drawn in.
- **Fills the square along its longest side**: the object spans 84 to 97 % of the icon's size (median about 0.91:
  2 to 5 px of margin on a 64 px icon), whatever its real size. Long weapons touch two corners.
- **Share of the square covered** (alpha over half): long thin things 4 to 13 % (arrows 4, bows 9, swords 10, spears
  11, pickaxes 12, atgeirs 13), axes and maces 18 to 26 %, food, materials, armour and helmets 34 to 38 %, round
  shields 46 %.
- **Angle of the long axis** (0 = horizontal, 90 = upright): swords, two-handed swords, spears, maces, bows and
  crossbows lie on the diagonal at 44 to 46 degrees, hilt or grip at the lower left and the point at the upper right
  (sheet `weapon.sword.png`); axes and pickaxes 30 to 42 degrees with the haft from the lower left and the head at the
  upper right, the edge facing right; knives steeper, about 61 degrees; atgeirs about 30; staffs, bottles, shields,
  capes, trophies and meads stand upright (about 90); legs and fish lie flat (about 5 and 15).

## How they are drawn

Looked at in `closeup.png` and the sheets:

- **They are renders of the 3D model** on its own game textures, not paintings: the same flat fields, grain and wraps
  as in the game, shaded by a soft light. Weapons and tools are seen side-on (the flat of the blade facing the
  viewer), objects in a three-quarter view from the front and above.
- **Chest and leg armour shows the dropped model**: a folded garment seen from above at a slant, its layers stacked,
  its colours and trim visible (ArmorTunic1, ArmorBronzeChest). A few show the worn shape on an invisible body
  (ArmorIronChest, ArmorWolfChest). Capes are shown hanging, from the back.
- **No outline and no drop shadow.** The outermost ring of pixels is as bright as the inside (sword icons: rim luma
  112, inside 109; food 105 and 118), and the edge is anti-aliased: 2 to 5 % of the square is half-transparent. The
  exceptions are glows, which are soft halos of the item's effect colour: Coins (32 % of the square semi-transparent,
  a yellow glow), gems (9 %), torches (11 %), staffs (9 %), and the elemental variants (AxeGold_BloodLightning's red
  crackle, the FrostFire variants' icy mist, AxeJotunBane's yellow rim glow).
- **Light from the front and a little from the upper left**: the top-left half of a silhouette is on average 0 to 25
  luma levels brighter than the bottom-right half (median about 5); no hard specular highlights, no rim light.
- **Mid tones and the game's colours**: inside median luma 64 to 197 (most 80 to 120 of 255), saturation 0.25 to 0.45
  (gems 0.65, torches 0.76, fish and bait 0.4).
- **Moulds and uncooked parts** (Deep North) add a flat white pictogram of the finished item in the top-right corner
  over the grey mould.

## Checklist for a new icon

- Render the finished model in Blender on its own textures, point filtered, with a soft key light from the front and
  upper left and a flat ambient; no rim light, no shadow on the ground, transparent background.
- 64 x 64 px (render larger, then shrink with a smooth filter, so the edge is anti-aliased); the object spanning 85 to
  95 % of the square.
- Weapons on the diagonal, grip lower left, point or head upper right; objects in a three-quarter view from the front
  and above; armour as its folded dropped model.
- Only a glowing or magical item gets a halo, in its effect colour.
- Put it beside the game's icons of its category (`out/items/icons/<category>.png`, drawn at 2x on a dark brown
  background like the inventory's) before shipping.

## References

Icons of `SwordIron`, `Battleaxe`, `ShieldBanded`, `HelmetIron`, `CookedMeat`, `Wood`, `MeadHealthMinor`,
`TrophyBoar`, `ArmorIronChest`, `CapeLox`, `Coins`, `Ruby` (the close-up sheet), under
`GameElements/Items/_icons/`.
