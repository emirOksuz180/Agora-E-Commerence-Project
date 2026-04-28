using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace webBackend.Models;

public partial class AgoraDbContext : IdentityDbContext<AppUser, AppRole, int>
{
    public AgoraDbContext()
    {
    }

    public AgoraDbContext(DbContextOptions<AgoraDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Favorite> Favorites { get; set; }

    public virtual DbSet<UserAddress> UserAddresses { get; set; }

    // AgoraDbContext.cs içine, diğer DbSet'lerin yanına bunları yapıştır:

    public virtual DbSet<Product> Products { get; set; } = null!;
    public virtual DbSet<Category> Categories { get; set; } = null!;
    public virtual DbSet<Slider> Sliders { get; set; } = null!;
    public virtual DbSet<Order> Orders { get; set; } = null!;
    public virtual DbSet<OrderItem> OrderItems { get; set; } = null!;
    public virtual DbSet<Cart> Carts { get; set; } = null!;
    public virtual DbSet<CartItem> CartItems { get; set; } = null!;
    public virtual DbSet<AppPermission> AppPermissions { get; set; } = null!;
    public virtual DbSet<ActionPermission> ActionPermissions { get; set; } = null!;
    public virtual DbSet<RoleActionPermission> RoleActionPermissions { get; set; } = null!;
    public virtual DbSet<UserActionPermission> UserActionPermissions { get; set; } = null!;
    
    public virtual DbSet<TblIl> TblIls { get; set; } = null!; // İl/İlçe tabloların için

    public virtual DbSet<ShippingRate> ShippingRates { get; set; }

    public virtual DbSet<ShippingRegion> ShippingRegions { get; set; }

    public virtual DbSet<ReturnRequest> ReturnRequests { get; set; }

    public virtual DbSet<OrderStatus> OrderStatuses { get; set; }

    public virtual DbSet<Carrier> Carriers { get; set; }
    public virtual DbSet<CarrierRegion> CarrierRegions { get; set; }

    

   

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)

        => optionsBuilder.UseSqlServer("Server=EMIR-HP\\MSSQLSERVER01;Database=AgoraDb;Trusted_Connection=True;TrustServerCertificate=True;");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<OrderItem>().ToTable("OrderItem");
        modelBuilder.Entity<TblIl>().ToTable("tbl_il");
        modelBuilder.Entity<TblIlce>().ToTable("tbl_ilce");

        modelBuilder.Entity<AppUser>(entity => entity.ToTable("AspNetUsers"));
        modelBuilder.Entity<AppRole>(entity => entity.ToTable("AspNetRoles"));

        modelBuilder.Entity<Microsoft.AspNetCore.Identity.IdentityUserRole<int>>(entity => {
            entity.ToTable("AspNetUserRoles");
            entity.HasKey(e => new { e.UserId, e.RoleId }); // PK Tanımı
        });

        modelBuilder.Entity<Microsoft.AspNetCore.Identity.IdentityUserLogin<int>>(entity => {
            entity.ToTable("AspNetUserLogins");
            entity.HasKey(e => new { e.LoginProvider, e.ProviderKey }); // PK Tanımı
        });

        modelBuilder.Entity<Microsoft.AspNetCore.Identity.IdentityUserToken<int>>(entity => {
            entity.ToTable("AspNetUserTokens");
            entity.HasKey(e => new { e.UserId, e.LoginProvider, e.Name }); 
        });

        modelBuilder.Entity<Microsoft.AspNetCore.Identity.IdentityUserClaim<int>>(entity => entity.ToTable("AspNetUserClaims"));
        modelBuilder.Entity<Microsoft.AspNetCore.Identity.IdentityRoleClaim<int>>(entity => entity.ToTable("AspNetRoleClaims"));

        modelBuilder.Entity<Product>(entity =>
        {
            entity.HasKey(e => e.ProductId).HasName("PK__Products__B40CC6CD0F77D7F2");

            entity.HasIndex(e => e.CategoryId, "IX_Products_CategoryId");

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("((1))")
                .HasColumnType("datetime");
            entity.Property(e => e.Desi)
                .HasComputedColumnSql("(case when [Width] IS NULL OR [Height] IS NULL OR [Length] IS NULL then (1) else case when ceiling((([Width]*[Height])*[Length])/(3000.0))<(1) then (1) else ceiling((([Width]*[Height])*[Length])/(3000.0)) end end)", true)
                .HasColumnType("numeric(38, 0)");
            entity.Property(e => e.Height).HasColumnType("decimal(10, 2)");
            entity.Property(e => e.ImageUrl).HasMaxLength(255);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.IsPhysical).HasDefaultValue(true);
            entity.Property(e => e.Length).HasColumnType("decimal(10, 2)");
            entity.Property(e => e.Price).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.ProductDescription).HasMaxLength(500);
            entity.Property(e => e.ProductName).HasMaxLength(100);
            entity.Property(e => e.Weight).HasColumnType("decimal(10, 2)");
            entity.Property(e => e.Width).HasColumnType("decimal(10, 2)");

            entity.HasOne(d => d.Category).WithMany(p => p.Products)
                .HasForeignKey(d => d.CategoryId)
                .HasConstraintName("FK_Products_Categories");

