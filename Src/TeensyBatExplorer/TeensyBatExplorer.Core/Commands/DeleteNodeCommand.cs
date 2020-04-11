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
using System.Threading;
using System.Threading.Tasks;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

using TeensyBatExplorer.Core.Models;

namespace TeensyBatExplorer.Core.Commands
{
    public class DeleteNodeCommand
    {
        public async Task ExecuteAsync(ProjectManager projectManager, int nodeId, IProgress<CountProgress> progress, CancellationToken cancellationToken)
        {
            if (progress == null)
            {
                progress = new NoopProgress<CountProgress>();
            }

            using (ProjectContext db = projectManager.GetContext())
            {
                using (IDbContextTransaction transaction = await db.Database.BeginTransactionAsync(cancellationToken))
                {
                    await db.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM Calls WHERE NodeId = {nodeId}", cancellationToken);
                    await db.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM Nodes WHERE Id = {nodeId}", cancellationToken);
                    await transaction.CommitAsync(cancellationToken);
                }
            }

        }
    }
}