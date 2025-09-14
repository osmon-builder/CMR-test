// Data/AppDbContext.cs
using Microsoft.EntityFrameworkCore;
using MiniCrm.Api.Models;

public class AppDbContext : DbContext
{
    private readonly Guid _tenantId; // vendrá del JWT en producción
    private readonly ITenantProvider _tenantProvider;

    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }
    public AppDbContext(DbContextOptions<AppDbContext> options, Guid tenantId) : base(options) => _tenantId = tenantId;
    public AppDbContext(DbContextOptions<AppDbContext> options, ITenantProvider tenantProvider) : base(options)
    {
        _tenantProvider = tenantProvider;
    }

    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Client> Clients => Set<Client>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<Tenant>().HasKey(x => x.Id);

        b.Entity<User>().HasKey(x => x.Id);
        b.Entity<User>().HasIndex(x => new { x.TenantId, x.Email }).IsUnique();
        b.Entity<User>().HasQueryFilter(x => _tenantProvider.TenantId == Guid.Empty || x.TenantId == _tenantProvider.TenantId);

        b.Entity<Client>().HasKey(x => x.Id);
        b.Entity<Client>().HasIndex(x => new { x.TenantId, x.Email });

        // Filtro global por tenant (cuando inyectes tenantId)
        b.Entity<Client>().HasQueryFilter(x => _tenantId == Guid.Empty || x.TenantId == _tenantId);
    }
}
