using System;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.EntityFrameworkCore;

using TeensyBatExplorer.Core.Infrastructure;
using TeensyBatExplorer.Core.Models;

namespace TeensyBatExplorer.Core.Commands
{
    public class UpdateNodeCommand
    {
        public async Task ExecuteAsync(ProjectManager projectManager, BatNode node, IProgress<CountProgress> progress, CancellationToken cancellationToken)
        {
            if (progress == null)
            {
                progress = new NoopProgress<CountProgress>();
            }

            using (ProjectContext db = projectManager.GetContext())
            {
                BatNode dbNode = await db.Nodes.SingleAsync(n => n.Id == node.Id, cancellationToken);

                dbNode.StartTime = node.StartTime;
                dbNode.Latitude = node.Latitude;
                dbNode.Longitude = node.Longitude;
                dbNode.NodeNumber = node.NodeNumber;

                await db.SaveChangesAsync(cancellationToken);
            }

        }
    }
}