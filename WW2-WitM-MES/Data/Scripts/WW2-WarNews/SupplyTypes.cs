using System.Collections.Generic;
using ProtoBuf;

namespace WW2WitM.Supply
{
    // Saved in the sandbox variable SupplySession.StateVariable. protobuf does not write false/0, so no field may have a
    // non-default initializer (knowledge protobuf_false_not_saved): a stock of 0 must reload as 0.
    [ProtoContract]
    public class SupplyState
    {
        [ProtoMember(1)] public int Version;
        [ProtoMember(2)] public double LastHourSeconds;
        [ProtoMember(3)] public List<SiteStock> Sites = new List<SiteStock>();
        [ProtoMember(4)] public List<Sortie> Sorties = new List<Sortie>();
    }

    // One port or airfield, keyed by its static-encounter spawn group, so it survives /witm refresh and respawns.
    [ProtoContract]
    public class SiteStock
    {
        [ProtoMember(1)] public string Site = "";
        [ProtoMember(2)] public int Stock;
        // "Gray", "Green", or "" for unowned. Ports keep their fixed faction.
        [ProtoMember(3)] public string Owner = "";
        // SupplySites.Normal / Low / Full.
        [ProtoMember(4)] public int Band;
        [ProtoMember(5)] public double LastNewsSeconds;
        [ProtoMember(6)] public string LastOwner = "";
        [ProtoMember(7)] public bool EverOwned;
        // True after a Low headline, until the matching "getting through again" one.
        [ProtoMember(8)] public bool NewsLow;
    }

    // A spawned sortie plane charged to an airfield; refunded if MES despawns it intact.
    [ProtoContract]
    public class Sortie
    {
        [ProtoMember(1)] public long EntityId;
        [ProtoMember(2)] public string Site = "";
        [ProtoMember(3)] public int Cost;
        [ProtoMember(4)] public bool Lost;
    }
}
