// 
// Teensy Bat Explorer - Copyright(C) 2020 Meinrad Jean-Richard
// 
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.

using System.Globalization;
using System.Text.RegularExpressions;
using System.Windows.Controls;

namespace TeensyBatExplorer.WPF.Controls
{
    public class TimeValidationRule : ValidationRule
    {
        public static readonly Regex TimePattern = new("^(?<h>[0-9]{1,2})[:.-](?<m>[0-9]{1,2})[:.-](?<s>[0-9]{1,2})$|^(?<h>[012]?[0-9])(?<m>[0-5][0-9])(?<s>[0-5][0-9])$");

        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            if (value is string stringValue)
            {
                if (TimePattern.IsMatch(stringValue.Trim()))
                {
                    return new ValidationResult(true, null);
                }
            }

            return new ValidationResult(false, "Bitte eine gültige Zeit eingeben");
        }
    }
}