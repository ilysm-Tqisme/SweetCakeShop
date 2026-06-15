using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SweetCakeShop.Models;

namespace SweetCakeShop.Data
{
    public class ApplicationDbContext : IdentityDbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Category> Categories { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderDetail> OrderDetails { get; set; }
        public DbSet<Ingredient> Ingredients { get; set; }
        public DbSet<Recipe> Recipes { get; set; }
        public DbSet<CustomerChatMessage> CustomerChatMessages { get; set; }

        // New feature tables
        public DbSet<CartItem> CartItems { get; set; }
        public DbSet<Review> Reviews { get; set; }
        public DbSet<Coupon> Coupons { get; set; }
        public DbSet<CouponUsage> CouponUsages { get; set; }
        public DbSet<Notification> Notifications { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // config relationship nếu cần
            builder.Entity<Order>()
                .HasOne(o => o.User)
                .WithMany()
                .HasForeignKey(o => o.UserId);

            // set precision / column types to avoid silent truncation
            builder.Entity<Product>()
                .Property(p => p.Price)
                .HasPrecision(18, 2);

            builder.Entity<Order>()
                .Property(o => o.TotalPrice)
                .HasPrecision(18, 2);

            builder.Entity<OrderDetail>()
                .Property(od => od.Price)
                .HasPrecision(18, 2);

            builder.Entity<Ingredient>()
                .Property(i => i.Quantity)
                .HasPrecision(10, 2);

            builder.Entity<Recipe>(entity =>
            {
                entity.ToTable("Recipe");

                entity.HasKey(r => r.RecipeID);

                entity.Property(r => r.Quantity)
                      .HasPrecision(10, 2);

                entity.HasOne(r => r.Product)
                    .WithMany()
                    .HasForeignKey(r => r.ProductID)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(r => r.Ingredient)
                    .WithMany()
                    .HasForeignKey(r => r.IngredientsID)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(r => new { r.ProductID, r.IngredientsID }).IsUnique();
            });

            builder.Entity<CustomerChatMessage>(entity =>
            {
                entity.ToTable("CustomerChatMessages");
                entity.HasKey(m => m.Id);
                entity.Property(m => m.Sender).HasMaxLength(16).IsRequired();
                entity.Property(m => m.Content).HasMaxLength(4000).IsRequired();
                entity.Property(m => m.ChatToken).HasMaxLength(64);
                entity.Property(m => m.UserId).HasMaxLength(450);
                entity.HasIndex(m => m.UserId);
                entity.HasIndex(m => m.ChatToken);
                entity.HasIndex(m => m.CreatedAt);
            });

            // ── CartItem ──
            builder.Entity<CartItem>(entity =>
            {
                entity.ToTable("CartItems");
                entity.HasKey(ci => ci.CartItemId);
                entity.Property(ci => ci.UserId).HasMaxLength(450).IsRequired();

                entity.HasOne(ci => ci.Product)
                    .WithMany()
                    .HasForeignKey(ci => ci.ProductId)
                    .OnDelete(DeleteBehavior.Cascade);

                // One cart entry per user per product
                entity.HasIndex(ci => new { ci.UserId, ci.ProductId }).IsUnique();
                entity.HasIndex(ci => ci.UserId);
            });

            // ── Review ──
            builder.Entity<Review>(entity =>
            {
                entity.ToTable("Reviews");
                entity.HasKey(r => r.ReviewId);
                entity.Property(r => r.UserId).HasMaxLength(450).IsRequired();
                entity.Property(r => r.Title).HasMaxLength(200);
                entity.Property(r => r.Content).HasMaxLength(2000);

                entity.HasOne(r => r.User)
                    .WithMany()
                    .HasForeignKey(r => r.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(r => r.Product)
                    .WithMany()
                    .HasForeignKey(r => r.ProductId)
                    .OnDelete(DeleteBehavior.Cascade);

                // Enforce 1 review per user per product
                entity.HasIndex(r => new { r.UserId, r.ProductId }).IsUnique();
                entity.HasIndex(r => r.ProductId);
            });

            // ── Coupon ──
            builder.Entity<Coupon>(entity =>
            {
                entity.ToTable("Coupons");
                entity.HasKey(c => c.CouponId);
                entity.Property(c => c.Code).HasMaxLength(50).IsRequired();
                entity.Property(c => c.Description).HasMaxLength(500);
                entity.Property(c => c.DiscountValue).HasPrecision(18, 2);
                entity.Property(c => c.MinOrderAmount).HasPrecision(18, 2);
                entity.Property(c => c.MaxDiscountAmount).HasPrecision(18, 2);

                entity.HasIndex(c => c.Code).IsUnique();
                entity.HasIndex(c => c.IsActive);
            });

            // ── CouponUsage ──
            builder.Entity<CouponUsage>(entity =>
            {
                entity.ToTable("CouponUsages");
                entity.HasKey(cu => cu.CouponUsageId);
                entity.Property(cu => cu.UserId).HasMaxLength(450).IsRequired();
                entity.Property(cu => cu.DiscountApplied).HasPrecision(18, 2);

                entity.HasOne(cu => cu.Coupon)
                    .WithMany(c => c.Usages)
                    .HasForeignKey(cu => cu.CouponId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(cu => cu.Order)
                    .WithMany()
                    .HasForeignKey(cu => cu.OrderId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(cu => new { cu.CouponId, cu.UserId });
            });

            // ── Notification ──
            builder.Entity<Notification>(entity =>
            {
                entity.ToTable("Notifications");
                entity.HasKey(n => n.NotificationId);
                entity.Property(n => n.UserId).HasMaxLength(450);
                entity.Property(n => n.SessionId).HasMaxLength(100);
                entity.Property(n => n.EventType).HasMaxLength(50).IsRequired();
                entity.Property(n => n.Title).HasMaxLength(200).IsRequired();
                entity.Property(n => n.Message).HasMaxLength(1000).IsRequired();
                entity.Property(n => n.Data).HasMaxLength(4000);

                entity.HasIndex(n => n.UserId);
                entity.HasIndex(n => n.CreatedAt);
                entity.HasIndex(n => new { n.UserId, n.IsRead });
            });

            // ── Order – Coupon relationship ──
            builder.Entity<Order>(entity =>
            {
                entity.Property(o => o.DiscountAmount).HasPrecision(18, 2);
                entity.Property(o => o.CouponCode).HasMaxLength(50);

                entity.HasOne(o => o.Coupon)
                    .WithMany()
                    .HasForeignKey(o => o.CouponId)
                    .OnDelete(DeleteBehavior.SetNull);
            });
        }
    }
}
