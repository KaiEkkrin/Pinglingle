using System.Net.NetworkInformation;
using Microsoft.EntityFrameworkCore;
using Pinglingle.Shared.Model;

namespace Pinglingle.Server;

public class MyContext : DbContext
{
    public MyContext(DbContextOptions<MyContext> options) : base(options)
    {
    }

    public DbSet<Digest>? Digests { get; set; }
    public DbSet<Sample>? Samples { get; set; }
    public DbSet<Target>? Targets { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Digest>()
            .HasOne(s => s.Target)
            .WithMany(t => t.Digests)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<Digest>().HasIndex(d => d.StartTime);
        modelBuilder.Entity<Digest>().HasIndex(d => d.TargetId);

        modelBuilder.Entity<Sample>()
            .HasOne(s => s.Target)
            .WithMany(t => t.Samples)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Sample>().HasIndex(s => s.Date);
        modelBuilder.Entity<Sample>().HasIndex(s => s.TargetId);
        modelBuilder.Entity<Sample>().HasIndex(s => s.IsDigested);

        modelBuilder.Entity<Target>()
            .HasIndex(t => t.Address)
            .IsUnique();

        modelBuilder.Entity<Target>()
            .Property(t => t.Frequency)
            .HasDefaultValue(1);
    }
}