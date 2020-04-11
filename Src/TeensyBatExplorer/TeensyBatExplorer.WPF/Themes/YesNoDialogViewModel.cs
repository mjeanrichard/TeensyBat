using System;
using System.Collections.Generic;
using System.Text;

using TeensyBatExplorer.WPF.Infrastructure;
using TeensyBatExplorer.WPF.Views.Device;

namespace TeensyBatExplorer.WPF.Themes
{
    public enum DialogResult
    {
        Yes,
        No,
        Cancel
    }

    public class YesNoDialogViewModel : DialogViewModel<DialogResult>
    {
        public string Message { get; set; }

        public YesNoDialogViewModel(string message, BaseViewModel ownerViewModel) : base(ownerViewModel)
        {
            Message = message;
        }
    }
}
