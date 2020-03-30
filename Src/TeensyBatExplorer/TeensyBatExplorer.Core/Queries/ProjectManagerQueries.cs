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

using TeensyBatExplorer.Core.Models;

namespace TeensyBatExplorer.Core.Queries
{
    public static class ProjectManagerQueries
    {
        public static async Task<IReadOnlyList<BatCall>> GetCallsForNode(this ProjectManager projectManager, int nodeNumber, CancellationToken cancellationToken)
        {
            using (ProjectContext db = projectManager.GetContext())
            {
                return await db.Calls.Where(c => c.Node.NodeNumber == nodeNumber).AsNoTracking().ToListAsync(cancellationToken);
            }
        }

        public static async Task<BatNode> GetBatNode(this ProjectManager projectManager, int nodeNumber, CancellationToken cancellationToken)
        {
            using (ProjectContext db = projectManager.GetContext())
            {
                return await db.Nodes.AsNoTracking().SingleOrDefaultAsync(n => n.NodeNumber == nodeNumber, cancellationToken);
            }
        }

        public static async Task<BatNode> GetBatNodeWithLogs(this ProjectManager projectManager, int nodeNumber, CancellationToken cancellationToken)
        {
            using (ProjectContext db = projectManager.GetContext())
            {
                return await db.Nodes
                    .Include(n => n.Logs).ThenInclude(l => l.Calls)
                    //.Include(l => l.Logs).ThenInclude(l => l.BatteryData)
                    //.Include(l => l.Logs).ThenInclude(l => l.TemperatureData)
                    .AsNoTracking().SingleOrDefaultAsync(n => n.NodeNumber == nodeNumber, cancellationToken);
            }
        }

        public static async Task<List<BatNode>> GetNodes(this ProjectManager projectManager, CancellationToken cancellationToken)
        {
            using (ProjectContext db = projectManager.GetContext())
            {
                return await db.Nodes.OrderBy(n => n.NodeNumber).AsNoTracking().ToListAsync(cancellationToken);
            }
        }
    }
}