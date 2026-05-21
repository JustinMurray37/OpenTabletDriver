using System;
using System.Collections.Generic;
using System.Linq;
using OpenTabletDriver.Plugin;
using OpenTabletDriver.Plugin.Attributes;
using OpenTabletDriver.Plugin.Devices;

namespace OpenTabletDriver.Devices.Serial
{
    /// <summary>
    /// Surfaces user-configured serial tablets as device endpoints. There is no
    /// auto-detection: the hub returns exactly the entries listed in
    /// <see cref="ISerialDeviceConfigurationProvider"/>, mapping each to a known
    /// <see cref="SerialTabletType"/>.
    /// </summary>
    [DeviceHub]
    public class SerialDeviceHub : IDeviceHub
    {
        private readonly List<IDeviceEndpoint> _endpoints;

        public SerialDeviceHub(ISerialDeviceConfigurationProvider configurationProvider)
        {
            _endpoints = BuildEndpoints(configurationProvider).ToList();
            if (_endpoints.Count > 0)
                Log.Write(nameof(SerialDeviceHub),
                    $"Loaded {_endpoints.Count} serial tablet endpoint(s): " +
                    string.Join(", ", _endpoints.Select(e => e.FriendlyName)));
        }

        // Required by IDeviceHub. Serial endpoints are static for a daemon's
        // lifetime — no plug/unplug events.
        public event EventHandler<DevicesChangedEventArgs>? DevicesChanged
        {
            add { }
            remove { }
        }

        public IEnumerable<IDeviceEndpoint> GetDevices() => _endpoints;

        private static IEnumerable<IDeviceEndpoint> BuildEndpoints(ISerialDeviceConfigurationProvider provider)
        {
            foreach (var entry in provider.Devices)
            {
                if (string.IsNullOrWhiteSpace(entry.Type))
                {
                    Log.Write(nameof(SerialDeviceHub),
                        $"Serial config entry for port '{entry.Port}' has no Type; skipping.",
                        LogLevel.Warning);
                    continue;
                }

                if (string.IsNullOrWhiteSpace(entry.Port))
                {
                    Log.Write(nameof(SerialDeviceHub),
                        $"Serial config entry of type '{entry.Type}' has no Port; skipping.",
                        LogLevel.Warning);
                    continue;
                }

                if (!SerialTabletTypes.TryGet(entry.Type, out var type))
                {
                    Log.Write(nameof(SerialDeviceHub),
                        $"Unknown serial tablet type '{entry.Type}' for port '{entry.Port}'; skipping.",
                        LogLevel.Warning);
                    continue;
                }

                yield return new SerialDeviceEndpoint(type, entry.Port);
            }
        }
    }
}
