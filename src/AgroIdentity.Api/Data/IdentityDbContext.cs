using AgroIdentity.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace AgroIdentity.Api.Data;

public class IdentityDbContext : DbContext
{
    public IdentityDbContext(DbContextOptions<IdentityDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // 1 DB único: isola o Identity no schema "identity"
        modelBuilder.HasDefaultSchema("identity");

        modelBuilder.Entity<User>()
            .HasIndex(u => u.Email)
            .IsUnique();

        base.OnModelCreating(modelBuilder);
    }
}