using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using PhoneShop.Models;

namespace PhoneShop.Data;

public class AppDbContext : IdentityDbContext<ApplicationUser>
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<Product> Products => Set<Product>();
    public DbSet<CartItem> CartItems => Set<CartItem>();
    public DbSet<Order> Orders => Set<Order>();

    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<ProductReview> ProductReviews
    => Set<ProductReview>();
    public DbSet<Promotion> Promotions => Set<Promotion>();
    public DbSet<Survey> Surveys => Set<Survey>();

    public DbSet<SurveyResponse> SurveyResponses
        => Set<SurveyResponse>();
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Product>()
            .Property(p => p.Price)
            .HasPrecision(18, 2);

        modelBuilder.Entity<Order>()
    .Property(o => o.TotalAmount)
    .HasPrecision(18, 2);

        modelBuilder.Entity<OrderItem>()
            .Property(i => i.UnitPrice)
            .HasPrecision(18, 2);

        modelBuilder.Entity<OrderItem>()
            .HasOne(i => i.Order)
            .WithMany(o => o.Items)
            .HasForeignKey(i => i.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<OrderItem>()
            .HasOne(i => i.Product)
            .WithMany()
            .HasForeignKey(i => i.ProductId)
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<ProductReview>()
    .HasOne(r => r.Product)
    .WithMany()
    .HasForeignKey(r => r.ProductId)
    .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<Order>()
    .HasIndex(o => o.PaymentReference)
    .IsUnique()
    .HasFilter("[PaymentReference] IS NOT NULL");
        modelBuilder.Entity<Promotion>()
    .Property(p => p.DiscountPercent)
    .HasPrecision(5, 2);

        modelBuilder.Entity<Promotion>()
            .Property(p => p.MinimumOrderAmount)
            .HasPrecision(18, 2);

        modelBuilder.Entity<Promotion>()
            .Property(p => p.MaximumDiscountAmount)
            .HasPrecision(18, 2);
        modelBuilder.Entity<Order>()
    .Property(o => o.Subtotal)
    .HasPrecision(18, 2);

        modelBuilder.Entity<Order>()
            .Property(o => o.DiscountAmount)
            .HasPrecision(18, 2);
        modelBuilder.Entity<SurveyResponse>()
    .HasOne(r => r.Survey)
    .WithMany(s => s.Responses)
    .HasForeignKey(r => r.SurveyId)
    .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<SurveyResponse>()
            .HasOne(r => r.User)
            .WithMany()
            .HasForeignKey(r => r.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}