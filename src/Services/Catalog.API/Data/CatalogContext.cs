using Catalog.API.Model;
using Microsoft.EntityFrameworkCore;

namespace Catalog.API.Data;

public class CatalogContext : DbContext
{
    public CatalogContext(DbContextOptions<CatalogContext> options) : base(options)
    {
    }

    public DbSet<CatalogItem> CatalogItems => Set<CatalogItem>();
    public DbSet<CatalogBrand> CatalogBrands => Set<CatalogBrand>();
    public DbSet<CatalogType> CatalogTypes => Set<CatalogType>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<CatalogItem>(entity =>
        {
            entity.ToTable("Catalog");

            entity.Property(ci => ci.Price)
                .HasColumnType("decimal(5,2)")
                .IsRequired();

            entity.HasOne(ci => ci.CatalogBrand)
                .WithMany()
                .HasForeignKey(ci => ci.CatalogBrandId)
                .IsRequired();

            entity.HasOne(ci => ci.CatalogType)
                .WithMany()
                .HasForeignKey(ci => ci.CatalogTypeId)
                .IsRequired();
        });

        modelBuilder.Entity<CatalogBrand>(entity =>
        {
            entity.ToTable("CatalogBrand");
            entity.Property(cb => cb.Brand)
                .IsRequired()
                .HasMaxLength(100);
        });

        modelBuilder.Entity<CatalogType>(entity =>
        {
            entity.ToTable("CatalogType");
            entity.Property(ct => ct.Type)
                .IsRequired()
                .HasMaxLength(100);
        });
    }
}
