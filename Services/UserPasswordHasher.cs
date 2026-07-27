using Microsoft.AspNetCore.Identity;
using RVPark.Models;

namespace RVPark.Services;

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
