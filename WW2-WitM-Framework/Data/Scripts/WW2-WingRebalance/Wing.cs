using System;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using VRage.Game;
using VRage.Game.Components;
using VRage.ModAPI;
using VRage.ObjectBuilders;
using VRage.Utils;
using VRageMath;

// See AerodynamicsMod.cs for the full rationale. Subtype coverage AND size-tier grouping match
// CAPTurbonerd's "Aerodynamics Override" (Workshop 3480334050) exactly - one shared forceMul per
// nominal size, applied uniformly across every wing from Consty's Wings, Randa's Thin Wings/Plane
// Parts & Control Surfaces, and Plane Parts, same as Turbonerd's own switch statement.
//
// Only the VALUES differ. Each tier below is set to HALF of Turbonerd's percentage reduction from
// that tier's original value. Reference for "original" is Consty's Wings for every size Consty
// actually has (1x3 through 5x5 - Consty is the mod actually used on your existing WW2 planes),
// falling back to Plane Parts' aero-wing values for 6x5/7x5, which don't exist in Consty at all.
// Randa's and Plane Parts' OWN originals for the overlapping sizes were considerably higher than
// Consty's (e.g. Randa's 5x5 = 1.0, Plane Parts 5x5-tier = 1.75, vs. Consty's 0.45) - since this
// is one shared tier now, not a separate one per source mod, we can only anchor to one baseline,
// and Consty is it.
//
//   Tier      Consty original -> Turbonerd -> ours (reduction halved)
//   1x3/3x1   0.05  -> 0.075 (+50%)   -> 0.0625 (+25%)   [only case that's an INCREASE - halved the same way]
//   2x3/3x2   0.25  -> 0.1   (-60%)   -> 0.175  (-30%)
//   3x3       0.3   -> 0.125 (-58.3%) -> 0.2125 (-29.2%)
//   5x1/1x5   0.25  -> 0.15  (-40%)   -> 0.20   (-20%)
//   5x2/2x5   0.3   -> 0.165 (-45%)   -> 0.2325 (-22.5%)
//   5x3/3x5   0.35  -> 0.175 (-50%)   -> 0.2625 (-25%)
//   5x4/4x5   0.4   -> 0.25  (-37.5%) -> 0.325  (-18.75%)
//   5x5       0.45  -> 0.25  (-44.4%) -> 0.35   (-22.2%)
//   6x5 (*)   1.75  -> 0.275 (-84.3%) -> 1.0125 (-42.1%)   (* Plane Parts baseline - no Consty 6x5)
//   7x5 (*)   2.0   -> 0.3   (-85%)   -> 1.15   (-42.5%)   (* Plane Parts baseline - no Consty 7x5)
//   CAPOMissileTubeWing/Fin   0.2 -> 0.1 (-50%) -> 0.15 (-25%)
//
//   Lift onset velocity, ALL families: speedSq >= 1 (~0.71 m/s), copied directly from Turbonerd
//   (not halved - the user asked to adopt this change as-is).
//
//   Atmosphere gate (0.4-0.7 air density) and the x10 large-grid multiplier are unchanged from
//   every source mod - Turbonerd didn't touch either of those, so neither do we.
namespace WW2WitM.Aerodynamics
{
    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_TerminalBlock), false,

        "SlimWing1x3", "SlimWing2x3", "SlimWing3x3", "SlimWing3x2", "SlimWing3x1",
        "SlimWing1x3Offset", "SlimWing2x3Offset", "SlimWing2x3OffsetMirror", "SlimWing3x2Offset",
        "SlimWing3x2OffsetMirror", "SlimWing3x3Offset", "SlimWing3x3OffsetMirror", "SlimWing3x1Offset",
        "SlimWing3x1OffsetMirror",

        "SlimWing5x1", "SlimWing5x2", "SlimWing5x3", "SlimWing5x4", "SlimWing5x5", "SlimWing4x5",
        "SlimWing3x5", "SlimWing2x5", "SlimWing1x5",
        "SlimWing5x1Offset", "SlimWing5x2Offset", "SlimWing5x3Offset", "SlimWing5x4Offset",
        "SlimWing5x5Offset", "SlimWing4x5Offset", "SlimWing3x5Offset", "SlimWing2x5Offset", "SlimWing1x5Offset",
        "SlimWing5x2OffsetMirror", "SlimWing5x3OffsetMirror", "SlimWing5x4OffsetMirror", "SlimWing5x5OffsetMirror",
        "SlimWing4x5OffsetMirror", "SlimWing3x5OffsetMirror", "SlimWing2x5OffsetMirror", "SlimWing5x1OffsetMirror",

        "SlimWingRound1x3", "SlimWingRound2x3", "SlimWingRound3x3", "SlimWingRound3x2", "SlimWingRound3x1",
        "SlimWingRound1x3Offset", "SlimWingRound2x3Offset", "SlimWingRound2x3OffsetMirror",
        "SlimWingRound3x2Offset", "SlimWingRound3x2OffsetMirror", "SlimWingRound3x3Offset",
        "SlimWingRound3x3OffsetMirror", "SlimWingRound3x1Offset", "SlimWingRound3x1OffsetMirror",

        "SlimWingRound5x1", "SlimWingRound5x2", "SlimWingRound5x3", "SlimWingRound5x4", "SlimWingRound5x5",
        "SlimWingRound4x5", "SlimWingRound3x5", "SlimWingRound2x5", "SlimWingRound1x5",
        "SlimWingRound5x1Offset", "SlimWingRound5x2Offset", "SlimWingRound5x3Offset", "SlimWingRound5x4Offset",
        "SlimWingRound5x5Offset", "SlimWingRound4x5Offset", "SlimWingRound3x5Offset", "SlimWingRound2x5Offset",
        "SlimWingRound1x5Offset",
        "SlimWingRound5x2OffsetMirror", "SlimWingRound5x3OffsetMirror", "SlimWingRound5x4OffsetMirror",
        "SlimWingRound5x5OffsetMirror", "SlimWingRound4x5OffsetMirror", "SlimWingRound3x5OffsetMirror",
        "SlimWingRound2x5OffsetMirror", "SlimWingRound5x1OffsetMirror",

        "CAPOMissileTubeWing", "CAPOMissileStabilizerFin",

        "3x2Canard", "3x3Canard",

        "PanelWing3x4", "PanelWing3x4Reverse", "PanelWing3x4Centre",

        "PanelWing1x5", "PanelWing2x5", "PanelWing2x5Reverse", "PanelWing3x5", "PanelWing3x5Reverse",
        "PanelWing4x5", "PanelWing4x5Reverse", "PanelWing5x5", "PanelWing5x5Reverse", "PanelWing5x4",
        "PanelWing5x4Reverse", "PanelWing5x3", "PanelWing5x3Reverse", "PanelWing5x2", "PanelWing5x2Reverse",
        "PanelWing5x1", "PanelWing5x1Reverse",

        "PanelWing1x5Centre", "PanelWing2x5Centre", "PanelWing3x5Centre", "PanelWing4x5Centre",
        "PanelWing5x5Centre", "PanelWing5x4Centre", "PanelWing5x3Centre", "PanelWing5x2Centre",
        "PanelWing5x1Centre",

        "PanelWing1x3", "PanelWing2x3", "PanelWing2x3Reverse", "PanelWing3x3", "PanelWing3x3Reverse",
        "PanelWing3x2", "PanelWing3x2Reverse", "PanelWing3x1", "PanelWing3x1Reverse",

        "PanelWing1x3Centre", "PanelWing2x3Centre", "PanelWing3x3Centre", "PanelWing3x2Centre",
        "PanelWing3x1Centre",

        "aero-wing_1x5x1_rounded_edge_Small", "aero-wing_2x5x1_rounded_edge_Small",
        "aero-wing_3x5x1_rounded_edge_Small", "aero-wing_4x5x1_rounded_edge_Small",
        "aero-wing_5x5x1_rounded_edge_Small", "aero-wing_6x5x1_rounded_edge_Small",
        "aero-wing_5x3x1_rounded_edge_Small", "aero-wing_5x2x1_rounded_edge_Small",
        "aero-wing_5x1x1_rounded_edge_Small",

        "aero-wing_1x5x1_rounded_edge_Large", "aero-wing_2x5x1_rounded_edge_Large",
        "aero-wing_3x5x1_rounded_edge_Large", "aero-wing_4x5x1_rounded_edge_Large",
        "aero-wing_5x5x1_rounded_edge_Large", "aero-wing_6x5x1_rounded_edge_Large",
        "aero-wing_5x3x1_rounded_edge_Large", "aero-wing_5x2x1_rounded_edge_Large",
        "aero-wing_5x1x1_rounded_edge_Large",

        "aero-wing_2x5x1_pointed_edge_Small", "aero-wing_3x5x1_pointed_edge_Small",
        "aero-wing_4x5x1_pointed_edge_Small", "aero-wing_5x5x1_pointed_edge_Small",
        "aero-wing_6x5x1_pointed_edge_Small", "aero-wing_7x5x1_pointed_edge_Small",
        "aero-wing_2x1x1_pointed_edge_Small",

        "aero-wing_2x5x1_pointed_edge_Large", "aero-wing_3x5x1_pointed_edge_Large",
        "aero-wing_4x5x1_pointed_edge_Large", "aero-wing_5x5x1_pointed_edge_Large",
        "aero-wing_6x5x1_pointed_edge_Large", "aero-wing_7x5x1_pointed_edge_Large",
        "aero-wing_2x1x1_pointed_edge_Large"
        )]
    public class Wing : MyGameLogicComponent
    {
        private IMyTerminalBlock block;
        private float atmosphere = 0;
        private int atmospheres = 0;

        // -- Lift onset: copied directly from Turbonerd's override, not halved. --
        private const int minSpeed = 1; // speedSq >= 1  =>  real velocity >= ~0.71 m/s

        // -- Lift magnitude: one shared value per nominal size (Turbonerd's own grouping), --
        // -- each set to half of Turbonerd's reduction from Consty's original (see header). --
        private const double LGmultiplier = 10;

        private const double wing_1x3 = 0.0625;
        private const double wing_2x3 = 0.175;
        private const double wing_3x3 = 0.2125;
        private const double wing_5x1 = 0.20;
        private const double wing_5x2 = 0.2325;
        private const double wing_5x3 = 0.2625;
        private const double wing_5x4 = 0.325;
        private const double wing_5x5 = 0.35;
        private const double wing_6x5 = 1.0125;
        private const double wing_7x5 = 1.15;
        private const double capoMissileWing = 0.15;

        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            block = (IMyTerminalBlock)Entity;
            NeedsUpdate = MyEntityUpdateEnum.BEFORE_NEXT_FRAME;
        }

        public override void UpdateOnceBeforeFrame()
        {
            try
            {
                if(block.CubeGrid?.Physics == null)
                    return;

                NeedsUpdate = MyEntityUpdateEnum.EACH_FRAME | MyEntityUpdateEnum.EACH_10TH_FRAME;
            }
            catch(Exception e)
            {
                Log.Error(e, "UpdateOnceBeforeFrame");
            }
        }

        public override void UpdateAfterSimulation()
        {
            try
            {
                var grid = block.CubeGrid;

                if(grid.Physics == null || grid.Physics.IsStatic || block.MarkedForClose || block.Closed || !block.IsWorking)
                    return;

                if(atmospheres == 0 || atmosphere <= float.Epsilon)
                    return;

                var blockMatrix = block.WorldMatrix;
                var vel = grid.Physics.GetVelocityAtPoint(blockMatrix.Translation);
                double speedSq = MathHelper.Clamp(vel.LengthSquared() * 2, 0, 10000);

                if(speedSq < minSpeed)
                    return;

                Vector3D fw = blockMatrix.Left;
                double forceMul = 0.15;

                switch(block.BlockDefinition.SubtypeId)
                {
                    case "SlimWing1x3":
                    case "SlimWing3x1":
                    case "SlimWing1x3Offset":
                    case "SlimWing3x1Offset":
                    case "SlimWing3x1OffsetMirror":
                    case "SlimWingRound1x3":
                    case "SlimWingRound3x1":
                    case "SlimWingRound1x3Offset":
                    case "SlimWingRound3x1Offset":
                    case "SlimWingRound3x1OffsetMirror":
                        forceMul = wing_1x3;
                        fw = Vector3D.Normalize(blockMatrix.Left + blockMatrix.Forward * 0.15);
                        break;

                    case "CAPOMissileTubeWing":
                    case "CAPOMissileStabilizerFin":
                        forceMul = capoMissileWing;
                        fw = Vector3D.Normalize(blockMatrix.Left + blockMatrix.Forward * 0.15);
                        break;

                    case "SlimWing2x3":
                    case "SlimWing2x3Offset":
                    case "SlimWing2x3OffsetMirror":
                    case "SlimWing3x2":
                    case "SlimWing3x2Offset":
                    case "SlimWing3x2OffsetMirror":
                    case "SlimWingRound2x3":
                    case "SlimWingRound2x3Offset":
                    case "SlimWingRound2x3OffsetMirror":
                    case "SlimWingRound3x2":
                    case "SlimWingRound3x2Offset":
                    case "SlimWingRound3x2OffsetMirror":
                        forceMul = wing_2x3;
                        fw = Vector3D.Normalize(blockMatrix.Left + blockMatrix.Forward * 0.15);
                        break;

                    case "SlimWing3x3":
                    case "SlimWing3x3Offset":
                    case "SlimWingRound3x3":
                    case "SlimWingRound3x3Offset":
                        forceMul = wing_3x3;
                        fw = Vector3D.Normalize(blockMatrix.Left + blockMatrix.Forward * 0.15);
                        break;

                    case "SlimWing5x1":
                    case "SlimWing1x5":
                    case "SlimWing5x1Offset":
                    case "SlimWing1x5Offset":
                    case "SlimWing5x1OffsetMirror":
                    case "SlimWingRound5x1":
                    case "SlimWingRound1x5":
                    case "SlimWingRound5x1Offset":
                    case "SlimWingRound1x5Offset":
                    case "SlimWingRound5x1OffsetMirror":

                    case "PanelWing1x5":
                    case "PanelWing5x1":
                    case "PanelWing5x1Reverse":
                    case "PanelWing1x5Centre":
                    case "PanelWing5x1Centre":
                        forceMul = wing_5x1;
                        fw = Vector3D.Normalize(blockMatrix.Left + blockMatrix.Forward * 0.15);
                        break;

                    case "SlimWing5x2":
                    case "SlimWing2x5":
                    case "SlimWing5x2Offset":
                    case "SlimWing2x5Offset":
                    case "SlimWing5x2OffsetMirror":
                    case "SlimWing2x5OffsetMirror":
                    case "SlimWingRound5x2":
                    case "SlimWingRound2x5":
                    case "SlimWingRound5x2Offset":
                    case "SlimWingRound2x5Offset":
                    case "SlimWingRound5x2OffsetMirror":
                    case "SlimWingRound2x5OffsetMirror":

                    case "3x3Canard":
                    case "3x2Canard":
                    case "PanelWing2x5":
                    case "PanelWing2x5Reverse":
                    case "PanelWing5x2":
                    case "PanelWing5x2Reverse":
                    case "PanelWing2x5Centre":
                    case "PanelWing5x2Centre":
                    case "PanelWing3x4":
                    case "PanelWing3x4Reverse":
                    case "PanelWing3x4Centre":

                    case "aero-wing_2x5x1_rounded_edge_Small":
                    case "aero-wing_5x2x1_rounded_edge_Small":
                        forceMul = wing_5x2;
                        fw = Vector3D.Normalize(blockMatrix.Left + blockMatrix.Forward * 0.15);
                        break;

                    case "SlimWing5x3":
                    case "SlimWing3x5":
                    case "SlimWing5x3Offset":
                    case "SlimWing3x5Offset":
                    case "SlimWing5x3OffsetMirror":
                    case "SlimWing3x5OffsetMirror":
                    case "SlimWingRound5x3":
                    case "SlimWingRound3x5":
                    case "SlimWingRound5x3Offset":
                    case "SlimWingRound3x5Offset":
                    case "SlimWingRound5x3OffsetMirror":
                    case "SlimWingRound3x5OffsetMirror":

                    case "PanelWing3x5":
                    case "PanelWing3x5Reverse":
                    case "PanelWing5x3":
                    case "PanelWing5x3Reverse":
                    case "PanelWing3x5Centre":
                    case "PanelWing5x3Centre":

                    case "aero-wing_3x5x1_rounded_edge_Small":
                    case "aero-wing_5x3x1_rounded_edge_Small":
                    case "aero-wing_3x5x1_pointed_edge_Small":
                    case "aero-wing_4x5x1_pointed_edge_Small":
                        forceMul = wing_5x3;
                        fw = Vector3D.Normalize(blockMatrix.Left + blockMatrix.Forward * 0.25);
                        break;

                    case "SlimWing5x4":
                    case "SlimWing4x5":
                    case "SlimWing5x4Offset":
                    case "SlimWing4x5Offset":
                    case "SlimWing5x4OffsetMirror":
                    case "SlimWing4x5OffsetMirror":
                    case "SlimWingRound5x4":
                    case "SlimWingRound4x5":
                    case "SlimWingRound5x4Offset":
                    case "SlimWingRound4x5Offset":
                    case "SlimWingRound5x4OffsetMirror":
                    case "SlimWingRound4x5OffsetMirror":

                    case "PanelWing4x5":
                    case "PanelWing4x5Reverse":
                    case "PanelWing5x4":
                    case "PanelWing5x4Reverse":
                    case "PanelWing4x5Centre":
                    case "PanelWing5x4Centre":

                    case "aero-wing_4x5x1_rounded_edge_Small":
                    case "aero-wing_5x5x1_pointed_edge_Small":
                        forceMul = wing_5x4;
                        fw = Vector3D.Normalize(blockMatrix.Left + blockMatrix.Forward * 0.35);
                        break;

                    case "SlimWing5x5":
                    case "SlimWing5x5Offset":
                    case "SlimWing5x5OffsetMirror":
                    case "SlimWingRound5x5":
                    case "SlimWingRound5x5Offset":
                    case "SlimWingRound5x5OffsetMirror":

                    case "PanelWing5x5":
                    case "PanelWing5x5Reverse":
                    case "PanelWing5x5Centre":
                        forceMul = wing_5x5;
                        fw = Vector3D.Normalize(blockMatrix.Left + blockMatrix.Forward * 0.15);
                        break;

                    case "aero-wing_5x5x1_rounded_edge_Small":
                    case "aero-wing_6x5x1_pointed_edge_Small":
                        forceMul = wing_6x5;
                        fw = Vector3D.Normalize(blockMatrix.Left + blockMatrix.Forward * 0.45);
                        break;

                    case "aero-wing_6x5x1_rounded_edge_Small":
                    case "aero-wing_7x5x1_pointed_edge_Small":
                        forceMul = wing_7x5;
                        fw = Vector3D.Normalize(blockMatrix.Left + blockMatrix.Forward * 0.55);
                        break;

                    // ----------------------------

                    case "PanelWing1x3Centre":
                    case "PanelWing1x3":
                    case "PanelWing3x1":
                    case "PanelWing3x1Reverse":
                    case "PanelWing3x1Centre":
                        forceMul = wing_1x3;
                        fw = Vector3D.Normalize(blockMatrix.Left + blockMatrix.Forward * 0.15);
                        break;

                    case "PanelWing2x3":
                    case "PanelWing2x3Reverse":
                    case "PanelWing2x3Centre":
                    case "PanelWing3x2":
                    case "PanelWing3x2Reverse":
                    case "PanelWing3x2Centre":
                        forceMul = wing_2x3;
                        fw = Vector3D.Normalize(blockMatrix.Left + blockMatrix.Forward * 0.15);
                        break;

                    case "PanelWing3x3":
                    case "PanelWing3x3Reverse":
                    case "PanelWing3x3Centre":
                        forceMul = wing_3x3;
                        fw = Vector3D.Normalize(blockMatrix.Left + blockMatrix.Forward * 0.15);
                        break;

                    // large grid wings
                    case "aero-wing_1x5x1_rounded_edge_Large":
                    case "aero-wing_5x1x1_rounded_edge_Large":
                        forceMul = wing_5x1 * LGmultiplier;
                        fw = Vector3D.Normalize(blockMatrix.Left);
                        break;
                    case "aero-wing_2x5x1_pointed_edge_Large":
                    case "aero-wing_2x5x1_rounded_edge_Large":
                    case "aero-wing_5x2x1_rounded_edge_Large":
                        forceMul = wing_5x2 * LGmultiplier;
                        fw = Vector3D.Normalize(blockMatrix.Left + blockMatrix.Forward * 0.15);
                        break;
                    case "aero-wing_3x5x1_pointed_edge_Large":
                    case "aero-wing_3x5x1_rounded_edge_Large":
                    case "aero-wing_5x3x1_rounded_edge_Large":
                        forceMul = wing_5x3 * LGmultiplier;
                        fw = Vector3D.Normalize(blockMatrix.Left + blockMatrix.Forward * 0.25);
                        break;
                    case "aero-wing_4x5x1_pointed_edge_Large":
                    case "aero-wing_4x5x1_rounded_edge_Large":
                        forceMul = wing_5x4 * LGmultiplier;
                        fw = Vector3D.Normalize(blockMatrix.Left + blockMatrix.Forward * 0.35);
                        break;
                    case "aero-wing_5x5x1_pointed_edge_Large":
                    case "aero-wing_5x5x1_rounded_edge_Large":
                        forceMul = wing_5x5 * LGmultiplier;
                        fw = Vector3D.Normalize(blockMatrix.Left + blockMatrix.Forward * 0.45);
                        break;
                    case "aero-wing_6x5x1_pointed_edge_Large":
                    case "aero-wing_6x5x1_rounded_edge_Large":
                        forceMul = wing_6x5 * LGmultiplier;
                        fw = Vector3D.Normalize(blockMatrix.Left + blockMatrix.Forward * 0.55);
                        break;
                    case "aero-wing_7x5x1_pointed_edge_Large":
                        forceMul = wing_7x5 * LGmultiplier;
                        fw = Vector3D.Normalize(blockMatrix.Left + blockMatrix.Forward * 0.55);
                        break;
                }

                double speedDir = fw.Dot(vel);

                if(speedDir > 0 || speedDir < 0)
                {
                    var upDir = blockMatrix.Up;
                    var forceVector = -upDir * upDir.Dot(vel) * forceMul * speedSq * atmosphere;
                    var applyForceAt = grid.Physics.CenterOfMassWorld;

                    grid.Physics.AddForce(MyPhysicsForceType.APPLY_WORLD_FORCE, forceVector, applyForceAt, null);
                }
            }
            catch(Exception e)
            {
                Log.Error(e, "UpdateAfterSimulation - WARNING: possible loss of wing lift");
            }
        }

        public override void UpdateBeforeSimulation10()
        {
            try
            {
                var grid = block.CubeGrid;

                if(grid.Physics == null || grid.Physics.IsStatic || block.MarkedForClose || block.Closed || !block.IsWorking)
                    return;

                var gridCenter = grid.Physics.CenterOfMassWorld;
                var planets = AerodynamicsMod.instance.planets;

                atmosphere = 0;
                atmospheres = 0;

                for(int i = planets.Count - 1; i >= 0; --i)
                {
                    var planet = planets[i];

                    if(planet.Closed || planet.MarkedForClose)
                    {
                        planets.RemoveAt(i);
                        continue;
                    }

                    if(Vector3D.DistanceSquared(gridCenter, planet.WorldMatrix.Translation) < (planet.AtmosphereRadius * planet.AtmosphereRadius))
                    {
                        atmosphere += planet.GetAirDensity(gridCenter);
                        atmospheres++;
                    }
                }

                if(atmospheres > 0)
                {
                    atmosphere /= atmospheres;
                    atmosphere = MathHelper.Clamp((atmosphere - AerodynamicsMod.MIN_ATMOSPHERE) / (AerodynamicsMod.MAX_ATMOSPHERE - AerodynamicsMod.MIN_ATMOSPHERE), 0f, 1f);
                }
            }
            catch(Exception e)
            {
                Log.Error(e, "UpdateBeforeSimulation10");
            }
        }
    }
}
