# WW2-WitM Territory Point System — Design

Detailed derivations and locked-in values for the per-anchor territory growth/shrink system outlined in `WW2-WitM-Roadmap.md`'s "Expansion 1 - Territory" section. The roadmap tracks build status; this document holds the math and the reasoning behind it so the roadmap doesn't have to.

## Reference implementation

Pattern is adapted from GVK Deserts of Kharak (Workshop 2653133286), `Data/Encounters/Alliance/`:

- One `CustomSandboxCounter` drives tier state.
- Each tier is a Timer-type RivalAI/MES AI Trigger pair (Enable/Disable) with `GreaterOrEqual`/`Less` conditions on the counter, linked via `ToggleWithTriggerProfile` so only one side is armed at a time.
- The enable/disable Action does `ChangeZoneAtPosition` + `ZoneToggleActiveMode`. GVK also does `ZoneRadiusChangeType:Set` on a single reused zone per tier; our project instead has all 6 radii pre-built as separate `[MES Zone]` entities per anchor (`Territory-Zones.sbc`), so the action only needs to toggle `Active`, no radius-set required.
- Points are awarded/deducted via Manual-type triggers tagged onto each encounter type's compromise/despawn/delivery event, using `IncreaseSandboxCounters`/`DecreaseSandboxCounters` actions. Losses transfer symmetrically — a deduction from one faction's counter is paired with an equal increase to the rival's, at the same anchor.

## Zone tiers (already built)

6 tiers per anchor, radii 6/10/15/21/28/36 km (`Territory-Zones.sbc`). Tier 1 is always `Active:true` — it's the anchor's free baseline, no points required.

| Tier | Radius | Area (km²) |
|---|---|---|
| 1 | 6 km | 113.10 |
| 2 | 10 km | 314.16 |
| 3 | 15 km | 706.86 |
| 4 | 21 km | 1,385.44 |
| 5 | 28 km | 2,463.01 |
| 6 | 36 km | 4,071.50 |

## Counter scope

One counter per anchor (not per anchor-per-faction — each anchor already belongs to one faction, per Roadmap's Primary Locations list): `LaSpezia_Points`, `Rome_Points`, `Tripoli_Points` (Gray); `Toulon_Points`, `Oran_Points`, `Alexandria_Points` (Green). 6 counters total. Every relevant event (kill, loss, delivery) is checked against *every* anchor's own rings independently and nudges that anchor's counter by however much the event's position falls within that anchor's ring bands — per the ring-weight rule already defined in the Roadmap (§Expansion 1, ring 1 = 4x, rings 2–3 = 3x, rings 4–5 = 2x, beyond ring 5 = 1x flat).

## Tier transition cost — LOCKED

**Formula:** `cost(N→N+1) = 0.8 × 90 × [Area(tier N+1) / Area(tier 1)]` = `72 × [Area(N+1)/Area(1)]`

Every transition's cost is anchored to a fixed reference — tier 1's own area — so cost scales with how much bigger the *target* tier is than the anchor's starting zone, not with the immediately preceding tier (that alternative made costs shrink each tier, which inverted the intended difficulty curve — see conversation history if this ever needs re-deriving).

| Transition | Area(N+1)/Area(1) | Marginal cost | Cumulative threshold |
|---|---|---|---|
| 1→2 | 2.778 | **200** | 200 |
| 2→3 | 6.25 | **450** | 650 |
| 3→4 | 12.25 | **882** | 1,532 |
| 4→5 | 21.778 | **1,568** | 3,100 |
| 5→6 | 36.0 | **2,592** | 5,692 |

- **Floor:** 0 (counter resets to 0 on underflow rather than going negative — matches GVK's `LessOrEqual -1 → Reset` pattern).
- **Ceiling:** ~6,850 (roughly 20% headroom above the tier-6 threshold, same margin GVK uses above its own top gate).
- The `CustomSandboxCountersTargets` values used in the actual RivalAI/MES AI conditions are the **cumulative threshold** column (200 / 650 / 1,532 / 3,100 / 5,692), not the marginal-cost column — marginal cost is only useful for reasoning about how much a single tier jump "costs" relative to the others.

## Growth events (points gained)

Base values reused from the Roadmap's own War Activity table (`WW2-WitM-Roadmap.md` §Expansion 2) so there's one consistent value scale project-wide, not two:

| Grid type | Base value | Grid type | Base value |
|---|---|---|---|
| Carrier | 50 | Utility Naval (Golo) | 10 |
| Battleship | 40 | Corvette | 5 |
| Heavy Cruiser | 25 | Utility Air (Ju52/F.222) | 5 |
| Cruiser | 20 | Attacker | 4 |
| Destroyer | 10 | Fighter | 3 |
| | | Bomber | 5 |

Kill an opposing combat grid → `+base × ring-multiplier` to the affected anchor(s)' counter(s).

**Logistics (separate from combat value, per the requested 6:1 ship:plane ratio):**
- Successful cargo plane delivery: **+10**
- Successful cargo ship delivery: **+60**

Delivery always resolves at the anchor itself, so it lands in ring 1 (4x) at the destination anchor automatically under the same per-anchor distance check — no special-casing needed.

## Shrink events (points lost)

Symmetric to growth: losing a friendly grid → `−base × ring-multiplier` to your own anchor counter, mirrored as `+` to the rival faction's nearest/relevant anchor (zero-sum transfer, matching GVK's paired `Decrease`/`Increase` actions). A cargo grid destroyed en route is the symmetric loss of its delivery value (−10 plane / −60 ship).

## Known build prerequisites (not yet in the .sbc files)

- **Cargo ships have no "arrived" trigger yet.** They currently just run out of `CustomWaypoints` and despawn implicitly (`Paths-Nautical.sbc` / `Triggers-CargoSpawn-Port.sbc`). Needs a final-waypoint-reached trigger mirroring the cargo plane's existing `Destination-Reached → FinalDespawn` pattern (`Triggers-Destination_Plane.sbc`) before the +60 delivery award can hook to anything.
- **Cargo plane "shot down before arrival" trigger is unconfirmed.** Cargo ships already have this via `WW2-Trigger-CargoDamage` (Type:Damage) and `WW2-Trigger-ReputationDamageHeavy-Ship-Gray/Green` (Type:Compromised). Need to verify or build the plane equivalent before the −10 loss can be wired.

## Explicitly out of scope for now

- Ground vehicle kills don't feed these counters — the territory system currently only covers the 6 naval/coastal anchors. Ground vehicle activity near vehicle depots/garrisons is Expansion 3+ and would need its own tie-in once those installations exist.
- Submarines aren't in the base-value table — not yet wired into spawn groups per the current roster survey; add a value (~15, between Corvette and Destroyer) once they're implemented.
