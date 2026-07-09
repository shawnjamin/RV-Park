using System.ComponentModel.DataAnnotations;

namespace RVPark.Models;

// ViewModel used by the employee create/edit forms.
// This combines fields from Employee and User so the view can have one clean model to work with.
public class EmployeeAccountFormViewModel
{
    // This is nullable because new employees don't have an ID yet.
    // Existing emplyoees will have an ID when editing.
    public int? Id { get; set; }

    [Required]
    [StringLength(100)]
    [Display(Name = "First Name")]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    [Display(Name = "Last Name")]
    public string LastName { get; set; } = string.Empty;

    // Email belongs to the related User account and not directly to Employee.
    [Required]
    [EmailAddress]
    [StringLength(256)]
    public string Email { get; set; } = string.Empty;

    // This controlls what level of access the employee has in the system.
    [Display(Name = "Access Level")]
    public EmployeeAccessLevel AccessLevel { get; set; } = EmployeeAccessLevel.Staff;

    // This is used to prevent an employee from accessing the system.
    [Display(Name = "Locked")]
    public bool IsLocked { get; set; }
}