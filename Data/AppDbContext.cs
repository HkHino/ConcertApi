using ConcertApi.Models;
using Microsoft.EntityFrameworkCore;

namespace ConcertApi.Data;

public class AppDbContext : DbContext
{
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Concert> Concerts => Set<Concert>();
    public DbSet<Ticket> Tickets => Set<Ticket>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<RevokedToken> RevokedTokens => Set<RevokedToken>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>().ToTable("users");
        modelBuilder.Entity<Concert>().ToTable("concerts");
        modelBuilder.Entity<Ticket>().ToTable("tickets");
        modelBuilder.Entity<Order>().ToTable("orders");
        modelBuilder.Entity<RevokedToken>().ToTable("revoked_tokens");

        modelBuilder.Entity<RevokedToken>().HasKey(x => x.Jti);

        // match column names if you used snake_case
        modelBuilder.Entity<User>().Property(x => x.PasswordHash).HasColumnName("password_hash");
        modelBuilder.Entity<User>().Property(x => x.CreatedAt).HasColumnName("created_at");

        modelBuilder.Entity<Concert>().Property(x => x.ConcertDate).HasColumnName("concert_date");
        modelBuilder.Entity<Concert>().Property(x => x.CreatedAt).HasColumnName("created_at");

        modelBuilder.Entity<Ticket>().Property(x => x.ConcertId).HasColumnName("concert_id");
        modelBuilder.Entity<Ticket>().Property(x => x.SeatNumber).HasColumnName("seat_number");
        modelBuilder.Entity<Ticket>().Property(x => x.CreatedAt).HasColumnName("created_at");

        modelBuilder.Entity<Order>().Property(x => x.UserId).HasColumnName("user_id");
        modelBuilder.Entity<Order>().Property(x => x.TicketId).HasColumnName("ticket_id");
        modelBuilder.Entity<Order>().Property(x => x.PurchasedAt).HasColumnName("purchased_at");

        modelBuilder.Entity<RevokedToken>().Property(x => x.ExpiresAt).HasColumnName("expires_at");
        modelBuilder.Entity<RevokedToken>().Property(x => x.RevokedAt).HasColumnName("revoked_at");

        modelBuilder.Entity<RefreshToken>(e =>
        {
            e.ToTable("refresh_tokens");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.UserId).HasColumnName("user_id");
            e.Property(x => x.TokenHash).HasColumnName("token_hash");
            e.Property(x => x.ExpiresAt).HasColumnName("expires_at");
            e.Property(x => x.RevokedAt).HasColumnName("revoked_at");
            e.Property(x => x.CreatedAt).HasColumnName("created_at");
        });
    }
}