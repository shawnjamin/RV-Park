using System.ComponentModel.DataAnnotations;

namespace RVPark.Models;

public class Employee
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

    [Display(Name = "Access Level")]
    public EmployeeAccessLevel AccessLevel { get; set; } = EmployeeAccessLevel.Staff;

    [Display(Name = "Locked")]
    public bool IsLocked { get; set; }

    public User User { get; set; } = null!;
}