            entity.HasMany(d => d.Carriers).WithMany(p => p.Products)
                .UsingEntity<Dictionary<string, object>>(
                    "ProductCarrier",
                    r => r.HasOne<Carrier>().WithMany()
                        .HasForeignKey("CarrierId")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("FK_ProductCarriers_Carriers"),
                    l => l.HasOne<Product>().WithMany()
                        .HasForeignKey("ProductId")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("FK_ProductCarriers_Products"),
                    j =>
                    {
                        j.HasKey("ProductId", "CarrierId");
                        j.ToTable("ProductCarriers");
                    });
        });

        modelBuilder.Entity<Favorite>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Favorite__3214EC07EED8645E");

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
        });

        modelBuilder.Entity<UserAddress>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__UserAddr__3214EC074008C934");

            entity.Property(e => e.AddressTitle).HasMaxLength(50);
            entity.Property(e => e.City).HasMaxLength(100);
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.District).HasMaxLength(100);
            entity.Property(e => e.FirstName).HasMaxLength(100);
            entity.Property(e => e.LastName).HasMaxLength(100);
            entity.Property(e => e.Phone).HasMaxLength(20);
            entity.Property(e => e.ZipCode).HasMaxLength(10);

            entity.HasOne(d => d.User).WithMany(p => p.UserAddresses)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("FK_UserAddresses_AspNetUsers");
        });

        modelBuilder.Entity<Cart>(entity =>
        {
            entity.ToTable("Carts");
            entity.HasKey(e => e.CartId);
        });

        modelBuilder.Entity<CartItem>(entity =>
        {
            
            entity.ToTable("CartItem"); 
            
            entity.HasKey(e => e.CartItemId);
        });

        modelBuilder.Entity<Carrier>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Carriers__3214EC07AE65072A");

            entity.Property(e => e.CarrierName).HasMaxLength(100);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
        });

        modelBuilder.Entity<Order>(entity =>
        {
            entity.Property(e => e.Ad).HasMaxLength(100);
            entity.Property(e => e.CargoTrackingCode).HasMaxLength(100);
            entity.Property(e => e.Email).HasMaxLength(256);
            entity.Property(e => e.Soyad).HasMaxLength(100);
            entity.Property(e => e.StatusId).HasDefaultValue(1);

            entity.HasOne(d => d.ShippingRate).WithMany(p => p.Orders)
                .HasForeignKey(d => d.ShippingRateId)
                .HasConstraintName("FK__Orders__Shipping__756D6ECB");

            entity.HasOne(d => d.Status).WithMany(p => p.Orders)
                .HasForeignKey(d => d.StatusId)
                .HasConstraintName("FK__Orders__StatusId__74794A92");
        });

        modelBuilder.Entity<OrderItem>(entity =>
        {
            entity.ToTable("OrderItem");

            entity.HasIndex(e => e.OrderId, "IX_OrderItem_OrderId");

            entity.HasIndex(e => e.UrunId, "IX_OrderItem_UrunId");
            entity.Property(e => e.ProductCodeSnapshot).HasMaxLength(100);
            entity.Property(e => e.ProductImageSnapshot).HasMaxLength(500);
            entity.Property(e => e.PriceAtOrder).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.ProductNameSnapshot).HasMaxLength(255);

            entity.HasOne(d => d.Order).WithMany(p => p.OrderItems).HasForeignKey(d => d.OrderId);

            entity.HasOne(d => d.Urun).WithMany(p => p.OrderItems).HasForeignKey(d => d.UrunId);
        });


        modelBuilder.Entity<OrderStatus>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__OrderSta__3214EC07EACB10FB");

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.StatusDisplayName).HasMaxLength(100);
            entity.Property(e => e.StatusKey).HasMaxLength(50);
        });

        modelBuilder.Entity<ReturnRequest>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__ReturnRe__3214EC07AE50B83B");

            entity.Property(e => e.IsRefunded).HasDefaultValue(false);
            entity.Property(e => e.RequestDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");

            entity.HasOne(d => d.CurrentStatus).WithMany(p => p.ReturnRequests)
                .HasForeignKey(d => d.CurrentStatusId)
                .HasConstraintName("FK__ReturnReq__Curre__719CDDE7");
        });


        modelBuilder.Entity<ShippingRate>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Shipping__3214EC070529BCA5");

            entity.Property(e => e.ExtraDesiPrice)
                .HasDefaultValue(0m)
                .HasColumnType("decimal(18, 2)");
            entity.Property(e => e.MaxDesi).HasColumnType("decimal(10, 2)");
            entity.Property(e => e.MinDesi).HasColumnType("decimal(10, 2)");
            entity.Property(e => e.Price).HasColumnType("decimal(18, 2)");

            entity.HasOne(d => d.Carrier).WithMany(p => p.ShippingRates)
                .HasForeignKey(d => d.CarrierId)
                .HasConstraintName("FK__ShippingR__Carri__69FBBC1F");

            entity.HasOne(d => d.Region).WithMany(p => p.ShippingRates)
                .HasForeignKey(d => d.RegionId)
                .HasConstraintName("FK__ShippingR__Regio__6AEFE058");
        });

        modelBuilder.Entity<ShippingRegion>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Shipping__3214EC0775C1B4C9");

            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.RegionName).HasMaxLength(100);
        });

        modelBuilder.Entity<CarrierRegion>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__CarrierR__3214EC0740EFEF37");

            entity.HasOne(d => d.Carrier).WithMany(p => p.CarrierRegions)
                .HasForeignKey(d => d.CarrierId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__CarrierRe__Carri__7849DB76");

            entity.HasOne(d => d.Region).WithMany(p => p.CarrierRegions)
                .HasForeignKey(d => d.RegionId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__CarrierRe__Regio__793DFFAF");
        });

        

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
