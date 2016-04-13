using System;

using Windows.UI.Core;
using Windows.UI.Xaml;

namespace TeensyBatMap.Common
{
	public static class DispatcherHelper
	{
		private static CoreDispatcher _uiDispatcher;

		public static void Init()
		{
			_uiDispatcher = Window.Current.Dispatcher;
		}

		public static void InvokeOnUI(Action action)
		{
			if (action == null)
			{
				return;
			}

			if (_uiDispatcher == null || _uiDispatcher.HasThreadAccess)
			{
				action();
			}
			else
			{
				_uiDispatcher.RunAsync(CoreDispatcherPriority.Normal, () => action());
			}
		}
	}
}