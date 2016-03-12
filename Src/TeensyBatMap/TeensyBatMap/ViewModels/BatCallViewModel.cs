using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;

using TeensyBatMap.Common;
using TeensyBatMap.Domain;
using TeensyBatMap.Domain.Bins;

using WinRtLib;

namespace TeensyBatMap.ViewModels
{
	public class SimpleIntBin : IBin
	{
		public SimpleIntBin(double value, string label)
		{
			Value = value;
			Label = label;
			SecondaryValue = value;
		}

		public double Value { get; }
		public double SecondaryValue { get; }
		public string Label { get; }
	}

	public class BatCallViewModel : INotifyPropertyChanged
	{
		private bool _isInitialized;
		private readonly BatNodeLog _log;
		private readonly BatCall _batCall;
		private ObservableCollection<SimpleIntBin> _frequencies;
		public event PropertyChangedEventHandler PropertyChanged;

		public BatCallViewModel(BatNodeLog log, BatCall batCall, int index)
		{
			Index = index;
			_log = log;
			_batCall = batCall;
		}

		public bool Enabled
		{
			get { return _batCall.Enabled; }
			set
			{
				if (_batCall.Enabled != value) { 
					_batCall.Enabled = value;
					OnPropertyChanged();
				}
			}
		}



		public string Duration
		{
			get { return string.Format(CultureInfo.CurrentCulture, "{0} ms", _batCall.Duration / 1000); }
		}

		public string StartTime
		{
			get
			{
				return _log.LogStart.AddMilliseconds(_batCall.StartTimeMs).ToString("HH:mm:ss.fff", CultureInfo.CurrentCulture);
			}
		}

		public string StartTimeFull
		{
			get
			{
				return _log.LogStart.AddMilliseconds(_batCall.StartTimeMs).ToString("dd.MM.yyyy HH:mm:ss.fff", CultureInfo.CurrentCulture);
			}
		}

		public BatCall BatCall
		{
			get { return _batCall; }
		}

		public int Index { get; private set; }

		protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}

		public ObservableCollection<SimpleIntBin> Frequencies
		{
			get { return _frequencies; }
			set
			{
				_frequencies = value;
				OnPropertyChanged();
			}
		}

		public void Initialize()
		{
			if (_isInitialized)
			{
				return;
			}
			_isInitialized = true;

			SimpleIntBin[] simpleIntBins = new SimpleIntBin[255];
			for (int i = 1; i < 256; i++)
			{
				simpleIntBins[i-1] = new SimpleIntBin(BitConverter.ToUInt16(_batCall.FftData, i * 2), (i/2).ToString(CultureInfo.CurrentCulture));
			}
			Frequencies = new ObservableCollection<SimpleIntBin>(simpleIntBins);
		}
	}
}