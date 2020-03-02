using System.Diagnostics;

namespace TeensyBatExplorer.Business.Models
{
    [DebuggerDisplay("{" + nameof(Temperature) + "}°C")]
    public class TemperatureData : AdditionalData
    {
        public int Id { get; set; }
        public double Temperature { get; set; }
    }
}