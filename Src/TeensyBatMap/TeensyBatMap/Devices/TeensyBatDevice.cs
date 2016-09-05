using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Windows.Devices.Enumeration;
using Windows.Devices.SerialCommunication;
using Windows.Storage.Streams;

namespace TeensyBatMap.Devices
{
	public class TeensyBatDevice : INotifyPropertyChanged
	{
		private DeviceInformation _currentDevice;
		private SerialDevice _serialDevice;
		private DataReader _serialDataReader;
		private DataWriter _dataWriter;
		private Task _readerTask;
		private bool _isConnected;

		public event EventHandler<LineReceivedArgs> LineReceived;

		public bool IsConnected
		{
			get { return _isConnected; }
			protected set
			{
				_isConnected = value;
				OnPropertyChanged();
			}
		}

		public CancellationTokenSource CancellationToken { get; private set; }

		public async Task Connect(DeviceInformation device)
		{
			if (_currentDevice != null)
			{
				DisconnectSerial();
			}
			_currentDevice = device;
			await ConnectSerial();
		}

		private void DisconnectSerial()
		{
			IsConnected = false;
			if (CancellationToken != null && CancellationToken.IsCancellationRequested == false)
			{
				CancellationToken.Cancel();
				CancellationToken.Dispose();
			}
			if (_serialDevice != null)
			{
				_serialDevice.Dispose();
				_serialDevice = null;
				_currentDevice = null;
			}
		}

		protected async Task ConnectSerial()
		{
			if (_currentDevice != null)
			{
				_serialDevice = await SerialDevice.FromIdAsync(_currentDevice.Id);
				if (_serialDevice == null)
				{
					return;
				}
				// Configure serial settings
				_serialDevice.WriteTimeout = TimeSpan.FromMilliseconds(500);
				_serialDevice.ReadTimeout = TimeSpan.FromMilliseconds(500);
				_serialDevice.BaudRate = 57600;
				_serialDevice.Parity = SerialParity.None;
				_serialDevice.StopBits = SerialStopBitCount.One;
				_serialDevice.DataBits = 8;
				_serialDevice.Handshake = SerialHandshake.None;

				CancellationToken = new CancellationTokenSource();

				_serialDataReader = new DataReader(_serialDevice.InputStream);
				_serialDataReader.InputStreamOptions = InputStreamOptions.Partial;
				_serialDataReader.InputStreamOptions = InputStreamOptions.Partial;

				_dataWriter = new DataWriter(_serialDevice.OutputStream);

				IsConnected = true;

				_readerTask = Task.Run(() =>
				{
					try
					{
						ReadLines();
					}
					catch (Exception)
					{
						DisconnectSerial();
					}
				});
			}
		}



		public async void Send(string command)
		{
		    if (_dataWriter != null)
		    {
		        byte[] buffer = Encoding.ASCII.GetBytes(command);
		        _dataWriter.WriteBytes(buffer);
		        await _dataWriter.StoreAsync();
		    }
		}

		protected void ReadLines()
		{
			string data = string.Empty;
			while (true)
			{
				CancellationToken.Token.ThrowIfCancellationRequested();
				uint bytesRead = _serialDataReader.LoadAsync(1024).AsTask(CancellationToken.Token).Result;
				if (bytesRead > 0)
				{
					byte[] buffer = new byte[bytesRead];
					_serialDataReader.ReadBytes(buffer);
					string newData = Encoding.ASCII.GetString(buffer);
					data = string.Join(data, newData);

					int newLinePos = data.IndexOf('\n');
					while (newLinePos >= 0)
					{
						CancellationToken.Token.ThrowIfCancellationRequested();
						if (newLinePos > 0)
						{
							LineReceived?.Invoke(this, new LineReceivedArgs(data.Substring(0, newLinePos)));
						}
						if (newLinePos >= 0)
						{
							if (data.Length > newLinePos + 1)
							{
								data = data.Substring(newLinePos + 1);
							}
							else
							{
								data = string.Empty;
							}
						}
						newLinePos = data.IndexOf('\n');
					}
				}
			}
		}

		public void Disonnect()
		{
			DisconnectSerial();
		}

		public event PropertyChangedEventHandler PropertyChanged;

		protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}
	}

	public class LineReceivedArgs
	{
		public LineReceivedArgs(string data)
		{
			Data = data;
		}

		public string Data { get; private set; }
	}
}