using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace TeensyBatExplorer.Helpers.ViewModels
{
    public abstract class BaseCommand : ICommand, INotifyPropertyChanged
    {
        /// <summary>
        ///     Raised when RaiseCanExecuteChanged is called.
        /// </summary>
        public event EventHandler CanExecuteChanged;

        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        ///     Method used to raise the <see cref="CanExecuteChanged" /> event
        ///     to indicate that the return value of the <see cref="CanExecute" />
        ///     method has changed.
        /// </summary>
        public virtual void RaiseCanExecuteChanged()
        {
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }

        public abstract bool CanExecute(object parameter);
        public abstract void Execute(object parameter);

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}