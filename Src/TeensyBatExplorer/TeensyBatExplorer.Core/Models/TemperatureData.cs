using System.Diagnostics;

namespace TeensyBatExplorer.Core.Models
{
    [DebuggerDisplay("{" + nameof(Temperature) + "}°C")]
    public class TemperatureData : AdditionalData
    {
        public int Id { get; set; }
        public double Temperature { get; set; }

        public BatNode Node { get; set; }
        public BatLog Log { get; set; }
    }
}