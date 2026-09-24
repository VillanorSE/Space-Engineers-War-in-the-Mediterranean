# MES debugging log

Reference notes for Claude across sessions. Not for mod comments — keep .sbc/.xml files
clean, short, and functional. Log dated findings here instead.

## 2026-09-22 — Nautical cargo ships

- `IgnoreCleanupRules` is a **SpawnGroup**-level tag (`ImprovedSpawnGroup.cs`), parsed from the
  `[Modular Encounters SpawnGroup]` block. It is NOT read by `SpawnConditionsProfile.cs` — putting
  it in a `[MES Spawn Conditions]` block is a silent no-op. It sets `Attributes.IgnoreCleanup`,
  which exempts the grid from `Cleaning.BasicCleanupChecks` (the generic distance/timer cleanup
  sweep in `Watchers/Cleaning.cs`). Live copy: `SpawnGroups-CargoShips-Nautical.sbc`, on each
  SpawnGroup itself.
- `CargoShip.cs` autopilot: `MaxSpeed = MaxSpeedOverride != -1 ? MaxSpeedOverride : IdealMaxSpeed`.
  The only branch that commands active forward thrust is `velocity < MaxSpeed - MaxSpeedTolerance`.
  `WW2-Autopilot-CargoShip` set `IdealMaxSpeed:10` but never set `MaxSpeedTolerance`, leaving it at
  the class default of 15 — making the accelerate check `< -5`, never true. Ships never got a real
  accelerate command; the "Drifting At Desired Speed" debug label is misleading with a tolerance
  that wide. Fix: `[MaxSpeedTolerance:1]`.
- `UseVelocityCollisionEvasion` (in the same autopilot profile) generates a temporary detour
  waypoint whenever `Collision.VelocityResult.CollisionImminent()` trips within
  `CollisionEvasionWaypointDistance` (100m) — independent of the separate `CollisionAvoidance` flag.
  Disabled for `WW2-Autopilot-CargoShip`: suspected (not log-confirmed) false positives against
  seafloor/water voxels for a surface ship at ordinary depths. Disabling this alone did NOT fix the
  slow-speed bug above — that was the separate `MaxSpeedTolerance` issue.
- `[CustomWaypoints:...]` on a `CargoShip`-subclass Behavior is parsed with
  `Vector3D.TryParse` (verified empirically via PowerShell + the real `VRage.Math.dll`). That parser
  wants **space-separated** X/Y/Z per point (`"1 2 3"`), NOT colon-separated (`"1:2:3"`) — the
  colon form silently fails and leaves the waypoint list empty. Comma still separates points in the
  list. `Paths-Nautical.sbc` now uses the space-separated form.
- Golo hull spawn Y offset: `13.75` (was `0.0`), assuming a 5m draft on a hull spanning y=0-14
  (37.5m). SE places planetary-installation prefabs by bounding-box center, not origin.

## 2026-09-23 — Air Cargo planes (airfield -> airfield)

Files: `Behaviors-AirCargo.sbc`, `Triggers/CargoPlane/Triggers-AirCargo.sbc`,
`Triggers/Installations/Triggers-Airfield-AirCargo.sbc`, SpawnGroups `WW2-AirCargo-Gray/Green`,
`WW2-Territory-Action-Delivery-AirCargo-*` in Territory-Points.sbc. All source-verified, not yet tested in game.

- Random wander: `[RivalAI Waypoint]` `[Waypoint:EntityRandom]` + `[RelativeEntity:Self]` makes a *static*
  point around the plane's current position at the time it's added. `RelativeRandom` stores an offset
  relative to the entity, so it moves with the plane and is never reached — don't use it for legs.
- CargoShip fires BehaviorTriggerD every time the waypoint list goes non-empty -> empty (including right
  after spawn with no CustomWaypoints). Used as the "add next leg" hook. Before the action lands the
  ship briefly heads to its auto despawn coords; harmless (far away).
- `WaypointWaitTimeTrigger` default is 5s; during the wait CargoShip runs autopilot mode None — bad for
  a plane. Set to 0 on the AirCargo autopilots.
- Origin exclusion: `TriggerSystem.ProcessCommandReceiveTriggerWatcher` records sender+code for any
  trigger with `[AllowUniqueCommandCodeSenderOnly:true]` BEFORE checking `UseTrigger`. So a reply that
  arrives while the trigger is disabled still blacklists that sender forever. The plane pings at 5s
  (3km radius) with DestinationAssigned disabled -> only origin replies -> origin blacklisted; trigger
  is armed at 30s.
