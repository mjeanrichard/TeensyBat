using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

using TeensyBatMap.Common;

namespace TeensyBatMap.Views.EditLog
{
	public abstract class PivotModelBase<T> : INotifyPropertyChanged
		where T : BaseViewModel
	{
		private readonly T _parentViewModel;

		protected PivotModelBase(T parentViewModel)
		{
			_parentViewModel = parentViewModel;
		}

		public T ParentViewModel
		{
			get { return _parentViewModel; }
		}

		public event PropertyChangedEventHandler PropertyChanged;

		protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}

		public abstract Task Initialize();

		protected IDisposable MarkBusy()
		{
			return _parentViewModel.MarkBusy();
		}
	}
}