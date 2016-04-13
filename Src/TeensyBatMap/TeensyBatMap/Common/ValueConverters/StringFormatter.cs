using System;

using Windows.UI.Xaml.Data;

namespace TeensyBatMap.Common.ValueConverters
{
	public class StringFormatter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, string language)
		{
			string formatString = parameter as string;
			if (!string.IsNullOrEmpty(formatString))
			{
				return string.Format(formatString, value);
			}

			return value.ToString();
		}

		public object ConvertBack(object value, Type targetType, object parameter, string language)
		{
			throw new NotImplementedException();
		}
	}
}