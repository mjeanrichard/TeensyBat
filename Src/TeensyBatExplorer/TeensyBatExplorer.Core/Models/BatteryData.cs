using System.Diagnostics;

namespace TeensyBatExplorer.Core.Models
{
    [DebuggerDisplay("{" + nameof(Voltage) + "}V")]
    public class BatteryData : AdditionalData
    {
        public int Id { get; set; }
        public double Voltage { get; set; }

        public BatNode Node { get; set; }
        public BatLog Log { get; set; }
    }
}