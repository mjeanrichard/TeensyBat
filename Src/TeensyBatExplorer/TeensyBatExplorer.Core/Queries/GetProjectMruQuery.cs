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
using System.Threading.Tasks;

using Microsoft.EntityFrameworkCore;

namespace TeensyBatExplorer.Core.Queries
{
    public class GetProjectMruQuery
    {
        private readonly Func<ApplicationContext> _appContextFactory;

        public GetProjectMruQuery(Func<ApplicationContext> appContextFactory)
        {
            _appContextFactory = appContextFactory;
        }

        public async Task<List<ProjectMruEntry>> Execute()
        {
            using (ApplicationContext context = _appContextFactory())
            {
                return await context.ProjectMruEntries.OrderByDescending(m => m.LastAccessTime).Take(10).AsNoTracking().ToListAsync();
            }
        }
    }
}