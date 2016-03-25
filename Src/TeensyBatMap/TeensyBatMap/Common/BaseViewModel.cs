using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace TeensyBatMap.Common
{
    public abstract class BaseViewModel : IBaseViewModel, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        private bool _isBusy;

	    protected bool IsInitialized { get; set; }

	    public bool IsBusy
        {
            get { return _isBusy; }
            protected set
            {
                _isBusy = value;
                OnPropertyChanged();
            }
        }

	    protected abstract Task InitializeInternal();

	    public async Task Initialize()
	    {
		    if (IsInitialized)
		    {
			    return;
		    }
			using (MarkBusy())
			{
				await InitializeInternal();
			}
			IsInitialized = true;
		}

		public abstract string Titel { get; }

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChangedEventHandler onPropertyChanged = PropertyChanged;
	        onPropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public IDisposable MarkBusy()
        {
            return new BusyState(this);
        }

        private class BusyState : IDisposable
        {
            private readonly BaseViewModel _baseViewModel;

            public BusyState(BaseViewModel baseViewModel)
            {
                _baseViewModel = baseViewModel;
                baseViewModel.IsBusy = true;
            }

            public void Dispose()
            {
                _baseViewModel.IsBusy = false;
            }
        }
    }
}