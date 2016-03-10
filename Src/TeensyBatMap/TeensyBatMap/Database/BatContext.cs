using Microsoft.Data.Entity;
using TeensyBatMap.Domain;

namespace TeensyBatMap.Database
{
	public class BatContext : DbContext
	{
		private const string DbName = "batdata.sqlite";

		public DbSet<BatNodeLog> Logs { get; set; }
		public DbSet<BatCall> Calls { get; set; }

		protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
		{
			optionsBuilder.UseSqlite("Filename=batdata.sqlite");
		}

		protected override void OnModelCreating(ModelBuilder modelBuilder)
		{
			modelBuilder.Entity<BatNodeLog>().ToTable("BatNodeLogs");
			modelBuilder.Entity<BatCall>().ToTable("BatCalls");
		}
	}
}