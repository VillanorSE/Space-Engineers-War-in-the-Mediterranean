// WitM override (owner 2026-11-04, D-068) of YtD 3431829641 YtD_SC20-37_Autocannon.cs: identical except Threats also include Characters,
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
        WeaponDefinition YtD_SC2037_autocannon => new WeaponDefinition
        {
            Assignments = new ModelAssignmentsDef 
            {
                MountPoints = new[] {
                    new MountPointDef {
                        SubtypeId = "YtD_SC2037_ac",
                        MuzzlePartId = "None", //Specifies what dummy is linked to whichever file the barrel dummy is in
                        AzimuthPartId = "None",
                        ElevationPartId = "None",
						DurabilityMod = 0.35f,
                    },
					 new MountPointDef {
                        SubtypeId = "YtD_SC2037_ac_off",
                        MuzzlePartId = "None", //Specifies what dummy is linked to whichever file the barrel dummy is in
                        AzimuthPartId = "None",
                        ElevationPartId = "None",
						DurabilityMod = 0.35f,
                    },
                },
                Muzzles = new [] {
                    "muzzle_projectile_1",
                },
            },
            Targeting = new TargetingDef  
            {
                Threats = new[] {
                    Grids, Characters, Projectiles,
                },
                SubSystems = new[] {
                    Thrust, Utility, Offense, Power, Production, Any,
                },
                ClosestFirst = true, // tries to pick closest targets first (blocks on grids, projectiles, etc...).
                MinimumDiameter = 0, // 0 = unlimited, Minimum radius of threat to engage.
                MaximumDiameter = 0, // 0 = unlimited, Maximum radius of threat to engage.
                TopTargets = 0, // 0 = unlimited, max number of top targets to randomize between.
                TopBlocks = 3, // 0 = unlimited, max number of blocks to randomize between
                StopTrackingSpeed = 1000, // do not track target threats traveling faster than this speed
            },
            HardPoint = new HardPointDef 
            {
                PartName = "SC20-37 Autocannon", // name of weapon in terminal
                DefinitionPriority = 10, // WitM override: YtD sets none (0)
                DeviateShotAngle = 0.18f,
                AimingTolerance = 1f, // 0 - 180 firing angle
                AimLeadingPrediction = Basic, // Off, Basic, Accurate, Advanced
                DelayCeaseFire = 0, // Measured in game ticks (6 = 100ms, 60 = 1 seconds, etc..).
				AddToleranceToTracking = false, // Allows turret to track to the edge of the AimingTolerance cone instead of dead centre.
				CanShootSubmerged = false, // Whether the weapon can be fired underwater when using WaterMod.
				NpcSafe = false, // This is you tell npc moders that your ammo was designed with them in mind, if they tell you otherwise set this to false.
				ScanTrackOnly = false, // This weapon only scans and tracks entities, this disables un-needed functionality and customizes for this purpose. 
                
                Ui = new UiDef {
                    RateOfFire = false,
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
					SuppressFire = false, // If enabled, weapon can only be fired manually.
					OverrideLeads = false, // Disable target leading on fixed weapons, or allow it for turrets.
					DefaultLeadGroup = 1, // Default LeadGroup setting, range 0-5, 0 is disables lead group.  Only useful for fixed weapons or weapons set to OverrideLeads.
					TargetGridCenter = false, // Does not target blocks, instead it targets grid center.
               },
                HardWare = new HardwareDef {
                    RotateRate = 0f,
                    ElevateRate = 0f,
                    MinAzimuth = 0,
                    MaxAzimuth = 0,
                    MinElevation = 0,
                    MaxElevation = 0,
                    FixedOffset = false,
                    Type = BlockWeapon, // BlockWeapon, HandWeapon, Phantom

                    IdlePower = 0.001f, // Power draw in MW while not charging, or for non-energy weapons. Defaults to 0.001.

                    InventorySize = 0.1f,
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
                    RateOfFire = 210,
                    BarrelSpinRate = 0, // visual only, 0 disables and uses RateOfFire
                    BarrelsPerShot = 1,
                    TrajectilesPerBarrel = 1, // Number of Trajectiles per barrel per fire event.
                    SkipBarrels = 0,

                    MagsToLoad = 1, // Number of physical magazines to consume on reload.
                    SpinFree = false, // Spin barrel while not firing
                    StayCharged = false, // Will start recharging whenever power cap is not full

                    ReloadTime = 320, // Measured in game ticks (6 = 100ms, 60 = 1 seconds, etc..).
                    DelayUntilFire = 0, // Measured in game ticks (6 = 100ms, 60 = 1 seconds, etc..).
                    HeatPerShot = 10, //heat generated per shot
                    MaxHeat = 5000, //max heat before weapon enters cooldown (70% of max heat)
                    Cooldown = .95f, //percent of max heat to be under to start firing again after overheat accepts .2-.95
                    HeatSinkRate = 400, //amount of heat lost per second
                    DegradeRof = false, // progressively lower rate of fire after 80% heat threshold (80% of max heat)
                    ShotsInBurst = 10,
                    DelayAfterBurst = 0, // Measured in game ticks (6 = 100ms, 60 = 1 seconds, etc..).
                    FireFull = false,
                },
                Audio = new HardPointAudioDef {
                    PreFiringSound = "",
                    FiringSound = "WepRifleShot", //"YtD20mmShot",
                    FiringSoundPerShot = true,
                    ReloadSound = "",//"LoadSound",
                    NoAmmoSound = "ArcWepShipGatlingNoAmmo",
                },
                Graphics = new HardPointParticleDef
                {
                    
                    Effect1 = new ParticleDef
                    {
                        Name = "Muzzle_Flash_Ironsights", // Smoke_LargeGunShot
                        Color = Color(red: 100, green: 150, blue: 100, alpha: 1),
                        Offset = Vector(x: 0, y: 0, z: 0),

                        Extras = new ParticleOptionDef
                        {
                            Loop = false,
                            Restart = false,
                            MaxDistance = 200,
                            MaxDuration = 3,
                            Scale = 0.5f,
                        },
                    },
                    /*Effect2 = new ParticleDef
                    {
                        Name = "Muzzle_Flash_MediumCalibre",
                        Color = Color(red: 10, green: 50, blue: 10, alpha: 1),
                        Offset = Vector(x: 0, y: 0, z: 0),

                        Extras = new ParticleOptionDef
                        {
                            Loop = false,
                            Restart = false,
                            MaxDistance = 300,
                            MaxDuration = 1,
                            Scale = 0.05f,
                        },
                    },*/
                    
                },
            },
            Ammos = new [] {
                YtD_SC20_AP_ammo,
				YtD_SC20_HE_ammo, YtD_CC30_Secondary,
            },
            //Animations =,
            // Don't edit below this line
        };
    }
}