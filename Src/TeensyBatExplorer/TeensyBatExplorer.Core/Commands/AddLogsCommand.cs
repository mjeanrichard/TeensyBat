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

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

using TeensyBatExplorer.Core.Infrastructure;
using TeensyBatExplorer.Core.Models;

namespace TeensyBatExplorer.Core.Commands
{
    public class AddLogsCommand
    {
        private readonly AnalyzeNodeCommand _analyzeNodeCommand;

        public AddLogsCommand(AnalyzeNodeCommand analyzeNodeCommand)
        {
            _analyzeNodeCommand = analyzeNodeCommand;
        }

        public async Task ExecuteAsync(ProjectManager projectManager, IEnumerable<BatDataFile> loadedFiles, StackableProgress progress, CancellationToken cancellationToken)
        {
            int i = 0;
            BatDataFile[] batLogs = loadedFiles.ToArray();

            StackableProgress addProgress = progress.Stack(80);

            foreach (BatDataFile batLog in batLogs)
            {
                cancellationToken.ThrowIfCancellationRequested();
                addProgress.Report($"Importiere '{batLog.Filename}'...", i++, batLogs.Length);

                using (ProjectContext db = projectManager.CreateContext())
                {
                    using (IDbContextTransaction transaction = await db.Database.BeginTransactionAsync(cancellationToken))
                    {
                        await AddBatLog(db, batLog, addProgress.Stack(1), cancellationToken);
                        await db.SaveChangesAsync(cancellationToken);
                        await transaction.CommitAsync(cancellationToken);
                    }
                }
            }

            StackableProgress analyzeProgress = progress.Stack(20);

            int[] nodeIds = batLogs.Select(b => b.NodeId).Distinct().ToArray();
            for (int index = 0; index < nodeIds.Length; index++)
            {
                int nodeId = nodeIds[index];
                analyzeProgress.Report($"Analysiere Gerätedaten {index + 1}/{nodeIds.Length}", index, nodeIds.Length);
                await _analyzeNodeCommand.Process(nodeId, analyzeProgress.Stack(1), cancellationToken);
            }
        }

        private async Task AddBatLog(ProjectContext db, BatDataFile batDataFile, StackableProgress progress, CancellationToken cancellationToken)
        {
            BatNode batNode = await db.Nodes.SingleOrDefaultAsync(n => n.NodeNumber == batDataFile.NodeNumber, cancellationToken);

            if (batNode == null)
            {
                batNode = new BatNode { NodeNumber = batDataFile.NodeNumber };
                batNode.CallEndThreshold = batDataFile.CallEndThreshold;
                batNode.CallStartThreshold = batDataFile.CallStartThreshold;
                batNode.StartTime = batDataFile.ReferenceTime;
                db.Nodes.Add(batNode);
            }

            if (batNode.StartTime > batDataFile.ReferenceTime)
            {
                batNode.StartTime = batDataFile.ReferenceTime;
            }

            batDataFile.Node = batNode;
            db.DataFiles.Add(batDataFile);

            progress.Report(20);

            foreach (BatDataFileEntry call in batDataFile.Entries)
            {
                call.DataFile = batDataFile;
            }

            await db.SaveChangesAsync(cancellationToken);

            progress.Report(70);

            foreach (BatteryData bat in batDataFile.BatteryData)
            {
                bat.DataFile = batDataFile;
            }

            await db.SaveChangesAsync(cancellationToken);

            progress.Report(90);

            foreach (TemperatureData temp in batDataFile.TemperatureData)
            {
                temp.DataFile = batDataFile;
            }

            await db.SaveChangesAsync(cancellationToken);

            progress.Report(100);
        }
    }
}