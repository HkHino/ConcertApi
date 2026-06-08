using ConcertApi.Models;
using Microsoft.EntityFrameworkCore;

namespace ConcertApi.Data;

public static class DbSeeder
{
    public static async Task SeedAsync(AppDbContext db)
    {
        //grant types are password and refresh_token
        // Ensure DB connection works
        await db.Database.CanConnectAsync();

        // Admin
        await UpsertUser(db,
            email: "admin@concert.local",
            password: "Password123!",
            role: "Admin"
        );

        // Normal user
        await UpsertUser(db,
            email: "user1@concert.local",
            password: "Password123!",
            role: "User"
        );

        await UpsertUser(db,
            email: "user2@concert.local",
            password: "Password123!",
            role: "User"
        );

        await db.SaveChangesAsync();
    }

    private static async Task UpsertUser(AppDbContext db, string email, string password, string role)
    {
        var user = await db.Users.SingleOrDefaultAsync(u => u.Email == email);

        if (user == null)
        {
            user = new User
            {
                Email = email,
                Role = role,
                CreatedAt = DateTime.UtcNow,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(password)
            };
            db.Users.Add(user);
            return;
        }

        // Update placeholder hash OR missing hash
        if (string.IsNullOrWhiteSpace(user.PasswordHash) || user.PasswordHash == "HASH_ME")
        {
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(password);
        }

        // Ensure role is correct (optional)
        user.Role = role;
    }
}