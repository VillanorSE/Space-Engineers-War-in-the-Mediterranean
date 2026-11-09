using System.Collections.Generic;
using VRageMath;
using static Scripts.Structure;
using static Scripts.Structure.WeaponDefinition.AmmoDef;
using static Scripts.Structure.WeaponDefinition.AnimationDef;
using static Scripts.Structure.WeaponDefinition.AnimationDef.RelMove;
using static Scripts.Structure.WeaponDefinition.AnimationDef.PartAnimationSetDef;
using static Scripts.Structure.WeaponDefinition.AnimationDef.PartAnimationSetDef.EventTriggers;

namespace Scripts
{
    // WeaponCore parts-template helpers, cut down to what WitM's definitions use.
    partial class Parts
    {
        internal ContainerDefinition Container = new ContainerDefinition();

        internal void PartDefinitions(params WeaponDefinition[] defs)
        {
            Container.WeaponDefs = defs;
        }

        internal static void GetBaseDefinitions(out ContainerDefinition baseDefs)
        {
            baseDefs = new Parts().Container;
        }

        internal static void SetModPath(ContainerDefinition baseDefs, string modContext)
        {
            if (baseDefs.WeaponDefs != null)
                for (int i = 0; i < baseDefs.WeaponDefs.Length; i++)
                    baseDefs.WeaponDefs[i].ModPath = modContext;
        }

        internal Randomize Random(float start, float end)
        {
            return new Randomize { Start = start, End = end };
        }

        internal Vector4 Color(float red, float green, float blue, float alpha)
        {
            return new Vector4(red, green, blue, alpha);
        }

        internal Vector3D Vector(double x, double y, double z)
        {
            return new Vector3D(x, y, z);
        }

        // Animation helpers from YtD's PartCompile.cs (3431829641), for the copied YtD CC54-38 animation (D-068).
        internal XYZ Transformation(double X, double Y, double Z)
        {
            return new XYZ { x = X, y = Y, z = Z };
        }

        internal Dictionary<EventTriggers, uint> Delays(uint FiringDelay = 0, uint ReloadingDelay = 0, uint OverheatedDelay = 0, uint TrackingDelay = 0, uint LockedDelay = 0, uint OnDelay = 0, uint OffDelay = 0, uint BurstReloadDelay = 0, uint OutOfAmmoDelay = 0, uint PreFireDelay = 0, uint StopFiringDelay = 0, uint StopTrackingDelay = 0, uint InitDelay = 0, uint HomingDelay = 0, uint TargetAlignedDelay = 0, uint WhileOnDelay = 0, uint TargetRanged100Delay = 0, uint TargetRanged75Delay = 0, uint TargetRanged50Delay = 0, uint TargetRanged25Delay = 0)
        {
            return new Dictionary<EventTriggers, uint>
            {
                [Firing] = FiringDelay,
                [Reloading] = ReloadingDelay,
                [Overheated] = OverheatedDelay,
                [Tracking] = TrackingDelay,
                [TurnOn] = OnDelay,
                [TurnOff] = OffDelay,
                [BurstReload] = BurstReloadDelay,
                [NoMagsToLoad] = OutOfAmmoDelay,
                [PreFire] = PreFireDelay,
                [EmptyOnGameLoad] = 0,
                [StopFiring] = StopFiringDelay,
                [StopTracking] = StopTrackingDelay,
                [LockDelay] = LockedDelay,
                [Init] = InitDelay,
                [Homing] = HomingDelay,
                [TargetAligned] = TargetAlignedDelay,
                [WhileOn] = WhileOnDelay,
                [TargetRanged100] = TargetRanged100Delay,
                [TargetRanged75] = TargetRanged75Delay,
                [TargetRanged50] = TargetRanged50Delay,
                [TargetRanged25] = TargetRanged25Delay,
            };
        }

        internal PartEmissive Emissive(string EmissiveName, bool CycleEmissiveParts, bool LeavePreviousOn, Vector4[] Colors, float IntensityFrom, float IntensityTo, string[] EmissivePartNames)
        {
            return new PartEmissive
            {
                EmissiveName = EmissiveName,
                Colors = Colors,
                CycleEmissivesParts = CycleEmissiveParts,
                LeavePreviousOn = LeavePreviousOn,
                EmissivePartNames = EmissivePartNames,
                IntensityRange = new[]{ IntensityFrom, IntensityTo }
            };
        }

        internal EventTriggers[] Events(params EventTriggers[] events)
        {
            return events;
        }

        internal string[] Names(params string[] names)
        {
            return names;
        }
    }
}
