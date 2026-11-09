// WitM override (owner 2026-11-04, D-068) of YtD 3431829641 SHA_Sh37_Gun.cs: identical except Threats also include Characters,
// so WeaponCore aims the turret controller at players on foot (the coax and hull MGs follow the turret), and
// DefinitionPriority 10 so this replaces YtD's definition (same PartName).
using static Scripts.Structure;
using static Scripts.Structure.WeaponDefinition;
using static Scripts.Structure.WeaponDefinition.ModelAssignmentsDef;
using static Scripts.Structure.WeaponDefinition.HardPointDef;
using static Scripts.Structure.WeaponDefinition.HardPointDef.Prediction;
using static Scripts.Structure.WeaponDefinition.TargetingDef.BlockTypes;
using static Scripts.Structure.WeaponDefinition.TargetingDef.Threat;
using static Scripts.Structure.WeaponDefinition.TargetingDef;
using static Scripts.Structure.WeaponDefinition.TargetingDef.CommunicationDef.Comms;
using static Scripts.Structure.WeaponDefinition.TargetingDef.CommunicationDef.SecurityMode;
using static Scripts.Structure.WeaponDefinition.HardPointDef.HardwareDef;
using static Scripts.Structure.WeaponDefinition.HardPointDef.HardwareDef.HardwareType;

namespace Scripts {   
    partial class Parts {
        // Don't edit above this line
        WeaponDefinition SHA_Sh37_Gun => new WeaponDefinition
        {
            Assignments = new ModelAssignmentsDef 
            {
                MountPoints = new[] {
                    new MountPointDef {
                        SubtypeId = "SHA_Sh37_gun",
                        MuzzlePartId = "None", //Specifies what dummy is linked to whichever file the barrel dummy is in
                        AzimuthPartId = "None",
                        ElevationPartId = "None",
						DurabilityMod = 0.3f,
                    },
                },
                Muzzles = new [] {
                    "muzzle_projectile_1",
                },
            },
            Targeting = new TargetingDef
            {
                Threats = new[] {
                    Grids, Characters,
                },
                SubSystems = new[] {
                    Thrust, Utility, Offense, Power, Production, Any,
                },
                ClosestFirst = true, // tries to pick closest targets first (blocks on grids, projectiles, etc...).
                MinimumDiameter = 0, // 0 = unlimited, Minimum radius of threat to engage.
                MaximumDiameter = 0, // 0 = unlimited, Maximum radius of threat to engage.
                TopTargets = 0, // 0 = unlimited, max number of top targets to randomize between.
                TopBlocks = 4, // 0 = unlimited, max number of blocks to randomize between
                StopTrackingSpeed = 1000, // do not track target threats traveling faster than this speed
                MaxTrackingTime = 120, // WitM 2026-11-07 (#62): drop the target every 2 s and re-acquire. A CTC-aimed fixed gun gets WC's terrain re-check only on (re)acquisition, so without this it fires through hills.
            },
            HardPoint = new HardPointDef 
            {
                PartName =  "Sh.37mm Tank Gun", // name of weapon in terminal
                DefinitionPriority = 10, // WitM override: YtD sets none (0)
                DeviateShotAngle = 0.14f,
                AimingTolerance = 1f, // 0 - 180 firing angle
                AimLeadingPrediction = Accurate, // Off, Basic, Accurate, Advanced
                DelayCeaseFire = 0, // Measured in game ticks (6 = 100ms, 60 = 1 seconds, etc..).

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
                    RotateRate = 0.0f,
                    ElevateRate = 0.0f,
                    MinAzimuth = 0,
                    MaxAzimuth = 0,
                    MinElevation = 0,
                    MaxElevation = 0,
                    FixedOffset = false,
                    Type = BlockWeapon, // BlockWeapon, HandWeapon, Phantom

                    IdlePower = 0.001f, // Power draw in MW while not charging, or for non-energy weapons. Defaults to 0.001.

                    InventorySize = 0.024f,
                    Offset = Vector(x: 0, y: 1, z: 0),
                },
                Other = new OtherDef {
                    ConstructPartCap = 6,
                    RotateBarrelAxis = 0,
                    EnergyPriority = 0,
                    MuzzleCheck = false,
                    Debug = false,
                },
                Loading = new LoadingDef {
                    RateOfFire = 18,
                    BarrelSpinRate = 0, // visual only, 0 disables and uses RateOfFire
                    BarrelsPerShot = 1,
                    TrajectilesPerBarrel = 1, // Number of Trajectiles per barrel per fire event.
                    SkipBarrels = 0,

                    MagsToLoad = 1, // Number of physical magazines to consume on reload.
                    SpinFree = false, // Spin barrel while not firing
                    StayCharged = false, // Will start recharging whenever power cap is not full

                    ReloadTime = 155, // Measured in game ticks (6 = 100ms, 60 = 1 seconds, etc..).
                    DelayUntilFire = 0, // Measured in game ticks (6 = 100ms, 60 = 1 seconds, etc..).
                    HeatPerShot = 50, //heat generated per shot
                    MaxHeat = 50000, //max heat before weapon enters cooldown (70% of max heat)
                    Cooldown = .95f, //percent of max heat to be under to start firing again after overheat accepts .2-.95
                    HeatSinkRate = 9000, //amount of heat lost per second
                    DegradeRof = false, // progressively lower rate of fire after 80% heat threshold (80% of max heat)
                    ShotsInBurst = 0,
                    DelayAfterBurst = 0, // Measured in game ticks (6 = 100ms, 60 = 1 seconds, etc..).
                    FireFull = false,
                },
                Audio = new HardPointAudioDef {
					PreFiringSound = "",
                    FiringSound = "AWG2pdrShot", // "WepShipGatlingShot",
                    FiringSoundPerShot = true,
                    ReloadSound = "", //"LoadSound",
                    NoAmmoSound = "ArcWepShipGatlingNoAmmo",
                },
                Graphics = new HardPointParticleDef
                {
                    
                    Effect1 = new ParticleDef
                    {
                        Name = "Smoke_Autocannon", // Muzzle_Flash_Large
                        Color = Color(red: 90, green: 90, blue: 90, alpha: 1),
                        Offset = Vector(x: 0, y: 0, z: 0),

                        Extras = new ParticleOptionDef
                        {
                            Loop = false,
                            Restart = false,
                            MaxDistance = 250,
                            MaxDuration = 0.1f,
                            Scale = 0.03f,
                        },
                    },
                    Effect2 = new ParticleDef
                    {
                        Name = "Muzzle_Flash_MediumCalibre",
                        Color = Color(red: 10, green: 10, blue: 10, alpha: 1),
                        Offset = Vector(x: 0, y: 0, z: -0.1),

                        Extras = new ParticleOptionDef
                        {
                            Loop = false,
                            Restart = false,
                            MaxDistance = 70,
                            MaxDuration = 0.075f,
                            Scale = 0.1f,
                        },
                    },
                    
                },
            },
            Ammos = new [] {
                YtD_Sh37_APHE,
				YtD_Sh37_HE, YtD_CC30_Secondary,
            },
            //Animations = ,
            // Don't edit below this line
        };
    }
}