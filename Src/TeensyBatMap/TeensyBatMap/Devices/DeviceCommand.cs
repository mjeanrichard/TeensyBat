using System;
using System.Threading;
using System.Threading.Tasks;

namespace TeensyBatMap.Devices
{
	public abstract class DeviceCommand<TResult>
	{
		private readonly ManualResetEvent _responseHandled = new ManualResetEvent(false);
		private TResult _result;

		protected DeviceCommand()
		{
			Timeout = TimeSpan.FromMilliseconds(500000);
		}

		protected abstract void ExecuteInternal(TeensyBatDevice device);
		protected abstract TResult HandleLine(TeensyBatDevice device, string data);

		public Task<TResult> Execute(TeensyBatDevice device)
		{
			Task<TResult> task = Task.Run(() =>
			{
				try
				{
					_responseHandled.Reset();
					device.LineReceived += DeviceOnLineReceived;
					ExecuteInternal(device);
					WaitHandle.WaitAny(new[] { device.CancellationToken.Token.WaitHandle, _responseHandled }, Timeout);
				}
				finally
				{
					device.LineReceived -= DeviceOnLineReceived;
				}
				return _result;
			});
			return task;
		}

		public TimeSpan Timeout { get; set; }

		private void DeviceOnLineReceived(object sender, LineReceivedArgs lineReceivedArgs)
		{
			_result = HandleLine((TeensyBatDevice)sender, lineReceivedArgs.Data);
			if (_result != null)
			{
				_responseHandled.Set();
			}
		}
	}
}