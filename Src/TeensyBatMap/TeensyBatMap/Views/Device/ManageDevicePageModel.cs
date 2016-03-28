using System;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;

using Windows.Devices.Enumeration;
using Windows.Devices.SerialCommunication;
using Windows.Storage.Streams;
using Windows.UI.Xaml.Navigation;

using TeensyBatMap.Common;

namespace TeensyBatMap.Views.Device
{
    public class ManageDevicePageModel : BaseViewModel
    {
	    private SerialDevice _serialDevice;
	    private DeviceInformation _selectedDevice;
	    private bool _isConnected;
	    private DataReader _serialDataReader;
	    private CancellationTokenSource _serialReaderCancellationToken;
	    private string _data;

	    public ManageDevicePageModel()
			:this(null, null)
	    {
		    
	    }

        public ManageDevicePageModel(NavigationEventArgs navigation, NavigationService navigationService)
        {
		}

        public override string Titel
        {
	        get
	        {
		        if (SelectedDevice != null)
		        {
			        return string.Format("Gerät verwalten ({0})", SelectedDevice.Name);
		        }
		        return "Kein Gerät gefunden.";
	        }
        }


	    protected override async Task InitializeInternal()
        {
			await FindDevice();
		    await InitDevice();
        }

	    private async Task InitDevice()
	    {
		    using (MarkBusy())
		    {
			    if (SelectedDevice != null)
			    {
					_serialDevice = await SerialDevice.FromIdAsync(SelectedDevice.Id);

					// Configure serial settings
					_serialDevice.WriteTimeout = TimeSpan.FromMilliseconds(500);
					_serialDevice.ReadTimeout = TimeSpan.FromMilliseconds(500);
					_serialDevice.BaudRate = 57600;
					_serialDevice.Parity = SerialParity.None;
					_serialDevice.StopBits = SerialStopBitCount.One;
					_serialDevice.DataBits = 8;
					_serialDevice.Handshake = SerialHandshake.None;

					_serialReaderCancellationToken = new CancellationTokenSource();

					IsConnected = true;

				    await Listen();
			    }
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
		}

		public bool IsConnected
	    {
		    get { return _isConnected; }
		    set
		    {
			    _isConnected = value;
			    OnPropertyChanged();
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

		private async Task Listen()
		{
			try
			{
				if (_serialDevice != null)
				{
					_serialDataReader = new DataReader(_serialDevice.InputStream);

					// keep reading the serial input
					while (true)
					{
						await ReadAsync(_serialReaderCancellationToken.Token);
					}
				}
			}
			catch (Exception ex)
			{
				IsConnected = false;
			}
			finally
			{
				// Cleanup once complete
				if (_serialDataReader != null)
				{
					_serialDataReader.DetachStream();
					_serialDataReader = null;
				}
			}
		}

		private async Task ReadAsync(CancellationToken cancellationToken)
		{
			Task<UInt32> loadAsyncTask;

			uint ReadBufferLength = 1024;

			// If task cancellation was requested, comply
			cancellationToken.ThrowIfCancellationRequested();

			// Set InputStreamOptions to complete the asynchronous read operation when one or more bytes is available
			_serialDataReader.InputStreamOptions = InputStreamOptions.Partial;

			// Create a task object to wait for data on the serialPort.InputStream
			loadAsyncTask = _serialDataReader.LoadAsync(ReadBufferLength).AsTask(cancellationToken);

			// Launch the task and wait
			UInt32 bytesRead = await loadAsyncTask;
			if (bytesRead > 0)
			{
				Data += _serialDataReader.ReadString(bytesRead);
			}
		}

	    public string Data
	    {
		    get { return _data; }
		    set
		    {
			    _data = value; 
			    OnPropertyChanged();
		    }
	    }
    }
}