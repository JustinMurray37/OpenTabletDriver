using System.Diagnostics.CodeAnalysis;
using OpenTabletDriver.Plugin.Tablet;

namespace OpenTabletDriver.Configurations.Parsers.Summagraphics
{
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]
    public class SummaSketchMMReportParser : IReportParser<IDeviceReport>
    {
        public IDeviceReport Parse(byte[] report)
        {
            if (!SummaSketchMMParser.TryParse(report, out var decoded))
                return new DeviceReport(report);

            if (!decoded.InProximity)
                return new OutOfRangeReport(report);

            return new SummaSketchMMTabletReport(report, decoded);
        }
    }
}
