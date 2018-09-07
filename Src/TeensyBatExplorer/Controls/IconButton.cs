using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace TeensyBatExplorer.Controls
{
    public class IconButton : Button
    {
        public static readonly DependencyProperty SymbolProperty = DependencyProperty.Register(
            "Symbol", typeof(Symbol), typeof(IconButton), new PropertyMetadata(Symbol.Accept));

        public Symbol Symbol
        {
            get { return (Symbol)GetValue(SymbolProperty); }
            set { SetValue(SymbolProperty, value); }
        }

        public IconButton()
        {
            DefaultStyleKey = typeof(IconButton);
        }
    }
}