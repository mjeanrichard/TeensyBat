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

using System;
using System.Threading.Tasks;

using Windows.System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;

namespace TeensyBatExplorer.Views.Devices
{
    public sealed partial class SetNodeIdDialog : ContentDialog
    {
        public static async Task<byte?> GetNodeId(byte currentNodeId)
        {
            SetNodeIdDialog dialog = new SetNodeIdDialog();
            dialog.txtNodeId.Text = currentNodeId.ToString();
            dialog._nodeId = null;

            await dialog.ShowAsync(ContentDialogPlacement.Popup);
            return dialog._nodeId;
        }

        private byte? _nodeId;

        public SetNodeIdDialog()
        {
            InitializeComponent();
            Opened += OnOpened;
        }

        private void OnOpened(ContentDialog sender, ContentDialogOpenedEventArgs args)
        {
            txtNodeId.SelectAll();
            txtNodeId.Focus(FocusState.Programmatic);
        }

        private void ContentDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            args.Cancel = !TryCloseDialog();
        }

        private bool TryCloseDialog()
        {
            if (!byte.TryParse(txtNodeId.Text, out byte newValue))
            {
                return false;
            }
            _nodeId = newValue;
            return true;
        }

        private void TxtNodeId_OnKeyDown(object sender, KeyRoutedEventArgs e)
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