using System.Net.NetworkInformation;
using Microsoft.EntityFrameworkCore;
using Pinglingle.Shared.Model;

namespace Pinglingle.Server;

public class MyContext : DbContext
{
    public MyContext(DbContextOptions<MyContext> options) : base(options)
    {
    }

    public DbSet<Sample>? Samples { get; set; }
    public DbSet<Target>? Targets { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Sample>()
            .HasOne(s => s.Target)
            .WithMany(t => t.Samples)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Sample>().HasIndex(s => s.Date);
        modelBuilder.Entity<Sample>().HasIndex(s => s.TargetId);

        modelBuilder.Entity<Target>()
            .HasIndex(t => t.Address)
            .IsUnique();
    }
}