- Command send/receive is synchronous (DelayTicks 0), and in ActionSystem BroadcastCommandProfiles runs
  before EnableTriggers. Enabling the receive trigger in the same action that sends the request would
  blacklist the real replier — hence the separate Arm timer.
- `ReturnToSender:true` on a reply command forces SingleRecipient to the requesting RC.
- `{FactionTag}` in `[CommandCode:]` is replaced with the sender RC owner's faction tag (PrepareCommand).
- `ChangeNpcFactionCredits` reads `ChangePlayerCreditsAmount`, NOT `ChangeNpcFactionCreditsAmount`
  (ActionSystem.cs ~1041). Don't also set `ChangePlayerCredits:true` or the player gets paid too.
- Altitudes (autopilot + all waypoints) are 500-700m. Autopilot altitude is water-surface based, but
  WaypointProfile.GetRandomCoords is seabed-relative (vanilla GetClosestSurfacePointGlobal). Pathing
  keeps the plane above water; CargoShip arrival checks the raw point, so WaypointTolerance is raised
  (cruise 600, loiter 300). MES issues filed 2026-09-23 (drafts in .claude/notes/upstream/).
- Nearest-anchor scoring: MES has no distance-to-coords condition, but CheckTrueSandboxBooleans replaces
  {SpawnGroupName} (ConditionProfile.cs). Airfields set static flags WW2-Airfield-X_Near{Faction}_{Anchor}
  once; 6 universal CommandReceived triggers check {SpawnGroupName}_Near{Faction}_{Anchor}. Nearest
  computed by angle between airfield StaticEncounterPlanetDirection and zone centers (planet at origin,
  r~30.1km). Close calls: Gibraltar Gray Tripoli 117.0 vs LaSpezia 118.7 deg; Bizerte Gray Rome 50.8 vs
  Tripoli 51.6 deg. SetSandboxCounter does NOT resolve {SpawnGroupName}/custom strings (only {Faction}).
- Ships now score +100 at the destination anchor only.

## 2026-09-23 — Trigger persistence (source-verified)

- Timer cooldown is re-rolled from Min/MaxCooldownMs after EVERY fire (TriggerSystem.cs ~1093), not
  once per load (InitRandomTimes only seeds the first one).
- The whole TriggerProfile list is serialized into the Remote Control's Storage (StoredSettings) and on
  reload REPLACES the list built from the .sbc (CoreBehavior.cs ~1240). Saved per trigger: Type,
  UseTrigger, Min/MaxCooldownMs, StartsReady, MaxActions, CooldownTime, LastTriggerTime (game time, so
  timers carry across saves), TriggerCount, and which action/condition SubtypeIds it uses.
- Action/Condition/Spawn/Command/Chat CONTENTS are looked up by SubtypeId at runtime, so edits to those
  apply to existing grids. Edits to trigger fields, or triggers added to/removed from a Behavior, only
  apply to grids spawned after the change.

## 2026-09-23 — Civitavecchia cargo "Could Not Generate Path" / Tripoli cargo plane missing

- Cargo ship: trigger fired (StartsReady ok) but CustomSpawn failed "SpawnGroup Path/Placement Invalid".
  OtherNPC placement (PathPlacements.CalculateOtherPath) only checks: spawn point inside a NON-planet
  voxel AABB (unless SkipVoxelSpawnChecks / spawner IgnoreSafetyChecks), and grid distance < 0 (never).
  Player was standing ~200m from the spawn point; same spawner succeeded 2026-09-22 with player far away.
  Hypothesis (unverified): planet MyVoxelPhysics sections near the player are not MyPlanet, so their AABB
  fails the check. Fix: [SkipVoxelSpawnChecks:true] on WW2-SpawnConditions-CargoShips-Nautical;
  [IgnoreSafetyChecks:true] on convoy raid ship spawners (raids always spawn near a player).
- Cargo plane: Tripoli airfield never spawned - player 42.5km away, airfield TriggerRadius 20km. Only
  Foggia (16.6km) loaded; it is outside every 5km territory zone so it stays UNOWNED and its AirCargo
  triggers stay disabled (by design).
