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
        // Twin Breda 38 hull ball mount (M13/40), WitM_Breda38_BallMount. Set up as YtD's MG6-35 ball mount
        // (3431829641 YtD_Lmg_Mounted.cs YtD_LMG_Ball_Mounted): vanilla turret type, GatlingTurretBase1/2 subparts.
        // Two guns at 350 rpm each, firing alternately (owner 2026-11-06); MG6-35 round, 200 m AI range.
        WeaponDefinition WitM_Breda38_Twin => new WeaponDefinition
        {
            Assignments = new ModelAssignmentsDef
            {
                MountPoints = new[] {
                    new MountPointDef {
                        SubtypeId = "WitM_Breda38_BallMount",
                        SpinPartId = "None",
                        MuzzlePartId = "GatlingTurretBase2",
                        AzimuthPartId = "GatlingTurretBase1",
                        ElevationPartId = "GatlingTurretBase2",
                        DurabilityMod = 0.255f,
                    },
                    new MountPointDef {
                        SubtypeId = "WitM_Breda38_BallMount_Housing",
                        SpinPartId = "None",
                        MuzzlePartId = "GatlingTurretBase2",
                        AzimuthPartId = "GatlingTurretBase1",
                        ElevationPartId = "GatlingTurretBase2",
                        DurabilityMod = 0.255f,
                    },
                },
                Muzzles = new[] {
                    "muzzle_projectile_1",
                    "muzzle_projectile_2",
                },
                Ejector = "",
                Scope = "muzzle_projectile_1",
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
                MinimumDiameter = 0,
                MaximumDiameter = 150,
                TopTargets = 0,
                TopBlocks = 3,
                StopTrackingSpeed = 100,
                MaxTrackingTime = 120, // WitM 2026-11-07 (#62): drop the target every 2 s and re-acquire. A CTC-aimed fixed gun gets WC's terrain re-check only on (re)acquisition, so without this it fires through hills.
                MaxTargetDistance = 200, // WitM AI MGs (owner 2026-10-31)
            },
            HardPoint = new HardPointDef
            {
                PartName = "Breda 38 Twin MG [Ball Mounted]",
                DeviateShotAngle = 0.24f, // MG6-35
                AimingTolerance = 1f,
                AimLeadingPrediction = Accurate,
                DelayCeaseFire = 0,
                AddToleranceToTracking = false,
                CanShootSubmerged = false,
                NpcSafe = false,
                ScanTrackOnly = false,

                Ui = new UiDef {
                    RateOfFire = true,
                    DamageModifier = false,
                    ToggleGuidance = false,
                    EnableOverload = false,
                },
                Ai = new AiDef {
                    TrackTargets = true,
                    TurretAttached = true,
                    TurretController = true,
                    PrimaryTracking = true,
                    LockOnFocus = true,
                },
                HardWare = new HardwareDef {
                    RotateRate = 0.05f,
                    ElevateRate = 0.05f,
                    MinAzimuth = -20,
                    MaxAzimuth = 20,
                    MinElevation = -10,
                    MaxElevation = 20,
                    FixedOffset = false,
                    Type = BlockWeapon,
                    IdlePower = 0f,
                    InventorySize = 0.18f,
                    Offset = Vector(x: 0, y: 0, z: 0),
                },
                Other = new OtherDef {
                    ConstructPartCap = 6,
                    RotateBarrelAxis = 0,
                    EnergyPriority = 0,
                    MuzzleCheck = false,
                    Debug = false,
                },
                Loading = new LoadingDef {
                    RateOfFire = 700, // 2 x 350 rpm, one barrel per shot, alternating
                    BarrelSpinRate = 0,
                    BarrelsPerShot = 1,
                    TrajectilesPerBarrel = 1,
                    SkipBarrels = 0,
                    MagsToLoad = 1,
                    SpinFree = false,
                    StayCharged = false,
                    ReloadTime = 280, // MG6-35
                    DelayUntilFire = 15,
                    HeatPerShot = 10,
                    MaxHeat = 70000,
                    Cooldown = .95f,
                    HeatSinkRate = 9000,
                    DegradeRof = false,
                    ShotsInBurst = 0,
                    DelayAfterBurst = 0,
                    FireFull = false,
                },
                Audio = new HardPointAudioDef {
                    PreFiringSound = "",
                    FiringSound = "AWGMGShot", // YtD
                    FiringSoundPerShot = true,
                    ReloadSound = "",
                    NoAmmoSound = "ArcWepShipGatlingNoAmmo",
                },
                Graphics = new HardPointParticleDef {
                    Effect1 = new ParticleDef {
                        Name = "Muzzle_Flash_Ironsights", // YtD
                        Color = Color(red: 180, green: 160, blue: 100, alpha: 1),
                        Offset = Vector(x: 0, y: 0, z: 0f),
                        Extras = new ParticleOptionDef {
                            Loop = false,
                            Restart = false,
                            MaxDistance = 300,
                            MaxDuration = 240,
                            Scale = 0.75f,
                        },
                    },
                },
            },
            Ammos = new[] {
                WitM_Breda38_Ammo,
            },
        };
    }
}
