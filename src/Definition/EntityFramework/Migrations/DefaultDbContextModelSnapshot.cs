using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

#nullable disable

namespace EntityFramework.Migrations;

[DbContext(typeof(DefaultDbContext))]
partial class DefaultDbContextModelSnapshot : ModelSnapshot
{
    protected override void BuildModel(ModelBuilder modelBuilder)
    {
#pragma warning disable 612, 618
        modelBuilder.HasAnnotation("ProductVersion", "10.0.0");

        // Common entities
        modelBuilder.Entity("Entity.Tenant", b =>
        {
            b.Property<Guid>("Id").ValueGeneratedOnAdd();
            b.Property<string>("Name").IsRequired().HasMaxLength(100);
            b.Property<string>("Description").HasMaxLength(500);
            b.Property<string>("DbConnectionString").HasMaxLength(500);
            b.Property<DateTimeOffset>("CreatedTime");
            b.Property<DateTimeOffset>("UpdatedTime");
            b.Property<bool>("IsDeleted");
            b.Property<Guid?>("TenantId");
            b.HasKey("Id");
            b.ToTable("Tenants");
        });

        modelBuilder.Entity("Entity.AuditLog", b =>
        {
            b.Property<Guid>("Id").ValueGeneratedOnAdd();
            b.Property<string>("Category").IsRequired();
            b.Property<string>("Event").IsRequired();
            b.Property<string>("SubjectId");
            b.Property<string>("Payload");
            b.Property<string>("IpAddress");
            b.Property<string>("UserAgent");
            b.Property<DateTimeOffset>("CreatedTime");
            b.Property<DateTimeOffset>("UpdatedTime");
            b.Property<bool>("IsDeleted");
            b.Property<Guid?>("TenantId");
            b.HasKey("Id");
            b.ToTable("AuditLogs");
        });

        modelBuilder.Entity("Entity.SystemSetting", b =>
        {
            b.Property<Guid>("Id").ValueGeneratedOnAdd();
            b.Property<string>("Key").IsRequired().HasMaxLength(256);
            b.Property<string>("Value");
            b.Property<string>("Description").HasMaxLength(500);
            b.Property<DateTimeOffset>("CreatedTime");
            b.Property<DateTimeOffset>("UpdatedTime");
            b.Property<bool>("IsDeleted");
            b.Property<Guid?>("TenantId");
            b.HasKey("Id");
            b.ToTable("SystemSettings");
        });

        // Identity entities defined in migration
        // Access entities defined in migration
#pragma warning restore 612, 618
    }
}
