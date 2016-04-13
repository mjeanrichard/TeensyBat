using System;
using System.ComponentModel;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Windows.Devices.Enumeration;
using Windows.Devices.SerialCommunication;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Navigation;

using TeensyBatMap.Common;
using TeensyBatMap.Devices;

namespace TeensyBatMap.Views.Device
{
	public class ManageDevicePageModel : BaseViewModel
	{
		private readonly TeensyBatDevice _teensyBatDevice;
		private readonly StringBuilder _data;

		private DeviceInformation _selectedDevice;
		private DeviceInfo _deviceInfo;
		private bool _showLog;
		private Timer _refreshTimer;
		private int _checkInProgress;

		public ManageDevicePageModel()
			: this(null, null)
		{
			DeviceInfo = new DeviceInfo { NodeId = 99, CurrentDate = DateTime.UtcNow, RecordingTime = DateTime.UtcNow.AddMilliseconds(215) };
		}

		public ManageDevicePageModel(NavigationEventArgs navigation, NavigationService navigationService)
		{
			_data = new StringBuilder();
			_teensyBatDevice = new TeensyBatDevice();
			_teensyBatDevice.LineReceived += TeensyBatDeviceOnLineReceived;
			_teensyBatDevice.PropertyChanged += TeensyBatDeviceOnPropertyChanged;
			ShowLogCommand = new RelayCommand(() => ShowLog = !ShowLog);
			RefreshCommand = new RelayCommand(Refresh);
			UpdateTimeCommand = new RelayCommand(UpdateTime, () => _teensyBatDevice.IsConnected);
			_refreshTimer = new Timer(RefreshTimerOnTick, null, 1000, 1000);
		}

		private async void RefreshTimerOnTick(object sender)
		{
			await CheckForDevice();
		}

		private void TeensyBatDeviceOnPropertyChanged(object sender, PropertyChangedEventArgs propertyChangedEventArgs)
		{
			OnPropertyChanged(nameof(Titel));
			OnPropertyChanged(nameof(IsConnected));
			UpdateTimeCommand.RaiseCanExecuteChanged();
			if (_teensyBatDevice.IsConnected)
			{
				_refreshTimer.Change(Timeout.Infinite, Timeout.Infinite);
			}
			else
			{
				_refreshTimer.Change(1000, 1000);
			}
		}

		public bool IsConnected
		{
			get { return _teensyBatDevice.IsConnected; }
		}

		private async void UpdateTime()
		{
			using (MarkBusy())
			{
				if (_teensyBatDevice != null && _teensyBatDevice.IsConnected)
				{
					UpdateTimeCommand command = new UpdateTimeCommand();
					await command.Execute(_teensyBatDevice);
					Refresh();
				}
			}
		}

		private async void Refresh()
		{
			using (MarkBusy())
			{
				if (_teensyBatDevice != null && _teensyBatDevice.IsConnected)
				{
					GetInfoCommand command = new GetInfoCommand();
					DeviceInfo = await command.Execute(_teensyBatDevice);
				}
				else
				{
					await CheckForDevice();
				}
			}
		}

		private void TeensyBatDeviceOnLineReceived(object sender, LineReceivedArgs e)
		{
			_data.AppendLine(e.Data);
			OnPropertyChanged(nameof(Data));
		}

		public bool ShowLog
		{
			get { return _showLog; }
			set
			{
				_showLog = value;
				OnPropertyChanged();
			}
		}

		public TeensyBatDevice TeensyBatDevice
		{
			get { return _teensyBatDevice; }
		}

		public override string Titel
		{
			get
			{
				if (SelectedDevice != null && _teensyBatDevice.IsConnected)
				{
					return string.Format("Gerät verwalten ({0})", SelectedDevice.Name);
				}
				return "Kein Gerät verbunden.";
			}
		}


		protected override async Task InitializeInternal()
		{
			await CheckForDevice();
		}

		private async Task CheckForDevice()
		{
			if (Interlocked.CompareExchange(ref _checkInProgress, 1, 0) == 1)
			{
				return;
			}
			try
			{
				if (!_teensyBatDevice.IsConnected)
				{
					await FindDevice();
					if (SelectedDevice != null)
					{
						await InitDevice();
					}
				}
			}
			catch (Exception e)
			{
				_data.AppendLine(e.Message);
			}
			finally
			{
				_checkInProgress = 0;
			}
		}

		private async Task InitDevice()
		{
			using (MarkBusy())
			{
				if (SelectedDevice != null)
				{
					await _teensyBatDevice.Connect(SelectedDevice);
					GetInfoCommand command = new GetInfoCommand();
					DeviceInfo = await command.Execute(_teensyBatDevice);
				}
			}
		}

		public DeviceInfo DeviceInfo
		{
			get { return _deviceInfo; }
			set
			{
				_deviceInfo = value;
				OnPropertyChanged();
			}
		}

		public override void Leave()
		{
			if (_teensyBatDevice != null)
			{
				_teensyBatDevice.Disonnect();
			}
		}

		private async Task FindDevice()
		{
			//\\?\USB#VID_16C0&PID_0483#1439500#{86e0d1e0-8089-11d0-9ce4-08003e301f73}
			string deviceSelector = SerialDevice.GetDeviceSelectorFromUsbVidPid(0x16C0, 0x0483);

			DeviceInformationCollection devices = await DeviceInformation.FindAllAsync(deviceSelector);
			if (devices.Count == 1)
			{
				SelectedDevice = devices[0];
				OnPropertyChanged(nameof(Titel));
			}
			else
			{
				SelectedDevice = null;
			}
		}

		public DeviceInformation SelectedDevice
		{
			get { return _selectedDevice; }
			set
			{
				if (_selectedDevice != value)
				{
					_selectedDevice = value;
					OnPropertyChanged();
				}
			}
		}

		public string Data
		{
			get { return _data.ToString(); }
		}

		public RelayCommand ShowLogCommand { get; private set; }

		public RelayCommand RefreshCommand { get; private set; }

		public RelayCommand UpdateTimeCommand { get; private set; }
	}
}