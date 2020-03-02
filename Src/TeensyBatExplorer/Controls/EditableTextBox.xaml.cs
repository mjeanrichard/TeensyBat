// 
// Teensy Bat Explorer - Copyright(C) 2018 Meinrad Jean-Richard
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

using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace TeensyBatExplorer.Controls
{
    public sealed partial class EditableTextBox : UserControl
    {
        public static readonly DependencyProperty TextProperty = DependencyProperty.Register(
            "Text", typeof(string), typeof(EditableTextBox), new PropertyMetadata(default(string), TextChanged));

        private static void TextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            EditableTextBox editableTextBox = d as EditableTextBox;
            if (editableTextBox != null)
            {
                editableTextBox.Update();
            }
        }

        public string Text
        {
            get { return (string)GetValue(TextProperty); }
            set { SetValue(TextProperty, value); }
        }

        public EditableTextBox()
        {
            InitializeComponent();
            UpdateControls();
        }

        public bool EditMode { get; private set; }

        private void Update()
        {
            txtInput.Text = Text;
        }

        private void BtnEdit_OnTapped(object sender, TappedRoutedEventArgs e)
        {
            EditMode = true;
            UpdateControls();
        }

        private void BtnAccept_OnTapped(object sender, TappedRoutedEventArgs e)
        {
            Text = txtInput.Text;
            EditMode = false;
            UpdateControls();
        }

        private void BtnCancel_OnTapped(object sender, TappedRoutedEventArgs e)
        {
            txtInput.Text = Text;
            EditMode = false;
            UpdateControls();
        }

        private void UpdateControls()
        {
            btnAccept.Visibility = EditMode ? Visibility.Visible : Visibility.Collapsed;
            btnCancel.Visibility = EditMode ? Visibility.Visible : Visibility.Collapsed;
            btnEdit.Visibility = EditMode ? Visibility.Collapsed : Visibility.Visible;
            txtInput.IsReadOnly = !EditMode;
        }
    }
}