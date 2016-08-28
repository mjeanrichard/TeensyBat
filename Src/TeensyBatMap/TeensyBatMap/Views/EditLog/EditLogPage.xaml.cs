using Windows.Foundation;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Navigation;

using TeensyBatMap.Common;

using UniversalMapControl.Interfaces;
using UniversalMapControl.Projections;

namespace TeensyBatMap.Views.EditLog
{
	public class EditLogPageBase : AppPage<EditLogViewModel>
	{
	}

	/// <summary>
	///     An empty page that can be used on its own or navigated to within a Frame.
	/// </summary>
	public sealed partial class EditLogPage : EditLogPageBase
	{
		public EditLogPage()
		{
			InitializeComponent();
		}

		private void ItemOnDoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
		{
			if (ViewModel.CallDetailsPivotModel.ToggleEnabledCommand.CanExecute())
			{
				ViewModel.CallDetailsPivotModel.ToggleEnabledCommand.Execute();
			}
		}

		private void MapOnDoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
		{
			Point position = e.GetPosition(_map);
			ILocation location = _map.GetLocationFromPoint(position);
			ViewModel.Location = location;
		}

		protected override void OnNavigatedTo(NavigationEventArgs e)
		{
			base.OnNavigatedTo(e);
			_map.MapCenter = ViewModel.Location;
		}
	}
}