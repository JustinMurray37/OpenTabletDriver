using System;
using System.IO;
using System.IO.Ports;
using OpenTabletDriver.Plugin.Devices;

namespace OpenTabletDriver.Devices.Serial
{
    /// <summary>
    /// Wraps a <see cref="SerialPort"/> for a single tablet type. Each call to
    /// <see cref="Read"/> returns exactly one framed packet, so the standard
    /// <c>DeviceReader</c> loop doesn't need to know the underlying transport.
    /// </summary>
    internal sealed class SerialDeviceEndpointStream : IDeviceEndpointStream
    {
        private readonly SerialPort _port;
        private readonly SerialTabletType _type;
        private bool _disposed;

        public SerialDeviceEndpointStream(SerialTabletType type, string portName)
        {
            _type = type;
            _port = _type.CreatePort(portName);
            try
            {
                _port.Open();
                _type.Initialize(_port);
            }
            catch
            {
                _port.Dispose();
                throw;
            }
        }

        public byte[] Read()
        {
            if (_disposed) throw new ObjectDisposedException(nameof(SerialDeviceEndpointStream));
            return _type.ReadPacket(_port);
        }

        public void Write(byte[] buffer)
        {
            if (_disposed) throw new ObjectDisposedException(nameof(SerialDeviceEndpointStream));
            _port.Write(buffer, 0, buffer.Length);
        }

        public void GetFeature(byte[] buffer) =>
            throw new NotSupportedException("Serial endpoints do not implement HID feature reports.");

        public void SetFeature(byte[] buffer) =>
            throw new NotSupportedException("Serial endpoints do not implement HID feature reports.");

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            try
            {
                if (_port.IsOpen)
                    _port.Close();
            }
            catch (IOException)
            {
                // Closing a port that's already gone (USB unplug) commonly throws; nothing to do.
            }
            _port.Dispose();
        }
    }
}
