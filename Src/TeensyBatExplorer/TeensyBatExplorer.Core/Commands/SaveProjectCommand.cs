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
using System.Threading;
using System.Threading.Tasks;

using Microsoft.EntityFrameworkCore;

using TeensyBatExplorer.Core.Models;

namespace TeensyBatExplorer.Core.Commands
{
    public class SaveProjectCommand
    {
        private readonly AddToMruCommand _addToMruCommand;

        public SaveProjectCommand(AddToMruCommand addToMruCommand)
        {
            _addToMruCommand = addToMruCommand;
        }

        public async Task Execute(ProjectManager projectManager, BatProject project, CancellationToken cancellationToken)
        {
            if (!projectManager.IsProjectOpen || projectManager.Filename == null)
            {
                throw new InvalidOperationException("Es ist kein Projekt offen!");
            }

            using (ProjectContext db = projectManager.CreateContext())
            {
                BatProject dbProject = await db.Projects.SingleAsync(cancellationToken);
                dbProject.Name = project.Name;
                await db.SaveChangesAsync(cancellationToken);
                await _addToMruCommand.Execute(dbProject, projectManager.Filename);
            }
        }
    }
}