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

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

using TeensyBatExplorer.Core.Models;

namespace TeensyBatExplorer.Core
{
    public class ProjectContext : DbContext
    {
        private readonly string _filename;

        public ProjectContext(string filename)
        {
            _filename = filename;
        }

        public DbSet<TemperatureData> TemperatureData { get; set; }
        public DbSet<BatteryData> BatteryData { get; set; }
        public DbSet<BatDataFileEntry> DataFileEntries { get; set; }
        public DbSet<BatNode> Nodes { get; set; }
        public DbSet<BatProject> Projects { get; set; }
        public DbSet<BatDataFile> DataFiles { get; set; }
        public DbSet<DataFileMessage> DataFileMessages { get; set; }
        public DbSet<FftBlock> FftBlocks { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder options)
        {
            options.UseSqlite($"Data Source={_filename}");
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<TemperatureData>();
            modelBuilder.Entity<BatteryData>();
            modelBuilder.Entity<BatDataFileEntry>().ToTable("DataFileEntries");
            modelBuilder.Entity<BatNode>().ToTable("Nodes");
            modelBuilder.Entity<BatDataFile>().ToTable("DataFiles");
            modelBuilder.Entity<DataFileMessage>().ToTable("DataFileMessages").Property(m => m.Level).HasConversion(new EnumToStringConverter<BatLogMessageLevel>());
            modelBuilder.Entity<BatProject>().ToTable("Projects");
            modelBuilder.Entity<FftBlock>().ToTable("FftBlocks");
        }
    }
}