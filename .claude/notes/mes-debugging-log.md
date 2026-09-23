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
