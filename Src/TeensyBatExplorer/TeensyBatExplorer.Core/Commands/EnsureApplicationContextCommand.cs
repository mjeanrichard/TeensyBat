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
using System.IO;
using System.Threading.Tasks;

using Microsoft.EntityFrameworkCore;

namespace TeensyBatExplorer.Core.Commands
{
    public class EnsureApplicationContextCommand
    {
        private readonly Func<ApplicationContext> _appContextFactory;

        public EnsureApplicationContextCommand(Func<ApplicationContext> appContextFactory)
        {
            _appContextFactory = appContextFactory;
        }

        public async Task Execute()
        {
            string folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "BatExplorer");
            if (!Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
            }

            if (!File.Exists(Path.Combine(folder, "data.db")))
            {
                using (ApplicationContext context = _appContextFactory())
                {
                    await context.Database.ExecuteSqlRawAsync(
                        @"
                            CREATE TABLE 'ProjectMruEntries' (
	                            'Id'	INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT UNIQUE,
	                            'ProjectName'	TEXT NOT NULL,
	                            'FullPath'	TEXT NOT NULL,
	                            'LastAccessTime'	TEXT NOT NULL
                            );
                        ");
                }
            }
        }
    }
}