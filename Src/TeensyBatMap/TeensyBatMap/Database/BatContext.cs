using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.Data.Entity;
using Microsoft.Data.Entity.Storage;
using Microsoft.Data.Sqlite;

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

		public async Task InsertLog(BatNodeLog log)
		{
			using (IRelationalTransaction transaction = await Database.BeginTransactionAsync())
			{
				List<BatCall> calls = log.Calls;
				log.Calls = new List<BatCall>();
				Logs.Add(log);
				await SaveChangesAsync();

				DbConnection dbConnection = Database.GetDbConnection();

				await InsertCalls(log, calls, dbConnection);
				transaction.Commit();
			}
		}

		private static async Task InsertCalls(BatNodeLog log, List<BatCall> calls, DbConnection dbConnection)
		{
			foreach (BatCall call in calls.Where(c => c.MaxPower >= 200))
			{
				DbCommand cmd = dbConnection.CreateCommand();
				cmd.CommandText = $"INSERT INTO BatCalls ([AvgFrequency],[BatNodeLogId],[ClippedSamples],[DcOffset],[Duration],[Enabled],[FftData],[MaxFrequency],[MaxPower],[MissedSamples],[PowerData],[StartTime],[StartTimeMs]) VALUES (@0,@1,@2,@3,@4,@5,@6,@7,@8,@9,@10,@11,@12);";
				cmd.Parameters.Add(new SqliteParameter("@0", call.AvgFrequency));
				cmd.Parameters.Add(new SqliteParameter("@1", log.Id));
				cmd.Parameters.Add(new SqliteParameter("@2", call.ClippedSamples));
				cmd.Parameters.Add(new SqliteParameter("@3", call.DcOffset));
				cmd.Parameters.Add(new SqliteParameter("@4", call.Duration));
				cmd.Parameters.Add(new SqliteParameter("@5", call.Enabled));
				cmd.Parameters.Add(new SqliteParameter("@6", call.FftData));
				cmd.Parameters.Add(new SqliteParameter("@7", call.MaxFrequency));
				cmd.Parameters.Add(new SqliteParameter("@8", call.MaxPower));
				cmd.Parameters.Add(new SqliteParameter("@9", call.MissedSamples));
				cmd.Parameters.Add(new SqliteParameter("@10", call.PowerData));
				cmd.Parameters.Add(new SqliteParameter("@11", call.StartTime));
				cmd.Parameters.Add(new SqliteParameter("@12", call.StartTimeMs));
				await cmd.ExecuteNonQueryAsync();
			}
		}

		public async Task<IEnumerable<BatCall>> LoadCalls(BatNodeLog batLog, bool includeDisabled, bool asReadonly = false)
		{
			IQueryable<BatCall> batCalls = Calls.Where(c => c.BatNodeLogId == batLog.Id);
			if (!includeDisabled)
			{
				batCalls = batCalls.Where(c => c.Enabled);
			}
			if (asReadonly)
			{
				batCalls = batCalls.AsNoTracking();
			}
			return await batCalls.OrderBy(c => c.StartTimeMs).ToListAsync();
		}

		public async Task<IEnumerable<BatInfo>> LoadInfos(BatNodeLog batLog)
		{
			return await Infos.Where(c => c.BatNodeLogId == batLog.Id).OrderBy(c => c.TimeMs).ToListAsync();
		}
	}
}