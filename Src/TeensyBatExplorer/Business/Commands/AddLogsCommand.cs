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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using LiteDB;

using TeensyBatExplorer.Business.Models;

namespace TeensyBatExplorer.Business.Commands
{
    public class AddLogsCommand
    {
        public async Task ExecuteAsync(ProjectManager projectManager, IEnumerable<BatLog> loadedFiles, IProgress<CountProgress> progress, CancellationToken cancellationToken)
        {
            await Task.Run(() => ExecuteInternalAsync(projectManager, loadedFiles, progress, cancellationToken), cancellationToken).ConfigureAwait(false);
        }

        private void ExecuteInternalAsync(ProjectManager projectManager, IEnumerable<BatLog> loadedFiles, IProgress<CountProgress> progress, CancellationToken cancellationToken)
        {
            LiteDatabase db = projectManager.GetDatabase();

            int i = 0;
            BatLog[] batLogs = loadedFiles.ToArray();
            foreach (BatLog batLog in batLogs)
            {
                cancellationToken.ThrowIfCancellationRequested();
                i++;
                progress.Report(new CountProgress { Current = i, Total = batLogs.Length, Text = $"'{batLog.Filename}'..." });
                db.BeginTrans();
                AddBatLog(db, batLog, cancellationToken);
                db.Commit();
                db.Checkpoint();
            }
        }

        private void AddBatLog(LiteDatabase db, BatLog batLog, CancellationToken cancellationToken)
        {
            ILiteCollection<BatLog> logCollection = db.GetCollection<BatLog>();
            ILiteCollection<BatNode> nodeCollection = db.GetCollection<BatNode>();
            ILiteCollection<BatCall> batCallCollection = db.GetCollection<BatCall>();
            ILiteCollection<BatteryData> batteryDataCollection = db.GetCollection<BatteryData>();
            ILiteCollection<TemperatureData> tempDataCollection = db.GetCollection<TemperatureData>();

            BatNode batNode = nodeCollection.FindOne(n => n.NodeId == batLog.NodeNumber);

            if (batNode == null)
            {
                batNode = new BatNode { NodeId = batLog.NodeNumber };
                nodeCollection.Insert(batNode);
            }

            batLog.NodeId = batNode.Id;
            logCollection.Insert(batLog);

            batNode.Logs.Add(batLog);
            
            foreach (BatCall call in batLog.Calls)
            {
                call.NodeId = batNode.Id;
                call.LogId = batLog.Id;
            }
            batCallCollection.InsertBulk(batLog.Calls);

            foreach (BatteryData bat in batLog.BatteryData)
            {
                bat.NodeId = batNode.Id;
                bat.LogId = batLog.Id;
            }
            batteryDataCollection.Insert(batLog.BatteryData);

            foreach (TemperatureData temp in batLog.TemperatureData)
            {
                temp.NodeId = batNode.Id;
                temp.LogId = batLog.Id;
            }
            tempDataCollection.Insert(batLog.TemperatureData);
        }
    }

    public class CountProgress
    {
        public int Total { get; set; }
        public int Current { get; set; }
        public string Text { get; set; }
    }
}