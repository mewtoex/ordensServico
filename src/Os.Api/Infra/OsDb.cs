using Microsoft.EntityFrameworkCore;
using Os.Api.Domain;

namespace Os.Api.Infra;

public class OsDb(DbContextOptions<OsDb> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<CatalogItem> Catalog => Set<CatalogItem>();
    public DbSet<ServiceOrder> Orders => Set<ServiceOrder>();
    public DbSet<AuditEntry> AuditEntries => Set<AuditEntry>();
    public DbSet<RefreshSession> RefreshSessions => Set<RefreshSession>();
    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<User>().Property(user => user.Version).IsRowVersion();
        b.Entity<RefreshSession>().Property(session => session.Id).ValueGeneratedNever();
        b.Entity<RefreshSession>().Property(session => session.Version).IsRowVersion();
        b.Entity<RefreshSession>().Property(session => session.TokenHash).HasMaxLength(64);
        b.Entity<RefreshSession>().HasIndex(session => session.TokenHash).IsUnique();
        b.Entity<RefreshSession>().HasIndex(session => session.ExpiresAt);
        b.Entity<RefreshSession>().HasOne(session => session.User).WithMany().HasForeignKey(session => session.UserId).OnDelete(DeleteBehavior.Cascade);
        b.Entity<AuditEntry>().Property(entry => entry.Id).ValueGeneratedNever();
        b.Entity<OrderItem>().Property(item => item.Id).ValueGeneratedNever();
        b.Entity<AuditEntry>().ToTable("AuditEntry");
        b.Entity<User>().HasIndex(x => x.Email).IsUnique();
        b.Entity<User>().Property(x => x.Email).HasMaxLength(254);
        b.Entity<User>().Property(x => x.Name).HasMaxLength(160);
        b.Entity<Customer>().Property(x => x.Name).HasMaxLength(160);
        b.Entity<CatalogItem>().Property(x => x.Price).HasPrecision(18, 2);
        b.Entity<OrderItem>().Property(x => x.UnitPrice).HasPrecision(18, 2);
        b.Entity<ServiceOrder>().Ignore(x => x.Total);
        b.Entity<ServiceOrder>().Property(x => x.Version).IsRowVersion();
        b.Entity<ServiceOrder>().HasIndex(x => new { x.Status, x.CreatedAt });
        b.Entity<ServiceOrder>().HasOne(x => x.Technician).WithMany().HasForeignKey(x => x.TechnicianId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<ServiceOrder>().HasOne(x => x.Customer).WithMany().HasForeignKey(x => x.CustomerId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<OrderItem>().HasOne(x => x.CatalogItem).WithMany().HasForeignKey(x => x.CatalogItemId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<AuditEntry>().HasOne(x => x.Actor).WithMany().HasForeignKey(x => x.ActorId).OnDelete(DeleteBehavior.Restrict);
    }
}
