using CmcTs.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace CmcTs.Core.Data;

public class CmcTsDbContext : DbContext
{
    public CmcTsDbContext(DbContextOptions<CmcTsDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Technology> Technologies => Set<Technology>();
    public DbSet<Project> Projects => Set<Project>();
    public DbSet<ProjectTechnology> ProjectTechnologies => Set<ProjectTechnology>();
    public DbSet<ProjectMember> ProjectMembers => Set<ProjectMember>();
    public DbSet<EstimateImport> EstimateImports => Set<EstimateImport>();
    public DbSet<EstimateCostSummary> EstimateCostSummaries => Set<EstimateCostSummary>();
    public DbSet<TaskItem> Tasks => Set<TaskItem>();
    public DbSet<WorkReport> WorkReports => Set<WorkReport>();
    public DbSet<ProjectDocument> ProjectDocuments => Set<ProjectDocument>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(e =>
        {
            e.HasIndex(u => u.SamAccountName).IsUnique();
            e.Property(u => u.SamAccountName).HasMaxLength(100);
            e.Property(u => u.DisplayName).HasMaxLength(200);
            e.Property(u => u.Email).HasMaxLength(200);
            e.Property(u => u.Department).HasMaxLength(200);
        });

        modelBuilder.Entity<Technology>(e =>
        {
            e.HasIndex(t => t.Name).IsUnique();
            e.Property(t => t.Name).HasMaxLength(100);
        });

        modelBuilder.Entity<Project>(e =>
        {
            e.Property(p => p.Name).HasMaxLength(300);
            e.Property(p => p.CaseCode).HasMaxLength(50);
            e.Property(p => p.FiscalYear).HasMaxLength(20);
            e.Property(p => p.RevenueAmount).HasPrecision(18, 2);

            // Project có 3 FK tới User (ProjectLead, CompletedBy, CreatedBy) — bắt buộc Restrict
            // để tránh lỗi "multiple cascade paths" của SQL Server khi xóa 1 User.
            e.HasOne(p => p.ProjectLead)
                .WithMany()
                .HasForeignKey(p => p.ProjectLeadUserId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(p => p.CompletedByUser)
                .WithMany()
                .HasForeignKey(p => p.CompletedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(p => p.CreatedByUser)
                .WithMany()
                .HasForeignKey(p => p.CreatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ProjectTechnology>(e =>
        {
            e.HasKey(pt => new { pt.ProjectId, pt.TechnologyId });

            e.HasOne(pt => pt.Project)
                .WithMany(p => p.ProjectTechnologies)
                .HasForeignKey(pt => pt.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(pt => pt.Technology)
                .WithMany(t => t.ProjectTechnologies)
                .HasForeignKey(pt => pt.TechnologyId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ProjectMember>(e =>
        {
            e.HasIndex(pm => new { pm.ProjectId, pm.UserId }).IsUnique();

            e.HasOne(pm => pm.Project)
                .WithMany(p => p.Members)
                .HasForeignKey(pm => pm.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(pm => pm.User)
                .WithMany(u => u.ProjectMemberships)
                .HasForeignKey(pm => pm.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<EstimateImport>(e =>
        {
            e.Property(ei => ei.FileName).HasMaxLength(300);
            e.Property(ei => ei.FilePath).HasMaxLength(1000);

            e.HasOne(ei => ei.Project)
                .WithMany(p => p.EstimateImports)
                .HasForeignKey(ei => ei.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(ei => ei.UploadedByUser)
                .WithMany()
                .HasForeignKey(ei => ei.UploadedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<EstimateCostSummary>(e =>
        {
            e.Property(ecs => ecs.Category).HasMaxLength(200);
            e.Property(ecs => ecs.Amount).HasPrecision(18, 2);

            e.HasOne(ecs => ecs.EstimateImport)
                .WithMany(ei => ei.CostSummaries)
                .HasForeignKey(ecs => ecs.EstimateImportId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<TaskItem>(e =>
        {
            e.Property(t => t.Code).HasMaxLength(20);
            e.Property(t => t.Name).HasMaxLength(1000);
            e.Property(t => t.StaffGroup).HasMaxLength(100);
            e.Property(t => t.HeadCount).HasPrecision(9, 2);
            e.Property(t => t.Days).HasPrecision(9, 2);
            e.Property(t => t.UnitPrice).HasPrecision(18, 2);
            e.Property(t => t.MandayPlan).HasPrecision(9, 2);
            e.Property(t => t.MandayActual).HasPrecision(9, 2);
            e.Property(t => t.CostPlan).HasPrecision(18, 2);

            e.HasOne(t => t.Project)
                .WithMany(p => p.Tasks)
                .HasForeignKey(t => t.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);

            // Cây tự tham chiếu: Restrict để tránh multiple cascade paths, xóa cha phải xử lý con thủ công.
            e.HasOne(t => t.ParentTask)
                .WithMany(t => t.Children)
                .HasForeignKey(t => t.ParentTaskId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(t => t.AssigneeUser)
                .WithMany()
                .HasForeignKey(t => t.AssigneeUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<WorkReport>(e =>
        {
            e.Property(wr => wr.MandayReported).HasPrecision(9, 2);

            e.HasOne(wr => wr.Task)
                .WithMany(t => t.WorkReports)
                .HasForeignKey(wr => wr.TaskId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(wr => wr.ReportedByUser)
                .WithMany()
                .HasForeignKey(wr => wr.ReportedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ProjectDocument>(e =>
        {
            e.Property(pd => pd.FileName).HasMaxLength(300);
            e.Property(pd => pd.FilePath).HasMaxLength(1000);

            e.HasOne(pd => pd.Project)
                .WithMany(p => p.Documents)
                .HasForeignKey(pd => pd.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(pd => pd.UploadedByUser)
                .WithMany()
                .HasForeignKey(pd => pd.UploadedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<AuditLog>(e =>
        {
            e.Property(a => a.EntityType).HasMaxLength(100);
            e.Property(a => a.FieldName).HasMaxLength(100);

            e.HasOne(a => a.ChangedByUser)
                .WithMany()
                .HasForeignKey(a => a.ChangedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
