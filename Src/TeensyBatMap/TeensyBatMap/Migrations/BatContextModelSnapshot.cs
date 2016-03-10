using System;
using Microsoft.Data.Entity;
using Microsoft.Data.Entity.Infrastructure;
using Microsoft.Data.Entity.Metadata;
using Microsoft.Data.Entity.Migrations;
using TeensyBatMap.Database;

namespace TeensyBatMap.Migrations
{
    [DbContext(typeof(BatContext))]
    partial class BatContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
            modelBuilder
                .HasAnnotation("ProductVersion", "7.0.0-rc1-16348");

            modelBuilder.Entity("TeensyBatMap.Domain.BatCall", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<int>("AvgFrequency");

                    b.Property<int>("AvgIntensity");

                    b.Property<int>("BatNodeLogId");

                    b.Property<int>("Duration");

                    b.Property<byte[]>("FftData");

                    b.Property<int>("MaxFrequency");

                    b.Property<int>("MaxIntensity");

                    b.Property<DateTime>("StartTime");

                    b.Property<int>("StartTimeMs");

                    b.HasKey("Id");

                    b.HasAnnotation("Relational:TableName", "BatCalls");
                });

            modelBuilder.Entity("TeensyBatMap.Domain.BatNodeLog", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<int>("CallCount");

                    b.Property<string>("Description");

                    b.Property<int?>("FirstCallId");

                    b.Property<int?>("LastCallId");

                    b.Property<double>("Latitude");

                    b.Property<DateTime>("LogStart");

                    b.Property<double>("Longitude");

                    b.Property<string>("Name");

                    b.HasKey("Id");

                    b.HasAnnotation("Relational:TableName", "BatNodeLogs");
                });

            modelBuilder.Entity("TeensyBatMap.Domain.BatCall", b =>
                {
                    b.HasOne("TeensyBatMap.Domain.BatNodeLog")
                        .WithMany()
                        .HasForeignKey("BatNodeLogId");
                });

            modelBuilder.Entity("TeensyBatMap.Domain.BatNodeLog", b =>
                {
                    b.HasOne("TeensyBatMap.Domain.BatCall")
                        .WithMany()
                        .HasForeignKey("FirstCallId");

                    b.HasOne("TeensyBatMap.Domain.BatCall")
                        .WithMany()
                        .HasForeignKey("LastCallId");
                });
        }
    }
}
