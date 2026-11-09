namespace Scripts
{
    partial class Parts
    {
        internal Parts()
        {
            // WitM overrides of third-party WeaponCore weapons. Each one keeps the original PartName and hardware type
            // and sets a higher HardPoint.DefinitionPriority, so WeaponCore replaces the original (SessionInit.cs).
            PartDefinitions(
                WitM_KGS_Coaxial_MG,
                // YtD tank main guns, Threats + Characters (D-068)
                YtD_CC5438_Gun,
                SHA_Sh37_Gun,
                YtD_SC2037_autocannon,
                // CAP 20 mm (Panhard 178, AB41), Threats + Characters (D-068)
                WitM_CAP_20mmAircraftCannon,
                // WitM's own blocks
                WitM_Breda38_Twin
            );
        }
    }
}
