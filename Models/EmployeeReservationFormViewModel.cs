using System.ComponentModel.DataAnnotations;

namespace RVPark.Models;

public class EmployeeReservationFormViewModel
{
    [Display(Name = "Existing Customer")]
    public int? CustomerId { get; set; } // Nullable in case walk-in does not have an ID yet (first time).

    [StringLength(100)]
    [Display(Name = "First Name")]
    public string? NewCustomerFirstName { get; set; }

    [StringLength(100)]
    [Display(Name = "Last Name")]
    public string? NewCustomerLastName { get; set; }

    [EmailAddress]
    [StringLength(255)]
    [Display(Name ="Email")]
    public string? NewCustomerEmail { get; set; }

    [Phone]
    [StringLength(30)]
    [Display(Name = "Phone")]
    public string? NewCustomerPhone { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Please select a site.")]
    [Display(Name = "Site")]
    public int SiteId { get; set; }

    [Required]
    [DataType(DataType.Date)]
    [Display(Name = "Check-In Date")]
    public DateTime StartDate { get; set; }

    [Required]
    [DataType(DataType.Date)]
    [Display(Name = "Check-Out Date")]
    public DateTime EndDate { get; set; } = DateTime.Today.AddDays(1);

    [Range(1, int.MaxValue)]
    [Display(Name = "Adults")]
    public int AdultCount { get; set; } = 1;

    [Range(0, int.MaxValue)]
    [Display(Name = "Children")]
    public int ChildCount { get; set; }

    [Range(0, int.MaxValue)]
    [Display(Name = "Pets")]
    public int PetCount { get; set; }

    [StringLength(1000)]
    [Display(Name = "Special Requests or Notes")]
    public string? SpecialRequestsOrNotes { get; set; }

    [Required]
    [Display(Name = "Payment Method")]
    public PaymentMethod PaymentMethod { get; set; }

    [StringLength(1000)]
    [Display(Name = "Payment Notes or Reference")]
    public string? PaymentNotes { get; set; }
}