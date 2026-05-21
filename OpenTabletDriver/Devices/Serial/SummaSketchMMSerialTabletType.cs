using System;
using System.IO;
using System.IO.Ports;
using System.Text;
using System.Threading;
using OpenTabletDriver.Configurations.Parsers.Summagraphics;

namespace OpenTabletDriver.Devices.Serial
{
    /// <summary>
    /// Common base for SummaSketch MM-format serial tablets. Holds the shared
    /// UART settings, 5-byte phasing-bit framing, and command-send helpers.
    /// The init sequence is owned by each concrete subclass — different
    /// SummaSketch models honor different subsets of the MM command set, and
    /// a one-size-fits-all <c>Initialize</c> doesn't apply cleanly.
    /// </summary>
    public abstract class SummaSketchMMSerialTabletType : SerialTabletType
    {
        public override int InputReportLength => SummaSketchMMParser.PacketSize;
        public override string Manufacturer => "Summagraphics";

        public override SerialPort CreatePort(string portName)
        {
            return new SerialPort(portName, 9600, Parity.Odd, 8, StopBits.One)
            {
                ReadTimeout = 500,
                WriteTimeout = 500,
                Handshake = Handshake.None,
                // Some USB-serial adapters need DTR/RTS asserted for the tablet to send.
                DtrEnable = true,
                RtsEnable = true,
            };
        }

        public override byte[] ReadPacket(SerialPort port)
        {
            var packet = new byte[SummaSketchMMParser.PacketSize];
            int index = 0;

            while (true)
            {
                int b;
                try
                {
                    b = port.ReadByte();
                }
                catch (TimeoutException)
                {
                    // No data this tick. Loop and try again; the surrounding
                    // DeviceReader checks Connected/cancellation, not us.
                    continue;
                }

                if (b < 0)
                    throw new IOException("I/O disconnected.");

                byte by = (byte)b;
                bool isFirstByte = SummaSketchMMParser.IsFirstByte(by);

                if (index == 0)
                {
                    // Skip noise until the phasing bit signals a packet boundary.
                    if (!isFirstByte)
                        continue;
                    packet[0] = by;
                    index = 1;
                }
                else if (isFirstByte)
                {
                    // Lost sync mid-packet. Restart from this byte.
                    packet[0] = by;
                    index = 1;
                }
                else
                {
                    packet[index++] = by;
                    if (index == SummaSketchMMParser.PacketSize)
                        return packet;
                }
            }
        }

        protected static void SendCommand(SerialPort port, byte command)
        {
            port.Write(new[] { command }, 0, 1);
        }

        protected static void SendCommand(SerialPort port, byte[] command)
        {
            port.Write(command, 0, command.Length);
        }
    }

    /// <summary>SummaSketch III Professional in MM format — 18″ × 12″ active area at 910 lpi.</summary>
    public sealed class SummaSketchIIIProfessionalMMSerialTabletType : SummaSketchMMSerialTabletType
    {
        public const string TypeName = "SummaSketchIIIProfessionalMM";

        public override string Name => TypeName;
        public override string ProductName => "SummaSketch III Professional (MM)";

        public override void Initialize(SerialPort port)
        {
            // Reset (NUL). The tablet takes a moment to come back up.
            SendCommand(port, 0x00);
            Thread.Sleep(250);
            port.DiscardInBuffer();

            SendCommand(port, (byte)'F');                       // absolute coordinates
            Thread.Sleep(50);
            SendCommand(port, (byte)'b');                       // origin upper-left
            Thread.Sleep(50);
            SendCommand(port, Encoding.ASCII.GetBytes("zb"));   // binary report format
            Thread.Sleep(50);

            // 'r' command with explicit per-axis max counts:
            // X = 18 × 910 = 16380 (0x3FFC), Y = 12 × 910 = 10920 (0x2AA8).
            SendCommand(port, new byte[] { (byte)'r', 0xFC, 0x3F, 0xA8, 0x2A });
            Thread.Sleep(100);

            SendCommand(port, (byte)'@');                       // stream mode
            Thread.Sleep(50);
            SendCommand(port, (byte)'Q');                       // 110 reports/sec (max)
            Thread.Sleep(50);
        }
    }

    /// <summary>SummaSketch III in MM format — 12″ × 12″ active area at 1000 lpi.</summary>
    public sealed class SummaSketchIIIMMSerialTabletType : SummaSketchMMSerialTabletType
    {
        public const string TypeName = "SummaSketchIIIMM";

        public override string Name => TypeName;
        public override string ProductName => "SummaSketch III (MM)";

        public override void Initialize(SerialPort port)
        {
            // Reset (NUL). The tablet takes a moment to come back up.
            SendCommand(port, 0x00);
            Thread.Sleep(250);
            port.DiscardInBuffer();

            SendCommand(port, (byte)'F');                       // absolute coordinates
            Thread.Sleep(50);
            SendCommand(port, (byte)'b');                       // origin upper-left
            Thread.Sleep(50);
            SendCommand(port, Encoding.ASCII.GetBytes("zb"));   // binary report format
            Thread.Sleep(50);

            // This model ignores 'r' definable-resolution; use the 'j' preset
            // which selects 1000 lpi on both axes.
            SendCommand(port, (byte)'j');
            Thread.Sleep(100);

            SendCommand(port, (byte)'@');                       // stream mode
            Thread.Sleep(50);
            SendCommand(port, (byte)'Q');                       // 110 reports/sec (max)
            Thread.Sleep(50);
        }
    }
}
