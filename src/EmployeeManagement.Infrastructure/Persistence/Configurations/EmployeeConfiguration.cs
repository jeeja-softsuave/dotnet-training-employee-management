using EmployeeManagement.Core.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EmployeeManagement.Infrastructure.Persistence.Configurations;

internal sealed class EmployeeConfiguration : IEntityTypeConfiguration<Employee>
{
    public void Configure(EntityTypeBuilder<Employee> builder)
    {
        builder.ToTable("Employees");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedOnAdd();

        // Inheritance: Table-Per-Hierarchy. Full-time and contract employees share one table;
        // the "EmploymentType" column says which class to create when reading a row.
        builder.HasDiscriminator<string>("EmploymentType")
            .HasValue<FullTimeEmployee>(nameof(EmploymentType.FullTime))
            .HasValue<ContractEmployee>(nameof(EmploymentType.Contract));
        builder.Property("EmploymentType").HasMaxLength(20);

        // Type is computed by each subclass, so it is not stored.
        builder.Ignore(e => e.Type);

        builder.Property(e => e.Name).HasMaxLength(Employee.MaxNameLength).IsRequired();
        builder.Property(e => e.Email).HasMaxLength(254);
        builder.Property(e => e.HireDate).IsRequired();

        // SQLite has no decimal type, and EF Core cannot compare or ORDER BY decimals stored
        // as TEXT. A value converter stores the salary as whole cents (INTEGER), which is exact
        // for 2-decimal money and lets WHERE/ORDER BY run in SQL. On SQL Server you would use
        // HasPrecision(18, 2) instead.
        builder.Property(e => e.Salary)
            .HasConversion(
                salary => (long)Math.Round(salary * 100m, MidpointRounding.AwayFromZero),
                cents => cents / 100m)
            .HasColumnName("SalaryCents")
            .IsRequired();

        // Relationship: many employees -> one department. Restrict means the database refuses
        // to delete a department that still has employees (no silent cascade delete).
        builder.HasOne(e => e.Department)
            .WithMany()
            .HasForeignKey(e => e.DepartmentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(e => e.Email).IsUnique().HasFilter("Email IS NOT NULL");
        builder.HasIndex(e => e.DepartmentId);
        builder.HasIndex(e => e.Name);
    }
}

internal sealed class ContractEmployeeConfiguration : IEntityTypeConfiguration<ContractEmployee>
{
    public void Configure(EntityTypeBuilder<ContractEmployee> builder)
    {
        // Get-only property: mapped explicitly so EF writes its backing field.
        builder.Property(c => c.ContractEndDate);
    }
}
