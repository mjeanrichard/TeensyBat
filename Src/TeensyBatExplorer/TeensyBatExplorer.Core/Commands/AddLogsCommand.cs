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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

using TeensyBatExplorer.Core.Models;

namespace TeensyBatExplorer.Core.Commands
{
    public class AddLogsCommand
    {
        public async Task ExecuteAsync(ProjectManager projectManager, IEnumerable<BatLog> loadedFiles, IProgress<CountProgress> progress, CancellationToken cancellationToken)
        {
            if (progress == null)
            {
                progress = new NoopProgress<CountProgress>();
            }

            int i = 0;
            BatLog[] batLogs = loadedFiles.ToArray();

            foreach (BatLog batLog in batLogs)
            {
                cancellationToken.ThrowIfCancellationRequested();
                i++;
                progress.Report(new CountProgress { Current = i, Total = batLogs.Length, Text = $"'{batLog.Filename}'..." });

                using (ProjectContext db = projectManager.GetContext())
                {
                    using (IDbContextTransaction transaction = await db.Database.BeginTransactionAsync(cancellationToken))
                    {
                        await AddBatLog(db, batLog, cancellationToken);
                        await db.SaveChangesAsync(cancellationToken);
                        await transaction.CommitAsync(cancellationToken);
                    }
                }
            }
        }

        private async Task AddBatLog(ProjectContext db, BatLog batLog, CancellationToken cancellationToken)
        {
            BatNode batNode = await db.Nodes.SingleOrDefaultAsync(n => n.NodeNumber == batLog.NodeNumber, cancellationToken);

            if (batNode == null)
            {
                batNode = new BatNode { NodeNumber = batLog.NodeNumber };
                db.Nodes.Add(batNode);
            }

            batLog.Node = batNode;
            db.Logs.Add(batLog);

            foreach (BatCall call in batLog.Calls)
            {
                call.Node = batNode;
                call.Log = batLog;
            }

            foreach (BatteryData bat in batLog.BatteryData)
            {
                bat.Node = batNode;
                bat.Log = batLog;
            }

            foreach (TemperatureData temp in batLog.TemperatureData)
            {
                temp.Node = batNode;
                temp.Log = batLog;
            }
        }
    }
}