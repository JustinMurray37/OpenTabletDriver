using System.Collections.Generic;
using OpenTabletDriver.Plugin.Devices;

namespace OpenTabletDriver.Devices.Serial
{
    /// <summary>
    /// Synthetic endpoint representing a user-configured serial tablet. The
    /// driver's existing matching logic treats this just like a HID endpoint;
    /// the VID/PID are sentinel values claimed by <see cref="SerialTabletType"/>.
    /// </summary>
    internal sealed class SerialDeviceEndpoint : IDeviceEndpoint
    {
        private readonly SerialTabletType _type;
        private readonly string _portName;

        public SerialDeviceEndpoint(SerialTabletType type, string portName)
        {
            _type = type;
            _portName = portName;
        }

        public int ProductID => SerialTabletProductIds.SerialProductId;
        public int VendorID => SerialTabletProductIds.SerialVendorId;
        public int InputReportLength => _type.InputReportLength;
        public int OutputReportLength => -1;
        public int FeatureReportLength => -1;

        public string? Manufacturer => _type.Manufacturer;
        public string? ProductName => _type.ProductName;
        public string? FriendlyName => $"{_type.ProductName} ({_portName})";
        public string? SerialNumber => string.Empty;

        public string DevicePath => _portName;
        public bool CanOpen => true;
        public IDictionary<string, string> DeviceAttributes { get; } = new Dictionary<string, string>();

        public IDeviceEndpointStream Open() => new SerialDeviceEndpointStream(_type, _portName);

        // Index 0 returns the tablet type's Name so that a TabletConfiguration
        // can discriminate between serial models via its DeviceStrings regex —
        // the same mechanism HID configurations use for product-name matching.
        public string? GetDeviceString(byte index) => index == 0 ? _type.Name : null;
    }
}
