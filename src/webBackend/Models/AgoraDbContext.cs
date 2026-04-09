using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Identity;
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

    

    public virtual DbSet<ActionPermission> ActionPermissions { get; set; }

    public virtual DbSet<AppPermission> AppPermissions { get; set; }


    public virtual DbSet<Cart> Carts { get; set; }

    public virtual DbSet<CartItem> CartItems { get; set; }

    public virtual DbSet<Category> Categories { get; set; }

    public virtual DbSet<Comment> Comments { get; set; }

    public virtual DbSet<Order> Orders { get; set; }

    public virtual DbSet<OrderItem> OrderItems { get; set; }

    public virtual DbSet<Product> Products { get; set; }

   

    public virtual DbSet<RoleActionPermission> RoleActionPermissions { get; set; }

    public virtual DbSet<Slider> Sliders { get; set; }

    public virtual DbSet<TblIl> TblIls { get; set; }

    public virtual DbSet<TblIlce> TblIlces { get; set; }

    

    public virtual DbSet<UserActionPermission> UserActionPermissions { get; set; }

    public virtual DbSet<UserMessage> UserMessages { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)

        => optionsBuilder.UseSqlServer("Server=EMIR-HP\\MSSQLSERVER01;Database=AgoraDb;Trusted_Connection=True;TrustServerCertificate=True;");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {

        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<IdentityUserLogin<int>>(entity => {
        entity.ToTable("AspNetUserLogins"); 
        entity.HasKey(l => new { l.LoginProvider, l.ProviderKey });
        });


       

        OnModelCreatingPartial(modelBuilder);

        modelBuilder.Entity<ActionPermission>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__ActionPe__3214EC0736357F55");

            entity.Property(e => e.ActionName).HasMaxLength(100);
            entity.Property(e => e.Category).HasMaxLength(50);
            entity.Property(e => e.ControllerName).HasMaxLength(100);
            entity.Property(e => e.Description).HasMaxLength(250);
        });

        modelBuilder.Entity<AppPermission>(entity =>
        {
            entity.HasIndex(e => e.PermissionKey, "UQ_AppPermissions_PermissionKey").IsUnique();

            entity.Property(e => e.Description).HasMaxLength(250);
            entity.Property(e => e.GroupName).HasMaxLength(50);
            entity.Property(e => e.PermissionKey).HasMaxLength(100);
        });

        

        modelBuilder.Entity<Category>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Categori__3214EC07CB4A737B");

            entity.HasIndex(e => e.Url, "CategoryIndex").IsUnique();

            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.Name).HasMaxLength(100);
            entity.Property(e => e.Url)
                .HasMaxLength(255)
                .HasDefaultValue("default-url");
        });

        modelBuilder.Entity<Comment>(entity =>
        {
            entity.HasKey(e => e.CommentId).HasName("PK__Comments__C3B4DFCABDFC24AD");

            entity.HasIndex(e => e.ProductId, "IX_Comments_ProductId");

            entity.HasIndex(e => e.UserId, "IX_Comments_UserID");

            entity.Property(e => e.CommentText).HasMaxLength(500);
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.UserId).HasColumnName("UserID");

            entity.HasOne(d => d.Product).WithMany(p => p.Comments)
                .HasForeignKey(d => d.ProductId)
                .HasConstraintName("FK__Comments__Products__59FA5E80");

            entity.HasOne(d => d.User).WithMany(p => p.Comments)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("FK__Comments__UserID__59063A47");
        });
        
        modelBuilder.Entity<Cart>(entity =>
        {
            entity.ToTable("Carts"); // "Invalid object name 'Cart'" hatasını bu satır çözer
            entity.HasKey(e => e.CartId);
        });

        // 2. CartItem Tablosu (Senin DB'de tekil)
        modelBuilder.Entity<CartItem>(entity =>
        {
            entity.ToTable("CartItem"); // Eğer bu da 's' ile bitseydi "CartItems" yapardık
            entity.HasKey(e => e.CartItemId);
            
            // İlişkiyi de garantiye alalım
            entity.HasOne(d => d.Cart)
                .WithMany(p => p.CartItems)
                .HasForeignKey(d => d.CartId);
        });

        modelBuilder.Entity<Order>(entity =>
        {
            entity.Property(e => e.AdSoyad).HasDefaultValue("");
            entity.Property(e => e.Email).HasMaxLength(256);
        });

        modelBuilder.Entity<OrderItem>(entity =>
        {
            entity.ToTable("OrderItem");

            entity.HasIndex(e => e.OrderId, "IX_OrderItem_OrderId");

            entity.HasIndex(e => e.UrunId, "IX_OrderItem_UrunId");

            entity.HasOne(d => d.Order).WithMany(p => p.OrderItems).HasForeignKey(d => d.OrderId);

            entity.HasOne(d => d.Urun).WithMany(p => p.OrderItems).HasForeignKey(d => d.UrunId);
        });

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
            entity.Property(e => e.Desi)
            .HasComputedColumnSql("(case when [Width] IS NULL OR [Height] IS NULL OR [Length] IS NULL then (1) else case when ceiling((([Width]*[Height])*[Length])/(3000.0))<(1) then (1) else ceiling((([Width]*[Height])*[Length])/(3000.0)) end end)", true)
            .ValueGeneratedOnAddOrUpdate();

            entity.HasOne(d => d.Category).WithMany(p => p.Products)
                .HasForeignKey(d => d.CategoryId)
                .HasConstraintName("FK_Products_Categories");
        });

        

        modelBuilder.Entity<RoleActionPermission>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__RoleActi__3214EC07876D2999");

            entity.HasIndex(e => new { e.RoleId, e.PermissionId }, "IX_RolePermission_Unique").IsUnique();

            entity.HasOne(d => d.Permission).WithMany(p => p.RoleActionPermissions)
                .HasForeignKey(d => d.PermissionId)
                .HasConstraintName("FK_RoleActionPermissions_Permissions");

            entity.HasOne(d => d.Role).WithMany(p => p.RoleActionPermissions)
                .HasForeignKey(d => d.RoleId)
                .HasConstraintName("FK_RoleActionPermissions_Roles");
        });

        modelBuilder.Entity<Slider>(entity =>
        {
            entity.HasKey(e => e.SliderId).HasName("PK__Sliders__24BC96F0B2E7D9B8");

            entity.Property(e => e.ImageUrl).HasMaxLength(250);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.SliderDescription).HasMaxLength(200);
            entity.Property(e => e.SliderTitle).HasMaxLength(100);
        });

        modelBuilder.Entity<TblIl>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__tbl_il__3213E83FD4A7F79E");

            entity.ToTable("tbl_il");

            entity.Property(e => e.Id)
                .ValueGeneratedNever()
                .HasColumnName("id");
            entity.Property(e => e.IlAdi)
                .HasMaxLength(50)
                .HasColumnName("ilAdi");
        });

        modelBuilder.Entity<TblIlce>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__tbl_ilce__3213E83F555E6A77");

            entity.ToTable("tbl_ilce");

            entity.HasIndex(e => e.IlId, "IX_tbl_ilce_ilId");

            entity.Property(e => e.Id)
                .ValueGeneratedNever()
                .HasColumnName("id");
            entity.Property(e => e.IlId).HasColumnName("ilId");
            entity.Property(e => e.IlceAdi)
                .HasMaxLength(50)
                .HasColumnName("ilceAdi");

            entity.HasOne(d => d.Il).WithMany(p => p.TblIlces)
                .HasForeignKey(d => d.IlId)
                .HasConstraintName("FK__tbl_ilce__ilId__72C60C4A");
        });

        

        modelBuilder.Entity<UserActionPermission>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__UserActi__3214EC07641F3BAB");

            entity.HasIndex(e => new { e.UserId, e.PermissionId }, "IX_UserPermission_Unique").IsUnique();

            entity.Property(e => e.IsAllowed).HasDefaultValue(true);

            entity.HasOne(d => d.Permission).WithMany(p => p.UserActionPermissions)
                .HasForeignKey(d => d.PermissionId)
                .HasConstraintName("FK_UserActionPermissions_Permissions");

            entity.HasOne(d => d.User).WithMany(p => p.UserActionPermissions)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("FK_UserActionPermissions_Users");
        });

        modelBuilder.Entity<UserMessage>(entity =>
        {
            entity.HasKey(e => e.MessageId).HasName("PK__userMess__C87C0C9C9AADA58E");

            entity.ToTable("userMessages");

            entity.Property(e => e.Content).HasMaxLength(500);
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.Email).HasMaxLength(100);
            entity.Property(e => e.FullName).HasMaxLength(100);
            entity.Property(e => e.MessageSubject).HasMaxLength(200);
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
