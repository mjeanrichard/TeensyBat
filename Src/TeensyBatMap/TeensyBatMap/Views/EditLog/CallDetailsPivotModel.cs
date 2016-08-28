using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

using Syncfusion.Data.Extensions;

using TeensyBatMap.Common;
using TeensyBatMap.Domain;
using TeensyBatMap.ViewModels;

namespace TeensyBatMap.Views.EditLog
{
	public class CallDetailsPivotModel : PivotModelBase<EditLogViewModel>
	{
		private ObservableCollection<BatCallViewModel> _calls;
		private BatCallViewModel _selectedCall;

		public CallDetailsPivotModel(EditLogViewModel parentViewModel) : base(parentViewModel)
		{
			ToggleEnabledCommand = new RelayCommand(() =>
			{
				if (SelectedCall != null)
				{
					SelectedCall.Enabled = !SelectedCall.Enabled;
				}
			});
		}

		public override async Task Initialize()
		{
			IEnumerable<BatCall> batCalls;
			if (ParentViewModel.Db != null)
			{
				batCalls = await ParentViewModel.Db.LoadCalls(ParentViewModel.BatLog, true);
			}
			else
			{
				batCalls = ParentViewModel.BatLog.Calls;
			}
			Calls = batCalls.Select((c, i) => new BatCallViewModel(ParentViewModel.BatLog, c, i)).ToObservableCollection();
			if (Calls.Any())
			{
				SelectedCall = Calls.First();
			}
		}

		public RelayCommand ToggleEnabledCommand { get; private set; }

		public ObservableCollection<BatCallViewModel> Calls
		{
			get { return _calls; }
			private set
			{
				_calls = value;
				OnPropertyChanged();
			}
		}

		public BatCallViewModel SelectedCall
		{
			get { return _selectedCall; }
			set
			{
				if (_selectedCall != value)
				{
					if (value != null)
					{
						value.Initialize();
					}
					_selectedCall = value;
					OnPropertyChanged();
				}
			}
		}
	}
}