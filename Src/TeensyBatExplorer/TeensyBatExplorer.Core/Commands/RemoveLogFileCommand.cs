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
using System.Data;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

using TeensyBatExplorer.Core.Infrastructure;
using TeensyBatExplorer.Core.Models;

namespace TeensyBatExplorer.Core.Commands
{
    public class RemoveLogFileCommand
    {
        public async Task Execute(ProjectManager projectManager, int dataFileId, IProgress<CountProgress> progress, CancellationToken cancellationToken)
        {
            await Task.Run(async () => await ExecuteInternal(projectManager, dataFileId, progress, cancellationToken), cancellationToken).ConfigureAwait(false);
        }

        private async Task ExecuteInternal(ProjectManager projectManager, int dataFileId, IProgress<CountProgress> progress, CancellationToken cancellationToken)
        {
            using (ProjectContext context = projectManager.GetContext())
            {
                IDbContextTransaction transaction = await context.Database.BeginTransactionAsync(cancellationToken);
                await context.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM BatteryData WHERE DataFileId = {dataFileId}", cancellationToken).ConfigureAwait(false);
                progress.Report(20, 100);
                await context.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM TemperatureData WHERE DataFileId = {dataFileId}", cancellationToken).ConfigureAwait(false);
                progress.Report(40, 100);
                await context.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM ProjectMessages WHERE DataFileId = {dataFileId}", cancellationToken).ConfigureAwait(false);
                progress.Report(50, 100);
                await context.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM DataFileEntries WHERE DataFileId = {dataFileId}", cancellationToken).ConfigureAwait(false);
                progress.Report(90, 100);
                await context.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM DataFiles WHERE Id = {dataFileId}", cancellationToken).ConfigureAwait(false);
                progress.Report(100, 100);
                await transaction.CommitAsync(cancellationToken);
            }
        }

    }
}