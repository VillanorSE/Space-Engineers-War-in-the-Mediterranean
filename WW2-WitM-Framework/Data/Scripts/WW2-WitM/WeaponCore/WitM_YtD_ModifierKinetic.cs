// WitM copy (owner 2026-11-04, D-068) of YtD 3431829641 KONTAKT Modifiers/ModifierKinetic.cs: unchanged; the copied YtD ammo uses it.
using static Scripts.Structure.WeaponDefinition;
using static Scripts.Structure.WeaponDefinition.AmmoDef;
using static Scripts.Structure.WeaponDefinition.AmmoDef.EjectionDef;
using static Scripts.Structure.WeaponDefinition.AmmoDef.EjectionDef.SpawnType;
using static Scripts.Structure.WeaponDefinition.AmmoDef.ShapeDef.Shapes;
using static Scripts.Structure.WeaponDefinition.AmmoDef.GraphicDef;
using static Scripts.Structure.WeaponDefinition.AmmoDef.TrajectoryDef;
using static Scripts.Structure.WeaponDefinition.AmmoDef.TrajectoryDef.GuidanceType;
using static Scripts.Structure.WeaponDefinition.AmmoDef.DamageScaleDef;
using static Scripts.Structure.WeaponDefinition.AmmoDef.DamageScaleDef.ShieldDef.ShieldType;
using static Scripts.Structure.WeaponDefinition.AmmoDef.AreaDamageDef;
using static Scripts.Structure.WeaponDefinition.AmmoDef.AreaDamageDef.AreaEffectType;
using static Scripts.Structure.WeaponDefinition.AmmoDef.AreaDamageDef.EwarFieldsDef;
using static Scripts.Structure.WeaponDefinition.AmmoDef.AreaDamageDef.EwarFieldsDef.PushPullDef.Force;
using static Scripts.Structure.WeaponDefinition.AmmoDef.GraphicDef.LineDef;
using static Scripts.Structure.WeaponDefinition.AmmoDef.GraphicDef.LineDef.TracerBaseDef;
using static Scripts.Structure.WeaponDefinition.AmmoDef.GraphicDef.LineDef.Texture;
using static Scripts.Structure.WeaponDefinition.AmmoDef.DamageScaleDef.DamageTypes.Damage;

namespace Scripts
{ // Don't edit above this line
    partial class Parts
    {
        private CustomScalesDef KineticModifer => new CustomScalesDef
        {
            IgnoreAllOthers = false,
            Types = new[]
            {
                new CustomBlocksDef
                {
                    SubTypeId = "KGS_Composite_Chobham_Slant_Var1",
                    Modifier = 1f,
                },
				new CustomBlocksDef
                {
                    SubTypeId = "KGS_Composite_Chobham_Cube_Var1",
                    Modifier = 1f,
                },
				new CustomBlocksDef
                {
                    SubTypeId = "KGS_Composite_Chobham_HalfSlab",
                    Modifier = 1f,
                },
				
				new CustomBlocksDef
                {
                    SubTypeId = "KGS_Composite_DeU_Cladding_Var1",
                    Modifier = 0.5f,
                },
				new CustomBlocksDef
                {
                    SubTypeId = "KGS_Composite_Nanoceramic",
                    Modifier = 0.5f,
                },				
				new CustomBlocksDef
                {
                    SubTypeId = "Plough_R",
                    Modifier = 0.8f,
                },
				new CustomBlocksDef
                {
                    SubTypeId = "Plough_L",
                    Modifier = 0.8f,
                },
				new CustomBlocksDef
                {
                    SubTypeId = "C1",
                    Modifier = 0.75f,
                },
				new CustomBlocksDef
                {
                    SubTypeId = "C2",
                    Modifier = 0.75f,
                },
				new CustomBlocksDef
                {
                    SubTypeId = "C3",
                    Modifier = 2f,
                },
				new CustomBlocksDef
                {
                    SubTypeId = "ERAFlat",
                    Modifier = 1.5f,
				},
					
				//IAP Spaced Armor
				new CustomBlocksDef { SubTypeId = "Spaced_Armour_HalfExternal", Modifier = 0.95f, },
				new CustomBlocksDef { SubTypeId = "Spaced_Armour_FullBlock", Modifier = 0.95f, },
				new CustomBlocksDef { SubTypeId = "Spaced_Armour_1x2Base", Modifier = 0.95f, },
				new CustomBlocksDef { SubTypeId = "Spaced_Armour_45Angle", Modifier = 0.95f, },
				new CustomBlocksDef { SubTypeId = "Spaced_Armour_45Angle_Small", Modifier = 0.95f, },
				new CustomBlocksDef { SubTypeId = "External_Spaced_Armour_Full1x2Tip", Modifier = 0.95f, },
				 //new CustomBlocksDef { SubTypeId = "", Modifier = 0.9f, },
				 
				 //WACK Armor Cube/Wedge
				new CustomBlocksDef { SubTypeId = "WACK_Composite Armor Cube", Modifier = 0.65f, },
				new CustomBlocksDef { SubTypeId = "WACK_Composite Armor Slope Full", Modifier = 0.65f, },
				new CustomBlocksDef { SubTypeId = "WACK_Composite Armor Slope Interior", Modifier = 0.65f, },
				new CustomBlocksDef { SubTypeId = "WACK_Composite Armor Wedge", Modifier = 0.65f, },
				new CustomBlocksDef { SubTypeId = "WACK_Composite Armor Wedge Corner", Modifier = 0.65f, },
				new CustomBlocksDef { SubTypeId = "WACK_Composite Armor Wedge Inverse Corner", Modifier = 0.65f, },
				new CustomBlocksDef { SubTypeId = "WACK_Composite Armor Wedge 1x2 Corner Base", Modifier = 0.65f, },
				new CustomBlocksDef { SubTypeId = "WACK_Composite Armor Wedge 1x2 CornerBase", Modifier = 0.65f, },
				new CustomBlocksDef { SubTypeId = "WACK_Composite Armor Wedge 1x2 CornerTip", Modifier = 0.65f, },
				
				 //WACK Armor Spall Liner
				new CustomBlocksDef { SubTypeId = "WACK_Spall Liner Plate", Modifier = 1.5f, },
				new CustomBlocksDef { SubTypeId = "WACK_Spall Liner Plate Slope", Modifier = 1.5f, },
				new CustomBlocksDef { SubTypeId = "WACK_Spall Liner Plate 1x2 Slope Tip", Modifier = 1.5f, },
				new CustomBlocksDef { SubTypeId = "WACK_Spall Liner Plate 1x2 Slope Base", Modifier = 1.5f, },
				new CustomBlocksDef { SubTypeId = "WACK_Spall Liner Plate Center", Modifier = 1.5f, },
				new CustomBlocksDef { SubTypeId = "WACK_Spall Liner Spaced Armor", Modifier = 1.5f, },
				new CustomBlocksDef { SubTypeId = "WACK_Spall Liner Spaced Armor 1x2 Slope Base", Modifier = 1.5f, },				
				
				
            },
        };
    }
}