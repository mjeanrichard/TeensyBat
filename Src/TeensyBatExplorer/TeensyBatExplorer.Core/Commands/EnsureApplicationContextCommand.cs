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