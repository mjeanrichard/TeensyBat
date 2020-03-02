using System.Diagnostics;

namespace TeensyBatExplorer.Business.Models
{
    [DebuggerDisplay("{" + nameof(Voltage) + "}V")]
    public class BatteryData : AdditionalData
    {
        public int Id { get; set; }
        public double Voltage { get; set; }
    }
}