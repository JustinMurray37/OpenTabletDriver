using System.IO.Ports;

namespace OpenTabletDriver.Devices.Serial
{
    /// <summary>
    /// Tablet-type specific behaviour for a serial tablet: the wire format's
    /// framing and the init sequence sent on open. All serial tablets share
    /// the sentinel VID/PID in <see cref="SerialTabletProductIds"/>; per-model
    /// binding to a <see cref="OpenTabletDriver.Plugin.Tablet.TabletConfiguration"/>
    /// is done by matching the <see cref="Name"/> via the configuration's
    /// <c>DeviceStrings</c> field.
    /// </summary>
    public abstract class SerialTabletType
    {
        /// <summary>
        /// The string a user puts in the <c>"Type"</c> field of their
        /// serial-tablets.json entry, and the value the matching
        /// <c>TabletConfiguration</c> JSON expects under
        /// <c>DeviceStrings[0]</c>.
        /// </summary>
        public abstract string Name { get; }

        /// <summary>
        /// Size of a single framed report this type emits from <see cref="ReadPacket"/>.
        /// Surfaced to OTD via <c>IDeviceEndpoint.InputReportLength</c> so tablet
        /// configurations can constrain matches on it like they do for HID.
        /// </summary>
        public abstract int InputReportLength { get; }

        /// <summary>
        /// Human-readable manufacturer/product names, used only for logging and UI.
        /// </summary>
        public abstract string Manufacturer { get; }
        public abstract string ProductName { get; }

        /// <summary>
        /// Build (but do not open) the <see cref="SerialPort"/> for this tablet
        /// type with the correct UART settings.
        /// </summary>
        public abstract SerialPort CreatePort(string portName);

        /// <summary>
        /// Run the tablet-specific initialization sequence over an already-open port.
        /// May block while it sleeps between commands.
        /// </summary>
        public abstract void Initialize(SerialPort port);

        /// <summary>
        /// Block until one complete framed report is available, then return it.
        /// Implementations are expected to resync on bad framing internally rather
        /// than throwing, so the caller can treat each return value as a valid packet.
        /// </summary>
        public abstract byte[] ReadPacket(SerialPort port);
    }
}
