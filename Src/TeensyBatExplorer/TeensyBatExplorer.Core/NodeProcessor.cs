// 
// Teensy Bat Explorer - Copyright(C)  Meinrad Jean-Richard
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

using Microsoft.EntityFrameworkCore;

using TeensyBatExplorer.Core.Commands;
using TeensyBatExplorer.Core.Models;

namespace TeensyBatExplorer.Core
{
    public class NodeProcessor
    {
        private readonly ProjectManager _projectManager;

        public NodeProcessor(ProjectManager projectManager)
        {
            _projectManager = projectManager;
        }

        public async Task Process(int nodeId, IProgress<CountProgress> progress, CancellationToken cancellationToken)
        {
            await Task.Run(async () => await ProcessInternal(nodeId, progress, cancellationToken), cancellationToken).ConfigureAwait(false);
        }

        private async Task ProcessInternal(int nodeId, IProgress<CountProgress> progress, CancellationToken cancellationToken)
        {
            using (ProjectContext context = _projectManager.GetContext())
            {
                BatNode node = await context.Nodes.Include(n => n.DataFiles).SingleOrDefaultAsync(n => n.Id == nodeId, cancellationToken).ConfigureAwait(false);
                await context.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM Calls WHERE NodeId = {nodeId}", cancellationToken).ConfigureAwait(false);
                progress.Report(10, 100);

                await ProcessNode(node, context, progress, cancellationToken).ConfigureAwait(false);
                
                progress.Report(80, 100);
                await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                progress.Report(100, 100);
            }
        }

        private async Task ProcessNode(BatNode node, ProjectContext context, IProgress<CountProgress> progress, CancellationToken cancellationToken)
        {
            double pp = 70d / node.DataFiles.Count;
            int i = 0;
            foreach (BatDataFile dataFile in node.DataFiles)
            {
                List<BatDataFileEntry> entries = await context.DataFileEntries.Include(e => e.FftData)
                    .OrderBy(e => e.StartTimeMicros)
                    .Where(e => e.DataFileId == dataFile.Id).ToListAsync(cancellationToken).ConfigureAwait(false);
                
                AddDataFile(node, dataFile, entries, context);
                await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                i++;
                progress.Report((int)(i * pp), 100);
            }
        }

        private void AddDataFile(BatNode node, BatDataFile dataFile, IEnumerable<BatDataFileEntry> entries, ProjectContext context)
        {
            BatDataFileEntry[] fileEntries = entries.ToArray();

            if (dataFile.FirmwareVersion < 3)
            {
                // These Files have an incorrect ReferenceTime. Use the on from the Node...
                dataFile.ReferenceTime = node.StartTime;
            }

            long previousEndTime = 0;
            BatCall currentCall = null;
            foreach (BatDataFileEntry entry in fileEntries)
            {
                long callDiff = entry.StartTimeMicros - previousEndTime;
                if (callDiff <= 0)
                {
                    entry.StartTimeMicros = previousEndTime;
                    entry.PauseFromPrevEntryMicros = 0;
                }
                else if (callDiff > 1000 * 1000)
                {
                    currentCall = new BatCall();
                    currentCall.StartTime = dataFile.ReferenceTime.AddMicros(entry.StartTimeMicros);

                    currentCall.StartTimeMicros = entry.StartTimeMicros;
                    currentCall.Node = node;
                    entry.PauseFromPrevEntryMicros = null;
                }
                else
                {
                    entry.PauseFromPrevEntryMicros = callDiff;
                }

                entry.Call = currentCall;

                previousEndTime = entry.StartTimeMicros + (entry.FftCount * 500);
            }
        }
    }
}