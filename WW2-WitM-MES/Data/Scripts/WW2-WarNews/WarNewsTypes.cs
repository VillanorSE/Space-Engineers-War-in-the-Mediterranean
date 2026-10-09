using System.Collections.Generic;
using ProtoBuf;

namespace WW2WitM.WarNewsSystem
{
    // One bulletin entry. Stamp and text are formatted on the server, so clients only display them.
    [ProtoContract]
    public class NewsItem
    {
        [ProtoMember(1)] public string Stamp = "";
        [ProtoMember(2)] public string Text = "";
        [ProtoMember(3)] public string Faction = "";
    }

    // A GPS marker shown to every player until EndSeconds (WarNewsSession.Now(), the saved game clock). Countdown markers carry the time left in their name.
    [ProtoContract]
    public class MarkerState
    {
        [ProtoMember(1)] public string Key = "";
        [ProtoMember(2)] public string Name = "";
        [ProtoMember(3)] public string Description = "";
        [ProtoMember(4)] public double X;
        [ProtoMember(5)] public double Y;
        [ProtoMember(6)] public double Z;
        [ProtoMember(7)] public double EndSeconds;
        [ProtoMember(8)] public bool Countdown;
        [ProtoMember(9)] public uint Color;
        [ProtoMember(10)] public string ShownLabel = "";
    }

    // A marker name that was removed; players who were offline at the time get it purged when they next appear.
    [ProtoContract]
    public class RetiredName
    {
        [ProtoMember(1)] public string Name = "";
        [ProtoMember(2)] public double UntilSeconds;
    }

    // Everything the server saves in the sandbox variable WarNewsSession.StateVariable.
    [ProtoContract]
    public class WarNewsState
    {
        [ProtoMember(1)] public List<NewsItem> Items = new List<NewsItem>();
        [ProtoMember(2)] public List<MarkerState> Markers = new List<MarkerState>();
        [ProtoMember(3)] public List<RetiredName> Retired = new List<RetiredName>();
        // Clock the times are on: 0 = ElapsedPlayTime (reset every load; replaced 2026-10-06), 2 = GameDateTime.
        [ProtoMember(4)] public int Clock;
    }

    // Network packet on WarNewsSession.NetChannel. Kind: 1 full feed (server to one client), 2 one new item (server to all), 3 feed request (client to server).
    [ProtoContract]
    public class WarNewsPacket
    {
        [ProtoMember(1)] public int Kind;
        [ProtoMember(2)] public List<NewsItem> Items = new List<NewsItem>();
    }
}
