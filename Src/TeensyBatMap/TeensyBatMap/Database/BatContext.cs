using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.Data.Entity;
using TeensyBatMap.Domain;

namespace TeensyBatMap.Database
{
	public class BatContext : DbContext
	{
		private const string DbName = "batdata.sqlite";

		public DbSet<BatNodeLog> Logs { get; set; }
		public DbSet<BatCall> Calls { get; set; }
		public DbSet<BatInfo> Infos { get; set; }

		protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
		{
			optionsBuilder.UseSqlite("Filename=batdata.sqlite");
		}

		protected override void OnModelCreating(ModelBuilder modelBuilder)
		{
			modelBuilder.Entity<BatNodeLog>().ToTable("BatNodeLogs");
			modelBuilder.Entity<BatCall>().ToTable("BatCalls");
			modelBuilder.Entity<BatInfo>().ToTable("BatInfos");
		}

		public async Task<IEnumerable<BatCall>> LoadCalls(BatNodeLog batLog, bool includeDisabled)
		{
			IQueryable<BatCall> batCalls = Calls.Where(c => c.BatNodeLogId == batLog.Id);
			if (!includeDisabled)
			{
				batCalls = batCalls.Where(c => c.Enabled);
			}
			return await batCalls.OrderBy(c => c.StartTimeMs).ToListAsync();
		}

		public async Task<IEnumerable<BatInfo>> LoadInfos(BatNodeLog batLog)
		{
			return await Infos.Where(c => c.BatNodeLogId == batLog.Id).OrderBy(c => c.TimeMs).ToListAsync();
		}
	}
}