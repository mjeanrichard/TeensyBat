using Windows.UI.Xaml.Input;

using TeensyBatMap.Common;

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
			if (ViewModel.ToggleEnabledCommand.CanExecute())
			{
				ViewModel.ToggleEnabledCommand.Execute();
			}
		}
	}
}