// WitM copy (owner 2026-11-04, D-068) of YtD 3431829641 KONTAKT Modifiers/ModifierChemical.cs: unchanged; the copied YtD ammo uses it.
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
		// bless you kmrii
        private CustomScalesDef ChemicalModifer => new CustomScalesDef
        {
            IgnoreAllOthers = false,
            Types = new[]
            {
                new CustomBlocksDef
                {
                    SubTypeId = "KGS_Composite_Chobham_Slant_Var1",
                    Modifier = 0.5f,
                },
				new CustomBlocksDef
                {
                    SubTypeId = "KGS_Composite_Chobham_Cube_Var1",
                    Modifier = 0.5f,
                },
				new CustomBlocksDef
                {
                    SubTypeId = "KGS_Composite_Chobham_HalfSlab",
                    Modifier = 0.5f,
                },
				
				new CustomBlocksDef
                {
                    SubTypeId = "KGS_Composite_DeU_Cladding_Var1",
                    Modifier = 1.5f,
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
                    Modifier = 0.8f,
                },
				new CustomBlocksDef
                {
                    SubTypeId = "C2",
                    Modifier = 0.95f,
                },
				new CustomBlocksDef
                {
                    SubTypeId = "C3",
                    Modifier = 0.1f,
                },
				new CustomBlocksDef
                {
                    SubTypeId = "ERAFlat",
                    Modifier = 0.6f,
                },

				new CustomBlocksDef { SubTypeId = "KGS_Armour_Slat_Plate", Modifier = 0.5f, },				
				new CustomBlocksDef { SubTypeId = "KGS_Armour_Slat_Plate_Centre", Modifier = 0.5f, },				
				new CustomBlocksDef { SubTypeId = "KGS_Armour_Slat_Half_1", Modifier = 0.5f, },				
				new CustomBlocksDef { SubTypeId = "KGS_Armour_Slat_Half_2", Modifier = 0.5f, },		
				new CustomBlocksDef { SubTypeId = "KGS_Armour_Slat_Half_Centred_1", Modifier = 0.5f,},					
				new CustomBlocksDef { SubTypeId = "KGS_Armour_Slat_Half_Centred_2", Modifier = 0.5f, },				
				new CustomBlocksDef { SubTypeId = "KGS_Armour_Slat_Slope", Modifier = 0.5f, },				
				new CustomBlocksDef { SubTypeId = "KGS_Armour_Slat_QuarterSlope", Modifier = 0.5f, },		
				new CustomBlocksDef { SubTypeId = "KGS_Armour_Slat_Plate_1x2_Tip_1", Modifier = 0.5f,},					
				new CustomBlocksDef { SubTypeId = "KGS_Armour_Slat_Plate_1x2_Base_1", Modifier = 0.5f, },					
				
				
				new CustomBlocksDef
                {
                    SubTypeId = "KGS_Armour_Cube_500mm", //get pranked idiot
                    Modifier = 50f,
                },							
				
				
				//I FUCKING LOVE DAMAGE MODIFIER TABLES (I REALLY DON'T) I WANT TO FUCKING
				new CustomBlocksDef { SubTypeId = "KGS_ERA_Stockade_1", Modifier = 0.5f, },					
				new CustomBlocksDef { SubTypeId = "KGS_ERA_Stockade_2", Modifier = 0.5f, },					
				new CustomBlocksDef { SubTypeId = "KGS_ERA_Stockade_3", Modifier = 0.5f, },		
				new CustomBlocksDef { SubTypeId = "KGS_ERA_Stockade_4", Modifier = 0.5f, },					
				
				new CustomBlocksDef { SubTypeId = "KGS_ERA_Plate_Centre", Modifier = 0.5f, },					
				new CustomBlocksDef { SubTypeId = "KGS_ERA_Plate_Flat", Modifier = 0.5f, },					
				new CustomBlocksDef { SubTypeId = "KGS_ERA_Half_Plate", Modifier = 0.5f, },		
				new CustomBlocksDef { SubTypeId = "KGS_ERA_Half_Plate_Flat", Modifier = 0.5f, },						
				new CustomBlocksDef { SubTypeId = "KGS_ERA_Brick", Modifier = 0.5f, },					
				new CustomBlocksDef { SubTypeId = "KGS_ERA_Brick_Offset", Modifier = 0.5f, },					
				new CustomBlocksDef { SubTypeId = "KGS_ERA_Brick_Double", Modifier = 0.5f, },		
				new CustomBlocksDef { SubTypeId = "KGS_ERA_Brick_DoubleSlope", Modifier = 0.5f, },					
				new CustomBlocksDef { SubTypeId = "KGS_ERA_Panel_Wedge", Modifier = 0.5f, },					
				new CustomBlocksDef { SubTypeId = "KGS_MalachitERA_01", Modifier = 0.5f, },					
				new CustomBlocksDef { SubTypeId = "KGS_MalachitERA_02", Modifier = 0.5f, },		
				new CustomBlocksDef { SubTypeId = "KGS_MalachitERA_03", Modifier = 0.5f, },					
				new CustomBlocksDef { SubTypeId = "KGS_MalachitERA_03mirror", Modifier = 0.5f, },					
				new CustomBlocksDef { SubTypeId = "KGS_MalachitERA_04", Modifier = 0.5f, },					
				new CustomBlocksDef { SubTypeId = "KGS_MalachitERA_04mirror", Modifier = 0.5f, },		
				new CustomBlocksDef { SubTypeId = "KGS_MalachitERA_05", Modifier = 0.5f, },					
				new CustomBlocksDef { SubTypeId = "KGS_MalachitERA_06", Modifier = 0.5f, },	
				new CustomBlocksDef { SubTypeId = "KGS_MalachitERA_4slope_1", Modifier = 0.5f, },					
				new CustomBlocksDef { SubTypeId = "KGS_MalachitERA_4slope_1mirror", Modifier = 0.5f, },					
				new CustomBlocksDef { SubTypeId = "KGS_MalachitERA_4slope_2", Modifier = 0.5f, },		
				new CustomBlocksDef { SubTypeId = "KGS_MalachitERA_4slope_2mirror", Modifier = 0.5f, },	
				new CustomBlocksDef { SubTypeId = "KGS_MalachitERA_4slope_3", Modifier = 0.5f, },					
				new CustomBlocksDef { SubTypeId = "KGS_MalachitERA_4slope_3mirror", Modifier = 0.5f, },					
				new CustomBlocksDef { SubTypeId = "KGS_MalachitERA_4slope_4", Modifier = 0.5f, },		
				new CustomBlocksDef { SubTypeId = "KGS_MalachitERA_4slope_4mirror", Modifier = 0.5f, },	
				new CustomBlocksDef { SubTypeId = "KGS_MalachitERA_8slope_1", Modifier = 0.5f, },					
				new CustomBlocksDef { SubTypeId = "KGS_MalachitERA_8slope_1mirror", Modifier = 0.5f, },					
				new CustomBlocksDef { SubTypeId = "KGS_MalachitERA_8slope_2", Modifier = 0.5f, },		
				new CustomBlocksDef { SubTypeId = "KGS_MalachitERA_8slope_2mirror", Modifier = 0.5f, },	
				new CustomBlocksDef { SubTypeId = "KGS_MalachitERA_8slope_3", Modifier = 0.5f, },					
				new CustomBlocksDef { SubTypeId = "KGS_MalachitERA_8slope_3mirror", Modifier = 0.5f, },					
				new CustomBlocksDef { SubTypeId = "KGS_MalachitERA_8slope_4", Modifier = 0.5f, },		
				new CustomBlocksDef { SubTypeId = "KGS_MalachitERA_8slope_4mirror", Modifier = 0.5f, },	
				new CustomBlocksDef { SubTypeId = "KGS_MalachitERA_8slope_5", Modifier = 0.5f, },					
				new CustomBlocksDef { SubTypeId = "KGS_MalachitERA_8slope_5mirror", Modifier = 0.5f, },					
				new CustomBlocksDef { SubTypeId = "KGS_MalachitERA_8slope_6", Modifier = 0.5f, },		
				new CustomBlocksDef { SubTypeId = "KGS_MalachitERA_8slope_6mirror", Modifier = 0.5f, },	
				new CustomBlocksDef { SubTypeId = "KGS_MalachitERA_8slope_7", Modifier = 0.5f, },					
				new CustomBlocksDef { SubTypeId = "KGS_MalachitERA_8slope_7mirror", Modifier = 0.5f, },					
				new CustomBlocksDef { SubTypeId = "KGS_MalachitERA_8slope_8", Modifier = 0.5f, },		
				new CustomBlocksDef { SubTypeId = "KGS_MalachitERA_8slope_8mirror", Modifier = 0.5f, },	
				
								//IAP Composite Screen Modifiers
				new CustomBlocksDef { SubTypeId = "2x1_Light_Composite_Screen_Front", Modifier = 0.8f, },
				new CustomBlocksDef { SubTypeId = "2x1_Light_Composite_Screen_Front_Mirrored", Modifier = 0.8f, },
				
				new CustomBlocksDef { SubTypeId = "2x2_Light_Composite_Screen_Front", Modifier = 0.8f, },
				new CustomBlocksDef { SubTypeId = "2x2_Light_Composite_Screen_Front_Mirrored", Modifier = 0.8f, },
				
				new CustomBlocksDef { SubTypeId = "2x3_Light_Composite_Screen_Front", Modifier = 0.8f, },
				new CustomBlocksDef { SubTypeId = "2x3_Light_Composite_Screen_Front_Mirrored", Modifier = 0.8f, },

				new CustomBlocksDef { SubTypeId = "2x4_Light_Composite_Screen_Front", Modifier = 0.8f, },
				new CustomBlocksDef { SubTypeId = "2x4_Light_Composite_Screen_Front_Mirrored", Modifier = 0.8f, },
				
				new CustomBlocksDef { SubTypeId = "2x6_Light_Composite_Screen_Front1", Modifier = 0.8f, },
				new CustomBlocksDef { SubTypeId = "2x6_Light_Composite_Screen_Front1_Mirrored", Modifier = 0.8f, },
				new CustomBlocksDef { SubTypeId = "2x6_Light_Composite_Screen_Front2", Modifier = 0.8f, },
				new CustomBlocksDef { SubTypeId = "2x6_Light_Composite_Screen_Front2_Mirrored", Modifier = 0.8f, },
				
				new CustomBlocksDef { SubTypeId = "2x8_Light_Composite_Screen_Front1", Modifier = 0.8f, },
				new CustomBlocksDef { SubTypeId = "2x8_Light_Composite_Screen_Front1_Mirrored", Modifier = 0.8f, },
				new CustomBlocksDef { SubTypeId = "2x8_Light_Composite_Screen_Front2", Modifier = 0.8f, },
				new CustomBlocksDef { SubTypeId = "2x8_Light_Composite_Screen_Front2_Mirrored", Modifier = 0.8f, },
				
				new CustomBlocksDef { SubTypeId = "2x1_Light_Composite_Screen", Modifier = 0.8f, },
				new CustomBlocksDef { SubTypeId = "2x2_Light_Composite_Screen", Modifier = 0.8f, },
				new CustomBlocksDef { SubTypeId = "2x3_Light_Composite_Screen", Modifier = 0.8f, },
				new CustomBlocksDef { SubTypeId = "2x4_Light_Composite_Screen", Modifier = 0.8f, },
				
				//IAP Spaced Armor
				new CustomBlocksDef { SubTypeId = "Spaced_Armour_HalfExternal", Modifier = 0.9f, },
				new CustomBlocksDef { SubTypeId = "Spaced_Armour_FullBlock", Modifier = 0.9f, },
				new CustomBlocksDef { SubTypeId = "Spaced_Armour_1x2Base", Modifier = 0.9f, },
				new CustomBlocksDef { SubTypeId = "Spaced_Armour_45Angle", Modifier = 0.9f, },
				new CustomBlocksDef { SubTypeId = "Spaced_Armour_45Angle_Small", Modifier = 0.9f, },
				new CustomBlocksDef { SubTypeId = "External_Spaced_Armour_Full1x2Tip", Modifier = 0.85f, },
				 //new CustomBlocksDef { SubTypeId = "", Modifier = 0.9f, },
				 
				 //WACK Armor Cube/Wedge
				new CustomBlocksDef { SubTypeId = "WACK_Composite Armor Cube", Modifier = 0.9f, },
				new CustomBlocksDef { SubTypeId = "WACK_Composite Armor Slope Full", Modifier = 0.9f, },
				new CustomBlocksDef { SubTypeId = "WACK_Composite Armor Slope Interior", Modifier = 0.9f, },
				new CustomBlocksDef { SubTypeId = "WACK_Composite Armor Wedge", Modifier = 0.9f, },
				new CustomBlocksDef { SubTypeId = "WACK_Composite Armor Wedge Corner", Modifier = 0.9f, },
				new CustomBlocksDef { SubTypeId = "WACK_Composite Armor Wedge Inverse Corner", Modifier = 0.85f, },
				new CustomBlocksDef { SubTypeId = "WACK_Composite Armor Wedge 1x2 Corner Base", Modifier = 0.85f, },
				new CustomBlocksDef { SubTypeId = "WACK_Composite Armor Wedge 1x2 CornerBase", Modifier = 0.85f, },
				new CustomBlocksDef { SubTypeId = "WACK_Composite Armor Wedge 1x2 CornerTip", Modifier = 0.85f, },
				
				 //WACK Armor Spall Liner
				new CustomBlocksDef { SubTypeId = "WACK_Spall Liner Plate", Modifier = 0.9f, },
				new CustomBlocksDef { SubTypeId = "WACK_Spall Liner Plate Slope", Modifier = 0.9f, },
				new CustomBlocksDef { SubTypeId = "WACK_Spall Liner Plate 1x2 Slope Tip", Modifier = 0.9f, },
				new CustomBlocksDef { SubTypeId = "WACK_Spall Liner Plate 1x2 Slope Base", Modifier = 0.9f, },
				new CustomBlocksDef { SubTypeId = "WACK_Spall Liner Plate Center", Modifier = 0.9f, },
				new CustomBlocksDef { SubTypeId = "WACK_Spall Liner Spaced Armor", Modifier = 0.85f, },
				new CustomBlocksDef { SubTypeId = "WACK_Spall Liner Spaced Armor 1x2 Slope Base", Modifier = 0.85f, },

            },
        };
    }
}