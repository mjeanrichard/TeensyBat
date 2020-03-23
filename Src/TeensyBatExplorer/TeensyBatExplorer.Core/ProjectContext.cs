using Microsoft.EntityFrameworkCore;

using TeensyBatExplorer.Core.Models;

namespace TeensyBatExplorer.Core
{
    public class ProjectContext : DbContext
    {
        private readonly string _filename;
        public DbSet<TemperatureData> TemperatureData { get; set; }
        public DbSet<BatteryData> BatteryData { get; set; }
        public DbSet<BatCall> Calls { get; set; }
        public DbSet<BatNode> Nodes { get; set; }
        public DbSet<BatProject> Projects { get; set; }
        public DbSet<BatLog> Logs{ get; set; }
        public DbSet<FftBlock> FftBlocks { get; set; }

        public ProjectContext(string filename)
        {
            _filename = filename;
        }

        protected override void OnConfiguring(DbContextOptionsBuilder options)
        {
            options.UseSqlite($"Data Source={_filename}");
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<TemperatureData>();
            modelBuilder.Entity<BatteryData>();
            modelBuilder.Entity<BatCall>().ToTable("Calls");
            modelBuilder.Entity<BatNode>().ToTable("Nodes");
            modelBuilder.Entity<BatLog>().ToTable("Logs");
            modelBuilder.Entity<BatProject>().ToTable("Projects");
            modelBuilder.Entity<FftBlock>().ToTable("FftBlocks");
        }
    }
}