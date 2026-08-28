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
  wake-up-hungry sequence. Weed pass-out now takes 4 joints instead of 3 (first 3 build up the high
  without maxing it out) — changed deliberately to leave room for the neighbor's 3-strike escalation
  to fully play out before the player can black out, see Neighbor system below.

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
  Food items (see below) reuse `ConsumableItem` with `useArmAnimation = false`: held like a general
  item (floats in front, scroll to rotate) instead of raised to an arm, eaten instantly on
  left-click via `PlayerStats.EatFood(hungerRestore, thirstDelta)`.

**Food delivery loop**
- `DeliveryDriverAI` + `DeliveryDriverClickable` + `FoodSite`: mirrors the drug delivery loop above
  (same NavMeshAgent-waypoint-and-stairs pattern, same knock/wait/handoff/leave state machine, now
  also driven by the same `AnimatorController` as `DealerAI` so both NPCs actually animate). Player
  orders from an in-game web portal (`snabbmat.se`); clicking the driver at the door spawns a
  physical `Bag.prefab` pickup (tag `Interactable`, Rigidbody + Collider) and hides the driver's
  visual hand-bag mesh.
- `FoodBagContents`: snapshot of exactly what was ordered (prefab + quantity per item), captured
  from the cart at handoff time before it resets, so each bag remembers its real contents instead
  of spawning a fixed set.
- Opening the bag: while holding it, left-click plays a quick squash effect then scatter-spawns its
  actual contents (Chicken Nuggets, Noodles, Chips, Beer) near the player and destroys the bag.
  Each food item restores hunger on eat; Chips also *raises* thirst (salty) via a negative
  `thirstRestore` value — sign convention documented on `ConsumableItem.thirstRestore`.

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
- **Neighbor complaint/escalation system** (`NeighborAI`, being imported now): reuses the
  `DealerAI`/`DeliveryDriverAI` NavMeshAgent-walk-to-door pattern rather than new locomotion code.
  Deliberately no hidden "heat" meter — just a 3-strike counter tied to joints smoked:
  1. 1st joint: he walks up, knocks, stands outside the door and yells.
  2. 2nd joint: yells longer, walks back toward his own apartment, then turns around and comes
     back to yell some more.
  3. 3rd joint: barges through the door regardless of whether the player opens it, running rather
     than walking. What happens next (knocked out, police called, etc.) is intentionally a stub
     for now — decide later.
  - **4th joint (pass-out joint — see below): automatic "final rush" beat, not a 4th strike.**
    The counter stays at 3. As soon as `highLevel` crosses the pass-out threshold, the neighbor
    (already having barged in on strike 3) makes one more running charge at the player through the
    door — but the *instant* `PlayerStats.PassOutSequence()` begins (the dizzy stumble kicking in,
    not the ~10s-later fade-to-black) already counts as "blacked out," so he never actually reaches
    the player. It's a near-miss/jump-scare beat, not a resolved confrontation — deliberately left
    unresolved until the player has a way to fight back (see baseball bat, below).
  - No hiding-spot mechanic for this pass (apartment doesn't have any yet — revisit once it does).
  - Yelling delivered as timed subtitle lines (same no-voice-acting pattern as `LectureManager`
    above), with placeholder/nonsense voice barks layered in later. Same treatment eventually
    planned for the dealer and delivery driver too, not recorded yet.
  - **Blocked on a day/sleep/world-time system that doesn't exist yet**: the strike counter needs
    something to reset against (sleeping? a day/night cycle? a real-time cooldown?), otherwise one
    early joint session permanently burns all 3 strikes for the rest of the playthrough. Not
    building full time-tracking yet since the game isn't fully playable end-to-end — flagged here
    so the reset logic isn't forgotten once that system exists.
  - **Running animation needed**: the shared Animator (`DealerAnimator`, also used by the delivery
    driver) only has `Idle`/`Walk` states, no `Run`. Strikes 3 and the joint-4 final rush both need
    him running, so a real Run clip has to come from Mixamo (same pipeline as everything else,
    see Asset pipeline above) and get wired into the controller as a proper speed-driven state —
    not faked by just cranking NavMeshAgent speed on the walk cycle.
  - **Planned later, not started**: a baseball bat the player can find and use to knock the
    neighbor out once he's inside. Needs its own design pass (how it's found, whether it's a
    prompt/QTE or a raycast hit, what happens to the strike counter afterward) — not happening
    until the barge-in/final-rush beats above are actually built and feel right.
  - **Visual asset plan**: the delivery driver's rigged character model is being reused as the
    neighbor's visual identity — it'll be duplicated out into its own independent prefab
    (`Assets/Prefab/Characters/Neighbour.prefab`, not a linked instance of `DeliveryDriver.prefab`)
    so edits to one never risk touching the other. The original `DeliveryDriver` GameObject stays
    exactly where it is under `FoodSituation` (that hierarchy isn't going away) and keeps working
    as before — it's already set up to run invisible at runtime (`hideModel` on `DeliveryDriverAI`,
    since there's nothing left to click once he's not shown), so the food-delivery loop is
    unaffected. The new Neighbour NPC gets its own scene container, `NeighbourSituation`, sibling
    to `FoodSituation`/`Dealer_Situation`.
- More environment/scene work around Tegnérlunden once the above systems exist to populate it.

## Open questions (fill in as they get decided)

- Can the rogue path ever be walked back, or is it one-way once triggered?
- What does "going rogue" actually change mechanically (new stats, new locations, NPC reactions)?
- How many lecture "days" happen before the story hands control over to the sandbox?
- What does the neighbor's strike counter reset against once a day/sleep/world-time system exists?
- What actually happens on the neighbor's 3rd-strike barge-in once he's inside — does he stay put
  until the joint-4 final rush, leave and come back, something else?
- What happens after the joint-4 final rush resolves (player is now passed out with the neighbor
  in the apartment) — does he leave once the player's asleep, wait, trigger something on wake-up?
- Baseball bat knockout: how is it found, how does the knock-out interaction work (prompt/QTE/
  raycast), what happens to the strike counter/neighbor afterward?
- Final title.

## Working with this doc

This file should be kept up to date as we build — when a new system lands or a design decision
gets made, update the relevant section instead of letting this go stale. When adding new planned
features, ask clarifying questions before writing them in rather than assuming.
