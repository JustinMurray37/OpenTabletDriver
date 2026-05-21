using System;

namespace OpenTabletDriver.Configurations.Parsers.Summagraphics
{
    /// <summary>
    /// Decoded SummaSketch MM-format report. All fields are derived from a single
    /// 5-byte packet. No OTD dependencies — designed to be unit-tested in isolation.
    /// </summary>
    public readonly struct SummaSketchMMReport
    {
        public int X { get; }
        public int Y { get; }
        public bool InProximity { get; }
        public bool XPositive { get; }
        public bool YPositive { get; }
        public bool TabletId { get; }
        public int Buttons { get; }
        public int RawFlagBits { get; }

        public SummaSketchMMReport(int x, int y, bool inProximity, bool xPositive, bool yPositive,
                                   bool tabletId, int buttons, int rawFlagBits)
        {
            X = x;
            Y = y;
            InProximity = inProximity;
            XPositive = xPositive;
            YPositive = yPositive;
            TabletId = tabletId;
            Buttons = buttons;
            RawFlagBits = rawFlagBits;
        }
    }

    /// <summary>
    /// Stateless decoder for SummaSketch MM/SummaSketch binary report packets.
    ///
    /// Packet layout (5 bytes, per Appendix H of the SummaSketch III manual):
    ///
    ///   Byte 1: PH PR T  Sx Sy Fc Fb Fa   (PH = phasing bit, always 1 on byte 1)
    ///   Byte 2: 0  X6 X5 X4 X3 X2 X1 X0
    ///   Byte 3: 0  X13 X12 X11 X10 X9 X8 X7
    ///   Byte 4: 0  Y6 Y5 Y4 Y3 Y2 Y1 Y0
    ///   Byte 5: 0  Y13 Y12 Y11 Y10 Y9 Y8 Y7
    ///
    /// PR is 0 when in proximity, 1 when out of proximity (note inverted sense).
    /// Sx/Sy: 1 = positive, 0 = negative. Always positive in absolute mode.
    /// </summary>
    public static class SummaSketchMMParser
    {
        public const int PacketSize = 5;

        public static bool IsFirstByte(byte b) => (b & 0x80) != 0;

        public static bool IsValidFraming(ReadOnlySpan<byte> packet)
        {
            if (packet.Length < PacketSize) return false;
            if ((packet[0] & 0x80) == 0) return false;
            for (int i = 1; i < PacketSize; i++)
                if ((packet[i] & 0x80) != 0) return false;
            return true;
        }

        public static SummaSketchMMReport Parse(ReadOnlySpan<byte> packet)
        {
            if (packet.Length < PacketSize)
                throw new ArgumentException(
                    $"MM packet requires {PacketSize} bytes, got {packet.Length}.", nameof(packet));

            if (!IsValidFraming(packet))
                throw new ArgumentException(
                    "Invalid packet framing: phasing bit must be set on byte 0 and cleared on bytes 1–4.",
                    nameof(packet));

            byte b0 = packet[0];

            bool inProximity = (b0 & 0x40) == 0;
            bool tabletId    = (b0 & 0x20) != 0;
            bool xPositive   = (b0 & 0x10) != 0;
            bool yPositive   = (b0 & 0x08) != 0;
            int  flagBits    = b0 & 0x07;

            int x = (packet[1] & 0x7F) | ((packet[2] & 0x7F) << 7);
            int y = (packet[3] & 0x7F) | ((packet[4] & 0x7F) << 7);

            int buttons = DecodeButtons(flagBits);

            return new SummaSketchMMReport(x, y, inProximity, xPositive, yPositive,
                                           tabletId, buttons, flagBits);
        }

        public static bool TryParse(ReadOnlySpan<byte> packet, out SummaSketchMMReport report)
        {
            if (packet.Length < PacketSize || !IsValidFraming(packet))
            {
                report = default;
                return false;
            }
            report = Parse(packet);
            return true;
        }

        // The 3-bit Fc/Fb/Fa field cannot uniquely encode all 16 combinations of a
        // 4-button cursor; the manual's table is what the tablet sends, so we map
        // each code to the simplest combination that produces it. Users who need
        // unambiguous 16-button input would have to switch the tablet to UIOF format.
        public static int DecodeButtons(int flagBits)
        {
            switch (flagBits & 0x07)
            {
                case 0b000: return 0b0000;
                case 0b001: return 0b0001;
                case 0b010: return 0b0010;
                case 0b011: return 0b0100;
                case 0b100: return 0b1000;
                case 0b101: return 0b1001;
                case 0b110: return 0b1010;
                case 0b111: return 0b1011;
                default: return 0;
            }
        }
    }
}
