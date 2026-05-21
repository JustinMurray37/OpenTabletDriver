using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using OpenTabletDriver.Plugin;

namespace OpenTabletDriver.Devices.Serial
{
    /// <summary>
    /// Registry of known serial tablet types. Concrete (non-abstract) subclasses
    /// of <see cref="SerialTabletType"/> with a public parameterless constructor
    /// are discovered automatically — adding a new tablet means writing a class,
    /// not editing this file.
    ///
    /// Lookup is case-insensitive on <see cref="SerialTabletType.Name"/>, which is
    /// the string a user puts in <c>"Type"</c> in their serial-tablets.json entry.
    /// </summary>
    public static class SerialTabletTypes
    {
        private static readonly Dictionary<string, SerialTabletType> _types = Discover();

        public static bool TryGet(string name, out SerialTabletType type)
        {
            return _types.TryGetValue(name, out type!);
        }

        public static IEnumerable<SerialTabletType> All => _types.Values;

        private static Dictionary<string, SerialTabletType> Discover()
        {
            var types = new Dictionary<string, SerialTabletType>(StringComparer.OrdinalIgnoreCase);

            foreach (var t in Assembly.GetExecutingAssembly().DefinedTypes
                .Where(t => !t.IsAbstract
                            && typeof(SerialTabletType).IsAssignableFrom(t)
                            && t.GetConstructor(Type.EmptyTypes) != null))
            {
                SerialTabletType instance;
                try
                {
                    instance = (SerialTabletType)Activator.CreateInstance(t)!;
                }
                catch (Exception ex)
                {
                    Log.Write(nameof(SerialTabletTypes),
                        $"Failed to instantiate serial tablet type '{t.FullName}': {ex.Message}",
                        LogLevel.Error);
                    continue;
                }

                if (types.TryGetValue(instance.Name, out var existing))
                {
                    Log.Write(nameof(SerialTabletTypes),
                        $"Duplicate serial tablet type Name '{instance.Name}': '{t.FullName}' " +
                        $"collides with '{existing.GetType().FullName}'. Keeping the first.",
                        LogLevel.Warning);
                    continue;
                }

                types[instance.Name] = instance;
            }

            return types;
        }
    }
}
