Ran into this too and traced it (MES 2.74.02). The subtraction itself is fine; the problem is how the result is stored and read back.

`WaypointProfile.cs` line 103 builds a world-space offset:

```csharp
var offset = GetRandomCoords(entity.PositionComp.WorldAABB.Center) - entity.PositionComp.WorldAABB.Center;
return new EncounterWaypoint(offset, entity);
```

That constructor makes it a `RelativeOffset` waypoint, and `EncounterWaypoint.GetCoords()` (line 144) reads it back with:

```csharp
LastValidWaypoint = Vector3D.Transform(Offset, Entity.WorldMatrix);
```

`Transform` treats `Offset` as a local offset, so the world-space vector gets rotated by the entity's current orientation before being added to its position. On a planet the "up" part of the offset usually ends up pointing sideways or down, which is why the waypoints land underground.

Also, because it's re-evaluated on every `GetCoords()`, the point moves with the entity. With `[RelativeEntity:Self]` it stays ahead of the grid and can never be reached. So in the current code `EntityRandom` is the fixed one and `RelativeRandom` is the one that tracks, the opposite of how the docs describe them.

A possible fix, if `RelativeRandom` is meant to keep following the entity, is to convert the offset into the entity's local space before storing it:

```csharp
var worldOffset = GetRandomCoords(entity.PositionComp.WorldAABB.Center) - entity.PositionComp.WorldAABB.Center;
var localOffset = Vector3D.TransformNormal(worldOffset, MatrixD.Transpose(entity.WorldMatrix));
return new EncounterWaypoint(localOffset, entity);
```

If it's meant to be a fixed point like the docs say, it can just do what `EntityRandom` does.

Workaround for now: use `EntityRandom` with `[RelativeEntity:Self]`.
