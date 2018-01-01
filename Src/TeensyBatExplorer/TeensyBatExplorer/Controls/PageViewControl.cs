// 
// Teensy Bat Explorer - Copyright(C) 2017 Meinard Jean-Richard
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
using Windows.UI.Xaml.Controls.Primitives;

using TeensyBatExplorer.Common;

namespace TeensyBatExplorer.Controls
{
    [TemplatePart(Name = PopupGridPartName, Type = typeof(Grid))]
    [TemplatePart(Name = ProgressPartName, Type = typeof(ProgressBar))]
    [TemplatePart(Name = MessagePartName, Type = typeof(TextBlock))]
    [TemplatePart(Name = PopupPartName, Type = typeof(Popup))]
    public sealed class PageViewControl : ContentControl
    {
        public static readonly DependencyProperty BusyStateProperty = DependencyProperty.Register(
            "BusyState", typeof(BusyState), typeof(PageViewControl), new PropertyMetadata(BusyState.Idle, PropertyChanged));

        public BusyState BusyState
        {
            get { return (BusyState)GetValue(BusyStateProperty); }
            set
            {
                SetValue(BusyStateProperty, value);
                UpdateProgress();
            }
        }

        private static void PropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            PageViewControl pageViewControl = (PageViewControl)d;
            pageViewControl.UpdateProgress();
        }

        private void UpdateProgress()
        {
            if (_progress == null)
            {
                return;
            }

            BusyState busyState = BusyState;
            if (busyState != null && busyState != BusyState.Idle)
            {
                if (busyState.MaxProgressValue == 0)
                {
                    _progress.IsIndeterminate = true;
                }
                else
                {
                    _progress.Maximum = busyState.MaxProgressValue;
                    _progress.Value = busyState.ProgressValue;
                    _progress.IsIndeterminate = false;
                }

                if (string.IsNullOrWhiteSpace(busyState.Message))
                {
                    _message.Visibility = Visibility.Collapsed;
                }
                else
                {
                    _message.Text = busyState.Message;
                    _message.Visibility = Visibility.Visible;
                }

                _popup.IsOpen = true;
            }
            else
            {
                _popup.IsOpen = false;
            }

        }

        private const string PopupGridPartName = "PART_PopupGrid";
        private const string PopupPartName = "PART_Popup";
        private const string ProgressPartName = "PART_Progress";
        private const string MessagePartName = "PART_Message";

        private Grid _popupGrid;
        private ProgressBar _progress;
        private TextBlock _message;
        private Popup _popup;

        public PageViewControl()
        {
            DefaultStyleKey = typeof(PageViewControl);
        }

        protected override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            _popupGrid = GetTemplateChild(PopupGridPartName) as Grid;
            _popup = GetTemplateChild(PopupPartName) as Popup;
            _progress = GetTemplateChild(ProgressPartName) as ProgressBar;
            _message = GetTemplateChild(MessagePartName) as TextBlock;
            if (_popupGrid != null)
            {
                _popupGrid.SizeChanged += PopupSizeChanged;
                SetPopupSize();
            }
        }

        private void PopupSizeChanged(object sender, SizeChangedEventArgs e)
        {
            SetPopupSize();
        }

        private void SetPopupSize()
        {
            _popupGrid.Height = Window.Current.Bounds.Height;
            _popupGrid.Width = Window.Current.Bounds.Width;
            _progress.Width = _popupGrid.Width * 0.75;
        }
    }
}