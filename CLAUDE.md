# Bedrot Simulator

Working title, taken from the repo name. Not finalized.

## The pitch

A first-person, never-ending life sim about an ordinary student in Stockholm who starts dealing
weed to get by and gradually spirals into something he can't walk back from. No win state, no
ending — the "never-ending simulation" part is the point, same way My Summer Car never really
ends, it just keeps piling consequences and systems onto a small persistent space you know
intimately.

The user (Cesar) previous project was an American Psycho dramatization (satire of a monster). This one is the
opposite angle: an ordinary person, sympathetic at the start, who makes one bad call after another
until he's in over his head. Tone reference: Tarantino / Reservoir Dogs — raw, dialogue-driven,
morally messy, not sanitized, but not shock-for-shock's-sake either. Grounded over cartoonish.

## Structure

Scripted intro, then it opens up into the sandbox:
1. Player is a student attending lectures (Java programming courses, university portal exams).
2. Intro beat: character meets classmate who gives him a joint. he getts hooked, gets dealers number. 

  the dealer calls him (phone call), which is how he first gets pulled in. he starts to smoke more and more, and gets into it for real when he pays via credit / helps the dealer out in some way to get drugs. 
3. From there the game becomes the open-ended sim: buy/craft/consume loops, needs management,
   money pressure.
4. Planned escalation: player gets more involved in the drug business, eventually acquires a gun
   and goes rogue. This should stay **tense and restrained** rather than becoming an action/crime
   sim — the gun is a weight/threat, not a toy. Think dread building up, not shootout gameplay.

Whether the rogue path is one-way or the player can climb back out is still undecided (see Open
Questions).

### Money & the debt mechanic

- Player income becomes tied to attending lectures/exams: a periodic (monthly-ish) payout for
  keeping up with university, building on the CSN payout already implemented in
  `UniversityPortal.SubmitExam()` rather than a separate system.
- After the dealer hands over weed, the player owes him cash for it (not prepaid).
- If the player doesn't pay up, the dealer takes him at gunpoint to an ATM (or similar) to force a
  withdrawal. Intended to be simple to build: a forced-walk/cutscene-style sequence, not a combat
  system — fits the "tense and restrained" gun rule above.

## Setting

Small, dense map — the area around Tegnérlunden park in Stockholm (apartment + park + a few
anchor buildings), not an open/large map. Deliberately staying small because:
- It's buildable without terrain/hills systems.
- It matches the tactile, "know every inch of this place" feeling the game is going for, the same
  way My Summer Car's world isn't big, it's just deeply interactive.

Countryside/open-world was considered and explicitly rejected for the main setting — it fits a
different game (isolation/survival) and drags in scope (driving distances, terrain, navmesh over
hills) that isn't worth it right now. Could revisit later if the story ever becomes a road-trip/
escape arc, but that's not the plan.

## Design philosophy

Steal My Summer Car's actual trick: it's not that the map is big, it's that ordinary actions are
broken into fiddly, tactile steps (twist the lid, grind the weed, roll the paper) instead of a
single "use item" button. Keep building systems this way rather than abstracting them into menus.

## Asset pipeline (important constraint)

No Blender. Workflow is Sketchfab → Mixamo → Unity:
1. Download a model from Sketchfab (rigged or not).
2. Run it through Mixamo.com to auto-rig and grab animations for free (idle, sit, walk, talk,
   fidget, etc.).
3. Import the FBX into Unity, set the rig to Humanoid.

This is the only animation pipeline available — any NPC/character plan needs to work within it.

## What's built so far

**Player systems**
- First-person movement/look (`PlayerMovement`, `MouseLook`), with camera sway driven by
  drunk/high state.
- `PlayerInteraction`: raycast-based pick up / hold / drop / throw, with different hold poses for
  general items vs. consumables (beer arm vs. joint arm), scroll-to-rotate held items.
- `PlayerStats`: money, hunger, thirst, craving (weed withdrawal), highLevel (weed), drunkLevel
  (alcohol) all ticking over time. Munchies mechanic (being high triples hunger rate). Passing out
  from being too drunk/high/thirsty triggers a full dizzy → fall → puke → blackout → fade → sleep →
  wake-up-hungry sequence.

**Drug economy loop**
- `DealerAI` + `DealerClickable` + `DrugSite`: player orders from a web portal, a dealer NPC
  walks to the apartment door (NavMeshAgent), knocks, hands off a jar, player pays. Jar model is
  toggled active/inactive on the dealer's hand-bone per delivery (currently has an unresolved bug
  where the jar doesn't reappear on the second delivery — likely an Inspector wiring issue, not a
  code issue, see git history / ask before re-debugging).
- `JointCraftingStation`: full multi-step minigame — remove jar lid, tilt jar to get a bud, put bud
  in grinder, close/grind (mouse-scroll minigame)/open grinder, spawn rolling paper, pour ground
  weed, roll the joint (hold-and-drag-up minigame). Bud count persists via `JointCraftingStation.
  globalBudCount` (static, survives scene changes). Jar goes visibly empty and gets swapped for a
  refill from the dealer once buds run out.
- `ConsumableItem` + consume flow in `PlayerInteraction`: drinking a beer or smoking a joint plays
  an arm-raise animation, triggers sounds, and calls into `PlayerStats` (`SmokeJoint`/`DrinkBeer`).

**University / exam system**
- `WebBrowser` + `UniversityPortal`: an in-game laptop browser simulates a real university portal
  (login, dashboard, pick a course, timed exam). The "exam" is a memorize-and-retype-this-Java-code
  minigame under a 4-minute timer, graded via Levenshtein-distance accuracy into a letter grade
  (A–U), which pays out simulated CSN (Swedish student aid) money to `PlayerStats.money`. This is
  the current implementation of "academic pressure" — not a physical lecture yet.

**Apartment interactables**
- `Bed` (sleep), `FridgeDoor`, `SinkFaucet`, `LightSwitch`, `RoomDoor`, `LaptopStation` /
  `ComputerStation` / `PCLook` (laptop/PC use), `CraftingClickable` / `StationClickable` (generic
  click-to-interact wiring), `CollisionSound`.

## What's planned / being designed

- **Physical lecture system** (currently only exists as the web-portal exam minigame). Plan: don't
  simulate real AI students, stage it — fixed seat transforms filled with a small pool of Mixamo-
  animated NPCs (sit/idle/fidget), a teacher NPC reusing the `DealerAI` NavMeshAgent-waypoint-patrol
  pattern, dialogue delivered as timed subtitle lines (no voice acting) rather than real speech.
  Wrap it in a reusable `LectureManager` (start → seat NPCs → run patrol + subtitle timers for N
  seconds → end) so future lectures are just new line arrays, not new hand-built scenes. This is
  where the player first meets the dealer, story-wise.
- **Escalation arc**: deeper into the drug business → acquiring a gun → going rogue. Needs actual
  design work — triggers, pacing, what "rogue" means mechanically. Not started.
- More environment/scene work around Tegnérlunden once the above systems exist to populate it.

## Open questions (fill in as they get decided)

- Can the rogue path ever be walked back, or is it one-way once triggered?
- What does "going rogue" actually change mechanically (new stats, new locations, NPC reactions)?
- How many lecture "days" happen before the story hands control over to the sandbox?
- Final title.

## Working with this doc

This file should be kept up to date as we build — when a new system lands or a design decision
gets made, update the relevant section instead of letting this go stale. When adding new planned
features, ask clarifying questions before writing them in rather than assuming.
