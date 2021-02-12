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

using TeensyBatExplorer.Core.Infrastructure;
using TeensyBatExplorer.Core.Models;

namespace TeensyBatExplorer.Core.Commands
{
    public class AnalyzeNodeCommand
    {
        private readonly ProjectManager _projectManager;

        public AnalyzeNodeCommand(ProjectManager projectManager)
        {
            _projectManager = projectManager;
        }

        public async Task Process(int nodeId, StackableProgress progress, CancellationToken cancellationToken)
        {
            await Task.Run(async () => await ProcessInternal(nodeId, progress, cancellationToken), cancellationToken).ConfigureAwait(false);
        }

        private async Task ProcessInternal(int nodeId, StackableProgress progress, CancellationToken cancellationToken)
        {
            using (ProjectContext context = _projectManager.CreateContext())
            {
                BatNode node = await context.Nodes.Include(n => n.DataFiles).SingleOrDefaultAsync(n => n.Id == nodeId, cancellationToken).ConfigureAwait(false);
                await context.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM Calls WHERE NodeId = {nodeId}", cancellationToken).ConfigureAwait(false);
                progress.Report(10, 100);

                await ProcessNode(node, context, progress.Stack(90), cancellationToken).ConfigureAwait(false);

                progress.Report(100, 100);
            }
        }

        private async Task ProcessNode(BatNode node, ProjectContext context, StackableProgress progress, CancellationToken cancellationToken)
        {
            progress.Report(0, node.DataFiles.Count);
            int i = 0;
            foreach (BatDataFile dataFile in node.DataFiles)
            {
                IQueryable<BatDataFileEntry> entries = context.DataFileEntries
                    .OrderBy(e => e.StartTimeMicros)
                    .Where(e => e.DataFileId == dataFile.Id);

                await AddDataFile(node, dataFile, entries, context).ConfigureAwait(false);
                await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                progress.Report(i++);
            }
        }

        private async Task AddDataFile(BatNode node, BatDataFile dataFile, IEnumerable<BatDataFileEntry> entries, ProjectContext context)
        {
            if (dataFile.FirmwareVersion < 3)
            {
                // These Files have an incorrect ReferenceTime. Use the one from the Node...
                AddMessage(context, node, BatLogMessageLevel.Warning, "Firmwareversion < 3: Verwende Referenzzeit vom Gerät.");
                dataFile.ReferenceTime = node.StartTime;
            }

            long previousEndTime = 0;
            BatCall? currentCall = null;
            foreach (BatDataFileEntry entry in entries)
            {
                long callDiff = entry.StartTimeMicros - previousEndTime;
                if (currentCall == null || callDiff > 1000 * 1000)
                {
                    if (currentCall != null)
                    {
                        await AnalyzeCall(currentCall, context).ConfigureAwait(false);
                    }

                    currentCall = new BatCall();
                    currentCall.StartTime = dataFile.ReferenceTime.AddMicros(entry.StartTimeMicros);
                    currentCall.StartTimeMicros = entry.StartTimeMicros;
                    currentCall.Node = node;
                    currentCall.IsBat = true;
                    entry.PauseFromPrevEntryMicros = null;
                }
                else if (callDiff <= 0)
                {
                    entry.StartTimeMicros = previousEndTime;
                    entry.PauseFromPrevEntryMicros = 0;
                }
                else
                {
                    entry.PauseFromPrevEntryMicros = callDiff;
                }

                entry.Call = currentCall;
                currentCall.Entries.Add(entry);

                previousEndTime = entry.StartTimeMicros + entry.FftCount * 500;
            }

            if (currentCall != null)
            {
                // analyze the last Call...
                await AnalyzeCall(currentCall, context).ConfigureAwait(false);
            }
        }

        private void AddMessage(ProjectContext context, BatNode node, BatLogMessageLevel level, string message, params object[] args)
        {
            context.ProjectMessages.Add(new ProjectMessage(level, MessageTypes.NodeAnalysis, message, args) { Node = node });
        }

        private async Task AnalyzeCall(BatCall call, ProjectContext context)
        {
            long lastEntryEndTime = 0;
            int[] freqs = new int[128];

            foreach (BatDataFileEntry entry in call.Entries)
            {
                lastEntryEndTime = entry.StartTimeMicros + entry.FftCount * 500;
                IQueryable<FftBlock> blocks = context.FftBlocks.Where(f => f.DataFileEntryId == entry.Id).AsNoTracking();

                foreach (FftBlock fftBlock in blocks)
                {
                    byte[] fftData = fftBlock.Data;
                    for (int j = 10; j < fftData.Length; j++)
                    {
                        freqs[j] += fftData[j];
                    }
                }
            }

            call.DurationMicros = lastEntryEndTime - call.StartTimeMicros;

            int localMax = 0;
            int maxIndex = 0;
            for (int i = 0; i < freqs.Length; i++)
            {
                if (freqs[i] > localMax)
                {
                    localMax = freqs[i];
                    maxIndex = i;
                }
            }

            call.PeakFrequency = maxIndex;
        }
    }
}