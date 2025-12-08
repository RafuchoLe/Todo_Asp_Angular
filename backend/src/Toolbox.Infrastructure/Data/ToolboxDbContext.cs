using Microsoft.EntityFrameworkCore;
using Toolbox.Core.Entities;

namespace Toolbox.Infrastructure.Data;

public class ToolboxDbContext : DbContext
{
    public ToolboxDbContext(DbContextOptions<ToolboxDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users { get; set; } = null!;
    public DbSet<TodoItem> TodoItems { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply akk configuration from assembly
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ToolboxDbContext).Assembly);
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var entries = ChangeTracker.Entries()
            .Where(e => e.Entity is BaseEntity && e.State == EntityState.Modified);

        foreach (var entry in entries)
        {
            ((BaseEntity)entry.Entity).UpdatedAt = DateTime.UtcNow;
        }
        return base.SaveChangesAsync(cancellationToken);
    }
}