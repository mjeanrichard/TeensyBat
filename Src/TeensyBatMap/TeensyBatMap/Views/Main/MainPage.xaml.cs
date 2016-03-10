using Windows.UI.Xaml.Input;
using TeensyBatMap.Common;

namespace TeensyBatMap.Views.Main
{
    public class MainPageBase : AppPage<MainPageModel>
    {}

    public sealed partial class MainPage : MainPageBase
    {
        public MainPage()
        {
            InitializeComponent();
        }

        public void ItemOnDoubleTapped(object sender, DoubleTappedRoutedEventArgs doubleTappedRoutedEventArgs)
        {
            if (ViewModel.DetailsCommand.CanExecute())
            {
                ViewModel.DetailsCommand.Execute();
            }
        }
    }
}