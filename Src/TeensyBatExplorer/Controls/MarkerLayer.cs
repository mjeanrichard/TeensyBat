//// 
//// Teensy Bat Explorer - Copyright(C) 2018 Meinard Jean-Richard
////  
//// This program is free software: you can redistribute it and/or modify
//// it under the terms of the GNU General Public License as published by
//// the Free Software Foundation, either version 3 of the License, or
//// (at your option) any later version.
////  
//// This program is distributed in the hope that it will be useful,
//// but WITHOUT ANY WARRANTY; without even the implied warranty of
//// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//// GNU General Public License for more details.
////  
//// You should have received a copy of the GNU General Public License
//// along with this program.  If not, see <http://www.gnu.org/licenses/>.

//using System;
//using System.Collections.Generic;
//using System.Collections.ObjectModel;
//using System.Collections.Specialized;
//using System.ComponentModel;
//using System.Diagnostics;
//using System.Linq;
//using System.Runtime.CompilerServices;

//using Windows.UI;
//using Windows.UI.Xaml;

//using Microsoft.Graphics.Canvas;

//using Newtonsoft.Json;

//using TeensyBatExplorer.Core.BatLog;

//using UniversalMapControl;
//using UniversalMapControl.Interfaces;

//namespace TeensyBatExplorer.Controls
//{
//    public class MarkerLayer : CanvasMapLayer
//    {
//        public static readonly DependencyProperty NodesProperty = DependencyProperty.Register(
//            "Nodes", typeof(ObservableCollection<MarkerModel>), typeof(MarkerLayer), new PropertyMetadata(null, PropertyChangedCallback));

//        private static void PropertyChangedCallback(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs e)
//        {
//            MarkerLayer markerLayer = (MarkerLayer)dependencyObject;

//            ObservableCollection<MarkerModel> oldCollection = e.OldValue as ObservableCollection<MarkerModel>;
//            ObservableCollection<MarkerModel> newCollection = e.NewValue as ObservableCollection<MarkerModel>;

//            markerLayer.UpdateEvents(oldCollection, newCollection);

//            if (oldCollection != null)
//            {
//                oldCollection.CollectionChanged -= markerLayer.OnCollectionChanged;
//            }
//            if (newCollection != null)
//            {
//                newCollection.CollectionChanged += markerLayer.OnCollectionChanged;
//            }
//        }

//        public ObservableCollection<MarkerModel> Nodes
//        {
//            get { return (ObservableCollection<MarkerModel>)GetValue(NodesProperty); }
//            set { SetValue(NodesProperty, value); }
//        }

//        private void OnCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
//        {
//            UpdateEvents(e.OldItems.OfType<MarkerModel>(), e.NewItems.OfType<MarkerModel>());
//        }

//        private void UpdateEvents(IEnumerable<MarkerModel> oldItems, IEnumerable<MarkerModel> newItems)
//        {
//            if (oldItems != null)
//            {
//                foreach (MarkerModel oldItem in oldItems)
//                {
//                    oldItem.PropertyChanged -= ItemChanged;
//                }
//            }
//            if (newItems != null)
//            {
//                foreach (MarkerModel canvasItem in newItems)
//                {
//                    canvasItem.PropertyChanged += ItemChanged;
//                }
//            }
//            Invalidate();
//        }

//        private void ItemChanged(object sender, EventArgs e)
//        {
//            Invalidate();
//        }

//        protected override void DrawInternal(CanvasDrawingSession drawingSession, Map parentMap)
//        {
//            ObservableCollection<MarkerModel> nodes = Nodes;
//            if (nodes == null)
//            {
//                return;
//            }

//            bool needsAnimation = false;
//            foreach (MarkerModel node in nodes)
//            {
//                needsAnimation |= node.UpdateAnimation();
//                double scaleFactor = parentMap.ViewPortProjection.CartesianScaleFactor(node.Location);
//                CartesianPoint point = ParentMap.ViewPortProjection.ToCartesian(node.Location);
//                drawingSession.FillCircle(Scale(point), (node.AnimatedValue + 5) * Scale(scaleFactor), Colors.Red);
//            }

//            if (needsAnimation)
//            {
//                Invalidate();
//            }
//        }
//    }

//    public class MarkerModel : INotifyPropertyChanged
//    {
//        private ILocation _location;
//        private int _value;
//        private BatNode _node;
//        private float _animationStepSize;

//        public MarkerModel(BatNode node)
//        {
//            _node = node;
//            _location = _node.Location;
//        }

//        public float AnimatedValue { get; set; }

//        public bool UpdateAnimation()
//        {
//            if (Math.Abs(_value - AnimatedValue) >= Math.Abs(_animationStepSize))
//            {
//                AnimatedValue += _animationStepSize;
//                return true;
//            }
//            AnimatedValue = Value;
//            return false;
//        }

//        public ILocation Location
//        {
//            get { return _location; }
//            set
//            {
//                _location = value;
//                OnPropertyChanged();
//            }
//        }

//        public int Value
//        {
//            get { return _value; }
//            set
//            {
//                if (value != _value)
//                {
//                    _value = value;
//                    _animationStepSize = (_value - AnimatedValue) / 5f;
//                    if (Math.Abs(_animationStepSize) < 1)
//                    {
//                        _animationStepSize = Math.Sign(_animationStepSize);
//                    }
//                    OnPropertyChanged();
//                }
//            }
//        }

//        public event PropertyChangedEventHandler PropertyChanged;

//        public void Refresh(long position, long windowWidth)
//        {
//            long endTime = position + windowWidth;
//            int sum = 0;
//            foreach (BatCall call in _node.NodeData.Calls)
//            {
//                uint time = call.StartTimeMs + _node.ProjectOffset;
//                if (time > position && time < endTime)
//                {
//                    sum += call.MergeCount;
//                }
//            }
//            Value = sum;
//        }

//        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
//        {
//            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
//        }
//    }
//}