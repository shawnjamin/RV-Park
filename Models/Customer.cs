using System.ComponentModel.DataAnnotations;

namespace RVPark.Models;

public class Customer
{
    public int Id { get; set; }

    [Required]
    [StringLength(100)]
    [Display(Name = "First Name")]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    [Display(Name = "Last Name")]
    public string LastName { get; set; } = string.Empty;

    [Phone]
    [StringLength(30)]
    public string? Phone { get; set; }

    [EmailAddress]
    [StringLength(256)]
    public string? Email { get; set; }

    public User User { get; set; } = null!;

    public ICollection<Reservation> Reservations { get; set; } = new List<Reservation>();
}
