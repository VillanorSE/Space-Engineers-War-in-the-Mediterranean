using System;
using System.Collections.Generic;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using VRage.Game.Components;
using VRage.ModAPI;
using VRageMath;

// WW2-WitM Wing Rebalance
//
// Replaces the wing lift logic shipped with Consty's Wings (Workshop 2758773086), Randa's
// "Thin Wings, Plane Parts & Control Surfaces" (2612368868), and Plane Parts (837058476) - all
// three are existing WW2-WitM dependencies. Ported from CAPTurbonerd's "Aerodynamics Override"
// (Workshop 3480334050) with two deliberate changes:
//
//   1. Lift onset velocity dropped from each mod's own original threshold (Consty: ~2.24 m/s,
//      Randa's/Plane Parts: ~5.0 m/s) down to a single ~0.71 m/s floor (speedSq >= 1), matching
//      Turbonerd's change exactly.
//   2. Lift magnitude (forceMul) is NOT copied from Turbonerd's mod - Turbonerd's own numbers
//      cut lift by roughly 40-86% depending on wing family/size, which our planes were never
//      built around. Instead each wing's forceMul is set HALFWAY between its original value and
//      Turbonerd's reduced value (i.e. half of Turbonerd's percentage reduction), preserving
//      each source mod's own relative size scaling rather than adopting Turbonerd's regrouped
//      tiers. See Wing.cs for the actual per-subtype values and the reduction math in comments.
//
// Atmosphere gating (0.4-0.7 air density ramp) and the x10 large-grid multiplier are left
// unchanged from all three original mods - Turbonerd didn't touch either of those either.
namespace WW2WitM.Aerodynamics
{
    [MySessionComponentDescriptor(MyUpdateOrder.AfterSimulation)]
    public class AerodynamicsMod : MySessionComponentBase
    {
        private const long WORKSHOP_ID_CONSTY = 2758773086;
        private const long WORKSHOP_ID_RANDAS = 2612368868;
        private const long WORKSHOP_ID_PLANEPARTS = 837058476;

        public static AerodynamicsMod instance = null;

        private bool init = false;
        private short planetRefreshTick = 0;
        private readonly Queue<long> modDisableQueue = new Queue<long>();
        private int modDisableDelay = 0;

        public readonly List<MyPlanet> planets = new List<MyPlanet>();

        private const short PLANET_REFRESH_TICKS = 60 * 5;
        public const float MIN_ATMOSPHERE = 0.4f;
        public const float MAX_ATMOSPHERE = 0.7f;

        public override void LoadData()
        {
            instance = this;
        }

        public void Init()
        {
            init = true;

            MyAPIGateway.Entities.GetEntities(null, IterateEntity);

            // Disable the original wing lift scripts on the mods we're replacing, so lift isn't
            // calculated twice. Same mechanism (and same three mod IDs) Turbonerd's override used.
            modDisableQueue.Enqueue(WORKSHOP_ID_CONSTY);
            modDisableQueue.Enqueue(WORKSHOP_ID_RANDAS);
            modDisableQueue.Enqueue(WORKSHOP_ID_PLANEPARTS);

            modDisableDelay = 30;
        }

        protected override void UnloadData()
        {
            instance = null;

            try
            {
                if(init)
                {
                    init = false;
                    planets.Clear();
                }
            }
            catch(Exception e)
            {
                Log.Error(e);
            }
        }

        public override void UpdateAfterSimulation()
        {
            try
            {
                if(!init)
                {
                    if(MyAPIGateway.Session == null)
                        return;

                    Init();
                }

                if(++planetRefreshTick >= PLANET_REFRESH_TICKS)
                {
                    planetRefreshTick = 0;
                    planets.Clear();
                    MyAPIGateway.Entities.GetEntities(null, IterateEntity);
                }

                if(modDisableDelay > 0)
                {
                    modDisableDelay--;
                }
                else if(modDisableQueue.Count > 0)
                {
                    long targetModId = modDisableQueue.Dequeue();
                    MyAPIGateway.Utilities.SendModMessage(targetModId, VRage.MyTuple.Create(false, "WW2-WitM Wing Rebalance"));
                    modDisableDelay = 30;
                }
            }
            catch(Exception e)
            {
                Log.Error(e);
            }
        }

        private bool IterateEntity(IMyEntity e)
        {
            var p = e as MyPlanet;

            if(p != null && p.HasAtmosphere)
                planets.Add(p);

            return false;
        }
    }
}
