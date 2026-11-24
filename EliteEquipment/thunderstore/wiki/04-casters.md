# Casters

Section `4. Casters`, one Server setting, off by default.

| Setting | Default | Meaning |
| --- | --- | --- |
| `Wands and Spells` | off | A healer and caster line: wands that cast spells, a spellbook for the left hand that holds the spell pages, seer's robes for every biome and eitr foods from the Meadows to the Plains. |

## What off means

- With `Wands and Spells` off, none of it can be crafted, no spell pages drop, pages go in no book, wands cast nothing and fire no bolt, worn gear gives no eitr, and there is no spell bar, Spellbook or spell tooltip line.
- Items you have already made stay in your inventory.
- Switch it on and the game's own eitr is added to from the start: worn seer's robes, a wielded wand and the eitr foods give eitr before the Mistlands.
- Required on the server and every client.

## Controls

Anyone holding a wand is a caster. A wand is one-handed; spells need a spellbook in the other hand. Without a book a wand fires only its bolt.

- **Left mouse:** fires the wand's bolt for 4 eitr (up to a third less with Elemental Magic). Quick clicks chain a three-move combo like a sword's swings.
- **Hold left mouse:** charges the bolt. The longer you hold, the stronger and bigger it gets, up to +30% damage at 3 seconds. Let go to throw it. You walk slowly and turn to aim while charging, and the charge costs no extra eitr.
- **Hold right mouse:** a ring of the spells in your spellbook opens in the middle of the screen. Move the mouse toward a spell and let go to cast it. Let go in the middle, or press Esc, to cast nothing. A spell you lack the eitr for is shown dim. The camera stays still while the ring is open; you can still walk.
- **Tap right mouse:** casts the last spell you picked on the ring again.
- **Right mouse held + jump:** dodges without casting.
- **Middle mouse:** casts the last spell too.
- **Dodge with a wand drawn:** a short blink instead of a roll, up to 5 m, stopped short of anything solid. It costs the roll's stamina and half that in eitr, has a 0.8 second cooldown, and nothing a roll would dodge hits you from the blink until half a second after.
- A caster does not block: the block button does nothing with a wand drawn.

## Spellbook

- Every page in the book you hold is on the right mouse ring, so all its spells are ready.
- Each wand remembers the last spell you picked with it. While a wand is drawn, a slot under the hotbar marked RMB shows that spell with its eitr cost. It is greyed, and a tap casts nothing, while its page is not in the book you hold.
- With the inventory open and a spellbook in hand, a Spellbook button appears. The window lists the pages in the book. Click one to read it, or take it out into your inventory.

## Spell pages

- A wand casts only the spells whose pages are in the spellbook in your left hand. A new book is empty: every spell, Mend too, is a page you find.
- Use a page from the inventory while holding a book to put it in. It is refused when the book is full (the Leatherbound Spellbook holds 2 pages, the Barkbound Grimoire 4, the Cinderbound Tome 8) or already has that page.
- Each page in the Spellbook window has a Take out button that puts it back in your inventory, so a page can be handed to a friend.
- The pages are kept in the book itself: a book dropped, traded or left in a chest keeps them.
- Pages are found, never crafted:

