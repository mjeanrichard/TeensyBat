using System.Diagnostics;

using LiteDB;

namespace TeensyBatExplorer.Business.Models
{
    [DebuggerDisplay("{" + nameof(Temperature) + "}°C")]
    public class TemperatureData : AdditionalData
    {
        public int Id { get; set; }
        public double Temperature { get; set; }

        public int NodeId { get; set; }
        public int LogId { get; set; }
    }
}