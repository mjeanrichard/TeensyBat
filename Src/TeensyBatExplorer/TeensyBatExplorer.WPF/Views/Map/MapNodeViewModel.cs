// 
// Teensy Bat Explorer - Copyright(C) 2020 Meinrad Jean-Richard
// 
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;

using MapControl;

using TeensyBatExplorer.Core.Maps;
using TeensyBatExplorer.Core.Models;
using TeensyBatExplorer.WPF.Annotations;
using TeensyBatExplorer.WPF.Controls;

namespace TeensyBatExplorer.WPF.Views.Map
{
    public class MapNodeViewModel : INotifyPropertyChanged
    {
        private static readonly double[] DistanceValue = { 1, 0.6, 0.5, 0.2, 0.1, 0 };

        private readonly CallFilter _filter;
        private bool _hasLocation;
        private Location? _location;
        private bool _isSelected;
        private List<BatCall>? _calls;
        private double _currentValue;
        private long _offsetMicros;
        private double? _swissGridX;
        private double? _swissGridY;

        public MapNodeViewModel(BatNode batNode, CallFilter filter)
        {
            _filter = filter;
            Node = batNode;
            UpdateLocationFromWgs84();
        }

        public IEnumerable<BatCall> Calls => _calls ?? Enumerable.Empty<BatCall>();

        public bool HasLocation
        {
            get => _hasLocation;
            set
            {
                if (value != _hasLocation)
                {
                    _hasLocation = value;
                    OnPropertyChanged();
                }
            }
        }

        public Location? Location
        {
            get => _location;
            set
            {
                if (!Equals(value, _location))
                {
                    Node.Longitude = value?.Longitude;
                    Node.Latitude = value?.Latitude;
                    UpdateLocationFromWgs84();
                }
            }
        }

        public double Longitude
        {
            get => Node.Longitude ?? 0;
            set
            {
                Node.Longitude = value;
                UpdateLocationFromWgs84();
            }
        }

        public double Latitude
        {
            get => Node.Latitude ?? 0;
            set
            {
                Node.Latitude = value;
                UpdateLocationFromWgs84();
            }
        }

        public double? SwissGridX
        {
            get => _swissGridX;
            set
            {
                if (value != _swissGridX)
                {
                    _swissGridX = value;
                    UpdateLocationFromSwissGrid();
                }
            }
        }

        public double? SwissGridY
        {
            get => _swissGridY;
            set
            {
                if (value != _swissGridY)
                {
                    _swissGridY = value;
                    UpdateLocationFromSwissGrid();
                }
            }
        }

        public string NodeNumber => Node.NodeNumber.ToString();

        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (value != _isSelected)
                {
                    _isSelected = value;
                    OnPropertyChanged();
                }
            }
        }

        public BatNode Node { get; }

        public bool HasCalls => _currentValue > 0;

        public double CurrentValue
        {
            get => _currentValue;
            set
            {
                _currentValue = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(HasCalls));
            }
        }

        private void UpdateLocationFromSwissGrid()
        {
            if (_swissGridX.HasValue && _swissGridY.HasValue)
            {
                SwissGridLocation newLocation = new(_swissGridX.Value, _swissGridY.Value);
                Wgs84Location wgs84Location = newLocation.ToWgs84Approx();
                Node.Latitude = wgs84Location.Latitude;
                Node.Longitude = wgs84Location.Longitude;
            }

            UpdateLocationFromWgs84();
        }

        private void UpdateLocationFromWgs84()
        {
            if (Node.Latitude.HasValue && Node.Longitude.HasValue)
            {
                _location = new Location(Node.Latitude.Value, Node.Longitude.Value);
                SwissGridLocation swissGrid = SwissGridLocation.FromWgs84Approx(Node.Latitude.Value, Node.Longitude.Value);
                _swissGridX = Math.Round(swissGrid.X);
                _swissGridY = Math.Round(swissGrid.Y);
                HasLocation = true;
            }
            else
            {
                HasLocation = false;
            }

            OnPropertyChanged(nameof(Latitude));
            OnPropertyChanged(nameof(Longitude));
            OnPropertyChanged(nameof(SwissGridX));
            OnPropertyChanged(nameof(SwissGridY));
            OnPropertyChanged(nameof(Location));
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public void LoadData(List<BatCall> calls, long offsetMicros)
        {
            _calls = calls;
            _offsetMicros = offsetMicros;
        }

        public void Update(long positionMillis, long windowWidthMillis)
        {
            if (windowWidthMillis <= 0 || positionMillis <= 0 || _calls == null)
            {
                CurrentValue = 0;
                return;
            }

            long pos = positionMillis - _offsetMicros / 1000;
            int sum = DistanceValue.Length - 1;

            foreach (BatCall call in _filter.Apply(_calls))
            {
                double distance = Math.Abs(Math.Round((pos - call.StartTimeMicros / 1000d) / windowWidthMillis));

                if (distance < sum)
                {
                    sum = (int)distance;
                }
            }

            CurrentValue = DistanceValue[sum];
        }
    }
}