| Source | Chance |
| --- | --- |
| Bosses | Each drops its own page, one for each player present (Eikthyr: Page: Stag's Swiftness; The Elder: Page: Elder's Volley; Moder: Page: Moder's Breath; The Queen: Page: Sanctuary) |
| Creatures with two stars or more, killed by a player | 5% |
| Dungeon chests, when their loot is first made | 10% |
| Dungeon chests (any dungeon), Page: Revive only | 2% |
| Any other creature a player kills | 0.5% |

Creatures and chests give a page from their own biome or an earlier one, never a boss's page. So far that is Page: Mend or Page: Hearthsong, and from the Swamp on Page: Cleanse and Page: Eir's Spring too, from the Mountains on Page: Ward and Page: Feather Fall, from the Plains on Page: Second Wind and Page: War Chant, from the Mistlands on Page: Chain Mend. Page: Revive comes only from dungeon chests, never from a creature. Tames and players drop none.

A character who learned spells before the spellbook gets one page of each of them in the inventory once.

## Spells

Spell costs are cut by up to a third with Blood Magic (the strikes', Elder's Volley and Moder's Breath, with Elemental Magic). Heals go to the friend you aim near within 30 m (other players and your tames); a gold marker shows who. Your character turns to them as you cast.

| Spell | Eitr | Effect |
| --- | --- | --- |
| Mend | 8 | Heals the marked friend, or yourself if none is marked, by the wand's heal power |
| Hearthsong | 15 | Chant: everyone within 10 m, you too, regains health 50% faster for 60 seconds |
| Stag's Swiftness | 15 | Chant: everyone within 10 m, you too, moves 10% faster and runs on 20% less stamina for 60 seconds |
| Elder's Volley | 20 | Strike: six of the Elder's seeds shoot from the wand's tip to where you aim within half a second, each with 60% of the wand's bolt damage and its damage types; hits only enemies |
| Moder's Breath | 25 | Strike: Moder's frost breathed from the wand's tip toward where you aim, about 7 m long and 30 degrees wide, six bursts in half a second, each with half the wand's bolt damage as frost and spirit; frost slows; hits only enemies |
| Cleanse | 12 | Takes every harmful effect off the marked friend, or yourself if none is marked (burning, poison, frost, lightning, smoke, tar, slime, wet, cold and the like; never rested, food, meads or chants), and heals a fifth of the wand's heal power. Cold, wet and smoke return while their cause lasts |
| Ward | 20 | A shield on the marked friend, or yourself if none is marked, for 20 seconds: it takes all of every hit until it has taken twice the wand's heal power. Cast again, it is whole again |
| Sanctuary | 40 | A 6 m dome at your feet for 10 seconds: every friend inside takes 30% less damage while they stay in it. Ready again 2 minutes after each cast |
| War Chant | 25 | Chant: everyone within 10 m, you too, deals 10% more damage for 60 seconds |
| Feather Fall | 15 | Chant: everyone within 10 m, you too, falls slowly and takes no fall damage for 30 seconds, as with the feather cape |
| Eir's Spring | 25 | A 4 m healing spring on the ground where you aim within 20 m (at your feet when you aim farther): every friend inside, you too, heals a tenth of the wand's heal power every second for 8 seconds |
| Second Wind | 20 | Everyone within 10 m, you too, gets 50 stamina back at once |
| Chain Mend | 20 | Mends the marked friend, or yourself if none is marked, then jumps to the nearest hurt friend within 8 m not healed yet, up to 4 in all, each heal 20% less than the one before |
| Revive | 50 | Aim at the grave of a friend who fell within the last 3 minutes and is still in the game: they are brought back to it with half their health, and both of you are told. Refused, with a message, when no such grave is aimed at. Once every 5 minutes (a status effect shows the wait) |

## Wands

Heal power rises by 1.5 a quality. Bolt damage is the pierce (blunt for the Greywood and Rootbound Wands, frost for the Frostsilver Wand, fire for the Ashwood Wand) and the spirit amount each, and spirit hurts skeletons, draugr and ghosts more. Both weigh 1 and have 100 durability. The eitr is gained while the wand is wielded.

| Wand | Station | Heal power | Eitr | Bolt | Cost |
| --- | --- | --- | --- | --- | --- |
| Gnarled Wand | Workbench | 15 | +10 | Pierce 6, spirit 6 | Wood 5 (+2), Resin 3 (+1), Feathers 2 (+1), Dandelion 3 (+1) |
| Greywood Wand | Forge | 25 | +15 | No bolt: a stone thrown straight at where you aim like a spell, with a dark green trail, blunt 7, spirit 7; charging grows a stone in a green glow at the tip, and the charged stone is bigger and breaks apart about 5 m out into more stones the longer you charged (up to 15), flying on in a ragged tight cone (each a share of the hit, with a little knockback) | Fine wood 4 (+2), Bronze 2 (+1), Greydwarf eye 6 (+3) |
| Rootbound Wand | Forge | 35 | +20 | No bolt: roots burst from the ground where you aim, as the Elder's do (1 tapped, up to 5 at full charge), each blunt 11, spirit 11 | Ancient bark 6 (+3), Iron 3 (+1), Roots 2 (+1) |
| Frostsilver Wand | Forge | 45 | +25 | No bolt: ice shards fly in a fan toward where you aim (1 tapped, up to 5 at full charge), each frost 15, spirit 15 (frost slows) | Fine wood 4 (+2), Silver 4 (+2), Freeze gland 3 (+1) |
| Blackmetal Wand | Forge | 60 | +30 | Pierce 19, spirit 19 | Black metal 4 (+2), Fine wood 4 (+2), Needle 5 (+2) |
| Yggdrasil Wand | Galdr table | 75 | +30, eitr regen +25% | Pierce 22, spirit 22 | Yggdrasil wood 6 (+3), Refined eitr 4 (+2), Royal jelly 3 (+1) |
| Ashwood Wand | Galdr table | 90 | +30, eitr regen +40% | No bolt: a firebolt, fire 27, spirit 27 (sets what it hits burning); a tap throws a small one, charged it is bigger, and at full charge it explodes where it lands (2 m, enemies only) | Blackwood 6 (+3), Flametal 3 (+1), Celestial feather 2 (+1) |

## Spellbooks

| Book | Station | Pages | Stats | Cost |
| --- | --- | --- | --- | --- |
| Leatherbound Spellbook | Workbench | 2 | Block 2 (+1 a level), eitr regen +10%, wand heal power +3 | Deer hide 3 (+1), Leather scraps 4 (+2), Resin 2 (+1), Feathers 2 (+1) |
| Barkbound Grimoire | Forge, level 2 | 4 | Block 4 (+1 a level), eitr regen +15%, wand heal power +6 | Ancient bark 4 (+2), Root 3 (+1), Iron 2 (+1) |
| Cinderbound Tome | Galdr table, level 2 | 8 | Block 8 (+1 a level), eitr regen +25%, wand heal power +12 | Asksvin hide 4 (+2), Flametal 3 (+1), Charred bone 4 (+2) |

Worn in the left hand like a shield, each with parry bonus 1.5x, weight 1 and durability 100. Its tooltip lists its pages. A book that already holds more pages than it has room for keeps them all, and they all still cast, but it takes no new page until you take enough out.

## Seer's robes

Eight sets of hood (or hat, circlet, crown), robe (or tunic, coat) and trousers. Each is crafted beside the matching game armour, at its station and level. Cost in brackets is per upgrade level. Armour rises by 2 a level. Robes and trousers lower movement by 2% (the Deerhide Vestments lower none), hoods none. The eitr is per worn piece. The set bonus needs all pieces worn.

| Set | Armour | Eitr | Set bonus |
| --- | --- | --- | --- |
| Deerhide Vestments (Meadows) | 1 | +5 | Mend costs 25% less eitr |
| Mossweave (Black Forest) | 4 | +8 | Chants last 50% longer |
| Rootweave (Swamp) | 7 | +10 | Poison resistant |
| Wolfwool (Mountains) | 10 | +12 | Frost resistant |
| Sunweave (Plains) | 13 | +15 | Your heals also restore 10 stamina |
| Mistweave (Mistlands) | 16 | +15 | Blood Magic +15 |
| Emberweave (Ashlands) | 19 | +15 | Fire resistant, Blood Magic +20 |
| Rimegild (Deep North) | 22 | +15 | Frost resistant, Blood Magic +25 |

| Piece | Crafted beside | Cost |
| --- | --- | --- |
| Deerhide Hood | Leather Helmet | Deer hide 2 (+1), Leather scraps 2 (+1), Dandelion 2 (+1) |
| Deerhide Robe | Leather Tunic | Deer hide 4 (+2), Leather scraps 3 (+1), Dandelion 3 (+1) |
| Deerhide Trousers | Leather Pants | Deer hide 3 (+1), Leather scraps 3 (+1), Dandelion 2 (+1) |
| Mossweave Headdress | Troll Leather Helmet | Troll hide 2 (+1), Thistle 3 (+1), Greydwarf eye 2 (+1) |
| Mossweave Robe | Troll Leather Tunic | Troll hide 4 (+2), Thistle 4 (+2), Greydwarf eye 3 (+1) |
| Mossweave Trousers | Troll Leather Pants | Troll hide 3 (+1), Thistle 3 (+1), Greydwarf eye 2 (+1) |
| Rootweave Hat | Root Helmet | Root 2 (+1), Ancient bark 4 (+2), Bloodbag 2 (+1) |
| Rootweave Tunic | Root Harnesk | Root 4 (+2), Ancient bark 6 (+3), Bloodbag 3 (+1) |
| Rootweave Trousers | Root Leggings | Root 3 (+1), Ancient bark 5 (+2), Bloodbag 2 (+1) |
| Wolfwool Hat | Drake Helmet | Wolf pelt 2 (+1), Silver 2 (+1), Wolf fang 2 (+1) |
| Wolfwool Coat | Wolf Hide Chestpiece | Wolf pelt 5 (+2), Silver 4 (+2), Wolf fang 3 (+1) |
| Wolfwool Trousers | Wolf Hide Trousers | Wolf pelt 4 (+2), Silver 3 (+1), Wolf fang 2 (+1) |
| Sunweave Hat | Padded Helmet | Linen thread 10 (+5), Lox pelt 1 (+1), Black metal 2 (+1) |
| Sunweave Robe | Padded Cuirass | Linen thread 20 (+10), Lox pelt 2 (+1), Black metal 4 (+2) |
| Sunweave Trousers | Padded Greaves | Linen thread 15 (+5), Lox pelt 2 (+1), Black metal 3 (+1) |
| Mistweave Circlet | Eitr-weave Hood | Linen thread 10 (+5), Jotun puffs 5 (+2), Carapace 6 (+3) |
| Mistweave Robe | Eitr-weave Robe | Linen thread 20 (+10), Jotun puffs 10 (+5), Carapace 10 (+5) |
| Mistweave Trousers | Eitr-weave Trousers | Linen thread 15 (+5), Jotun puffs 8 (+3), Carapace 8 (+4) |
| Emberweave Hat | Hood of Embla | Asksvin hide 4 (+2), Flametal 4 (+2), Celestial feather 2 (+1) |
| Emberweave Robe | Robes of Embla | Asksvin hide 8 (+4), Flametal 6 (+3), Celestial feather 3 (+1) |
| Emberweave Trousers | Trousers of Embla | Asksvin hide 6 (+3), Flametal 5 (+2), Celestial feather 2 (+1) |
| Rimegild Hat | Hood of the Caller | Norn thread 6 (+3), Seal hide 4 (+2), Moose sinew 2 (+1) |
| Rimegild Robe | Robes of the Caller | Norn thread 12 (+6), Seal hide 6 (+3), Moose sinew 3 (+1) |
| Rimegild Trousers | Trousers of the Caller | Norn thread 9 (+4), Seal hide 5 (+2), Moose sinew 2 (+1) |

- With `Separate Boots` on (see [Boots](wiki:Boots)), each seer's trousers split into trousers and boots like every leggings, 80/20 in stats, weight and cost, and the set needs one more piece. The boots are Deerhide Foot Wraps, Mossweave Foot Wraps, Rootweave Foot Wraps, Wolfwool Boots, Sunweave Boots, Mistweave Boots, Emberweave Boots and Rimegild Boots. The Deerhide Trousers' +5 eitr is split 4 and 1.

## Eitr foods

Eitr food for the biomes before the Mistlands, where the game has none. Each gives its eitr for the time shown. All are eaten like any food. Snacks make 2 a craft.

| Food | Station | Health / stamina / eitr | Lasts | Regen | Ingredients |
| --- | --- | --- | --- | --- | --- |
| Dandelion Mash | Workbench | 12 / 6 / 35 | 25 min | 1 | Dandelion 3, Honey 1, Mushroom 1 |
| Candied Thistle | Cauldron 1 | 15 / 15 / 15 | 15 min | 1 | Thistle 3, Honey 1 |
| Glowcap Broth | Cauldron 1 | 15 / 8 / 40 | 25 min | 2 | Yellow mushroom 2, Thistle 2, Carrot 1 |
| Seer's Porridge | Cauldron 1 | 15 / 8 / 45 | 30 min | 2 | Blueberries 4, Thistle 2, Honey 1 |
| Turnip Crisps | Cauldron 2 | 18 / 18 / 18 | 15 min | 1 | Turnip 3, Thistle 1 |
| Blood Draught | Cauldron 2 | 18 / 9 / 50 | 25 min | 2 | Bloodbag 2, Thistle 2, Turnip 1 |
| Mire Stew | Cauldron 2 | 18 / 9 / 55 | 30 min | 2 | Turnip 3, Bloodbag 1, Yellow mushroom 1 |
| Glacier Tea | Cauldron 3 | 22 / 11 / 60 | 25 min | 2 | Onion 2, Freeze gland 1, Honey 1, Thistle 1 |
| Wolfheart Stew | Cauldron 3 | 22 / 11 / 65 | 30 min | 2 | Wolf meat 1, Onion 3, Freeze gland 2 |
| Candied Cloudberries | Cauldron 4 | 25 / 25 / 25 | 15 min | 1 | Cloudberries 4, Honey 1 |
| Lox Marrow Broth | Cauldron 4 | 25 / 12 / 70 | 25 min | 3 | Lox meat 1, Cloudberries 2, Barley flour 3 |
| Sunbarley Porridge | Cauldron 4 | 25 / 12 / 75 | 30 min | 3 | Barley flour 4, Cloudberries 3, Honey 1 |

Candied Thistle, Turnip Crisps and Candied Cloudberries make 2.
