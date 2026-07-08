using System.ComponentModel.DataAnnotations;

namespace RVPark.Models;

public class User
{
    public int Id { get; set; }

    [Required]
    [EmailAddress]
    [StringLength(256)]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string PasswordHash { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Customer? Customer { get; set; }

    public Employee? Employee { get; set; }
}
