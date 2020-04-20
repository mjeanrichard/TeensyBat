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

namespace TeensyBatExplorer.Core.Models
{
    public class ProjectMessage
    {
        public ProjectMessage()
        {
        }

        public ProjectMessage(BatLogMessageLevel level, MessageTypes type, string format, params object[] args)
        {
            MessageType = type;
            Level = level;
            Message = string.Format(format, args);
        }

        public int Id { get; set; }
        public DateTime Timestamp { get; private set; }
        public string Message { get; set; }
        public long? Position { get; set; }
        public BatLogMessageLevel Level { get; set; }
        public MessageTypes MessageType { get; set; }

        public int? DataFileId { get; set; }
        public BatDataFile DataFile { get; set; }

        public int? NodeId { get; set; }
        public BatNode Node { get; set; }
    }
}