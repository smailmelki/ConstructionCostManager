using ConstructionCostManager.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace ConstructionCostManager.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Project> Projects => Set<Project>();
    public DbSet<ConstructionPhase> ConstructionPhases => Set<ConstructionPhase>();
    public DbSet<MaterialPurchase> MaterialPurchases => Set<MaterialPurchase>();
    public DbSet<MaterialItem> MaterialItems => Set<MaterialItem>();
    public DbSet<Worker> Workers => Set<Worker>();
    public DbSet<WorkerWorkEntry> WorkerWorkEntries => Set<WorkerWorkEntry>();
    public DbSet<OtherExpense> OtherExpenses => Set<OtherExpense>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<AppSetting> AppSettings => Set<AppSetting>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<AppSetting>().HasKey(s => s.Key);

        // Global soft-delete query filters to protect financial records from accidental deletion
        modelBuilder.Entity<Project>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<ConstructionPhase>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<MaterialPurchase>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<MaterialItem>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<Worker>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<WorkerWorkEntry>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<OtherExpense>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<Payment>().HasQueryFilter(e => !e.IsDeleted);

        // Relationships with Restrict delete behavior for financial safety
        modelBuilder.Entity<ConstructionPhase>()
            .HasOne(p => p.Project)
            .WithMany(pr => pr.Phases)
            .HasForeignKey(p => p.ProjectId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<MaterialPurchase>()
            .HasOne(mp => mp.Project)
            .WithMany(pr => pr.MaterialPurchases)
            .HasForeignKey(mp => mp.ProjectId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<MaterialPurchase>()
            .HasOne(mp => mp.Phase)
            .WithMany(ph => ph.MaterialPurchases)
            .HasForeignKey(mp => mp.PhaseId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<MaterialItem>()
            .HasOne(mi => mi.MaterialPurchase)
            .WithMany(mp => mp.Items)
            .HasForeignKey(mi => mi.MaterialPurchaseId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Worker>()
            .HasOne(w => w.Project)
            .WithMany(pr => pr.Workers)
            .HasForeignKey(w => w.ProjectId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<WorkerWorkEntry>()
            .HasOne(we => we.Project)
            .WithMany(pr => pr.WorkerWorkEntries)
            .HasForeignKey(we => we.ProjectId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<WorkerWorkEntry>()
            .HasOne(we => we.Worker)
            .WithMany(w => w.WorkEntries)
            .HasForeignKey(we => we.WorkerId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<WorkerWorkEntry>()
            .HasOne(we => we.Phase)
            .WithMany(ph => ph.WorkerWorkEntries)
            .HasForeignKey(we => we.PhaseId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<OtherExpense>()
            .HasOne(oe => oe.Project)
            .WithMany(pr => pr.OtherExpenses)
            .HasForeignKey(oe => oe.ProjectId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<OtherExpense>()
            .HasOne(oe => oe.Phase)
            .WithMany(ph => ph.OtherExpenses)
            .HasForeignKey(oe => oe.PhaseId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Payment>()
            .HasOne(p => p.Project)
            .WithMany(pr => pr.Payments)
            .HasForeignKey(p => p.ProjectId)
            .OnDelete(DeleteBehavior.Restrict);

        // Useful indexes for fast project-isolated queries
        modelBuilder.Entity<MaterialPurchase>().HasIndex(x => new { x.ProjectId, x.PurchaseDate });
        modelBuilder.Entity<WorkerWorkEntry>().HasIndex(x => new { x.ProjectId, x.WorkDate });
        modelBuilder.Entity<OtherExpense>().HasIndex(x => new { x.ProjectId, x.ExpenseDate });
        modelBuilder.Entity<Payment>().HasIndex(x => new { x.ProjectId, x.ObligationType, x.ReferenceId });
    }

    public override int SaveChanges()
    {
        ApplyAuditTimestamps();
        return base.SaveChanges();
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        ApplyAuditTimestamps();
        return base.SaveChangesAsync(cancellationToken);
    }

    private void ApplyAuditTimestamps()
    {
        var now = DateTime.UtcNow;
        foreach (var entry in ChangeTracker.Entries<BaseEntity>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAt = now;
                entry.Entity.UpdatedAt = now;
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAt = now;
            }
        }
    }
}
