using System;
using System.Collections.Generic;
using Sandbox.Game;
using Sandbox.ModAPI;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRage.Utils;
using VRageMath;

namespace WW2WitM.RespawnWheels
{
    // Keeps the respawn trucks' wheels on. Water Mod (WaterConfig.xml RespawnPodConfig) moves a respawn ship to dry land
    // with IMyCubeGrid.Teleport, which shifts the whole grid group but keeps its rotation, and then sets WorldMatrix on
    // the main grid only. The suspension wheels are subgrids, so they stay at the old rotation and tear off.
    // At RespawnShipSpawned the wheels are not attached yet (in game 2026-11-02 the mechanical group held only the main
    // grid), so Teleport leaves them behind too. Here: when a WitM respawn truck spawns, note every small loose grid
    // inside its box and where it sits relative to the main grid; the first sudden rotation of the main grid (Water
    // Mod's WorldMatrix set) puts them back there, in the same call, before physics steps, so the suspensions attach.
    // Server only (respawn ships spawn there).
    [MySessionComponentDescriptor(MyUpdateOrder.AfterSimulation)]
    public class RespawnWheelsSession : MySessionComponentBase
    {
        const string PrefabPrefix = "Respawn-WW2-";
        const string LogPrefix = "WW2-WitM respawn wheels: ";
        const int WatchTicks = 600;          // stop watching 10 s after the spawn
        const double TeleportDot = 0.996;    // a rotation over ~5 degrees between two position updates is the teleport
        const int MaxPartBlocks = 20;        // wheel and suspension-top grids; anything bigger is not a truck part
        const double BoxMargin = 3;          // m around the main grid's box

        class Part
        {
            public IMyCubeGrid Grid;
            public MatrixD Rel;              // part world = Rel * main world
        }

        class Watch
        {
            public IMyCubeGrid Grid;
            public MatrixD Last;
            public int Ticks;
            public readonly List<Part> Parts = new List<Part>();
        }

        readonly List<Watch> watches = new List<Watch>();
        readonly HashSet<IMyEntity> entities = new HashSet<IMyEntity>();
        readonly List<IMySlimBlock> blocks = new List<IMySlimBlock>();
        bool registered;

        public override void BeforeStart()
        {
            if (!MyAPIGateway.Session.IsServer)
                return;
            MyVisualScriptLogicProvider.RespawnShipSpawned += RespawnShipSpawned;
            registered = true;
        }

        protected override void UnloadData()
        {
            if (registered)
                MyVisualScriptLogicProvider.RespawnShipSpawned -= RespawnShipSpawned;
            foreach (Watch w in watches)
                w.Grid.PositionComp.OnPositionChanged -= PositionChanged;
            watches.Clear();
        }

        void RespawnShipSpawned(long shipEntityId, long playerId, string respawnShipPrefabName)
        {
            try
            {
                if (respawnShipPrefabName == null || !respawnShipPrefabName.StartsWith(PrefabPrefix))
                    return;
                IMyCubeGrid grid = MyAPIGateway.Entities.GetEntityById(shipEntityId) as IMyCubeGrid;
                if (grid == null)
                    return;
                Watch w = new Watch { Grid = grid, Last = grid.WorldMatrix };
                FindParts(w);
                watches.Add(w);
                grid.PositionComp.OnPositionChanged += PositionChanged;
            }
            catch (Exception e)
            {
                MyLog.Default.WriteLineAndConsole(LogPrefix + "error: " + e);
            }
        }

        void PositionChanged(VRage.Game.Components.MyPositionComponentBase pos)
        {
            Watch w = Find(pos.Entity);
            if (w == null)
                return;
            try
            {
                MatrixD now = w.Grid.WorldMatrix;
                if (Vector3D.Dot(now.Up, w.Last.Up) < TeleportDot || Vector3D.Dot(now.Forward, w.Last.Forward) < TeleportDot)
                {
                    Reseat(w, now);
                    Stop(w);
                    return;
                }
                w.Last = now;
            }
            catch (Exception e)
            {
                MyLog.Default.WriteLineAndConsole(LogPrefix + "error: " + e);
                Stop(w);
            }
        }

        // Small non-static grids inside the main grid's box at spawn: the prefab's wheels, still unattached.
        void FindParts(Watch w)
        {
            BoundingBoxD box = w.Grid.WorldAABB.Inflate(BoxMargin);
            MatrixD invMain = MatrixD.Invert(w.Grid.WorldMatrix);
            entities.Clear();
            MyAPIGateway.Entities.GetEntities(entities, e => e is IMyCubeGrid && e != w.Grid && box.Contains(e.WorldAABB.Center) != ContainmentType.Disjoint);
            foreach (IMyEntity e in entities)
            {
                IMyCubeGrid part = (IMyCubeGrid)e;
                if (part.IsStatic || part.MarkedForClose)
                    continue;
                blocks.Clear();
                part.GetBlocks(blocks);
                if (blocks.Count > MaxPartBlocks)
                    continue;
                w.Parts.Add(new Part { Grid = part, Rel = part.WorldMatrix * invMain });
            }
            entities.Clear();
            blocks.Clear();
        }

        // Every part goes back to its spawn transform relative to the main grid: part = Rel * new main.
        void Reseat(Watch w, MatrixD after)
        {
            int n = 0;
            foreach (Part p in w.Parts)
            {
                if (p.Grid.MarkedForClose)
                    continue;
                p.Grid.WorldMatrix = p.Rel * after;
                if (p.Grid.Physics != null)
                {
                    p.Grid.Physics.LinearVelocity = Vector3.Zero;
                    p.Grid.Physics.AngularVelocity = Vector3.Zero;
                }
                n++;
            }
            MyLog.Default.WriteLineAndConsole(LogPrefix + w.Grid.DisplayName + " moved by the respawn teleport, " + n + " wheel grids re-seated");
        }

        public override void UpdateAfterSimulation()
        {
            for (int i = watches.Count - 1; i >= 0; i--)
            {
                Watch w = watches[i];
                if (++w.Ticks >= WatchTicks || w.Grid.MarkedForClose)
                    Stop(w);
            }
        }

        Watch Find(IMyEntity entity)
        {
            foreach (Watch w in watches)
                if (w.Grid == entity)
                    return w;
            return null;
        }

        void Stop(Watch w)
        {
            w.Grid.PositionComp.OnPositionChanged -= PositionChanged;
            watches.Remove(w);
        }
    }
}
