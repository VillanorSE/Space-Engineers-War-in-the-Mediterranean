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
        [ProtoMember(5)] public List<Truck> Trucks = new List<Truck>();
        [ProtoMember(6)] public List<Shipment> Shipments = new List<Shipment>();
    }

    // One port or airfield, keyed by its static-encounter spawn group, so it survives /witm refresh and respawns.
    [ProtoContract]
    public class SiteStock
    {
        [ProtoMember(1)] public string Site = "";
        // The Home box (D-047): everything the site spends reads it. Airfields have only this box.
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
        // Ports only: the Onward box, which only ships and trucks load from.
        [ProtoMember(9)] public int Onward;
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

    // A convoy truck charged to its port (SupplyConvoys); credits the airfield once, refunded if MES removes it intact before that.
    [ProtoContract]
    public class Truck
    {
        [ProtoMember(1)] public long EntityId;
        [ProtoMember(2)] public string Route = "";
        [ProtoMember(3)] public int Cost;
        [ProtoMember(4)] public bool Delivered;
    }

    // A cargo ship (charged to its HQ's Onward box) or a cargo plane (charged to its airfield); refunded if MES removes it
    // intact before it delivers.
    [ProtoContract]
    public class Shipment
    {
        [ProtoMember(1)] public long EntityId;
        [ProtoMember(2)] public string Origin = "";
        // Ships: the destination port's spawn group. Planes: "".
        [ProtoMember(3)] public string Destination = "";
        [ProtoMember(4)] public int Cost;
        [ProtoMember(5)] public int Load;
        [ProtoMember(6)] public bool Delivered;
        [ProtoMember(7)] public bool Lost;
        [ProtoMember(8)] public bool Plane;
    }
}
