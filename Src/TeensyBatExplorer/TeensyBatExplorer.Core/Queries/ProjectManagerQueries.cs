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

namespace TeensyBatExplorer.Core.Queries
{
    public static class ProjectManagerQueries
    {
        public static async Task<int> CountDataFileEntries(this ProjectManager projectManager, int dataFileId, CancellationToken cancellationToken)
        {
            return await Task.Run(async () =>
            {
                using (ProjectContext db = projectManager.CreateContext())
                {
                    return await db.DataFileEntries.CountAsync(e => e.DataFileId == dataFileId, cancellationToken).ConfigureAwait(false);
                }
            }, cancellationToken);
        }

        public static async Task<BatNode> GetBatNodeWithFiles(this ProjectManager projectManager, int nodeNumber, CancellationToken cancellationToken)
        {
            return await Task.Run(async () =>
            {
                using (ProjectContext db = projectManager.CreateContext())
                {
                    return await db.Nodes
                        .Include(n => n.DataFiles)
                        .SingleOrDefaultAsync(n => n.NodeNumber == nodeNumber, cancellationToken).ConfigureAwait(false);
                }
            }, cancellationToken);
        }

        public static async Task<List<BatCall>> GetCalls(this ProjectManager projectManager, int nodeId, bool batsOnly, StackableProgress progress, CancellationToken cancellationToken)
        {
            return await Task.Run(async () =>
            {
                using (ProjectContext db = projectManager.CreateContext())
                {
                    IQueryable<BatCall> callQuery = db.Calls.Where(c => c.NodeId == nodeId);
                    if (batsOnly)
                    {
                        callQuery = callQuery.Where(c => c.IsBat);
                    }

                    int count = await callQuery.CountAsync(cancellationToken);
                    List<BatCall> calls = new(count);
                    progress.Report(0, count);

                    bool hasMoreData = true;
                    while (hasMoreData)
                    {
                        IQueryable<BatCall> batCalls = callQuery.AsNoTracking()
                            .OrderBy(c => c.StartTime)
                            .Skip(calls.Count).Take(200);
                        List<BatCall> list = await batCalls.ToListAsync(cancellationToken).ConfigureAwait(false);

                        if (list.Any())
                        {
                            calls.AddRange(list);
                            progress.Report(calls.Count);
                        }
                        else
                        {
                            hasMoreData = false;
                        }
                    }

                    return calls;
                }
            }, cancellationToken);
        }


        public static async Task<List<BatDataFileEntry>> GetCall(this ProjectManager projectManager, int callId, StackableProgress progress, CancellationToken cancellationToken)
        {
            return await Task.Run(async () =>
            {
                using (ProjectContext db = projectManager.CreateContext())
                {
                    List<BatDataFileEntry> entries = await db.DataFileEntries
                        .Include(e => e.FftData)
                        .Where(c => c.CallId == callId)
                        .AsNoTracking()
                        .ToListAsync(cancellationToken)
                        .ConfigureAwait(false);
                    return entries;
                }
            }, cancellationToken);
        }

        public static async Task<List<BatteryData>> GetBatteryData(this ProjectManager projectManager, int nodeId, CancellationToken cancellationToken)
        {
            return await Task.Run(async () =>
            {
                using (ProjectContext db = projectManager.CreateContext())
                {
                    return await db.BatteryData.Where(b => b.DataFile!.NodeId == nodeId).OrderBy(b => b.DateTime).AsNoTracking().ToListAsync(cancellationToken).ConfigureAwait(false);
                }
            }, cancellationToken);
        }

        public static async Task<List<TemperatureData>> GetTemperatureData(this ProjectManager projectManager, int nodeId, CancellationToken cancellationToken)
        {
            return await Task.Run(async () =>
            {
                using (ProjectContext db = projectManager.CreateContext())
                {
                    return await db.TemperatureData.Where(b => b.DataFile!.NodeId == nodeId).OrderBy(b => b.DateTime).AsNoTracking().ToListAsync(cancellationToken).ConfigureAwait(false);
                }
            }, cancellationToken);
        }

        public static async Task<List<BatNode>> GetNodes(this ProjectManager projectManager, CancellationToken cancellationToken)
        {
            return await Task.Run(async () =>
            {
                using (ProjectContext db = projectManager.CreateContext())
                {
                    return await db.Nodes.OrderBy(n => n.NodeNumber).AsNoTracking().ToListAsync(cancellationToken).ConfigureAwait(false);
                }
            }, cancellationToken);
        }

        public static async Task<int> GetCallCount(this ProjectManager projectManager, int nodeId, CancellationToken cancellationToken)
        {
            return await Task.Run(async () =>
            {
                using (ProjectContext db = projectManager.CreateContext())
                {
                    return await db.Calls.CountAsync(c => c.NodeId == nodeId, cancellationToken).ConfigureAwait(false);
                }
            }, cancellationToken);
        }

        public static async Task<int> GetDataFileCount(this ProjectManager projectManager, int nodeId, CancellationToken cancellationToken)
        {
            return await Task.Run(async () =>
            {
                using (ProjectContext db = projectManager.CreateContext())
                {
                    return await db.DataFiles.CountAsync(c => c.NodeId == nodeId, cancellationToken).ConfigureAwait(false);
                }
            }, cancellationToken);
        }
    }
}