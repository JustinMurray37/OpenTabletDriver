namespace OpenTabletDriver.Devices.Serial
{
    /// <summary>
    /// Sentinel USB identifiers used by all serial tablets.
    ///
    /// Serial endpoints don't have real USB-IF identifiers, so OTD's matching
    /// layer needs *something* to compare against. All serial tablets share
    /// the same synthetic VID + PID; per-model discrimination is done via
    /// <c>DeviceStrings</c> in the <c>TabletConfiguration</c> JSON, matched
    /// against the type name returned by
    /// <see cref="SerialDeviceEndpoint.GetDeviceString"/>.
    /// </summary>
    public static class SerialTabletProductIds
    {
        /// <summary>
        /// Synthetic VendorID shared by all serial tablets. Chosen to fall
        /// outside USB-IF assignments so it can't collide with real hardware.
        /// </summary>
        public const int SerialVendorId = 0xFFFE;

        /// <summary>
        /// Synthetic ProductID shared by all serial tablets. Discrimination
        /// between models is done by matching <c>DeviceStrings</c>, not by PID.
        /// </summary>
        public const int SerialProductId = 0x0001;
    }
}
