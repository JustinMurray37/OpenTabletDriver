using System.Numerics;
using OpenTabletDriver.Plugin.Tablet;

namespace OpenTabletDriver.Configurations.Parsers.Summagraphics
{
    /// <summary>
    /// MM-format report shape. The MM wire format has no pressure and at most
    /// four buttons; the UIOF/Microgrid format (not yet implemented) has more
    /// capabilities and will need its own report type.
    /// </summary>
    public struct SummaSketchMMTabletReport : ITabletReport, IProximityReport
    {
        public SummaSketchMMTabletReport(byte[] raw, SummaSketchMMReport decoded)
        {
            Raw = raw;
            Position = new Vector2(decoded.X, decoded.Y);
            Pressure = 0;
            PenButtons =
            [
                (decoded.Buttons & 0b0001) != 0,
                (decoded.Buttons & 0b0010) != 0,
                (decoded.Buttons & 0b0100) != 0,
                (decoded.Buttons & 0b1000) != 0,
            ];
            NearProximity = decoded.InProximity;
            HoverDistance = 0;
        }

        public byte[] Raw { set; get; }
        public Vector2 Position { set; get; }
        public uint Pressure { set; get; }
        public bool[] PenButtons { set; get; }
        public bool NearProximity { set; get; }
        public uint HoverDistance { set; get; }
    }
}
