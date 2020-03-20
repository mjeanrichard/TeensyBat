// 
// Teensy Bat Explorer - Copyright(C) 2019 Meinrad Jean-Richard
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

using LiteDB;

using TeensyBatExplorer.Helpers.ViewModels;

namespace TeensyBatExplorer.Business.Models
{
    public class BatLog : Observable
    {
        public int Id { get; set; }

        public int NodeNumber { get; set; }
        public int FirmwareVersion { get; set; }
        public int HardwareVersion { get; set; }
        public bool Debug { get; set; }
        
        public int PreCallBufferSize { get; set; }
        public int AfterCallBufferSize { get; set; }
        public int CallStartThreshold { get; set; }
        public int CallEndThreshold { get; set; }

        public int ErrorCountCallBuffFull { get; set; }
        public int ErrorCountPointerBufferFull { get; set; }
        public int ErrorCountDataBufferFull { get; set; }
        public int ErrorCountProcessOverlap { get; set; }
        
        public string Filename { get; set; }

        public DateTime StartTime { get; set; }

        [BsonRef(nameof(BatCall))]
        public List<BatCall> Calls { get; set; } = new List<BatCall>();

        [BsonRef(nameof(Models.BatteryData))]
        public List<BatteryData> BatteryData { get; set; } = new List<BatteryData>();

        [BsonRef(nameof(Models.TemperatureData))]
        public List<TemperatureData> TemperatureData { get; set; } = new List<TemperatureData>();

        public int NodeId { get; set; }
    }
}