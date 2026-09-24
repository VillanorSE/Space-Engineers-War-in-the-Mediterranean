# Title
docs: waypoint profiles, CargoShip waypoint queue, command reply timing, trigger timers, NPC faction credits

# Body

Findings from building procedural cargo routes (wandering planes that request a destination from other installations). All verified against MES 2.74.02 source.

**`references/behaviors_and_autopilot.md`**
- §5.C Command delivery:
  - Commands are delivered synchronously.
  - In one action, `BroadcastCommandProfiles` runs before `EnableTriggers`, so a reply can arrive before its listener is enabled.
  - `AllowUniqueCommandCodeSenderOnly` records the sender even while the trigger is disabled (a trap, and a usable trick).
  - `ReturnToSender`, and `{FactionTag}` in `[CommandCode:]`.
- §7 Waypoint profiles & the CargoShip waypoint queue:
  - What each `[Waypoint:]` type actually stores.
  - `RelativeRandom` world/local offset bug (links MES #321).
  - Random waypoints ignore the Water Mod (seabed-relative), while pathing is water-aware and the arrival check uses the raw point.
  - BehaviorTrigger A/B/C/D meanings for CargoShip, including D firing right after spawn.
  - `WaypointWaitTimeTrigger` 5s default with autopilot mode `None`, which is bad for aircraft.
- §8 Trigger timers:
  - Cooldowns re-roll on every fire.
  - `CooldownTime`/`LastTriggerTime` persist in the RC storage across save/load, so long timers survive short play sessions.

**`references/economy_and_stores.md`**
- §4 `ChangeNpcFactionCredits`:
  - Reads `ChangePlayerCreditsAmount`; `ChangeNpcFactionCreditsAmount` is ignored.
  - Debits never fail: the `amount > credits` check is always false for a negative amount, in all three credit blocks, so `PaymentFailure` never fires on a charge.

**`SKILL.md`**
- Two short pitfall bullets pointing at the above. SKILL.md is ~4,730 estimated tokens after this (was ~4,640).

Docs only, no script or tag-cache changes (PATCH under VERSIONING.md).
