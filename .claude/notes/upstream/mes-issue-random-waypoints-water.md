# Title
Random waypoint altitude ignores the Water Mod (measured from the seabed)

# Body

`WaypointProfile.GetRandomCoords` (MES 2.74.02, used by `StaticRandom`, `EntityRandom` and `RelativeRandom`) measures altitude from the voxel surface only:

```csharp
// WaypointProfile.cs lines 126 and 132
var surfaceAtCoords = planet.GetClosestSurfacePointGlobal(coords);
...
var roughCoords = planet.GetClosestSurfacePointGlobal(randForwardDist * randForward + coords);
```

With the Water Mod loaded, `[MinAltitude:]`/`[MaxAltitude:]` on these waypoints end up measured from the seabed, while everything in the autopilot uses the water-aware `PlanetEntity.SurfaceCoordsAtPosition`. Over water deeper than the waypoint altitude, the waypoint is underwater.

Planetary pathing hides most of this: `CalculateSafePlanetPathWaypoint` (`AutoPilotSystem.cs` ~line 1883) lifts the pending waypoint, so the grid stays above the water and looks like it's flying to the point fine. But the arrival check uses the raw waypoint. In `CargoShip.cs`, `GetDistanceToWaypoint` (line 306) measures to `State.InitialWaypoint` and compares against `hypot(WaypointTolerance, WaypointTolerance)` (line 290).

A plane held at `MinimumPlanetAltitude` above the water never gets within tolerance of a point that's hundreds of meters below that. It never "reaches" the waypoint, so `BehaviorTriggerA`/`D` never fire and a waypoint-driven route stalls.

Example: `EntityRandom` waypoint with `[MinAltitude:500]`/`[MaxAltitude:700]`, autopilot `MinimumPlanetAltitude:500`, `WaypointTolerance:100`, over 400m of water. The raw waypoint sits 100-300m above the water, the plane can't go below 500m, and the arrival radius is about 141m, so it never arrives.

**Proposed fix**

Use the same water-aware surface lookup as the autopilot. `GetRandomCoords` already has the vanilla planet; it could grab the `PlanetEntity` and call `SurfaceCoordsAtPosition` instead:

```csharp
// needs: using ModularEncountersSystems.Entities;
var planetEntity = PlanetManager.GetNearestPlanet(coords);
...
var surfaceAtCoords = planetEntity?.SurfaceCoordsAtPosition(coords) ?? planet.GetClosestSurfacePointGlobal(coords);
...
var roughCoords = planetEntity?.SurfaceCoordsAtPosition(randForwardDist * randForward + coords)
                  ?? planet.GetClosestSurfacePointGlobal(randForwardDist * randForward + coords);
```

`SurfaceCoordsAtPosition` already returns whichever of terrain or water is higher, and falls back to terrain when there's no water, so nothing changes on dry land or without the Water Mod.

Workaround for now: raise `[WaypointTolerance:]` on the autopilot (e.g. 600 gives an arrival radius of ~850m), or give random waypoints enough altitude to clear the deepest water.
