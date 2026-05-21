using System.Collections.Generic;

namespace OpenTabletDriver.Devices.Serial
{
    /// <summary>
    /// Provides the list of serial-tablet entries declared by the user. The core
    /// implementation returns nothing; the desktop project supplies the real one
    /// which reads from the user's AppData directory.
    /// </summary>
    public interface ISerialDeviceConfigurationProvider
    {
        IEnumerable<SerialDeviceConfiguration> Devices { get; }
    }

    public sealed class EmptySerialDeviceConfigurationProvider : ISerialDeviceConfigurationProvider
    {
        public IEnumerable<SerialDeviceConfiguration> Devices => System.Array.Empty<SerialDeviceConfiguration>();
    }
}
