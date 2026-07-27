using Microsoft.AspNetCore.Identity;
using RVPark.Models;

namespace RVPark.Services;

// Registered in program.cs, use with DI, inject the interface type IPasswordHasher<User> into the constructor of a class that needs to hash passwords or verify them.
public sealed class UserPasswordHasher : IPasswordHasher<User>
{
    private readonly PasswordHasher<User> passwordHasher = new();

    public string HashPassword(User user, string password)
    {
        return passwordHasher.HashPassword(user, password);
    }

    public PasswordVerificationResult VerifyHashedPassword(
        User user,
        string hashedPassword,
        string providedPassword)
    {
        return passwordHasher.VerifyHashedPassword(user, hashedPassword, providedPassword);
    }
}
