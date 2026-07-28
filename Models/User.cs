using System.ComponentModel.DataAnnotations;
using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace RVPark.Models;

public class User : IdentityUser
{
    [Display(Name = "User ID")]
    public int Id { get; set; }
    
    [Required]
    [MinLength(6)]
    [MaxLength(50)]
    [Display(Name = "Username")]
    public string Username { get; set; }

    [Required]
    [EmailAddress]
    [MaxLength(256)]
    [Display(Name = "Email")]
    public string Email { get; set; }

    [Required]
    [Phone]
    [StringLength(10)]
    [Display(Name = "Phone Number")]
    public string Phone { get; set; }
    
    [Required]
    [MaxLength(100)]
    [Display(Name = "First Name")]
    public string FirstName { get; set; }
    
    [Required]
    [MaxLength(100)]
    [Display(Name = "Last Name")]
    public string LastName { get; set; }
    
    [Required]
    public string PasswordHash { get; set; }

    [Required]
    [DataType(DataType.DateTime)]
    [Display(Name = "Account Creation Date")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Customer is the default level
    [Required]
    [Display(Name = "Access Level")]
    public AccessLevel AccessLevel { get; set; } = AccessLevel.Customer;
    
    [Required]
    [Display(Name = "Reservation List")]
    public ICollection<Reservation> Reservations { get; set; } = new List<Reservation>();
    
    // This is used to prevent an employee from accessing the system.
    [Display(Name = "Account Locked")]
    public bool IsLocked { get; set; } = false;
}
