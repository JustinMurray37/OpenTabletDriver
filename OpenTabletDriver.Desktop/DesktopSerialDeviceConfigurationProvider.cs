using System.Collections.Generic;
using System.IO;
using OpenTabletDriver.Devices.Serial;
using OpenTabletDriver.Plugin;

namespace OpenTabletDriver.Desktop
{
    /// <summary>
    /// Reads <c>$AppData/OpenTabletDriver/serial-tablets.json</c>. Missing or
    /// unreadable file is not an error — the resulting <see cref="Devices"/>
    /// list is just empty, and the daemon proceeds as if no serial tablets
    /// were configured.
    /// </summary>
    public class DesktopSerialDeviceConfigurationProvider : ISerialDeviceConfigurationProvider
    {
        public const string ConfigFileName = "serial-tablets.json";

        public IEnumerable<SerialDeviceConfiguration> Devices => _devices ??= Load();

        private List<SerialDeviceConfiguration>? _devices;

        private static List<SerialDeviceConfiguration> Load()
        {
            var path = Path.Join(AppInfo.Current.AppDataDirectory, ConfigFileName);
            if (!File.Exists(path))
                return new List<SerialDeviceConfiguration>();

            try
            {
                var file = new FileInfo(path);
                var parsed = Serialization.Deserialize<SerialDeviceConfigurationFile>(file);
                Log.Write(nameof(DesktopSerialDeviceConfigurationProvider),
                    $"Loaded {parsed?.Devices.Count ?? 0} serial tablet entries from '{path}'.");
                return parsed?.Devices ?? new List<SerialDeviceConfiguration>();
            }
            catch (System.Exception ex)
            {
                Log.Write(nameof(DesktopSerialDeviceConfigurationProvider),
                    $"Failed to load '{path}': {ex.Message}", LogLevel.Error);
                return new List<SerialDeviceConfiguration>();
            }
        }
    }
}
