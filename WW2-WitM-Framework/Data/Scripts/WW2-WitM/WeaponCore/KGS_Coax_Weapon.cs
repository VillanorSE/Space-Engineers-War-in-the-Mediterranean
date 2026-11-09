using static Scripts.Structure;
using static Scripts.Structure.WeaponDefinition;
using static Scripts.Structure.WeaponDefinition.ModelAssignmentsDef;
using static Scripts.Structure.WeaponDefinition.HardPointDef;
using static Scripts.Structure.WeaponDefinition.HardPointDef.Prediction;
using static Scripts.Structure.WeaponDefinition.TargetingDef.BlockTypes;
using static Scripts.Structure.WeaponDefinition.TargetingDef.Threat;
using static Scripts.Structure.WeaponDefinition.TargetingDef;
using static Scripts.Structure.WeaponDefinition.HardPointDef.HardwareDef;
using static Scripts.Structure.WeaponDefinition.HardPointDef.HardwareDef.HardwareType;

namespace Scripts
{
    partial class Parts
    {
        // KGS (2930049835) MG-7A "Hammer" LMG and coaxial MG, replacing KGS_Coax_Weapon.cs. Copied from KGS with the
        // MG6-35's (YtD 3431829641, YtD_Lmg_Mounted.cs) rate of fire, reload, spread and ammo. Owner 2026-10-29.
        WeaponDefinition WitM_KGS_Coaxial_MG => new WeaponDefinition
        {
            Assignments = new ModelAssignmentsDef
            {
                MountPoints = new[] {
                    new MountPointDef {
                        SubtypeId = "KGS_CoaxialMG",
                        SpinPartId = "None",
                        MuzzlePartId = "None",
                        AzimuthPartId = "None",
                        ElevationPartId = "None",
                        DurabilityMod = 0.2f,
                    },
                    new MountPointDef {
                        SubtypeId = "KGS_LMG_Hammer_762",
                        SpinPartId = "None",
                        MuzzlePartId = "None",
                        AzimuthPartId = "None",
                        ElevationPartId = "None",
                        DurabilityMod = 0.2f,
                    },
                },
                Muzzles = new[] {
                    "muzzle_projectile_1",
                },
                Ejector = "",
                Scope = "muzzle_projectile_1"
            },
            Targeting = new TargetingDef
            {
                Threats = new[] {
                    Grids, Characters,
                },
                SubSystems = new[] {
                    Thrust, Utility, Offense, Power, Production, Any,
                },
                ClosestFirst = true,
                MaxTrackingTime = 120, // WitM 2026-11-07 (#62): drop the target every 2 s and re-acquire. A CTC-aimed fixed gun gets WC's terrain re-check only on (re)acquisition, so without this it fires through hills.
            },
            HardPoint = new HardPointDef
            {
                PartName = "Coaxial Machine Gun", // must stay KGS's name for the override to replace it
                DefinitionPriority = 10, // KGS sets none (0)
                DeviateShotAngle = 0.24f, // MG6-35 (KGS 0.2)
                AimingTolerance = 1f,
                AimLeadingPrediction = Basic,
                DelayCeaseFire = 0,
                AddToleranceToTracking = false,
                CanShootSubmerged = false,

                Ui = new UiDef {
                    RateOfFire = true,
                    DamageModifier = false,
                    ToggleGuidance = false,
                    EnableOverload = false,
                },
                Ai = new AiDef {
                    TrackTargets = false,
                    TurretAttached = false,
                    TurretController = false,
                    PrimaryTracking = false,
                    LockOnFocus = false,
                },
                HardWare = new HardwareDef {
                    InventorySize = 0.15f,
                    Offset = Vector(x: 0, y: 0, z: 0),
                    Type = BlockWeapon,
                },
                Loading = new LoadingDef {
                    RateOfFire = 450, // MG6-35 (KGS 600)
                    BarrelsPerShot = 1,
                    TrajectilesPerBarrel = 1,
                    ReloadTime = 280, // MG6-35 (KGS 120)
                    DelayUntilFire = 0,
                    HeatPerShot = 0,
                    MaxHeat = 70000,
                    Cooldown = .95f,
                    HeatSinkRate = 9000,
                },
                Audio = new HardPointAudioDef {
                    FiringSound = "KGS_762_Shot",
                    FiringSoundPerShot = true,
                },
                Graphics = new HardPointParticleDef {
                    Effect1 = new ParticleDef {
                        Name = "KGS_RWS_MuzzleFlash",
                        Color = Color(red: 0, green: 0, blue: 0, alpha: 1),
                        Offset = Vector(x: 0, y: 0, z: -0.25f),
                        Extras = new ParticleOptionDef {
                            MaxDistance = 1000,
                            Scale = 1f,
                        },
                    },
                },
            },
            Ammos = new[] {
                WitM_KGS_762_MG,
            },
        };
    }
}
