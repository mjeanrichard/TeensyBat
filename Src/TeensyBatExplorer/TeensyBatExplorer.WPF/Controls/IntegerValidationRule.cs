using System.Globalization;
using System.Windows.Controls;

namespace TeensyBatExplorer.WPF.Controls
{
    public class IntegerValidationRule : ValidationRule
    {
        public int MaxValue { get; set; } = int.MaxValue;
        public int MinValue { get; set; } = 0;

        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            if (value is string stringValue)
            {
                if (int.TryParse(stringValue, out int intValue))
                {
                    if (intValue <= MaxValue && intValue >= MinValue)
                    {
                        return new ValidationResult(true, null);
                    }

                    return new ValidationResult(false, $"Die Zahl muss zwischen {MinValue} und {MaxValue} liegen.");
                }
            }

            return new ValidationResult(false, "Bitte eine gültige Zahl eingeben");
        }
    }
}