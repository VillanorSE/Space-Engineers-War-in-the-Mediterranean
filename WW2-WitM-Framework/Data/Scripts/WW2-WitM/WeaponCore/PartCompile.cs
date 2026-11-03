using VRageMath;
using static Scripts.Structure;
using static Scripts.Structure.WeaponDefinition.AmmoDef;

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
    }
}
