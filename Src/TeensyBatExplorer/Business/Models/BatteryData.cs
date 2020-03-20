using System.Diagnostics;

using LiteDB;

namespace TeensyBatExplorer.Business.Models
{
    [DebuggerDisplay("{" + nameof(Voltage) + "}V")]
    public class BatteryData : AdditionalData
    {
        public int Id { get; set; }
        public double Voltage { get; set; }

        public int NodeId { get; set; }
        public int LogId { get; set; }
    }
}