# Adding a serial tablet

Serial tablets bypass the HID device hub and are described by a
[`SerialTabletType`](SerialTabletType.cs) subclass that owns three things:
UART settings, the model's init sequence, and the framing logic that turns a
stream of bytes into one report per `ReadPacket` call. The standard
`DeviceReader` pipeline does the rest; once an endpoint is constructed, the
rest of OTD treats it like any other tablet.

All serial tablets share a single synthetic VID/PID — defined in
[`SerialTabletProductIds`](SerialTabletProductIds.cs) — and are bound to
their `TabletConfiguration` by name via the configuration's
`DeviceStrings` field. Authors never allocate identifiers.

This recipe walks through adding a new model, using the existing
[`SummaSketchIIIMMSerialTabletType`](SummaSketchMMSerialTabletType.cs) as a
reference implementation.

### Naming when a tablet has multiple wire formats

Many serial tablets support more than one wire format (e.g. SummaSketch
supports both MM and UIOF/Microgrid). When that's the case, embed the
format suffix in every user-facing name: the type's `Name`, its
`ProductName`, the JSON filename, and the JSON's `Name` field. The
existing SummaSketch entries follow this convention:

- `SummaSketchIIIProfessionalMM` (MM-format Pro) lives in
  `SummaSketch III Professional MM.json`. A future UIOF implementation
  would be `SummaSketchIIIProfessionalUIOF` in
  `SummaSketch III Professional UIOF.json`.
- The C# class hierarchy mirrors this: a per-format abstract base
  (`SummaSketchMMSerialTabletType`) holds shared protocol code; the
  concrete subclasses carry only per-model identity and overrides.

## 1. Write the `SerialTabletType` subclass

In [`OpenTabletDriver/Devices/Serial/`](.), create a class deriving from
`SerialTabletType` (or from a shared protocol base, like
`SummaSketchMMSerialTabletType`, if multiple models share framing/init).

```csharp
public sealed class MyTabletType : SerialTabletType
{
    public const string TypeName = "MyTablet";

    public override string Name              => TypeName;
    public override int    InputReportLength => 7;       // bytes per framed report
    public override string Manufacturer      => "Acme";
    public override string ProductName       => "Acme Doodler";

    public override SerialPort CreatePort(string portName) =>
        new SerialPort(portName, 9600, Parity.None, 8, StopBits.One)
        {
            ReadTimeout = 500, WriteTimeout = 500,
            DtrEnable = true, RtsEnable = true,
        };

    public override void Initialize(SerialPort port)
    {
        // send init bytes, sleep between commands, etc.
    }

    public override byte[] ReadPacket(SerialPort port)
    {
        // block until one full framed report is assembled, then return it
        // resync internally on bad framing — don't throw on transient noise
    }
}
```

The registry in [`SerialTabletTypes.cs`](SerialTabletTypes.cs) discovers
non-abstract subclasses by reflection, so the class is picked up
automatically — no central list to edit. Two subclasses with the same
`Name` are detected at startup and logged.

A few constraints worth knowing:

- `ReadPacket` must be self-resyncing. The surrounding `DeviceReader`
  loop catches a few exception types but treats anything else as a
  device disconnect.
- `CreatePort` must not open the port; the stream constructor opens it
  and then calls `Initialize`.
- `Initialize` runs synchronously on the device-reader thread, so any
  `Thread.Sleep` between commands directly delays the tablet's first
  reported sample. Keep it reasonable.

## 2. Write the report parser

OTD's report pipeline expects an `IReportParser<IDeviceReport>` returning
some flavor of `ITabletReport` (and optionally `IProximityReport`,
`IEraserReport`, etc.). The parser turns the bytes from `ReadPacket`
into an OTD-shaped report.

Drop your parser, report struct, and any wire-format-specific decoder
into `OpenTabletDriver.Configurations/Parsers/<Vendor>/`. Same workflow
as adding a parser for a HID tablet — see
[`OpenTabletDriver.Configurations/Parsers/Summagraphics/`](../../../OpenTabletDriver.Configurations/Parsers/Summagraphics/)
for a working example.

## 3. Write the TabletConfiguration JSON

Add a file under `OpenTabletDriver.Configurations/Configurations/<Vendor>/`.
The `VendorID` and `ProductID` are always the shared sentinel values
(`65534` and `1`); model discrimination happens via the `DeviceStrings`
field, which regex-matches against the `Name` you declared on the
`SerialTabletType` subclass.

```json
{
  "Name": "Acme Doodler",
  "Specifications": {
    "Digitizer": { "Width": 304.8, "Height": 304.8, "MaxX": 12000, "MaxY": 12000 },
    "Pen": { "MaxPressure": 0, "ButtonCount": 2 }
  },
  "DigitizerIdentifiers": [
    {
      "VendorID": 65534,
      "ProductID": 1,
      "InputReportLength": 7,
      "ReportParser": "OpenTabletDriver.Configurations.Parsers.Acme.MyTabletReportParser",
      "DeviceStrings": { "0": "^MyTablet$" }
    }
  ]
}
```

The `^...$` anchors are important — without them, `"MyTablet"` would
also match a longer name like `"MyTabletPro"`.

If the implied LPI (`MaxX / WidthInches`) isn't one of the standard
values, add `"Attributes": { "VerifiedLPI": "<lpi>" }` to make the
configuration-validation test pass.

## 4. Verify

```
dotnet build OpenTabletDriver.Linux.slnf
dotnet test  OpenTabletDriver.Tests/OpenTabletDriver.Tests.csproj
```

The configuration-validation suite picks up your JSON automatically and
will complain about identifier collisions, unreasonable dimensions, etc.

## 5. Tell the user how to enable it

Users opt into a serial tablet by listing it in
`$AppData/OpenTabletDriver/serial-tablets.json`:

```json
{
  "Devices": [
    { "Type": "MyTablet", "Port": "/dev/ttyUSB0" }
  ]
}
```

`"Type"` must match the `Name` your `SerialTabletType` returns. On Linux
the user also needs membership in the `uucp` (or `dialout`) group to
read `/dev/ttyUSB*`.
