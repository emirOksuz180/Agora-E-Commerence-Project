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
            entity.HasKey(e => new { e.UserId, e.LoginProvider, e.Name }); // PK Tanımı
        });

        modelBuilder.Entity<Microsoft.AspNetCore.Identity.IdentityUserClaim<int>>(entity => entity.ToTable("AspNetUserClaims"));
        modelBuilder.Entity<Microsoft.AspNetCore.Identity.IdentityRoleClaim<int>>(entity => entity.ToTable("AspNetRoleClaims"));

        // 3. Decimal Hassasiyeti Uyarılarını Kapatmak İçin (Build'deki sarı uyarılar için bonus)
        modelBuilder.Entity<Product>(entity =>
        {
            entity.Property(e => e.Price).HasColumnType("decimal(10,2)");
            entity.Property(e => e.Weight).HasColumnType("decimal(10,2)");
            entity.Property(e => e.Width).HasColumnType("decimal(10,2)");
            entity.Property(e => e.Length).HasColumnType("decimal(10,2)");
            entity.Property(e => e.Height).HasColumnType("decimal(10,2)");
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
        });

        modelBuilder.Entity<Cart>(entity =>
        {
            // Veritabanında 'Carts' (çoğul) olduğu için aynen bırakıyoruz
            entity.ToTable("Carts");
            entity.HasKey(e => e.CartId);
        });

        modelBuilder.Entity<CartItem>(entity =>
        {
            // DİKKAT: Burayı 'CartItem' (tekil) yapıyoruz çünkü SQL'de öyleymiş!
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
            entity.HasOne(d => d.Status)
                .WithMany(p => p.Orders)
                .HasForeignKey(d => d.StatusId);
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

        

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
