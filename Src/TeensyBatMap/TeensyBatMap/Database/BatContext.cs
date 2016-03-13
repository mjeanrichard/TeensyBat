﻿using System.Collections.Generic;
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

		protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
		{
			optionsBuilder.UseSqlite("Filename=batdata.sqlite");
		}

		protected override void OnModelCreating(ModelBuilder modelBuilder)
		{
			modelBuilder.Entity<BatNodeLog>().ToTable("BatNodeLogs");
			modelBuilder.Entity<BatCall>().ToTable("BatCalls");
		}

		public async Task<IEnumerable<BatCall>> LoadCalls(BatNodeLog batLog)
		{
			return await Calls.Where(c => c.BatNodeLogId == batLog.Id).OrderBy(c => c.StartTimeMs).ToListAsync();
		}
	}
}