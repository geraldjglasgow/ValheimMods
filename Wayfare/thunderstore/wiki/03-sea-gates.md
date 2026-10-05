# Sea Gates

**Experimental.** A sea gate is a portal for ships: two pillars with water between them. A ship sailing through comes
out of another sea gate with everyone aboard, keeping its speed. Only ships jump: a swimmer or a walker passing
through is not moved.

## Building the pillars

- The **Sea Gate Pillar** is in the hammer's Misc tab, built near a workbench. It costs 10 Stone, 5 Fine Wood,
  5 Greydwarf Eyes and 1 Surtling Core (`Pillar Recipe`); taking it down returns all of it.
- A pillar is about 4.5 m tall and stands on land or on the bottom of water at most 2 m deep. Deeper, the ghost turns
  red and cannot be placed.
- Build a second pillar across the water. The two pair by themselves when:
  - they stand 10 to 15 m apart, measured along the ground (`Min Gate Width`, `Max Gate Width`);
  - their bases are within 4 m of each other's height;
  - the water between them is at least `Min Water Depth` (1 m) deep at the middle and halfway to each pillar;
  - on at least one side, it is that deep 5, 10 and 15 m out from the middle. A side too shallow is never used as an
    exit.
- Depth is measured to the ground: digging the seabed deeper counts, a dock or other building piece does not.
- A pillar pairs with the nearest pillar that is not already in a gate.
- Once paired, a portal swirl fills the gap: dim while the gate has no destination, bright once it has one. The
  builder, if within 40 m, sees "The sea gate is open".
- Removing either pillar closes the gate. A gate formed again, with another pillar, starts without a destination.

## The placement preview

While you hold a pillar ghost, or look at a placed pillar that is not paired, and a free pillar stands within 30 m
(twice `Max Gate Width`):

- a line runs to that pillar: green with "Pairs with this pillar", or red with the reason and its numbers;
- a post stands at each of the nine measured spots, from the seabed up out of the water: green where deep enough, red
  where not (a short red post on land);
- a lane on the water runs out to each side: green where ships may come out, red where not.

| Reason (red) | Fix |
| --- | --- |
| Too far from the other pillar: X m, at most 15 m | move closer |
| Too close to the other pillar: X m, at least 10 m | move apart |
| X m higher or lower than the other pillar, at most 4 m | build on more even ground |
| No water between the pillars | build across water |
| Too shallow at the red marks: X m deep, a ship needs 1 m | dig the red spots deeper, or move a pillar |
| Too deep for a pillar: X m under water, at most 2 m | build in shallower water |

A placed pillar keeps trying by itself, so fixing the problem (digging, moving or removing a pillar) opens the gate
without rebuilding: its hover text says "Fix it and the gate opens by itself". With no other pillar in reach it says
"Not paired: build a second pillar 10 to 15 m away across the water".

## Destination, name and access

Look at either pillar of a gate: the hover text shows the gate's name, its destination and its access mode.

- **E** opens the map as the sea gate picker: only the sea gates you may sail to, and this gate in gold with a line to
  its current destination. Click another gate to make it the destination ("Ships sailing through now go to ...").
  Click this gate to rename it (up to 20 characters; a gate without a name is "Sea gate"). Closing the map changes
  nothing.
- **Shift+E** cycles the gate's access mode, exactly as on a portal ([Portals and Access](wiki:Portals and Access)):
  Public, Private, Admin.
- Setting the destination, renaming and changing the mode need the same right: the gate has no owner yet, you own it,
  or you are an admin ("You don't own this sea gate"); under a ward you need access to it.
- A destination is one way: each gate has its own. For the way back, set the other gate's destination too.
- Who may sail there is the destination gate's access mode, judged for the player at the helm.
- On the map a sea gate is a dark blue disc with two waves in a teal ring. Gates show while you pick, and on the
  ordinary map with `Toggle Icons Key` (P).

## Sailing through

1. Someone must be at the helm. Sail between the pillars, from either side.
2. A quarter of the ship's length through, everyone aboard sees the game's teleport screen.
3. Everyone lands on deck where they stood, facing the same way. Whoever steered has the helm again; anyone seated is
   standing. The ship keeps its speed, sail and rudder.

- The ship comes out of the destination gate on the side it was heading for, at the same place across the gate and
  the same angle, half a ship's length plus 4 m beyond it. If that side is too shallow there, it comes out of the
  other side, turned around.
- If another ship or a large object is in the way, the ship is placed clear of it, up to 20 m further out.
- The ship's storage goes along. Tamed animals and items lying loose on deck stay behind.

When a ship does not jump, everyone aboard sees why and the ship sails on through; turn and sail through again to
retry.

| Message | Meaning |
| --- | --- |
| A ship passes through a sea gate only with someone at the helm | nobody steers |
| This sea gate has no destination | set one with E |
| The destination sea gate is gone | its pillars were removed or no longer pair |
| Only the destination gate's owner may sail there | the destination is Private |
| Only a server admin may sail to the destination gate | the destination is Admin |
| Ore and other restricted cargo can't pass through a sea gate | see below |
| The sea gate wasn't ready: sail through again | the gate could not open in time |
| This sea gate is closed | sea gates are switched off |
| Portal travel is blocked here | a world modifier blocks portals |

**Restricted cargo:** as with portals, no jump while the ship's storage or anyone aboard carries ore, metal or other
items portals refuse (a world modifier that lets everything through portals counts). `Allow Restricted Cargo` on lets
them through.

## Safety

- For `Protection Seconds` (5) after arriving, everyone who jumped takes no damage of any kind (hits, falls,
  drowning, cold, fire, poison), and neither does the ship. A ship in the middle of a jump cannot be damaged either.
- If the ship has not arrived after `Crew Wait Seconds` (20, at least 5), you are set ashore on dry ground beside a
  destination pillar, or on top of one, with the same protection: "The ship didn't arrive in time: you were set ashore
  by the gate". The ship still comes out at the gate.
- Logging out or losing the connection during a jump: you come back beside the destination gate, never at sea.
- `Sea Gates / Enabled` off: no ship jumps and the pillar leaves the build menu; pillars already built stay.

Every sea gate setting: [Quick Jumps and Settings](wiki:Quick Jumps and Settings).
