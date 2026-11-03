namespace Scripts
{
    partial class Parts
    {
        internal Parts()
        {
            // WitM overrides of third-party WeaponCore weapons. Each one keeps the original PartName and hardware type
            // and sets a higher HardPoint.DefinitionPriority, so WeaponCore replaces the original (SessionInit.cs).
            PartDefinitions(
                WitM_KGS_Coaxial_MG
            );
        }
    }
}
