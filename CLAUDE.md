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
  - **Scene wiring status: fully wired and working.** `Neighbour` (now under its own
    `NeighbourSituation` container, sibling to `Dealer_Situation`/`FoodSituation`) has
    `NavMeshAgent`, a body-sized `CapsuleCollider`, `Animator`, two `AudioSource`s, `NeighborAI`,
    and `NeighborClickable`. Every `NeighborAI` field is wired: `stopPoint` (reuses `Dealer_StopPos`),
    `retreatPoint`, `playerStats` (→ `Apartment_Floor_3/Player/Main Camera`, where `PlayerStats`
    actually lives), `attackPoint` (→ his `mixamorig:RightHand` bone), the four stair-waypoint
    transforms (reuse the Dealer's stair markers), `knockSound`/`footstepSound`, `dialogueSubtitle`
    (→ the shared `DealerSubtitleCanvas`), and all three dialogue line arrays have placeholder text
    in them. `PlayerStats.neighbor` → `NeighborAI` is wired.
  - **Code fixes landed in `NeighborAI.cs` this session** (on top of the original implementation):
    coroutines now activate the GameObject before `StartCoroutine` (was throwing "coroutine
    couldn't start, GameObject inactive" every time, silently eating all 3 strikes); added a
    `Waiting` state so he stands at the door doing nothing until you click him — only then does he
    start yelling (previously auto-played on arrival with no player input needed, click was
    dismiss-only); fixed a bug where smoking a joint while he was still mid-walk from the previous
    strike silently dropped the escalation entirely (a stale `CurrentState != Idle` guard) — now
    every joint always interrupts whatever he's doing and jumps straight to the correct strike;
    replaced the old "turn 90° relative to whatever heading he arrived with" facing logic
    (unreliable, arrival heading isn't consistent) with `FaceRotationRoutine` that rotates to
    `stopPoint`'s own absolute rotation instead; `Update()` now forces the animator's `Speed` to his
    target speed (not literal `agent.velocity.magnitude`) while in any actively-moving state, to
    stop NavMeshAgent's `autoBraking` deceleration from flickering him into Idle mid-slide.
  - **Animator status**: `NeighborAnimator.controller` was fully rebuilt this session. Root cause of
    "no animation at all" was that his rig uses `mixamorig:` bone names but every downloaded clip
    (Walking/Running/Angry/Angry Point/Short Step Forward/Step Backward/Hook/Jab Cross, all from
    `Assets/Animations/`) used `mixamorig7:` — a Mixamo namespace-collision artifact from a later
    download batch — so nothing could bind under the old Generic setup. Fix: deleted the old
    controller, started from a copy of `DealerAnimator.controller` (Idle+Walk only, proven-working),
    then converted `DeliveryDriver.fbx` (his actual model source) plus `Walking.fbx`, `Running.fbx`,
    `Angry Point.fbx`, and `Yelling.fbx` to **Humanoid** with independently auto-generated avatars
    each — Humanoid retargeting maps by skeleton role, not literal bone names, which sidesteps the
    `mixamorig` vs `mixamorig7` mismatch entirely. Current states: `Idle` (loops the Walking clip at
    normal speed — an empty-motion Idle state visibly sinks him into the floor under Humanoid mode,
    learned that one the hard way), `Walk`/`Run` (Speed-blended), `Yelling` (`AngryTrigger` — strike
    1 and strike 2's first yell) and `AngryPoint` (`AngryPointTrigger` — strike 2's second, more
    escalated yell), both Any-State transitions tightened to a 0.05s blend so cutting in mid-walk-
    cycle doesn't read as a spasm. `Angry.fbx`, `Short Step Forward.fbx`, `Step Backward.fbx`,
    `Hook.fbx`, `Jab Cross.fbx` are still Generic/unconverted and unused in the controller — same
    namespace-mismatch treatment would be needed if/when the punch-combo states get built.
  - **Idle clip and stair-climb clip landed — both wired in.** Cesar supplied `Idle.fbx` and
    `StairsUp.fbx` (`Assets/Animations/`), converted to Humanoid with their own auto-generated
    avatars, same treatment as the other clips. `NeighborAnimator.controller`'s `Idle` state now
    plays the real `Idle.fbx` clip instead of the `Walking` clip standing still — the walk-in-idle
    look is gone, and no floor-sink regression. Stair climbing got a dedicated `ClimbStairs` state
    (motion = `StairsUp.fbx`) reached via a new `Climbing` bool parameter — an Any-State transition
    into it when `Climbing` is true, back to `Idle` when false, both 0.05s blends matching the
    existing Yelling/AngryPoint transitions. `ClimbStairs()` in `NeighborAI.cs` now sets
    `animator.SetBool("Climbing", true)` right when it takes over from the `NavMeshAgent` and back
    to `false` once the last waypoint is reached, so the manual root-slide up the stairs plays the
    stairs clip instead of flat `Walk`. The existing `Speed`-based `SetFloat` calls during the climb
    were left in place (harmless, no longer visually relevant since `ClimbStairs` isn't a Speed-
    blended state) rather than ripped out, to keep the diff small. Worth eyeballing in Play mode to
    confirm the clip's stride actually matches `stairMoveSpeed` (1.5) — if it looks too fast/slow,
    that's a clip-speed tweak on the `ClimbStairs` state, not a script change.
  - **Bug found and fixed right after landing the above: "left leg stuck in the air" during the
    climb.** Root cause was neither the avatar nor the clip's actual keyframe data — confirmed by
    sampling `StairsUp.fbx`'s clip directly on the model in the editor (`UnityEditor.AnimationMode`
    + `screenshot-isolated`) at several points in the cycle: every sampled frame was a completely
    normal mid-stride climbing pose, both legs animating fine. The real cause: **`Loop Time` was off
    on both new clips' import settings** (`ModelImporterClipAnimation.loopTime`, defaults to false on
    a fresh FBX import). `StairsUp.fbx`'s clip is only 1.2s — one step cycle, meant to be looped —
    but with looping off it plays once and then freezes on its final frame for the rest of the climb
    (which takes several seconds at `stairMoveSpeed`), and that final frame happens to be mid-lift on
    the left leg. Fixed by setting `loopTime = true` and `loopPose = true` on both `StairsUp.fbx` and
    `Idle.fbx` (the latter had the identical setting off — not yet visibly complained about, but same
    latent bug, fixed pre-emptively) and reimporting. **Lesson for next Mixamo clip added to this
    project**: always check/set Loop Time on import for anything meant to repeat (idles, walk/run
    cycles, stair climbs) — `ModelImporterAnimationType`/`avatarSetup` getting attention doesn't mean
    looping did. Also confirmed **don't use `CopyFromOther` avatar setup** across clips from
    different Mixamo download batches — `StairsUp.fbx`/`Idle.fbx` use `mixamorig7:` bone names (the
    same namespace-collision artifact as the original Walking/Running batch) while the working
    shared-avatar candidate (`DeliveryDriverAvatar`, from `DeliveryDriver.fbx`) uses plain
    `mixamorig:`, so pointing one at the other throws a hard Rig Error ("Transform hierarchy does not
    match") and produces a broken avatar. `CreateFromThisModel` (independent auto-generated avatar
    per clip, as already established for every clip on this character) remains the only working
    approach until/unless every future Mixamo download is confirmed to share one bone-name
    convention.
  - **NavMesh coverage gap — Door (2) fixed and kept; stairs experiment tried and reverted.**
    Root cause of the original coverage complaints: `Dealer_Situation`'s `NavMeshSurface.Size`/
    `Center` bake volume was undersized — it only covered roughly the ground floor (y up to 7.35,
    z up to 8.9), so the entire upper hallway, the stairwell, and all of `Apartment_Floor_3` were
    outside the box that ever got scanned, no matter how many times it was rebaked. **Kept**: the
    volume is now resized to `size=(25,20,34) center=(-6.6,5,4.5)` (ground floor through the
    apartment ceiling with margin) with `minRegionArea` dropped from 2 to 0.25, and the hallway↔
    apartment threshold gap at **Door (2)** is bridged with a `NavMeshLink` (`Door2_NavMeshLink`,
    under `Dealer_Situation`) — confirmed working, `NavMesh.CalculatePath` across it returns
    `PathComplete`.
    **Reverted**: a full session was spent trying to give the stairs real NavMesh coverage (an
    invisible `StairNavMesh` ramp/landing/link rig, `NavMeshModifier.ignoreFromBuild` on the real
    staircase model's 13 renderers) so `NeighborAI`/`DealerAI` could use plain `NavMeshAgent`
    movement instead of the manual waypoint slide. It technically got `NavMesh.CalculatePath` to
    report `PathComplete` end-to-end, but the in-game result looked worse than the original
    floating — Cesar's call, reverted in full: `StairNavMesh` and everything under it deleted, all
    the `NavMeshModifier` exclusions on the staircase model removed. The stairs are back to having
    **zero real NavMesh coverage**, exactly like before this experiment, and `ClimbStairs()` in
    `NeighborAI.cs` (manually sliding the root position between `bottomStairPos`/
    `midStairDownPos`/`midStairPos`/`topStairPos` with the flat Walk clip playing) is the only
    thing that gets an NPC up or down them — this is intentional now, not a bug to fix. Any future
    attempt at real stair NavMesh should be treated as a fresh experiment, not a resumption of this
    one; nothing about it survived.
  - **`NeighbourSpawnPos` height — false alarm, reverted.** A previous revision of this doc claimed
    `NeighbourSpawnPos` (`y≈7.23`, ground floor) was a bug and "fixed" it by moving it to `y≈10.02`
    (apartment-floor height). That was wrong and has been reverted — `NeighbourSpawnPos` and the
    live `Neighbour` instance are back to their original ground-floor position
    (`(-7.57, 7.23, 11.78)` / `(-7.24, 7.47, 11.95)`). What actually happened: a `Physics.Raycast`
    straight down from above his position hit something at `y=10` and that was assumed to be "the
    real floor," without checking which way the hit surface was facing. It was almost certainly the
    **underside of the apartment floor above him** (a ceiling, not a floor) — he was legitimately
    standing in the ground-floor space below it the whole time. The tell that should have caught
    this immediately: this entire session's work was building and fixing a **stair-climbing
    animation system** for him — that only makes sense if he spawns on the ground floor and climbs
    up to reach the apartment door, which is exactly what the pre-existing `ClimbStairs()` /
    `bottomStairPos`→`topStairPos` waypoint sequence in `NeighborAI.cs` already does. Moving his
    spawn to apartment-floor height put him inside/on top of the floor slab he's supposed to climb
    up to, which is why he then appeared to "float" onto the player's floor when a joint-smoking
    strike triggered his approach — he was already there instead of climbing. **Lesson: when a
    raycast is used to sanity-check a height, check `hit.normal.y` (positive = floor, negative =
    ceiling) before trusting the hit as "the ground," and cross-check any conclusion about where an
    NPC is "supposed" to be against what the surrounding code/animation work already implies about
    the intended flow — a whole session spent on stair-climbing animations is strong evidence he's
    meant to use the stairs, not skip them.**
  - **Still open, not fixed this session: he clips partway into the stairs while climbing** (separate
    from the spawn-height false alarm above — this is real and was already present before today).
    `ClimbStairs()` moves his root in straight lines between the four waypoints
    (`bottomStairPos`→`midStairDownPos`→`midStairPos`→`topStairPos`), but the real stair mesh rises
    in actual discrete steps, not a smooth diagonal — so at points along each straight segment his
    tracked height falls slightly below the real tread surface directly under him, and he visibly
    sinks partway into the stairs before rising back out on the next segment. This was always true
    of the manual-slide approach; giving him a proper climbing animation this session likely made it
    *more* noticeable (a stepping animation implies his feet should land on treads, so a mismatch
    reads worse than it did with the old flat `Walk` clip). Real NavMesh coverage for the stairs was
    tried and fully reverted earlier this session at Cesar's explicit call ("stairs suck," see
    above) — so fixing this properly means either another NavMesh attempt (not requested, previous
    one didn't land well) or adding more intermediate waypoints along the real stair geometry so the
    straight-line segments hug the actual treads more closely.
    **Tried and reverted: lowering `stairMoveSpeed` from `1.5` to `0.8`.** Reasoning at the time:
    `ClimbStairs()`'s root slide runs at a constant speed completely decoupled from the `StairsUp`
    clip's own footstep timing (no root motion, no per-step sync of any kind), so a speed mismatch
    could plausibly read as floating. Cesar tested it in Play mode and it made things worse, not
    better — reverted back to `1.5` (both the script default and the live value on the `Neighbour`
    GameObject's `NeighborAI` component). **Important process note for next time**: this MCP/editor
    connection has no working Play-mode control or live-gameplay observation from Claude's side —
    every change here was verified only by static analysis (reading code, checking Animator Controller
    data, sampling clips via `AnimationMode` in Edit mode), never by actually watching him climb.
    Don't present a speed/timing tweak like this as a confident fix; frame it as an untested
    hypothesis and wait for Cesar's in-game confirmation before treating it as done. The underlying
    clipping-into-stairs issue described above is still unfixed and still needs either more
    waypoints or a NavMesh attempt — Cesar's call on which.
    **Then fully reverted: the whole `Climbing`/`ClimbStairs` animator hookup from the entry above.**
    After the speed tweak made things look worse, Cesar asked to go back further than just the speed
    — back to "when he was just moving from the transform positions," i.e. before the dedicated
    stairs-climbing animation existed at all. Done: `ClimbStairs()` in `NeighborAI.cs` no longer
    calls `animator.SetBool("Climbing", ...)` at all (both call sites removed). During the climb he's
    now back to exactly the pre-session behavior — pure position slide between the four waypoints,
    with only the `Speed` float driving the ordinary `Idle`/`Walk` blend, same as `DealerAI`. The
    `ClimbStairs` state, the `Climbing` bool parameter, and the `StairsUp.fbx` asset itself are all
    still sitting in `NeighborAnimator.controller` / `Assets/Animations/` — nothing deleted — but
    they're dead/unreachable now since nothing sets `Climbing` to true. Harmless to leave (same as
    the unused `Angry.fbx`/`Hook.fbx`/etc. clips already sitting there); safe to delete later if
    Cesar wants the hierarchy cleaner, and just as safe to resume if a future attempt actually solves
    the underlying position/animation sync problem. **Do not re-add the `Climbing` hookup without
    being asked** — this was tried twice this session (the stairs-clip integration itself, then the
    speed tweak on top of it) and made things worse both times per Cesar's own in-game testing.
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
