using System.Threading.Tasks;

using Windows.UI.Xaml.Controls;
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
			//if (ViewModel.ToggleEnabledCommand.CanExecute())
			//{
			//	ViewModel.ToggleEnabledCommand.Execute();
			//}
		}

		private void Pivot_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
		{
		//	PivotItem pivotItem = ((Pivot)sender).SelectedItem as PivotItem;
		//	if (pivotItem != null)
		//	{
		//		ViewModel.PivotModelChanged(pivotItem.DataContext as PivotModelBase);
		//	}
		}
	}
}