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
using System.Threading.Tasks;

using Windows.System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;

// The Content Dialog item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace TeensyBatExplorer.Views.Devices
{
    public sealed partial class SetVoltageDialog : ContentDialog
    {
        public static async Task<double?> GetVoltage(double initialVoltage)
        {
            SetVoltageDialog dialog = new SetVoltageDialog();
            dialog.txtVoltage.Text = initialVoltage.ToString("N2");
            dialog._voltage = null;

            await dialog.ShowAsync(ContentDialogPlacement.Popup);
            return dialog._voltage;
        }

        private double? _voltage;

        public SetVoltageDialog()
        {
            InitializeComponent();
            Opened += OnOpened;
        }

        private void OnOpened(ContentDialog sender, ContentDialogOpenedEventArgs args)
        {
            this.Focus(FocusState.Programmatic);
            txtVoltage.SelectAll();
            txtVoltage.Focus(FocusState.Programmatic);
        }

        private void ContentDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            args.Cancel = !TryCloseDialog();
        }

        private bool TryCloseDialog()
        {
            if (!double.TryParse(txtVoltage.Text, out double newValue))
            {
                return false;
            }
            _voltage = newValue;
            return true;
        }

        private void TxtVoltage_OnKeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == VirtualKey.Enter)
            {
                if (TryCloseDialog())
                {
                    Hide();
                }
            }
        }
    }
}