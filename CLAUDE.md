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
- **Neighbor complaint/escalation system** (`NeighborAI` + `NeighborClickable`, implemented but not
  fully wired up yet — see wiring status below): copies the `DealerAI`/`DeliveryDriverAI`
  NavMeshAgent-walk-to-door/stair-climb/knock/footstep pattern rather than new locomotion code.
  No persistent "heat" meter — an in-memory 3-strike counter (`NeighborAI.jointsSmoked`) tied to
  joints smoked this session, driven by `NeighborAI.OnJointSmoked()`, which `PlayerStats.SmokeJoint()`
  calls through a new `PlayerStats.neighbor` reference field:
  1. 1st joint: walks up, knocks, stands outside and yells (`strike1Lines`, `AngryTrigger` anim).
  2. 2nd joint: yells (`strike2LinesFirst`), retreats partway toward his own place (`retreatPoint`),
     turns around, comes back, and yells again — more escalated this time (`strike2LinesSecond`,
     `AngryPointTrigger` anim, the pointing/finger-jabbing variant).
  3. 3rd+ joint: drops whatever he was doing and **runs** into the apartment, chasing the player's
     live position (not a fixed stop point) rather than barging in once and stopping.
  - Click-to-dismiss (`Interact()`, same pattern as dapping up the dealer) only works while he's
    actively yelling (strikes 1 & 2) — once he's charging (strike 3+) it's a no-op, by design.
  - **How the charge resolves (decided and implemented)**: rather than an "instant blackout" timed
    to the exact moment of the 4th joint, the charge is a genuine race. While charging, `NeighborAI`
    checks every frame whether an `attackPoint` (his hand bone) is within `attackRadius` (default
    1.2) of the player; if so, he's caught them and calls the new `PlayerStats.KnockedOut()`.
    Separately, the player's own 4-joints-to-black-out threshold (see PlayerStats above) can also
    fire first if the player smokes fast enough. Both paths reuse the exact same
    `PassOutSequence()` — `KnockedOut()` is a placeholder reuse for now; a distinct "decked by the
    neighbor" beat can be designed later. This supersedes the earlier "he never actually reaches
    you" idea — he genuinely can catch the player now if they dawdle on the 4th joint.
  - **No baseball bat for the neighbor** — decided against it, to keep him reading as a grounded
    "pissed-off neighbor" rather than an armed home invasion. Instead he has a bare-handed moveset:
    `Idle`/`Walk`/`Run` (speed-blended, `NeighborAnimator.controller`) plus one-shot `Angry`/
    `AngryPoint` (wired to the yelling beats above) and `StepForward`/`StepBack`/`JabCross`/`Hook`
    (added to the controller as available states, but **nothing calls them yet** — the actual
    punch-combo timing during the charge hasn't been designed).
  - Player-side baseball bat (found later, used to knock the neighbor out) is still the planned
    counter to this — not started. Needs its own design pass (how it's found, prompt/QTE/raycast,
    what happens after).
  - No hiding-spot mechanic for this pass (apartment doesn't have any yet — revisit once it does).
  - Yelling delivered as timed subtitle lines (same no-voice-acting pattern as `LectureManager`
    above), placeholder/nonsense voice barks layered in later.
  - **Blocked on a day/sleep/world-time system that doesn't exist yet**: `jointsSmoked` is a plain
    in-memory int with nothing to reset against (sleeping? a day/night cycle? a real-time cooldown?)
    — deliberately left as a "problem for later" rather than building time-tracking now.
  - **Visual asset**: reused the delivery driver's rigged model, duplicated directly into the scene
    as a `Neighbour` GameObject (not yet packaged as its own prefab asset). `DeliveryDriver` stays
    exactly where it is under `FoodSituation`, still running invisible at runtime as before —
    unaffected. A dedicated `NeighbourSituation` scene container (sibling to
    `FoodSituation`/`Dealer_Situation`) is still to be created and the Neighbour moved into it.
  - **Scene wiring status**: `Neighbour` has `NavMeshAgent`, a body-sized `CapsuleCollider`,
    `Animator` (assigned to `NeighborAnimator.controller`), two `AudioSource`s, `NeighborAI`, and
    `NeighborClickable` — all added, with the two audio sources and the clickable→AI link wired.
    `PlayerStats.neighbor` → `NeighborAI` is wired (this was a live bug: it sat unwired for a
    while, so smoking joints silently no-op'd instead of triggering him — fixed). **Still needs to
    be dragged in by hand**: `stopPoint` (required — currently null and will throw a
    NullReferenceException the moment he tries to approach), `retreatPoint`, `playerStats`,
    `attackPoint`, the four stair-waypoint transforms, `knockSound`/`footstepSound`,
    `dialogueSubtitle`, and the three line arrays (`strike1Lines`/`strike2LinesFirst`/
    `strike2LinesSecond`).
- More environment/scene work around Tegnérlunden once the above systems exist to populate it.

## Open questions (fill in as they get decided)

- Can the rogue path ever be walked back, or is it one-way once triggered?
- What does "going rogue" actually change mechanically (new stats, new locations, NPC reactions)?
- How many lecture "days" happen before the story hands control over to the sandbox?
- What does the neighbor's strike counter reset against once a day/sleep/world-time system exists?
- What happens after the neighbor's charge resolves — whether he catches the player (`KnockedOut()`)
  or the player blacks out first from the 4th joint — with him now standing in the apartment: does
  he leave once the player's passed out, wait, trigger something on wake-up?
- Punch-combo timing: when exactly during the charge should `JabCross`/`Hook`/`StepForward`/
  `StepBack` actually fire? Not designed yet — states exist in `NeighborAnimator.controller` but
  nothing triggers them.
- Baseball bat knockout: how is it found, how does the knock-out interaction work (prompt/QTE/
  raycast), what happens to the strike counter/neighbor afterward?
- Final title.

## Working with this doc

This file should be kept up to date as we build — when a new system lands or a design decision
gets made, update the relevant section instead of letting this go stale. When adding new planned
features, ask clarifying questions before writing them in rather than assuming.
