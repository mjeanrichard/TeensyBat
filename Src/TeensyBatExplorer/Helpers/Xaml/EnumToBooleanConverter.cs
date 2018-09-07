// 
// Teensy Bat Explorer - Copyright(C) 2018 Meinard Jean-Richard
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

using System;

using Windows.UI.Xaml.Data;

namespace TeensyBatExplorer.Helpers.Xaml
{
    public class EnumToBooleanConverter : IValueConverter
    {
        public Type EnumType { get; set; }

        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (parameter is string enumString)
            {
                if (!Enum.IsDefined(EnumType, value))
                {
                    throw new ArgumentException("ExceptionEnumToBooleanConverterValueMustBeAnEnum".GetLocalized());
                }

                var enumValue = Enum.Parse(EnumType, enumString);

                return enumValue.Equals(value);
            }

            throw new ArgumentException("ExceptionEnumToBooleanConverterParameterMustBeAnEnumName".GetLocalized());
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            if (parameter is string enumString)
            {
                return Enum.Parse(EnumType, enumString);
            }

            throw new ArgumentException("ExceptionEnumToBooleanConverterParameterMustBeAnEnumName".GetLocalized());
        }
    }
}