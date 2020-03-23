using System.Windows;

namespace TeensyBatExplorer.WPF
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        public MainWindow(MainViewModel mainViewModel) : this()
        {
            DataContext = mainViewModel;
        }
    }
}