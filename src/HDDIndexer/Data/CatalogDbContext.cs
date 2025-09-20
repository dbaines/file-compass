using Microsoft.EntityFrameworkCore;
using HDDIndexer.Models;
using System;

namespace HDDIndexer.Data
{
    public class CatalogDbContext : DbContext
    {
        public DbSet<Drive> Drives { get; set; } = null!;
        public DbSet<FileEntry> Files { get; set; } = null!;

        public CatalogDbContext(DbContextOptions<CatalogDbContext> options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Drive entity configuration
            modelBuilder.Entity<Drive>(entity =>
            {
                entity.HasKey(d => d.DriveId);
                entity.Property(d => d.DriveName).IsRequired();
                entity.HasIndex(d => d.SerialNumber);
                entity.HasIndex(d => d.ScanDate);
            });

            // FileEntry entity configuration
            modelBuilder.Entity<FileEntry>(entity =>
            {
                entity.HasKey(f => f.FileId);
                entity.Property(f => f.FileName).IsRequired();
                entity.Property(f => f.FilePath).IsRequired();

                // Indexes for performance
                entity.HasIndex(f => f.FileName);
                entity.HasIndex(f => f.FileExtension);
                entity.HasIndex(f => f.DriveId);
                entity.HasIndex(f => f.FilePath);
                entity.HasIndex(f => f.ParentFileId);
                entity.HasIndex(f => new { f.DriveId, f.FileName });
                entity.HasIndex(f => new { f.DriveId, f.FileExtension });

                // Relationships
                entity.HasOne(f => f.Drive)
                    .WithMany(d => d.Files)
                    .HasForeignKey(f => f.DriveId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(f => f.ParentFile)
                    .WithMany(f => f.ChildFiles)
                    .HasForeignKey(f => f.ParentFileId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // Seed data for testing (optional)
            modelBuilder.Entity<Drive>().HasData(
                new Drive
                {
                    DriveId = -1,
                    DriveName = "Sample Drive",
                    VolumeLabel = "SAMPLE",
                    FileSystem = "NTFS",
                    TotalSize = 1000000000,
                    ScanDate = DateTime.UtcNow,
                    Description = "Sample drive for testing"
                }
            );
        }
    }
}