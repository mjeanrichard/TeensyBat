// 
// Teensy Bat Explorer - Copyright(C) 2017 Meinard Jean-Richard
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

using Windows.Storage;

using Newtonsoft.Json;

namespace TeensyBatExplorer.Core.BatLog
{
    public class BatProject
    {
        public BatProject()
        {
            Nodes = new List<BatNode>();
        }

        public string Name { get; set; }

        public List<BatNode> Nodes { get; private set; }

        public DateTime ProjectStart { get; set; }

        public uint Duration { get; set; }
        public string FolderName { get; set; }

        public void AddNode(BatNode node)
        {
            node.Number = Nodes.Count + 1;
            node.FolderName = $"Node-{node.Number:D4}";
            Nodes.Add(node);
        }

        public void Refresh()
        {
            //Duration = 0;
            //ProjectStart = Logs.Min(l => l.LogStart);
            //foreach (RawNodeData log in Logs)
            //{
            //    log.ProjectOffset = (uint)(log.LogStart - ProjectStart).TotalMilliseconds;
            //    Duration = Math.Max(Duration, log.ProjectOffset + log.Duration);
            //}
        }
    }
}