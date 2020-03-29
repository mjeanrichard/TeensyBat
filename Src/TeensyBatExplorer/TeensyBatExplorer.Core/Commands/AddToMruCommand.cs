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
using System.Threading.Tasks;

using Microsoft.EntityFrameworkCore;

using TeensyBatExplorer.Core.Models;

namespace TeensyBatExplorer.Core.Commands
{
    public class AddToMruCommand
    {
        private readonly Func<ApplicationContext> _appContextFactory;
        private readonly EnsureApplicationContextCommand _ensureApplicationContextCommand;

        public AddToMruCommand(Func<ApplicationContext> appContextFactory, EnsureApplicationContextCommand ensureApplicationContextCommand)
        {
            _appContextFactory = appContextFactory;
            _ensureApplicationContextCommand = ensureApplicationContextCommand;
        }

        public async Task Execute(BatProject batProject, string fullPath)
        {
            await _ensureApplicationContextCommand.Execute();

            using (ApplicationContext context = _appContextFactory())
            {
                ProjectMruEntry mruEntry = await context.ProjectMruEntries.FirstOrDefaultAsync(m => m.FullPath == fullPath.ToLowerInvariant());
                if (mruEntry == null)
                {
                    mruEntry = new ProjectMruEntry();
                    mruEntry.FullPath = fullPath.ToLowerInvariant();
                    context.ProjectMruEntries.Add(mruEntry);
                }

                mruEntry.ProjectName = batProject.Name;

                await context.SaveChangesAsync();
            }
        }
    }
}