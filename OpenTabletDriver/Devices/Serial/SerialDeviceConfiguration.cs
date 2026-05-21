using System.Collections.Generic;

namespace OpenTabletDriver.Devices.Serial
{
    /// <summary>
    /// Root object of the serial-tablets.json config file.
    /// </summary>
    public class SerialDeviceConfigurationFile
    {
        public List<SerialDeviceConfiguration> Devices { get; set; } = new();
    }

    /// <summary>
    /// One entry in the serial config file. Identifies a serial tablet by its
    /// type (which selects framing/init code in <see cref="SerialTabletTypes"/>)
    /// and the OS-specific port path.
    /// </summary>
    public class SerialDeviceConfiguration
    {
        public string Type { get; set; } = string.Empty;
        public string Port { get; set; } = string.Empty;
    }
}
